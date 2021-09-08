using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

public static class Networking
{
    public static UdpClient UdpSocket = new UdpClient();
    public static Socket TcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static Dictionary<byte, NetworkingPlayer> Players = new Dictionary<byte, NetworkingPlayer>();
    public static IPEndPoint ServerEndPoint;

    public static NetworkEvents NetworkEvents;
    public static NetworkMono NetworkMono;

    public static byte LocalPlayerId;
    public static string LocalPlayerName = "Name";
    public static bool PlayerAlive = false;

    public static bool IsHost = false;
    public static bool IsConnected = false;

    public static IPAddress GetLocalIP() 
    {
        foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork && x.ToString()[1] == '9'))
            return ip;
        
        throw new Exception("Could not find local ipaddres probably not connected to internet or atleast lan");
    }
    public static byte[] EncodeString(string stringToEncode) => Encoding.ASCII.GetBytes(stringToEncode);
    public static string DecodeString(byte[] buffer, int received) => Encoding.ASCII.GetString(buffer, 0, received);
    public static byte[] EncodeJson<T>(T objectToEncode) => EncodeString(JsonConvert.SerializeObject(objectToEncode));
    public static T DecodeJson<T>(byte[] buffer, int received) => JsonConvert.DeserializeObject<T>(DecodeString(buffer, received));
    public static GameObject InitNewPlayerEntity(byte id = 0)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = "Player " + id;

        Rigidbody body = obj.AddComponent<Rigidbody>();
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints.FreezePosition;

        if (IsHost)
            obj.AddComponent<ServerPlayer>().SetId(id);
        
        return obj;
    }
    public static void Connect()
    {
        TcpSocket.Connect(ServerEndPoint);
        TcpSocket.Send(EncodeString(LocalPlayerName));
        TcpSocket.ReceiveTimeout = 500;

        byte[] buffer = new byte[200];
        int receveied = TcpSocket.Receive(buffer);
        JoinResponse ServerResponse = DecodeJson<JoinResponse>(buffer, receveied);
        foreach (var player in ServerResponse.Players)
            Players.Add(player.id, player);
        LocalPlayerId = ServerResponse.id;
        UdpSocket.Send(new byte[] { 0, ServerResponse.id }, 2, ServerEndPoint);
        IsHost = false;
        IsConnected = true;
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class NetworkingPlayer
{
    public GameObject Entity;
    public IPEndPoint listenPoint = null;
    public Socket socket;
    public bool ServerConnected = false;
    public bool Alive = false;
    public float ServerRecconectTimer = 0.0f, SinceLastUpdate = 0.0f;
    [JsonProperty]
    public byte id;
    [JsonProperty]
    public string username;
}

public class ProjectileInfo 
{
    public Vector3 origin, rotation;
    public byte id;
}
public class JoinResponse
{
    public List<NetworkingPlayer> Players = new List<NetworkingPlayer>();
    public byte id;
}

public enum UDPMessageType : byte
{
    DummyPacket = 0,
    UpdatePos = 1
}

public enum TCPMessageType : byte
{
    PlayerConnect = 0,
    SetPosition = 1,
    PlayerShot = 2,
    ServerPing = 3,
    PlayerDisconnect = 4,
    PlayerDied = 5
}

public class NetworkIO
{
    public MemoryStream WriteStream, ReadStream;
    public BinaryWriter Writer;
    public BinaryReader Reader;

    public void LRead(byte[] buffer)// Load read mode
    {
        ReadStream = new MemoryStream(buffer);
        Reader = new BinaryReader(ReadStream);
    }

    public void LRead(byte[] buffer, int count)// Load read mode
    {
        ReadStream = new MemoryStream(buffer, 0 , count);
        Reader = new BinaryReader(ReadStream);
    }

    public void LWrite()// Load write mode
    {
        WriteStream = new MemoryStream();
        Writer = new BinaryWriter(WriteStream);
    }

    public void RDispose() // Dispose of readers
    {
        Reader.Dispose();
        ReadStream.Dispose();
    }

    public void WDispose() // Dispose of writers
    {
        WriteStream.Dispose();
        Writer.Dispose();
    }

    public void WriteTransform(byte id, Vector3 pos, Vector3 rot)
    {
        Writer.Write(id);
        WriteVector3(pos);
        WriteVector3(rot);
    }

    public void WriteVector3(Vector3 vec)
    {
        Writer.Write(vec.x);
        Writer.Write(vec.y);
        Writer.Write(vec.z);
    }

    public void WriteShot(Vector3 pos, Vector3 rot, byte id)
    {
        Writer.Write((byte)TCPMessageType.PlayerShot);
        Writer.Write(id);
        WriteVector3(pos);
        WriteVector3(rot);
    }
    public ProjectileInfo ReadProjectileInfo()
    {
        ProjectileInfo info = new ProjectileInfo();
        byte readId = Reader.ReadByte();
        info.origin = ReadVector3();
        info.rotation = ReadVector3();
        info.id = readId;
        return info;
    }
    public Vector3 ReadVector3() => new Vector3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle());

    public void ReadPlayerPos()
    {
        byte playerId = Reader.ReadByte();
        Vector3 pos = ReadVector3();
        Vector3 rot = ReadVector3();
        if (playerId == Networking.LocalPlayerId) return;
        NetworkingPlayer player = Networking.Players[playerId];
        player.SinceLastUpdate = 0f;
        NetworkMono.MainThreadInvokes.Enqueue(() =>
        {
            player.Entity.transform.position = pos;
            player.Entity.transform.eulerAngles = rot;
        });
    }
}