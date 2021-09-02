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
    public static UdpClient udpSocket = new UdpClient();
    public static Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static Dictionary<byte, NetworkingPlayer> Players = new Dictionary<byte, NetworkingPlayer>();
    public static IPEndPoint ServerEndPoint;

    public static byte playerId;
    public static string PlayerName = "Name";

    public static bool Host = false;
    public static bool Connected = false;

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
    public static GameObject InitNewPlayerEntity()
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Rigidbody body = obj.AddComponent<Rigidbody>();
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints.FreezePosition;
        return obj;
    }

    public static void ReadPlayerPos()
    {
        byte playerId = NetworkMono.UdpIO.Reader.ReadByte();
        Vector3 pos = NetworkMono.UdpIO.ReadVector3();
        Vector3 rot = NetworkMono.UdpIO.ReadVector3();
        if (playerId == Networking.playerId) return;
        NetworkingPlayer player = Players[playerId];
        player.SinceLastUpdate = 0f;
        NetworkMono.MainThreadInvokes.Enqueue(() =>
        {
            player.Entity.transform.position = pos;
            player.Entity.transform.eulerAngles = rot;
        });
    }

    public static void Connect()
    {
        tcpSocket.Connect(ServerEndPoint);
        tcpSocket.Send(EncodeString(Networking.PlayerName));
        tcpSocket.ReceiveTimeout = 500;

        byte[] buffer = new byte[200];
        int receveied = tcpSocket.Receive(buffer);
        JoinResponse response = DecodeJson<JoinResponse>(buffer, receveied);
        foreach (var player in response.Players)
            Players.Add(player.id, player);
        Networking.playerId = response.id;
        udpSocket.Send(new byte[] { 0, response.id }, 2, ServerEndPoint);
        Networking.Host = false;
        Networking.Connected = true;
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class NetworkingPlayer
{
    public GameObject Entity;
    public IPEndPoint listenPoint = null;
    public Socket socket;
    public bool Spawned = false;
    public bool ServerConnected = false;
    public float ServerRecconectTimer = 0.0f, SinceLastUpdate = 0.0f;
    [JsonProperty]
    public byte id;
    [JsonProperty]
    public string username;
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
    PlayerDisconnect = 4
}

public class ProtocolIO
{
    public MemoryStream WriteStream, ReadStream;
    public BinaryWriter Writer;
    public BinaryReader Reader;

    public void LRead(byte[] buffer)// Load read mode
    {
        ReadStream = new MemoryStream(buffer);
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

    public Vector3 ReadVector3() => new Vector3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle());
}