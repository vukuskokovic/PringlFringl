using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Assets.Scripts;

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

    private static MemoryStream stream;
    private static BinaryWriter writer;
    public static IPAddress GetLocalIP() 
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
            if(ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString()[1] == '9')
                return ip;
        
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }
    public static byte[] EncodeString(string str)
    {
        return System.Text.Encoding.ASCII.GetBytes(str);
    }

    public static string DecodeString(byte[] buffer, int received)
    {
        return System.Text.Encoding.ASCII.GetString(buffer, 0, received);
    }
    public static byte[] EncodeJson<T>(T obj)
    {
        return EncodeString(JsonConvert.SerializeObject(obj));
    }

    public static T DecodeJson<T>(byte[] buffer, int receved)
    {
        return JsonConvert.DeserializeObject<T>(DecodeString(buffer, receved));
    }
    public static Vector3 ReadVector3(ref BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public static GameObject InitNewPlayerEntity()
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var body = obj.AddComponent<Rigidbody>();
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints.FreezePosition;
        return obj;
    }

    public static void ReadPlayerPos()
    {
        byte playerId = NetworkMono.UdpIO.Reader.ReadByte();
        Vector3 pos = ReadVector3(ref NetworkMono.UdpIO.Reader);
        Vector3 rot = ReadVector3(ref NetworkMono.UdpIO.Reader);
        if (playerId == Networking.playerId) return;
        NetworkingPlayer player = Players[playerId];
        player.SinceLastUpdate = 0f;
        Assets.Scripts.NetworkMono.MainThreadInvokes.Enqueue(() =>
        {
            player.Entity.transform.position = pos;
            player.Entity.transform.eulerAngles = rot;
        });
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
        Writer.Write(pos.x);
        Writer.Write(pos.y);
        Writer.Write(pos.z);
        Writer.Write(rot.x);
        Writer.Write(rot.y);
        Writer.Write(rot.z);
    }
}