using System.Data;
using System.Net;

namespace TestUDPSpeed
{
    public class SendItem
    {
        public byte[] data;
        public IPEndPoint endPoint;

        public SendItem(byte[] data, IPEndPoint endPoint)
        {
            this.data = data;
            this.endPoint = endPoint;
        }
    }
}