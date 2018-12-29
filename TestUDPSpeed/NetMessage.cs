namespace TestUDPSpeed
{
    public enum NetMessage : byte
    {
        ConnectRequest,
        ConnectAccept,
        Payload,
        Disconnect,
        ConnectDenied
    }
}