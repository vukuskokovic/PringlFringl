using System.Net;

public interface INetworkingInterface
{
    void ReadUdp(UDPMessageType type, int bufferLength, IPEndPoint RemoteIpEndPoint);
    void UpdatePosition();
    void SendTcp(byte[] buffer);
}