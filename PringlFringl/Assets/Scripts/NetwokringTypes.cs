using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NetworkingPlayer 
{
    [JsonIgnore]
    public GameObject Entity;
    [JsonIgnore]
    public IPEndPoint listenPoint= null;
    [JsonIgnore]
    public Socket socket;
    [JsonIgnore]
    public Vector3 updatePos, updateRot;
    [JsonIgnore]
    public bool updateAvalible = false;
    [JsonIgnore]
    public bool Spawned = false;
    public byte id;
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
    SetPosition = 1
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
}