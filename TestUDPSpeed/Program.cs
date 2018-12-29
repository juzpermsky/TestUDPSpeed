using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TestUDPSpeed
{
    class Program
    {
        public class TestObj
        {
            private static readonly object sendLock = new object();
            public byte[] sample = new byte[100];
            public int count = 10000;
            public Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            public IPEndPoint sender;
            public IPEndPoint receiver = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 5000);

            public Queue<byte[]> sendQueue = new Queue<byte[]>();
            public Queue<byte[]> receiveQueue = new Queue<byte[]>();
            private bool sendEnqueuing = true;
            private bool receiving = true;

            public TestObj(int senderPort)
            {
                sender = new IPEndPoint(IPAddress.Parse("192.168.1.100"), senderPort);
                socket.Bind(sender);
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
                    lock (sendQueue)
                    {
                        sendQueue.Enqueue(unreliableMsg);
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
                            case NetMessage.Payload:
                                var payload = new byte[rcv - 1];
                                Array.Copy(rcvBuffer, 1, payload, 0, rcv - 1);
                                ReceivePayload(payload, (IPEndPoint) endPoint);
                                break;
                        }

                        var data = new byte[rcv];
                        Array.Copy(rcvBuffer, data, rcv);
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
                        lock (receiveQueue)
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
                        lock (receiveQueue)
                        {
                            var data = receiveQueue.Dequeue();
                            i++;
                            if (i % 1000 == 0)
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
                        byte[] curSample;
                        lock (sendQueue)
                        {
                            curSample = sendQueue.Dequeue();
                        }

                        lock (sendLock)
                        {
                            socket.SendTo(curSample, receiver);
                            i++;
                        }
                    }
                }

                Console.WriteLine($"{i} samples from port {sender.Port} sent in  {DateTime.Now - t1}");
            }
        }

        static void Main(string[] args)
        {
            var testObj1 = new TestObj(5000);

            var th11 = new Thread(testObj1.ReceiveEnqueuing);
            var th12 = new Thread(testObj1.ReceiveDequeuing);

            var testObj2 = new TestObj(5001);

            var th21 = new Thread(testObj2.SendEnqueuing);
            var th22 = new Thread(testObj2.SendingFromQueue);
            var testObj3 = new TestObj(5002);

            var th31 = new Thread(testObj3.SendEnqueuing);
            var th32 = new Thread(testObj3.SendingFromQueue);
            var testObj4 = new TestObj(5003);

            var th41 = new Thread(testObj4.SendEnqueuing);
            var th42 = new Thread(testObj4.SendingFromQueue);

            var t1 = DateTime.Now;
            th11.Start();
            th12.Start();
            th21.Start();
            th22.Start();
            th31.Start();
            th32.Start();
            th41.Start();
            th42.Start();

            th11.Join();
            th12.Join();
            th21.Join();
            th22.Join();
            th31.Join();
            th32.Join();
            th41.Join();
            th42.Join();

            Console.WriteLine($"total time {DateTime.Now - t1}");
        }

        static void MultiSending(TestObj testObj1, TestObj testObj2, TestObj testObj3)
        {
            var t1 = DateTime.Now;
            var i = 0;
            while (i < 30000)
            {
                switch (i % 3)
                {
                    case 0:
                        testObj1.socket.SendTo(testObj1.sample, testObj1.receiver);
                        break;
                    case 1:
                        testObj2.socket.SendTo(testObj2.sample, testObj2.receiver);
                        break;
                    case 2:
                        testObj3.socket.SendTo(testObj3.sample, testObj3.receiver);
                        break;
                }

                i++;
            }

            Console.WriteLine($"{i} samples sent in {DateTime.Now - t1}");
        }
    }
}