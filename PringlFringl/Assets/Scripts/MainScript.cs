using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class MainScript : MonoBehaviour
{
    public static bool Running = true;
    public static List<NetworkPlayer> Players = new List<NetworkPlayer>();
    BinaryReader reader;
    BinaryWriter writer;
    MemoryStream readStream;
    public GameObject LocalPlayerEntity;
    public Rigidbody PlayerRigidBody;
    static int addId = -1;
    void Start()
    {
        if (MainMenu.CreateRoom) 
        {
            new Thread( AcceptThread ).Start();
            Players.Add(new NetworkPlayer() { 
                entity = LocalPlayerEntity,
                id = 0,
                name = "Server",
                socket = null
            });
        }
        else
        {
            Players = MainMenu.response.Players;
            foreach(NetworkPlayer p in Players)
                p.entity = getNewPlayer();
            
        }
        Application.runInBackground = true;
    }

    private void OnApplicationQuit()
    {
        Running = false;
    }
    float Timer = 0.0f;
    // Update is called once per frame
    void Update()
    {
        if (Input.anyKey)
        {
            Vector3 position = Vector3.zero;
            float speed = 3 * Time.deltaTime;
            if (Input.GetKey(KeyCode.W)) position += LocalPlayerEntity.transform.forward;
            if (Input.GetKey(KeyCode.S)) position -= LocalPlayerEntity.transform.forward;
            if (Input.GetKey(KeyCode.A)) position -= LocalPlayerEntity.transform.right;
            if (Input.GetKey(KeyCode.D)) position += LocalPlayerEntity.transform.right;
            LocalPlayerEntity.transform.position += position * speed;
        }

        if(addId > -1)
        {
            Players.Find(x => x.id == addId).entity = getNewPlayer();
            addId = -1;
        }
        Timer += Time.deltaTime;
        if (MainMenu.CreateRoom)
        {
            NetworkPlayer[] players = Players.ToArray();
            bool write = false;
            if (Timer >= 1.0/70)
            {
                Timer = 0;
                readStream = new MemoryStream();
                writer = new BinaryWriter(readStream);
                writer.Write((byte)TCPMessageType.UpdatePos);
                write = true;
            }
            for(int i = 0; i < players.Length; i++)
            {
                if (players[i].socket != null && players[i].socket.Available > 0)
                {
                    byte[] buffer = new byte[players[i].socket.Available];
                    players[i].socket.Receive(buffer);
                    readStream = new MemoryStream(buffer);
                    reader = new BinaryReader(readStream);
                    TCPMessageType type = (TCPMessageType)reader.ReadByte();
                    if (type == TCPMessageType.UpdatePos)
                    {
                        Vector3 pos = Networking.ReadVec(ref reader);
                        Vector3 rot = Networking.ReadVec(ref reader);
                        players[i].entity.transform.position = pos;
                        players[i].entity.transform.eulerAngles = rot;
                    }
                }
                if (write)
                {
                    writer.Write((byte)players[i].id);
                    writer.Write(players[i].entity.transform.position.x);
                    writer.Write(players[i].entity.transform.position.y);
                    writer.Write(players[i].entity.transform.position.z);
                    writer.Write(players[i].entity.transform.eulerAngles.x);
                    writer.Write(players[i].entity.transform.eulerAngles.y);
                    writer.Write(players[i].entity.transform.eulerAngles.z);
                }
            }
            if (write)
            {
                SendEveryone(readStream.ToArray());
                writer.Dispose();
                readStream.Dispose();
            }
        }
        else 
        {
            if(MainMenu.Socket.Available > 0)
            {
                byte[] buffer = new byte[MainMenu.Socket.Available];
                MainMenu.Socket.Receive(buffer);
                readStream = new MemoryStream(buffer);
                reader = new BinaryReader(readStream);
                TCPMessageType type = (TCPMessageType)reader.ReadByte();
                if(type == TCPMessageType.UpdatePos)
                {
                    int size = (buffer.Length - 1) / 25;
                    for (int i = 0; i < size; i++)
                    {
                        byte id = reader.ReadByte();
                        Vector3 pos = Networking.ReadVec(ref reader), rot = Networking.ReadVec(ref reader);
                        var player = Players.Find(x => x.id == id);
                        if(player != null)
                        {
                            player.entity.transform.position = pos;
                            player.entity.transform.eulerAngles = rot;
                        }
                    }
                }else if(type == TCPMessageType.PlayerConnect)
                {
                    byte id = reader.ReadByte();
                    string name = reader.ReadString();
                    Players.Add(new NetworkPlayer() {
                        entity = getNewPlayer(),
                        id = id,
                        name = name,
                        socket = null
                    });
                }
                reader.Dispose();
                readStream.Dispose();
            }
            if(Timer >= 1.0f/70)
            {
                Timer = 0;
                readStream = new MemoryStream();
                writer = new BinaryWriter(readStream);
                writer.Write((byte)TCPMessageType.UpdatePos);
                writer.Write(LocalPlayerEntity.transform.position.x);
                writer.Write(LocalPlayerEntity.transform.position.y);
                writer.Write(LocalPlayerEntity.transform.position.z);
                writer.Write(LocalPlayerEntity.transform.eulerAngles.x);
                writer.Write(LocalPlayerEntity.transform.eulerAngles.y);
                writer.Write(LocalPlayerEntity.transform.eulerAngles.z);
                MainMenu.Socket.Send(readStream.ToArray());
                readStream.Dispose();
                writer.Dispose();
            }
        }
    }
    GameObject getNewPlayer()
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var b= obj.AddComponent<Rigidbody>();
        b.freezeRotation = true;
        return obj;
    }
    static void SendEveryone(byte[] buffer)
    {
        foreach(NetworkPlayer player in Players)
        {
            if(player.socket != null)
                player.socket.Send(buffer);
        }
    }

    static void AcceptThread() 
    {
        BinaryWriter fr;
        MemoryStream stream;
        while (Running)
        {
            Socket newUser = MainMenu.Socket.Accept();
            byte[] buffer = new byte[52];
            newUser.ReceiveTimeout = 500;
            int received = newUser.Receive(buffer);
            JoinRequest request = Networking.DecodeMessage<JoinRequest>(buffer, received);
            int id = 0;
            for(int i = 0; i < 30; i++)
            {
                if (!Players.Exists(x => x.id == i))
                {
                    id = i;
                    break;
                }
            }
            JoinResponse response = new JoinResponse() {
                id = id,
                Players = Players
            };
            Networking.SendMessage(response, newUser);
            stream = new MemoryStream();
            fr = new BinaryWriter(stream);
            fr.Write((byte)TCPMessageType.PlayerConnect);
            fr.Write((byte)id);
            fr.Write(request.name);
            fr.Close();
            SendEveryone(stream.ToArray());
            stream.Dispose();
            fr.Dispose();
            Players.Add(new NetworkPlayer() { 
                id = id,
                name = request.name,
                socket = newUser
            });
            addId = id;
            Debug.Log("Player connected " + request.name);
        }
    }
}
