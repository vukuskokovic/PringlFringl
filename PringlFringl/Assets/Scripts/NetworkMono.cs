using System;
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
        // Public variables
        public GameObject LocalPlayer;
        public List<Transform> PlayerSpawns;
        public int Ticks;
        [HideInInspector]
        public NetworkingPlayer[] PlayersCurrentFrame;

        // Private variables
        private INetworkingInterface NetworkingInterface;
        public static ProtocolIO UdpIO = new ProtocolIO(),
                           TcpIO = new ProtocolIO();
        private float UpdateTimer = 0.0f;
        private bool GameRunning = true;
        public static Queue<Action> MainThreadInvokes = new Queue<Action>();
        void Start()
        {
            if (Host) NetworkingInterface = gameObject.AddComponent<ServerNetworking>();
            else NetworkingInterface = gameObject.AddComponent<ClientNetworking>();
            new Thread(UDPThread).Start();
        }

        private void OnApplicationQuit()
        {
            GameRunning = false;
            Connected = false;
            try
            {
                Debug.Log("Quiting");
                tcpSocket.Dispose();
                udpSocket.Dispose();
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);
            }
        }
        void Update()
        {
            PlayersCurrentFrame = Players.Values.ToArray();//All the players that are in the current frame(used in case the collection gets modified so it does not raise an excpetion)
            while (MainThreadInvokes.Count > 0) MainThreadInvokes.Dequeue()();
            // End recieve tcp client

            UpdateTimer += Time.deltaTime;
            if (UpdateTimer >= 1.0f / Ticks)
            {
                UpdateTimer = 0f;
                UpdatePos();
            }
        }

        void UpdatePos()
        {
            UdpIO.LWrite();
            NetworkingInterface.UpdatePosition();
            UdpIO.WDispose();
        }

        void UDPThread()
        {
            while (GameRunning)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = udpSocket.Receive(ref RemoteIpEndPoint);
                UdpIO.LRead(buffer);
                UDPMessageType type = (UDPMessageType)UdpIO.Reader.ReadByte();
                NetworkingInterface.ReadUdp(type, buffer.Length, RemoteIpEndPoint);
                UdpIO.RDispose();
            }
        }
    }
}