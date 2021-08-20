using Newtonsoft.Json;
using System.Net.Sockets;
using UnityEngine;

public class NetworkPlayer
{
    [JsonIgnore]
    public Socket socket;
    [JsonIgnore]
    public GameObject entity;
    public int id;
    public string name;
}