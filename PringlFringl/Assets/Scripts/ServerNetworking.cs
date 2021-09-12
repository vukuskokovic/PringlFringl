using System.Collections;
using UnityEngine;
using System.Linq;
using System.Net;
using System;
using System.Net.Sockets;
using static Networking;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ServerNetworking : MonoBehaviour, INetworkingInterface
{
    private readonly NetworkIO TcpIO = Networking.NetworkMono.TcpIO;
    private readonly NetworkIO UdpIO = Networking.NetworkMono.UdpIO;
    List<int> usedSpawns = new List<int>();
    bool roundStarting = false;
    float roundTimer = 0.0f;
    // Use this for initialization
    void Start()
    {
        Players.Add(0, new NetworkingPlayer()
        {
            Entity = Networking.NetworkMono.LocalPlayer,
            id = 0,
            username = "Host",
            ServerConnected = true
        });
        Networking.NetworkMono.LocalPlayer.AddComponent<ServerPlayer>().SetId(0);
        TcpSocket.BeginAccept(new AsyncCallback(TCPEndAccept), null);
        Networking.PlayerAlive = true;
        SpawnPlayer(Players[0]);
    }

    // Update is called once per frame
    void Update()
    {
        var currentPlayers = Networking.NetworkMono.PlayersCurrentFrame;
        List<NetworkingPlayer> alivePlayers = new List<NetworkingPlayer>();
        for (int i = 0; i < currentPlayers.Length; i++)
        {
            var player = currentPlayers[i];
            if (player.Alive) alivePlayers.Add(player);
            if (player.id == 0) continue;

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
                    Task.Run(() => 
                    {
                        byte[] receivedBuffer = new byte[128];
                        int bytesReceived = player.socket.Receive(receivedBuffer);
                        TcpIO.LRead(receivedBuffer, bytesReceived);
                        while (TcpIO.ReadStream.Position != TcpIO.ReadStream.Length)
                            ReadTcp();

                        TcpIO.RDispose();
                    });
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
                    SendTcp(buffer);
                    TcpIO.WDispose();
                    Debug.Log("player disconnected");
                }
            }
        }
        if (alivePlayers.Count <= 1 && !roundStarting && Networking.Players.Count != 1)
        {
            if (alivePlayers.Count == 1)
                Networking.NetworkEvents.ServerPlayerDies(alivePlayers[0].id);

            TcpIO.LWrite();
            TcpIO.Writer.Write((byte)TCPMessageType.RoundEnd);
            TcpIO.Writer.Write(alivePlayers.Count == 1);//Is there a winner
            TcpIO.Writer.Write(alivePlayers.Count == 1 ? alivePlayers[0].id : (byte)0);//Winners id 0 if none
            TcpIO.Writer.Write(3.0f);//New round starts in
            SendTcp(TcpIO.WriteStream.ToArray());
            TcpIO.WDispose();

            roundStarting = true;
            roundTimer += Time.deltaTime;
        }
        else if (roundStarting)
        {
            roundTimer += Time.deltaTime;
            if(roundTimer >= 3.0f)
                StartNewRound();
        }
    }

    void StartNewRound()
    {
        usedSpawns.Clear();
        for (int i = 0; i < Networking.NetworkMono.PlayersCurrentFrame.Length; i++)
        {
            var player = Networking.NetworkMono.PlayersCurrentFrame[i];
            SpawnPlayer(player);
        }
        roundTimer = 0.0f;
        roundStarting = false;
    }

    void ReadTcp()
    {
        try
        {
            TCPMessageType type = (TCPMessageType)TcpIO.Reader.ReadByte();
            if (type == TCPMessageType.PlayerShot)
            {
                var projInfo = TcpIO.ReadProjectileInfo();
                if (Players[projInfo.id].Alive)
                {
                    TcpIO.LWrite();
                    TcpIO.WriteShot(projInfo.origin, projInfo.rotation, projInfo.id);
                    byte[] buffer = TcpIO.WriteStream.ToArray();
                    TcpIO.WDispose();
                    SendTcp(buffer);
                    Networking.NetworkEvents.SpawnBullet(projInfo);
                }
            }
        }catch(SocketException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    void TCPEndAccept(IAsyncResult result) // Server
    {
        Socket socket = TcpSocket.EndAccept(result);
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
            Debug.Log("Player connected " + ((IPEndPoint)socket.RemoteEndPoint).Port);
            NetworkMono.MainThreadInvokes.Enqueue(() =>
            {
                try
                {
                    player.Entity = Networking.NetworkMono.InitNewPlayerEntity(player.id, player.username);
                    SetPlayerPosition(player, Networking.NetworkMono.Respawns[0].position, false);
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
        TcpSocket.BeginAccept(new AsyncCallback(TCPEndAccept), null);
    }

    public void ReadUdp(UDPMessageType type, int bufferLength, IPEndPoint RemoteIpEndPoint)
    {
        if (type == UDPMessageType.DummyPacket)
        {
            byte playerId = UdpIO.Reader.ReadByte();
            Players[playerId].listenPoint = RemoteIpEndPoint;
        }
        else if (type == UDPMessageType.UpdatePos)
            UdpIO.ReadPlayerPos();
    }

    public void UpdatePosition()
    {
        UdpIO.Writer.Write((byte)UDPMessageType.UpdatePos);
        NetworkingPlayer[] players = Players.Values.ToArray();
        for (int i = 0; i < players.Length; i++)
            if (players[i].ServerConnected)
            {
                UdpIO.WriteTransform(players[i].id, players[i].Entity.transform.position, players[i].Entity.transform.eulerAngles);//Transform
                UdpIO.Writer.Write(players[i].Alive);// Is player alive
            }

        byte[] buffer = UdpIO.WriteStream.ToArray();
        for (int i = 0; i < players.Length; i++)
            if (players[i].listenPoint != null && players[i].ServerConnected)
                UdpSocket.Send(buffer, buffer.Length, players[i].listenPoint);
    }

    void SendJoinMessage(string username, int id) // Server
    {
        //TcpIO is not used becouse this is in another thread and might overlap with already working TcpIO
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte)TCPMessageType.PlayerConnect);
        writer.Write((byte)id);
        writer.Write(username);
        byte[] buff = stream.ToArray();
        SendTcp(buff);
        stream.Dispose();
        writer.Dispose();
    }

    void SpawnPlayer(NetworkingPlayer player) // Server
    {
        System.Random r = new System.Random();
        int spawnId = r.Next(0, Networking.NetworkMono.PlayerSpawns.Count);
        while(usedSpawns.Contains(spawnId)){
            spawnId = r.Next(0, Networking.NetworkMono.PlayerSpawns.Count);
        }
        usedSpawns.Add(spawnId);
        Transform spawnPoint = Networking.NetworkMono.PlayerSpawns[spawnId];
        if (player.id != 0)
            SetPlayerPosition(player, spawnPoint.position, true);
        
        else
        {
            Networking.NetworkMono.LocalPlayer.transform.position = spawnPoint.position;
            Networking.PlayerAlive = true;
        }

        player.Alive = true;
    }

    void SetPlayerPosition(NetworkingPlayer player, Vector3 position, bool alive = false)
    {
        TcpIO.LWrite();
        TcpIO.Writer.Write((byte)TCPMessageType.SetPosition);
        TcpIO.WriteVector3(position);
        TcpIO.Writer.Write(alive);
        player.socket.Send(TcpIO.WriteStream.ToArray());
        TcpIO.WDispose();
    }

    public void SendTcp(byte[] buffer)
    {
        for(int i = 0; i < Networking.NetworkMono.PlayersCurrentFrame.Length; i++)
        {
            var player = Networking.NetworkMono.PlayersCurrentFrame[i];
            if (player.id == 0 || !player.ServerConnected) continue;
            try
            {
                player.socket.Send(buffer);
            }
            catch(SocketException ex) {
                player.ServerConnected = false;
                Debug.LogError(ex);
            }
        }
    }
}
