using System;
using System.Net;

namespace TestUDPSpeed
{
    public class Connection
    {
        public int id;
        public bool accepted;
        public int tryConnectNum;
        public ushort uSeqId;
        public IPEndPoint endPoint;
        public NewNet newNet;
        public TimeSpan rtt;

        public Connection(int id, IPEndPoint endPoint, bool accepted, NewNet newNet)
        {
            this.id = id;
            this.accepted = accepted;
            this.newNet = newNet;
            this.endPoint = endPoint;
            rtt = TimeSpan.FromMilliseconds(100);
            tryConnectNum = 0;
            uSeqId = 0;
        }
    }
}