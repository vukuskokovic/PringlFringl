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
    public PopupPanel popupPanel;
    public void ShowPanel(int index)
    {
        JoinPanel.SetActive(index == 0);
        CreatePanel.SetActive(index == 1);
        SettingsPanel.SetActive(index == 2);
    }

    private void Start()
    {
        IPField.text = GetLocalIP().ToString() + ":1420";
        PortField.text = "1420";
        JoinPanel.SetActive(false);
        CreatePanel.SetActive(false);
        SettingsPanel.SetActive(false);
        QuitButton.onClick.AddListener(() => {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif 
        });
        }

    public void JoinRoom()
    {
        if (PlayerName == "") return;
        try
        {
            string[] split = IPField.text.Split(':');
            if(split.Length == 1)
            {
                popupPanel.ShowPanel("Please enter correctly", "Enter the ip address in a valid format.\n ip:port example('192.168.0.0:1000')", 10);
                return;
            }
            if (!IPAddress.TryParse(split[0], out IPAddress RoomAddress))
            {
                popupPanel.ShowPanel("Please enter the ip address correctly", "You have typed an invalid address format", 10);
                return;
            }
            if (!int.TryParse(split[1], out int RoomPort))
            {
                popupPanel.ShowPanel("Please enter the port correctly", "The port that you have typed in is not a valid number", 10);
                return;
            }
            IPEndPoint RoomEndPoint = new IPEndPoint(RoomAddress, RoomPort);

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
            Networking.Connected = true;
            SceneManager.LoadScene(1);
        }
        catch (Exception ex)
        {
            popupPanel.ShowPanel("Could not connect to server", ex.Message, 10);
        }
    }

    public void CreateRoom()
    {
        if (!int.TryParse(PortField.text, out int port))
        {
            popupPanel.ShowPanel("Please enter the port correctly", "The port that you have typed in is not a valid number", 10);
            return;
        }
        else if(port == 0)
        {
            popupPanel.ShowPanel("Please enter the port correctly", "The port cannot be 0", 10);
            return;
        }
        try
        {
            IPEndPoint point = new IPEndPoint(GetLocalIP(), port);
            tcpSocket.Bind(point);
            udpSocket.Client.Bind(point);
            tcpSocket.Listen(3);
            Host = true;
            Connected = true;
            SceneManager.LoadScene(1);
        }catch(Exception ex)
        {
            popupPanel.ShowPanel("Could not create a room", ex.Message, 10);
            return;
        }
    }
}
