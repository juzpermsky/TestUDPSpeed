using System;
using System.Net;
using System.Net.Sockets;

namespace TestUDPSpeed
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Parse("192.168.1.100"), 5001));

            var sample = new byte[10];
            var receiver = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 5000);
            var i = 0;
            var t1 = DateTime.Now;
            while (i < 10000)
            {
                socket.SendTo(sample, receiver);
                i++;
            }

            Console.WriteLine($"{i} samples sent in {DateTime.Now - t1}");
        }
    }
}