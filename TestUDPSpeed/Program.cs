using System;
using System.Threading;

namespace TestUDPSpeed
{
    public class Program
    {
        public const int maxConnections = 100;

        static void Main(string[] args)
        {
            var testObj1 = new NewNet(5000, maxConnections);

            var th11 = new Thread(testObj1.ReceiveEnqueuing);
            var th12 = new Thread(testObj1.ReceiveDequeuing);
            var th13 = new Thread(testObj1.SendingFromQueue);

            var testObj2 = new NewNet(5001, maxConnections);

            var th21 = new Thread(testObj2.ReceiveEnqueuing);
            var th22 = new Thread(testObj2.ReceiveDequeuing);
            var th23 = new Thread(testObj2.SendingFromQueue);
            var th24 = new Thread(testObj2.SendEnqueuing);
            
            var testObj3 = new NewNet(5002, maxConnections);

            var th31 = new Thread(testObj3.ReceiveEnqueuing);
            var th32 = new Thread(testObj3.ReceiveDequeuing);
            var th33 = new Thread(testObj3.SendingFromQueue);
            var th34 = new Thread(testObj3.SendEnqueuing);


            var testObj4 = new NewNet(5003, maxConnections);

            var th41 = new Thread(testObj4.ReceiveEnqueuing);
            var th42 = new Thread(testObj4.ReceiveDequeuing);
            var th43 = new Thread(testObj4.SendingFromQueue);
            var th44 = new Thread(testObj4.SendEnqueuing);
            

            var t1 = DateTime.Now;
            th11.Start();
            th12.Start();
            th13.Start();

            th21.Start();
            th22.Start();
            th23.Start();
            accepted2
            var conn2 = testObj2.Connect(testObj2.receiver.Address, testObj2.receiver.Port);
            
            
            
            th24.Start();
            
            th31.Start();
            th32.Start();
            th33.Start();
            th34.Start();
            
            th41.Start();
            th42.Start();
            th43.Start();
            th44.Start();

            //----------------------------------
            
            th11.Join();
            th12.Join();
            th13.Join();
            
            th21.Join();
            th22.Join();
            th23.Join();
            th24.Join();

            th31.Join();
            th32.Join();
            th33.Join();
            th34.Join();

            th41.Join();
            th42.Join();
            th43.Join();
            th44.Join();

            Console.WriteLine($"total time {DateTime.Now - t1}");
        }

        static void MultiSending(NewNet testObj1, NewNet testObj2, NewNet testObj3)
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