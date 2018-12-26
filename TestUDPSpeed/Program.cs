using System;
using System.Collections.Generic;
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
                    lock (sendQueue)
                    {
                        sendQueue.Enqueue(sample);
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
                while (receiving)
                {
                    socket.ReceiveFrom(rcvBuffer, ref endPoint);
                    //todo: imhere
                    lock (receiveQueue)
                    {
                        receiveQueue.Enqueue(sample);
                    }
                }
            }

            public void ReceiveDequeuing()
            {
                var i = 0;
                while (receiving)
                {
                    lock (receiveQueue)
                    {
                        receiveQueue.Enqueue(sample);
                        i++;
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
            var testObj1 = new TestObj(5001);

            var t1 = DateTime.Now;
            var th11 = new Thread(testObj1.SendEnqueuing);
            var th12 = new Thread(testObj1.SendingFromQueue);


            th11.Start();
            th12.Start();
            th11.Join();
            th12.Join();
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