using System.Collections;
using System.Net;
using UnityEngine;

namespace Assets.Scripts
{
    public class ClientNetworking : MonoBehaviour, INetworkingInterface
    {
        private NetworkMono NetworkingScript;
        // Use this for initialization
        void Start()
        {
            NetworkingScript = GetComponent<NetworkMono>();

            foreach (NetworkingPlayer player in Networking.Players.Values)
                player.Entity = Networking.InitNewPlayerEntity();
        }

        // Update is called once per frame
        void Update()
        {
            if (Networking.tcpSocket.Available > 0) //Recieve tcp client
            {
                byte[] buffer = new byte[Networking.tcpSocket.Available];
                Networking.tcpSocket.Receive(buffer);
                NetworkMono.TcpIO.LRead(buffer);
                ReadTcp();
            }
        }

        public void ReadUdp(UDPMessageType type, int bufferLength, IPEndPoint RemoteEndPoint)
        {
            if (type == UDPMessageType.UpdatePos)
            {
                int length = (bufferLength - 1) / 25;
                for (int i = 0; i < length; i++)
                    Networking.ReadPlayerPos();
            }
        }

        public void ReadTcp()
        {
            while(NetworkMono.TcpIO.ReadStream.Length != NetworkMono.TcpIO.ReadStream.Position)
            {
                TCPMessageType type = (TCPMessageType)NetworkMono.TcpIO.Reader.ReadByte();
                if (type == TCPMessageType.PlayerConnect)
                {
                    byte playerId = NetworkMono.TcpIO.Reader.ReadByte();
                    string username = NetworkMono.TcpIO.Reader.ReadString();
                    Networking.Players.Add(playerId, new NetworkingPlayer()
                    {
                        Entity = Networking.InitNewPlayerEntity(),
                        id = playerId,
                        username = username
                    });
                }
                else if (type == TCPMessageType.SetPosition)
                    NetworkingScript.LocalPlayer.transform.position = Networking.ReadVector3(ref NetworkMono.TcpIO.Reader);
            }
            NetworkMono.TcpIO.RDispose();
        }

        public void UpdatePosition()
        {
            NetworkMono.UdpIO.Writer.Write((byte)UDPMessageType.UpdatePos);
            NetworkMono.UdpIO.WriteTransform(Networking.playerId, NetworkingScript.LocalPlayer.transform.position, NetworkingScript.LocalPlayer.transform.eulerAngles);
            byte[] buffer = NetworkMono.UdpIO.WriteStream.ToArray();
            Networking.udpSocket.Send(buffer, buffer.Length, Networking.ServerEndPoint);
        }
    }
}