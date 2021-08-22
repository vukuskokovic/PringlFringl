using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Networking 
{
    public static UdpClient udpSocket = new UdpClient();
    public static Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static List<NetworkingPlayer> Players = new List<NetworkingPlayer>();
    public static IPEndPoint serverPoint;
    public static byte id;
    public static bool Host = false;

    private static MemoryStream stream;
    private static BinaryWriter writer;
    public static IPAddress GetLocalIP() 
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
            if(ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "172.25.48.1")
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

    public static void WriteVec3(ref BinaryWriter wr, Vector3 pos)
    {
        wr.Write(pos.x);
        wr.Write(pos.y);
        wr.Write(pos.z);
    }
    public static Vector3 ReadVector3(ref BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public static void SendJoinMessage(string username, int id)
    {
        stream = new MemoryStream();
        writer = new BinaryWriter(stream);
        writer.Write((byte)0);
        writer.Write((byte)id);
        writer.Write(username);
        byte[] buff = stream.ToArray();
        NetworkingPlayer[] players = Players.ToArray();
        for(int i = 0; i < players.Length; i++)
            if (players[i].socket != null)
                players[i].socket.Send(buff);
        
    }
}