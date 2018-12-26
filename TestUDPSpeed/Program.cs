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
            private bool enqueuing = true;

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

            public void Enqueuing()
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

                enqueuing = false;
                Console.WriteLine($"{i} samples from port {sender.Port} enqueued in {DateTime.Now - t1}");
            }

            public void SendingFromQueue()
            {
                var i = 0;
                while (enqueuing || sendQueue.Count > 0)
                {
                    while (sendQueue.Count > 0)
                    {
                        lock (sendQueue)
                        {
                            var sample = sendQueue.Dequeue();
                            if (sample == null)
                            {
                                Console.WriteLine($"{i}-sample is null");
                                i++;
                            }
                            else
                            {
                                lock (sendLock)
                                {
                                    socket.SendTo(sample, receiver);
                                    i++;
                                    if (i % 1000 == 0)
                                    {
                                        Console.WriteLine($"{i} samples sent");
                                    }
                                }
                            }
                        }
                    }
                }
                Console.WriteLine($"{i} samples processed");

            }
        }

        static void Main(string[] args)
        {
            var testObj1 = new TestObj(5001);

            var t1 = DateTime.Now;
            //var th1 = new Thread(testObj1.Sending);
            var th1 = new Thread(testObj1.Enqueuing);
            var th2 = new Thread(testObj1.SendingFromQueue);
            th1.Start();
            th2.Start();
            th1.Join();
            th2.Join();
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