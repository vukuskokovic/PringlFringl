using System.Collections;
using UnityEngine;
using System.Linq;
using System.Net;
using System;
using System.Net.Sockets;
using static Networking;
using static Assets.Scripts.NetworkMono;
using System.IO;

namespace Assets.Scripts
{
    public class ServerNetworking : MonoBehaviour, INetworkingInterface
    {
        private NetworkMono NetworkingScript;
        

        // Use this for initialization
        void Start()
        {
            NetworkingScript = GetComponent<NetworkMono>();
            Players.Add(0, new NetworkingPlayer()
            {
                Entity = NetworkingScript.LocalPlayer,
                id = 0,
                username = "Host",
                ServerConnected = true
            });
            tcpSocket.BeginAccept(new AsyncCallback(TCPEndAccept), null);
            SpawnPlayer(Players[0]);
        }

        // Update is called once per frame
        void Update()
        {
            var currentPlayers = NetworkingScript.PlayersCurrentFrame;
            for (int i = 0; i < currentPlayers.Length; i++)
            {
                var player = currentPlayers[i];

                if (player.socket == null) continue;

                else if (player.ServerConnected)
                {
                    player.SinceLastUpdate += Time.deltaTime;
                    if (player.SinceLastUpdate >= 1.0f) // If one seccond has passed since the player last updated its position (Most probably due to disconnect)
                    {
                        player.SinceLastUpdate = 0f;
                        try
                        {
                            player.socket.Send(new byte[] { (byte)TCPMessageType.ServerPing });
                        }
                        catch (SocketException)// Player is disconnected
                        {
                            Debug.Log("Player disconnected");
                            player.ServerConnected = false;
                        }
                    }else if(player.socket.Available > 0)
                    {
                        byte[] buffer = new byte[32];
                        int received = player.socket.Receive(buffer);
                        TcpIO.LRead(buffer);
                        while(TcpIO.ReadStream.Position != TcpIO.ReadStream.Length)
                            ReadTcp();

                        TcpIO.RDispose();
                    }
                }
                else
                {
                    player.ServerRecconectTimer += Time.deltaTime;
                    if (player.ServerRecconectTimer >= 5.0f)// Player did not recconect in 5 secconds, removing the player
                    {
                        Destroy(player.Entity);
                        player.socket.Dispose();
                        Players.Remove(player.id);
                        TcpIO.LWrite();
                        TcpIO.Writer.Write((byte)TCPMessageType.PlayerDisconnect);
                        TcpIO.Writer.Write(player.id);
                        byte[] buffer = TcpIO.WriteStream.ToArray();
                        TCPSendEveryone(ref buffer);
                        TcpIO.WDispose();
                        Debug.Log("player disconnected");
                    }
                }
            }
        }

        void ReadTcp()
        {
            TCPMessageType type = (TCPMessageType)TcpIO.Reader.ReadByte();
            if(type == TCPMessageType.PlayerShot)
            {

            }
        }

        void TCPEndAccept(IAsyncResult result) // Server
        {
            Socket socket = tcpSocket.EndAccept(result);
            try
            {
                byte[] buffer = new byte[20];
                int rec = socket.Receive(buffer);
                string username = DecodeString(buffer, rec);
                if (Players.Any(x => x.Value.username == username))
                {
                    NetworkingPlayer returnedPlayer = Players.First(x => x.Value.username == username).Value;
                    returnedPlayer.ServerConnected = true;
                    returnedPlayer.ServerRecconectTimer = 0.0f;
                }


                System.Random r = new System.Random();
                int assignedId = r.Next(0, 100);
                while (Players.ContainsKey((byte)assignedId)) assignedId = r.Next(0, 100);

                JoinResponse response = new JoinResponse()
                {
                    id = (byte)assignedId,
                    Players = Players.Values.ToList()
                };
                byte[] sendBuffer = EncodeJson(response);
                socket.Send(sendBuffer);
                SendJoinMessage(username, assignedId);

                NetworkingPlayer player = new NetworkingPlayer()
                {
                    id = (byte)assignedId,
                    username = username,
                    socket = socket,
                    ServerConnected = true
                };
                MainThreadInvokes.Enqueue(() =>
                {
                    try
                    {
                        player.Entity = InitNewPlayerEntity();
                        SpawnPlayer(player);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("WHEN SPAWNING PLAYER:\n" + ex);
                    }
                });//Invoke to main thread
                Players.Add(player.id, player);
            }
            catch (Exception ex)
            {
                Debug.Log("ACCEPT FUNCTION:\n" + ex);
            }
            tcpSocket.BeginAccept(new AsyncCallback(TCPEndAccept), null);
        }

        public void ReadUdp(UDPMessageType type, int bufferLength, IPEndPoint RemoteIpEndPoint)
        {
            if (type == UDPMessageType.DummyPacket)
            {
                byte playerId = UdpIO.Reader.ReadByte();
                Players[playerId].listenPoint = RemoteIpEndPoint;
            }
            else if (type == UDPMessageType.UpdatePos)
                ReadPlayerPos();
        }

        public void UpdatePosition()
        {
            UdpIO.Writer.Write((byte)UDPMessageType.UpdatePos);
            NetworkingPlayer[] players = Players.Values.ToArray();
            for (int i = 0; i < players.Length; i++)
                if (players[i].ServerConnected)
                    UdpIO.WriteTransform(players[i].id, players[i].Entity.transform.position, players[i].Entity.transform.eulerAngles);

            byte[] buffer = UdpIO.WriteStream.ToArray();
            for (int i = 0; i < players.Length; i++)
                if (players[i].listenPoint != null && players[i].ServerConnected)
                    udpSocket.Send(buffer, buffer.Length, players[i].listenPoint);
        }

        void SendJoinMessage(string username, int id) // Server
        {
            //TcpIO is not used becouse this is in another thread and might overlap with already working TcpIO
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((byte)0);
            writer.Write((byte)id);
            writer.Write(username);
            byte[] buff = stream.ToArray();
            TCPSendEveryone(ref buff);
            stream.Dispose();
            writer.Dispose();
        }

        void TCPSendEveryone(ref byte[] buffer)
        {
            NetworkingPlayer[] players = Players.Values.ToArray();
            for (int i = 0; i < players.Length; i++)
                if (players[i].socket != null)
                    players[i].socket.Send(buffer);

            buffer = null;
        }

        void SpawnPlayer(NetworkingPlayer player) // Server
        {
            int spawnId = new System.Random().Next(0, NetworkingScript.PlayerSpawns.Count);
            Transform t = NetworkingScript.PlayerSpawns[spawnId];
            if (player.socket != null)
            {
                TcpIO.LWrite();
                TcpIO.Writer.Write((byte)TCPMessageType.SetPosition);
                TcpIO.Writer.Write(t.position.x);
                TcpIO.Writer.Write(t.position.y);
                TcpIO.Writer.Write(t.position.z);
                player.socket.Send(TcpIO.WriteStream.ToArray());
                TcpIO.WDispose();
            }
            else
                NetworkingScript.LocalPlayer.transform.position = t.position;
        }
    }
}