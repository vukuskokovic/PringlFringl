using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using static Networking;

public class ClientNetworking : MonoBehaviour, INetworkingInterface
{
    private float LastServerUpdate = 0.0f;
    private float ReconnectTimer = 0.0f;
    private readonly NetworkIO TcpIO = Networking.NetworkMono.TcpIO;
    private readonly NetworkIO UdpIO = Networking.NetworkMono.UdpIO;
    // Use this for initialization
    void Start()
    {
        foreach (NetworkingPlayer player in Players.Values)
            player.Entity =  Networking.NetworkMono.InitNewPlayerEntity(player.id, player.username);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsConnected)
        {
            LastServerUpdate += Time.deltaTime;
            if (TcpSocket.Available > 0) //Recieve tcp client
            {
                byte[] buffer = new byte[TcpSocket.Available];
                TcpSocket.Receive(buffer);
                TcpIO.LRead(buffer);
                ReadTcp();
                LastServerUpdate = 0.0f;
            }
            else if (LastServerUpdate >= 1.0f)
            {
                LastServerUpdate = 0f;
                try
                {
                    int sent = TcpSocket.Send(new byte[] { (byte)TCPMessageType.ServerPing });
                    if (sent == 0) throw new SocketException();
                }
                catch (SocketException)
                {
                    if (!Connect())
                    {
                        IsConnected = false;
                        _ = Networking.NetworkMono.popupPanel.ShowPanel("Disconnected from server", "Trying to reconnect in 3 secconds.", 2);
                    }
                }
            }
        }
        else 
        {
            ReconnectTimer += Time.deltaTime;
            if(ReconnectTimer == 3.0f)
            {
                ReconnectTimer = 0.0f;
                if(!Connect())
                    Networking.NetworkMono.popupPanel.ShowPanel("Disconnected from server", "Trying to reconnect in 3 secconds.", 2);
            }
        }
    }

    public void ReadUdp(UDPMessageType type, int bufferLength, IPEndPoint RemoteEndPoint)
    {
        LastServerUpdate = 0f;
        if (type == UDPMessageType.UpdatePos)
        {
            int length = (bufferLength - 1) / 25;
            for (int i = 0; i < length; i++)
                UdpIO.ReadPlayerPos(readAlive: true);
        }
    }

    public void ReadTcp()
    {
        while(TcpIO.ReadStream.Length != TcpIO.ReadStream.Position)
        {
            TCPMessageType MessageType = (TCPMessageType)TcpIO.Reader.ReadByte();
            if (MessageType == TCPMessageType.PlayerConnect)
            {
                byte playerId = TcpIO.Reader.ReadByte();
                string username = TcpIO.Reader.ReadString();
                Players.Add(playerId, new NetworkingPlayer()
                {
                    Entity = Networking.NetworkMono.InitNewPlayerEntity(playerId, username),
                    id = playerId,
                    username = username
                });
            }
            else if (MessageType == TCPMessageType.SetPosition)
            {
                Networking.NetworkMono.LocalPlayer.GetComponent<Rigidbody>().velocity = Vector3.zero;
                Networking.NetworkMono.LocalPlayer.transform.position = TcpIO.ReadVector3();
                PlayerAlive = TcpIO.Reader.ReadBoolean();
            }
            else if (MessageType == TCPMessageType.PlayerShot)
                Networking.NetworkEvents.SpawnBullet(TcpIO.ReadProjectileInfo());
            else if (MessageType == TCPMessageType.RoundEnd)
            {
                bool winner = TcpIO.Reader.ReadBoolean();
                byte winnerId = TcpIO.Reader.ReadByte();
                float newRoundStartsIn = TcpIO.Reader.ReadSingle();
                Networking.NetworkMono.popupPanel.ShowPanel("Round end", winner ? "Winner is " + Networking.Players[winnerId].username +"\nRound starts in" + newRoundStartsIn: "Nobody won round starts in:" + newRoundStartsIn, newRoundStartsIn);
            }
        }
        TcpIO.RDispose();
    }

    public void UpdatePosition()
    {
        if (IsConnected) {
            UdpIO.Writer.Write((byte)UDPMessageType.UpdatePos);
            UdpIO.WriteTransform(LocalPlayerId, Networking.NetworkMono.LocalPlayer.transform.position, Networking.NetworkMono.LocalPlayer.transform.eulerAngles);
            byte[] buffer = UdpIO.WriteStream.ToArray();
            UdpSocket.Send(buffer, buffer.Length, ServerEndPoint);
        }
    }

    public void SendTcp(byte[] buffer)
    {
        if(IsConnected)
        {
            try
            {
                TcpSocket.Send(buffer);
            }
            catch (SocketException)
            {
                IsConnected = false;
            }
        }
    }
}