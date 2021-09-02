using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ClientNetworking : MonoBehaviour, INetworkingInterface
{
    private NetworkMono NetworkingScript;
    private float sinceServerUpdated = 0.0f;
    private float tryConnectTimer = 0.0f;
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
        if (Networking.Connected)
        {
            sinceServerUpdated += Time.deltaTime;
            if (Networking.tcpSocket.Available > 0) //Recieve tcp client
            {
                byte[] buffer = new byte[Networking.tcpSocket.Available];
                Networking.tcpSocket.Receive(buffer);
                NetworkMono.TcpIO.LRead(buffer);
                ReadTcp();
            }
            else if (sinceServerUpdated >= 1.0f)
            {
                sinceServerUpdated = 0f;
                try
                {
                    Networking.tcpSocket.Send(new byte[] { (byte)TCPMessageType.ServerPing });
                }
                catch (SocketException)
                {
                    Networking.Connected = false;
                    NetworkingScript.popupPanel.ShowPanel("Disconnected from server", "Trying to reconnect in 3 secconds.", 2);
                }
            }
        }
        else 
        {
            tryConnectTimer += Time.deltaTime;
            if(tryConnectTimer == 3.0f)
            {
                tryConnectTimer = 0.0f;
                try
                {
                    Networking.Connect();
                }
                catch (SocketException)
                {
                    NetworkingScript.popupPanel.ShowPanel("Disconnected from server", "Trying to reconnect in 3 secconds.", 2.5f);
                }
            }
        }
    }

    public void ReadUdp(UDPMessageType type, int bufferLength, IPEndPoint RemoteEndPoint)
    {
        sinceServerUpdated = 0f;
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
                NetworkingScript.LocalPlayer.transform.position = NetworkMono.TcpIO.ReadVector3();
            else if(type == TCPMessageType.PlayerShot)
            {
                byte id = NetworkMono.TcpIO.Reader.ReadByte();
                Vector3 position = NetworkMono.TcpIO.ReadVector3();
                Vector3 rotation = NetworkMono.TcpIO.ReadVector3();
                NetworkingScript.SpawnBullet(position, rotation);
            }
        }
        NetworkMono.TcpIO.RDispose();
    }

    public void UpdatePosition()
    {
        if (Networking.Connected) {
            NetworkMono.UdpIO.Writer.Write((byte)UDPMessageType.UpdatePos);
            NetworkMono.UdpIO.WriteTransform(Networking.playerId, NetworkingScript.LocalPlayer.transform.position, NetworkingScript.LocalPlayer.transform.eulerAngles);
            byte[] buffer = NetworkMono.UdpIO.WriteStream.ToArray();
            Networking.udpSocket.Send(buffer, buffer.Length, Networking.ServerEndPoint);
        }
    }

    public void SendTcp(byte[] buffer)
    {
        Networking.tcpSocket.Send(buffer);
        Debug.Log("sent");
    }
}