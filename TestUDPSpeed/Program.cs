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
            public byte[] sample = new byte[1];
            public int count = 10000;
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            public IPEndPoint sender;
            public IPEndPoint receiver = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 5000);

            public TestObj(int senderPort)
            {
                sender = new IPEndPoint(IPAddress.Parse("192.168.1.100"), senderPort);
                socket.Bind(sender);
            }

            public void Sending()
            {
                var t1 = DateTime.Now;
                var i = 0;
                while (i < count)
                {
                    socket.SendTo(sample, receiver);
                    i++;
                }

                Console.WriteLine($"{i} samples sent from port {sender.Port} in {DateTime.Now - t1}");
            }
        }

        static void Main(string[] args)
        {
            var testObj1 = new TestObj(5001);
            var testObj2 = new TestObj(5002);
            var testObj3 = new TestObj(5003);
            
            var th1 = new Thread(testObj1.Sending);
            var th2 = new Thread(testObj2.Sending);
            var th3 = new Thread(testObj3.Sending);
            th1.Start();
            th2.Start();
            th3.Start();
            th1.Join();
            th2.Join();
            th3.Join();
        }
    }
}