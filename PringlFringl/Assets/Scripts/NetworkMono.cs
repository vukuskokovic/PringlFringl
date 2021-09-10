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
    public GameObject LocalPlayer, BulletPrefab, PlayerPrefab;
    public List<Transform> PlayerSpawns;
    public List<Transform> Respawns;
    public PopupPanel popupPanel;
    public int Ticks;

    [HideInInspector]
    public NetworkingPlayer[] PlayersCurrentFrame;

    public static Queue<Action> MainThreadInvokes = new Queue<Action>();
    public INetworkingInterface NetworkingInterface;
    public NetworkIO UdpIO = new NetworkIO(),
                             TcpIO = new NetworkIO();
    // Private variables

    private float UpdateTimer = 0.0f;
    private bool GameRunning = true;
    
    void Start()
    {
        Networking.NetworkMono = this;
        Physics.IgnoreLayerCollision(6, 7);//Makes bullets not collide with bullet through objects
        if (IsHost) NetworkingInterface = gameObject.AddComponent<ServerNetworking>();
        else NetworkingInterface = gameObject.AddComponent<ClientNetworking>();
        new Thread(UDPThread).Start();
    }

    private void OnApplicationQuit()
    {
        GameRunning = false;
        IsConnected = false;
        try
        {
            TcpSocket.Dispose();
            UdpSocket.Dispose();
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

    void UDPThread()
    {
        while (GameRunning)
        {
            if (!IsConnected) continue;
            try 
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = UdpSocket.Receive(ref RemoteIpEndPoint);
                UdpIO.LRead(buffer);
                UDPMessageType type = (UDPMessageType)UdpIO.Reader.ReadByte();
                NetworkingInterface.ReadUdp(type, buffer.Length, RemoteIpEndPoint);
                UdpIO.RDispose();
            }catch(SocketException ex)
            {
                if (!IsConnected && UdpSocket == null) return;
                else if (ex.ErrorCode == 10054 || ex.ErrorCode == 10004)// For some reason this exception is thrown when player is disconnecting(server side, code 10054)
                    continue;
                else Debug.LogError(ex.ErrorCode);
            }
        }
    }

    public GameObject InitNewPlayerEntity(byte playerid, string username)
    {
        GameObject obj = Instantiate(PlayerPrefab);
        obj.name = "Player " + playerid;
        obj.GetComponentInChildren<TextMesh>().text = username;
        if (IsHost)
            obj.AddComponent<ServerPlayer>().SetId(playerid);
        

        return obj;
    }
}
