using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button JoinRoomButton, CreateRoomButton;

    public static bool CreateRoom = false;
    public static Socket Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    void Start()
    {
        Debug.Log(LocalIPAddress());
        CreateRoomButton.onClick.AddListener(() => 
        {
            Socket.Bind(new IPEndPoint(LocalIPAddress(), 1420));
            Socket.Listen(2);
        });
    }

    IPAddress LocalIPAddress()
    {
        IPHostEntry host;
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
            if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "172.27.0.1")
                return ip;
            
        
        return null;
    }
}
