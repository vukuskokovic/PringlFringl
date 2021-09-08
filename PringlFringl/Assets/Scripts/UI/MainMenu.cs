using System;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Networking;

public class MainMenu : MonoBehaviour
{
    public Button QuitButton, CreateRoomButton, JoinButton;
    public GameObject JoinPanel, CreatePanel, SettingsPanel;
    public InputField IPField, PortField;
    public PopupPanel popupPanel;
    public void ShowPanel(int index)
    {
        JoinPanel.SetActive(index == 0);
        CreatePanel.SetActive(index == 1);
        SettingsPanel.SetActive(index == 2);
        CreateRoomButton.onClick.AddListener(() => CreateRoom());
        JoinButton.onClick.AddListener(() => JoinRoom());
        Screen.SetResolution(800, 600, false);
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
        QualitySettings.SetQualityLevel(6);
    }

    public int JoinRoom()
    {
        if (LocalPlayerName == "") return 0;
        try
        {
            string[] split = IPField.text.Split(':');
            if(split.Length == 1) 
                return popupPanel.ShowPanel("Please enter correctly", "Enter the ip address in a valid format.\n ip:port example('192.168.0.0:1000')", 10);
            if (!IPAddress.TryParse(split[0], out IPAddress RoomAddress)) 
                return popupPanel.ShowPanel("Please enter the ip address correctly", "You have typed an invalid address format", 10);
            if (!int.TryParse(split[1], out int RoomPort)) 
                return popupPanel.ShowPanel("Please enter the port correctly", "The port that you have typed in is not a valid number", 10);
            
            ServerEndPoint = new IPEndPoint(RoomAddress, RoomPort);
            Networking.Connect();
            SceneManager.LoadScene(1);
            return 1;
        }
        catch (Exception ex)
        {
            return popupPanel.ShowPanel("Could not connect to server", ex.Message, 10);
        }
    }

    public int CreateRoom()
    {
        if (!int.TryParse(PortField.text, out int port))
            return popupPanel.ShowPanel("Please enter the port correctly", "The port that you have typed in is not a valid number", 10);
        else if(port == 0)
            return popupPanel.ShowPanel("Please enter the port correctly", "The port cannot be 0", 10);
        try
        {
            IPEndPoint point = new IPEndPoint(GetLocalIP(), port);
            TcpSocket.Bind(point);
            UdpSocket.Client.Bind(point);
            TcpSocket.Listen(3);
            IsHost = true;
            IsConnected = true;
            SceneManager.LoadScene(1);
            return 1;
        }catch(Exception ex)
        {
            return popupPanel.ShowPanel("Could not create a room", ex.Message, 10);
        }
    }
}
