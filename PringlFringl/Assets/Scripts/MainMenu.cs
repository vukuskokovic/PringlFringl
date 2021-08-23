using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using static Networking;
using System;

public class MainMenu : MonoBehaviour
{
    public Button CreateRoomButton, JoinRoomButton;
    public InputField IPField, NameField;
    public Text text;
    public int Port;
    void Start()
    {
        IPField.text = "192.168.0.17";
        NameField.text = "Test";
        CreateRoomButton.onClick.AddListener(() => 
        {
            IPEndPoint point = new IPEndPoint(GetLocalIP(), Port);
            tcpSocket.Bind(point);
            udpSocket.Client.Bind(point);
            tcpSocket.Listen(3);
            Host = true;
            name = NameField.text;
            SceneManager.LoadScene(1);
        });
        JoinRoomButton.onClick.AddListener(() => 
        {
            try
            {
                IPEndPoint point = new IPEndPoint(IPAddress.Parse(IPField.text), Port);
                udpSocket.Client.Bind(new IPEndPoint(GetLocalIP(), 0));
                string username = NameField.text;
                byte[] sendBuffer = EncodeString(username);
                tcpSocket.Connect(point);
                tcpSocket.Send(sendBuffer);
                tcpSocket.ReceiveTimeout = 500;
                byte[] buffer = new byte[200];
                int receveied = tcpSocket.Receive(buffer);
                JoinResponse response = DecodeJson<JoinResponse>(buffer, receveied);
                foreach (var player in response.Players)
                {
                    Players.Add(player.id, player);
                }
                id = response.id;
                udpSocket.Send(new byte[] { 0, response.id }, 2, point);
                serverPoint = point;
                Host = false;
                name = NameField.text;
                SceneManager.LoadScene(1);
            }catch(Exception ex)
            {
                GUIUtility.systemCopyBuffer = ex.ToString();
            }
        });
    }
}
