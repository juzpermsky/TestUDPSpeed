using System;
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
        }

        static void Main(string[] args)
        {
            var testObj1 = new TestObj(5001);
            var testObj2 = new TestObj(5002);
//            var testObj3 = new TestObj(5003);
//            testObj1.Sending();
//            testObj1.Sending();
//            MultiSending(testObj1, testObj2, testObj3);

            var t1 = DateTime.Now;
            var th1 = new Thread(testObj1.Sending);
            var th2 = new Thread(testObj2.Sending);
//            var th3 = new Thread(testObj3.Sending);
            th2.Start();
            th1.Start();
//            th3.Start();
            th1.Join();
            th2.Join();
//            th3.Join();
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