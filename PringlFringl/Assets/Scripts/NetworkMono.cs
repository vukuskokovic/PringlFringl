﻿using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using static Networking;
namespace Assets.Scripts
{
    public class NetworkMono : MonoBehaviour
    {

        public GameObject LocalPlayer;
        public List<Transform> PlayerSpawns;
        private ProtocolIO UdpIO = new ProtocolIO(), TcpIO = new ProtocolIO();
        public int Ticks;
        private float Timer = 0.0f;
        private Queue<byte> PlayersToAdd = new Queue<byte>();
        private Queue<NetworkingPlayer> PlayersToSpawn = new Queue<NetworkingPlayer>();
        private bool Connected = true;
        void Start()
        {
            if (Host) 
            {
                Players.Add(0, new NetworkingPlayer() { 
                    Entity = LocalPlayer,
                    id = 0,
                    username = "SERVERF"
                });
                new Thread(AcceptThread).Start();
                SpawnPlayer(Players[0]);
            }
            else 
            {
                foreach(NetworkingPlayer player in Players.Values)
                    player.Entity = InitPlayerEntity();
                
            }
            new Thread(UDPThread).Start();
        }
        private void OnApplicationQuit()
        {
            Connected = false;
        }
        void Update()
        {
            Timer += Time.deltaTime;
            while(PlayersToAdd.Count != 0) Players[PlayersToAdd.Dequeue()].Entity = InitPlayerEntity();
            while (PlayersToSpawn.Count != 0) SpawnPlayer(PlayersToSpawn.Dequeue());
            foreach (KeyValuePair<byte, NetworkingPlayer> player in Players.Where(x => x.Value.updateAvalible && (!Host || x.Value.socket != null)))
            {
                player.Value.updateAvalible = false;
                player.Value.Entity.transform.position = player.Value.updatePos;
                player.Value.Entity.transform.eulerAngles = player.Value.updateRot;
            }
            if(!Host && tcpSocket.Available > 0) 
            {
                byte[] buffer = new byte[tcpSocket.Available];
                tcpSocket.Receive(buffer);
                TcpIO.LRead(buffer);
                TCPMessageType type = (TCPMessageType)TcpIO.Reader.ReadByte();
                if(type == TCPMessageType.PlayerConnect)
                {
                    byte playerId = TcpIO.Reader.ReadByte();
                    string username = TcpIO.Reader.ReadString();
                    Players.Add(playerId, new NetworkingPlayer() { 
                        Entity = InitPlayerEntity(),
                        id = playerId,
                        username = username
                    });
                }
                else if(type == TCPMessageType.SetPosition)
                    LocalPlayer.transform.position = ReadVector3(ref TcpIO.Reader);

                TcpIO.RDispose();
            }

            if (Timer >= 1.0f / Ticks)
            {
                Timer = 0f;
                UpdatePos();
            }
        }

        GameObject InitPlayerEntity()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var body = obj.AddComponent<Rigidbody>();
            body.freezeRotation = true;
            body.constraints = RigidbodyConstraints.FreezePosition;
            return obj;
        }

        void SpawnPlayer(NetworkingPlayer player)
        {
            int spawnId = new System.Random().Next(0, PlayerSpawns.Count);
            Transform t = PlayerSpawns[spawnId];
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
                LocalPlayer.transform.position = t.position;
        }

        void UpdatePos()
        {
            UdpIO.LWrite();
            // If process is in host mode
            if (Host) 
            {
                UdpIO.Writer.Write((byte)UDPMessageType.UpdatePos);
                NetworkingPlayer[] players = Players.Values.ToArray();
                for (int i = 0; i < players.Length; i++)
                    UdpIO.WriteTransform(players[i].id, players[i].Entity.transform.position, players[i].Entity.transform.eulerAngles);
                
                byte[] buffer = UdpIO.WriteStream.ToArray();
                for (int i = 0; i < players.Length; i++)
                    if(players[i].socket != null && players[i].listenPoint != null)
                        udpSocket.Send(buffer, buffer.Length, players[i].listenPoint);
            }
            // If process in in client mode
            else
            {
                UdpIO.Writer.Write((byte)UDPMessageType.UpdatePos);
                UdpIO.WriteTransform(id, LocalPlayer.transform.position, LocalPlayer.transform.eulerAngles);
                byte[] buffer = UdpIO.WriteStream.ToArray();
                udpSocket.Send(buffer, buffer.Length, serverPoint);
            }
            UdpIO.WDispose();
        }

        void ReadPlayerPos()
        {
            byte playerId = UdpIO.Reader.ReadByte();
            Vector3 pos = ReadVector3(ref UdpIO.Reader);
            Vector3 rot = ReadVector3(ref UdpIO.Reader);
            if (playerId == id) return;
            NetworkingPlayer player = Players[playerId];
            player.updatePos = pos;
            player.updateRot = rot;
            player.updateAvalible = true;
        }

        void AcceptThread()
        {
            while (Connected)
            {
                Socket socket = tcpSocket.Accept();
                byte[] buffer = new byte[20];
                int rec = socket.Receive(buffer);
                string username = DecodeString(buffer, rec);
                int assignedId = 0;
                System.Random r = new System.Random();
                assignedId = r.Next(0, 100);
                while (Players.ContainsKey((byte)assignedId)) assignedId = r.Next(0, 100);

                JoinResponse response = new JoinResponse() { 
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
                    socket = socket
                };
                Players.Add(player.id, player);
                PlayersToAdd.Enqueue(player.id);
                PlayersToSpawn.Enqueue(player);
            }
        }
        private void UDPThread()
        {
            while (Connected)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] Buffer = udpSocket.Receive(ref RemoteIpEndPoint);
                UdpIO.LRead(Buffer);
                UDPMessageType type = (UDPMessageType)UdpIO.Reader.ReadByte();
                if (Host)
                {
                    if (type == UDPMessageType.DummyPacket)
                    {
                        byte playerId = UdpIO.Reader.ReadByte();
                        Players[playerId].listenPoint = RemoteIpEndPoint;
                    }
                    else if (type == UDPMessageType.UpdatePos)
                        ReadPlayerPos();
                }
                else
                {
                    if (type == UDPMessageType.UpdatePos)
                    {
                        int length = (Buffer.Length - 1) / 25;
                        for (int i = 0; i < length; i++)
                            ReadPlayerPos();
                    }
                }
                UdpIO.RDispose();
            }
        }
    }
}