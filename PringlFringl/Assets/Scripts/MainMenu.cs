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
    public Button QuitButton;
    public GameObject JoinPanel, CreatePanel, SettingsPanel;
    public InputField IPField, PortField;

    public void ShowPanel(int index)
    {
        JoinPanel.SetActive(index == 0);
        CreatePanel.SetActive(index == 1);
        SettingsPanel.SetActive(index == 2);
    }
    void Start()
    {
        IPField.text = "192.168.0.17:1420";
        PortField.text = "1420";
        JoinPanel.SetActive(false);
        CreatePanel.SetActive(false);
        SettingsPanel.SetActive(false);
        QuitButton.onClick.AddListener(() => { Application.Quit(); });
    }

    public void JoinRoom()
    {
        if (PlayerName == "") return;
        try
        {
            string[] split = IPField.text.Split(':');
            if (!IPAddress.TryParse(split[0], out IPAddress RoomAddress)) return;
            if (!int.TryParse(split[1], out int RoomPort)) return;
            IPEndPoint RoomEndPoint = new IPEndPoint(RoomAddress, RoomPort);

            udpSocket.Client.Bind(new IPEndPoint(GetLocalIP(), 0));
            tcpSocket.Connect(RoomEndPoint);
            tcpSocket.Send(EncodeString(Networking.PlayerName));
            tcpSocket.ReceiveTimeout = 500;

            byte[] buffer = new byte[200];
            int receveied = tcpSocket.Receive(buffer);
            JoinResponse response = DecodeJson<JoinResponse>(buffer, receveied);
            foreach (var player in response.Players)
                Players.Add(player.id, player);
            Networking.playerId = response.id;
            udpSocket.Send(new byte[] { 0, response.id }, 2, RoomEndPoint);
            Networking.ServerEndPoint = RoomEndPoint;
            Networking.Host = false;
            SceneManager.LoadScene(1);
        }
        catch (Exception ex)
        {
            GUIUtility.systemCopyBuffer = ex.ToString();
        }
    }

    public void CreateRoom()
    {
        IPEndPoint point = new IPEndPoint(GetLocalIP(), int.Parse(PortField.text));
        tcpSocket.Bind(point);
        udpSocket.Client.Bind(point);
        tcpSocket.Listen(3);
        Host = true;
        SceneManager.LoadScene(1);
    }
}
