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
public class NetworkMono : MonoBehaviour
{
    // Public variables
    public GameObject LocalPlayer;
    public List<Transform> PlayerSpawns;
    public PopupPanel popupPanel;
    public int Ticks;
    public GameObject BulletPrefab;
    [HideInInspector]
    public NetworkingPlayer[] PlayersCurrentFrame;

    // Private variables
    public static INetworkingInterface NetworkingInterface;
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
        catch (Exception ex)
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
        if (UpdateTimer >= 1.0f / Ticks) // Update position for client, for server update all clients with positions
        {
            UpdateTimer = 0f;
            UdpIO.LWrite();
            NetworkingInterface.UpdatePosition();
            UdpIO.WDispose();
        }
    }

    public void SpawnBullet(Vector3 position, Vector3 rotation)
    {
        var obj = Instantiate(BulletPrefab);
        obj.GetComponent<BulletScript>().SetPosition(position, rotation);
    }

    void UDPThread()
    {
        while (GameRunning)
        {
            if (!Connected) continue;
            try 
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = udpSocket.Receive(ref RemoteIpEndPoint);
                UdpIO.LRead(buffer);
                UDPMessageType type = (UDPMessageType)UdpIO.Reader.ReadByte();
                NetworkingInterface.ReadUdp(type, buffer.Length, RemoteIpEndPoint);
                UdpIO.RDispose();
            }catch(SocketException ex)
            {
                if (!Connected) continue;
                else if (ex.ErrorCode == 10054)// For some reason this exception is thrown when player is disconnecting(server side, code 10054)
                    continue;
                else Debug.LogError(ex.ErrorCode);
            }
        }
    }
}
