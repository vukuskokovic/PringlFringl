using System.Net;

namespace Assets.Scripts
{
    interface INetworkingInterface
    {
        void ReadUdp(UDPMessageType type, int bufferLength, IPEndPoint RemoteIpEndPoint);
        void UpdatePosition();
    }
}
