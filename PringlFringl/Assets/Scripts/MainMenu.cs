using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Networking;

public class MainMenu : MonoBehaviour
{
    public Button CreateRoomButton, JoinRoomButton;
    public InputField IPField;
    void Start()
    {
        IPField.text = "192.168.0.17";
        CreateRoomButton.onClick.AddListener(() => 
        {
            IPEndPoint point = new IPEndPoint(GetLocalIP(), 1420);
            tcpSocket.Bind(point);
            udpSocket.Client.Bind(point);
            tcpSocket.Listen(3);
            Host = true;
            SceneManager.LoadScene(1);
        });
        JoinRoomButton.onClick.AddListener(() => 
        {
            IPEndPoint point = new IPEndPoint(IPAddress.Parse(IPField.text), 1420);
            udpSocket.Client.Bind(new IPEndPoint(GetLocalIP(), 0));
            string username = "NIGGERS";
            byte[] sendBuffer = EncodeString(username);
            tcpSocket.Connect(point);
            tcpSocket.Send(sendBuffer);
            tcpSocket.ReceiveTimeout = 500;
            byte[] buffer = new byte[200];
            int receveied = tcpSocket.Receive(buffer);
            JoinResponse response = DecodeJson<JoinResponse>(buffer, receveied);
            Players = response.Players;
            id = response.id;
            udpSocket.Send(new byte[] { 0, response.id }, 2, point);
            serverPoint = point;
            Host = false;
            SceneManager.LoadScene(1);
        });
    }
}
