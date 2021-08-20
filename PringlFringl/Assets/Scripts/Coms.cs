using System.Collections.Generic;
public class JoinRequest 
{
    public string name, password;
}

public class JoinResponse 
{
    public int id;
    public List<NetworkPlayer> Players;
}

public enum TCPMessageType : byte
{
    PlayerConnect = 0,
    PlayerDisconnect = 1,
    UpdatePos = 2
}