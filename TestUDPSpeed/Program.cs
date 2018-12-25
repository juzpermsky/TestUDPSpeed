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
            public byte[] sample = new byte[10];
            public int count = 10000;
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            public IPEndPoint sender = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 5001);
            public IPEndPoint receiver = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 5000);

            public TestObj()
            {
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

                Console.WriteLine($"{i} samples sent in {DateTime.Now - t1}");
            }
        }

        static void Main(string[] args)
        {
            var testObj = new TestObj();
            var th = new Thread(testObj.Sending);
            th.Start();
            th.Join();

        }
    }
}