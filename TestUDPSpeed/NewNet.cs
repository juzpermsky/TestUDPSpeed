using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TestUDPSpeed
{
    public class NewNet
    {
        public const int TryConnectLimit = 10;
        private int maxConnections;
        private int numConnections;
        public Connection[] connections;
        private bool[] usedConnections;

        private static readonly object sendLock = new object();
        private static readonly object receiveQueueLock = new object();
        private static readonly object sendQueueLock = new object();
        public byte[] sample = new byte[100];
        public int count = 10000;
        public Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public IPEndPoint sender;
        public IPEndPoint receiver = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 5000);

        public Queue<SendItem> sendQueue = new Queue<SendItem>();
        public Queue<byte[]> receiveQueue = new Queue<byte[]>();
        public bool sendEnqueuing = true;
        public bool receiving = true;

        public NewNet(int senderPort, int maxConnections)
        {
            this.maxConnections = maxConnections;
            numConnections = 0;
            usedConnections = new bool[maxConnections];
            connections = new Connection[maxConnections];
            sender = new IPEndPoint(IPAddress.Parse("192.168.1.100"), senderPort);
            try
            {

                socket.Bind(sender);
            }
            catch(SocketException e)
            {
                Console.WriteLine(e.ErrorCode);
                throw e;
            }
        }

        public int Connect(IPAddress ip, int port)
        {
            // Занимаем свободный Connection слот
            var connId = -1;
            for (var i = 0; i < maxConnections; i++)
            {
                if (!usedConnections[i])
                {
                    // Найден свободный слот
                    connId = i;
                    var endPoint = new IPEndPoint(ip, port);
                    usedConnections[i] = true;
                    connections[i] = new Connection(connId, endPoint, false, this);
                    numConnections++;
                    // В отдельном потоке делаем попытки коннекта несколько раз

                    var connThread = new Thread(Connecting);

                    connThread.Start(connections[i]);
                    break;
                }
            }

            // Возврат connId еще не гарантирует установку соединения, только сообщает выделенный слот.
            // Надо мониторить состояние соединения (Connected) в NetEvent
            return connId;
        }

        private void Connecting(object connObj)
        {
            var connection = (Connection) connObj;
            var data = new[] {(byte) NetMessage.ConnectRequest};
            while (!connection.accepted && connection.tryConnectNum < TryConnectLimit)
            {
                lock (sendLock)
                {
                    socket.SendTo(data, 1, SocketFlags.None, connection.endPoint);
                }

                Thread.Sleep(connection.rtt);
                connection.tryConnectNum++;
//                Console.WriteLine($"try connect {connection.tryConnectNum}");
            }

            if (!connection.accepted && connection.tryConnectNum == TryConnectLimit)
            {
                // Все попытки подключиться исчерпаны, делаем Disconnect 
//                Console.WriteLine("disconnecting");
                ProcessDisconnect(connection.id);
            }
        }


        private void ProcessDisconnect(int connId)
        {
            connections[connId] = null;
            usedConnections[connId] = false;
            numConnections--;
        }

        private void ProcessDisconnect(IPEndPoint endPoint)
        {
            for (var i = 0; i < usedConnections.Length; i++)
            {
                if (usedConnections[i] && connections[i].endPoint.Equals(endPoint))
                {
                    ProcessDisconnect(i);
                    return;
                }
            }
        }

        public void Sending()
        {
            var i = 0;
            var t1 = DateTime.Now;
            while (i < count)
            {
                lock (sendLock)
                {
                    socket.SendTo(sample, receiver);
                    i++;
                }

//                Thread.Sleep(1);
            }

            Console.WriteLine($"{i} samples sent from port {sender.Port} in {DateTime.Now - t1}");
        }

        public void SendEnqueuing()
        {
            var i = 0;
            var t1 = DateTime.Now;
            while (i < count)
            {
                var unreliableMsg = new byte[sample.Length + 2];
                unreliableMsg[0] = (byte) NetMessage.Payload;
                unreliableMsg[1] = (byte) QOS.Unreliable;
                Array.Copy(sample, 0, unreliableMsg, 2, sample.Length);
                lock (sendQueueLock)
                {
                    sendQueue.Enqueue(new SendItem(unreliableMsg, receiver));
                    i++;
                }
            }

            sendEnqueuing = false;
            Console.WriteLine($"{i} samples from port {sender.Port} enqueued in {DateTime.Now - t1}");
        }

        public void ReceiveEnqueuing()
        {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            var rcvBuffer = new byte[1000];
            using (var ms = new MemoryStream(rcvBuffer))
            using (var br = new BinaryReader(ms))
                while (receiving)
                {
                    var rcv = socket.ReceiveFrom(rcvBuffer, ref endPoint);
                    ms.Position = 0;
                    var netMsg = (NetMessage) br.ReadByte();
                    switch (netMsg)
                    {
                        case NetMessage.ConnectRequest:
                            if (numConnections < maxConnections)
                            {
                                // Запрос на соединение
                                ProcessConnectRequest((IPEndPoint) endPoint);
                            }
                            else
                            {
                                var deniedMsg = new[] {(byte) NetMessage.ConnectDenied};
                                lock (sendQueueLock)
                                {
                                    sendQueue.Enqueue(new SendItem(deniedMsg, (IPEndPoint) endPoint));
                                }
                            }

                            break;
                        case NetMessage.ConnectAccept:
                            ProcessConnectAccept((IPEndPoint) endPoint);
                            break;
                        case NetMessage.Payload:
                            var payload = new byte[rcv - 1];
                            Array.Copy(rcvBuffer, 1, payload, 0, rcv - 1);
                            ReceivePayload(payload, (IPEndPoint) endPoint);
                            break;
                        default:
                            Console.WriteLine("unknown format");
                            break;
                    }


//                    Thread.Sleep(1);
                }
        }

        private void ProcessConnectRequest(IPEndPoint endPoint)
        {
//            Console.WriteLine("connect request received");
            var freeId = -1;
            for (var i = 0; i < usedConnections.Length; i++)
            {
                if (usedConnections[i])
                {
                    if (connections[i].endPoint.Equals(endPoint))
                    {
                        // Соединение с таким адресом уже установлено => просто скипаем этот пакет
                        return;
                    }
                }
                else
                {
                    // Сохраним первый свободный id - может пригодиться в дальнейшем, для установки подключения
                    if (freeId < 0)
                    {
                        freeId = i;
                    }
                }
            }

            if (freeId >= 0)
            {
                // Нашли свободный слот - добавляем клиента
                // Добавляем в список акцептованных подключений
                usedConnections[freeId] = true;
                connections[freeId] = new Connection(freeId, endPoint, true, this);

                lock (sendQueueLock)
                {
//                    Console.WriteLine($"enqueuing connect accept to {endPoint}");
                    // Отправляем ConnectAccept
                    sendQueue.Enqueue(new SendItem(new[] {(byte) NetMessage.ConnectAccept}, endPoint));
                }

                numConnections++;
            }
            else
            {
                lock (sendQueueLock)
                {
                    // Отправляем ConnectDenied
                    sendQueue.Enqueue(new SendItem(new[] {(byte) NetMessage.ConnectDenied}, endPoint));
                }
            }
        }

        private void ProcessConnectAccept(IPEndPoint endPoint)
        {
//            Console.WriteLine($"connect accept received from {endPoint}");
            for (var i = 0; i < usedConnections.Length; i++)
            {
                if (usedConnections[i] && connections[i].endPoint.Equals(endPoint) && !connections[i].accepted)
                {
                    connections[i].accepted = true;
//                    Console.WriteLine($"connection {i} accepted");
                    return;
                }
            }
        }

        private void ReceivePayload(byte[] payload, IPEndPoint endPoint)
        {
            var qos = (QOS) payload[0];
            switch (qos)
            {
                case QOS.Unreliable:
                    var unreliableMsg = new byte[payload.Length - 1];
                    Array.Copy(payload, 1, unreliableMsg, 0, unreliableMsg.Length);
                    lock (receiveQueueLock)
                    {
                        receiveQueue.Enqueue(unreliableMsg);
                    }

                    break;
            }
        }

        public void ReceiveDequeuing()
        {
            var t1 = DateTime.Now;
            var i = 0;
            while (receiving)
            {
                if (receiveQueue.Count > 0)
                {
                    lock (receiveQueueLock)
                    {
                        var data = receiveQueue.Dequeue();

                        
                        i++;
                        if (i % 10000 == 0)
                        {
                            Console.WriteLine($"{i} packets received at {DateTime.Now - t1}");
                        }
                    }
                }
            }
        }


        public void SendingFromQueue()
        {
            var i = 0;
            var t1 = DateTime.Now;
            while (sendEnqueuing || sendQueue.Count > 0)
            {
                while (sendQueue.Count > 0)
                {
                    SendItem sendItem;
                    lock (sendQueueLock)
                    {
                        sendItem = sendQueue.Dequeue();
                    }

                    lock (sendLock)
                    {
                        
                        socket.SendTo(sendItem.data, sendItem.endPoint);
                        if ((NetMessage) sendItem.data[0] == NetMessage.Payload)
                        {
                            i++;
                        }
                    }
                }
            }

            Console.WriteLine($"{i} samples from port {sender.Port} sent in  {DateTime.Now - t1}");
        }
    }
}