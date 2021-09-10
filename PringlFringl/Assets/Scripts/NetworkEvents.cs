using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkEvents : MonoBehaviour
{
    public GameObject BulletPrefab;
    private void Start()
    {
        Networking.NetworkEvents = this;
    }

    public void Shoot(Vector3 origin, Vector3 rotation)
    {
        Networking.NetworkMono.TcpIO.LWrite();
        Networking.NetworkMono.TcpIO.WriteShot(origin, rotation, Networking.LocalPlayerId);
        Networking.NetworkMono.NetworkingInterface.SendTcp(Networking.NetworkMono.TcpIO.WriteStream.ToArray());
        Networking.NetworkMono.TcpIO.WDispose();
        if (Networking.IsHost)
        {
            SpawnBullet(new ProjectileInfo()
            {
                rotation = Camera.main.transform.eulerAngles,
                origin = Camera.main.transform.position,
                id = Networking.LocalPlayerId
            });
        }
    }

    //Spawns the bullet prefab and sets its params
    public void SpawnBullet(ProjectileInfo info) => NetworkMono.MainThreadInvokes.Enqueue(() => Instantiate(BulletPrefab).GetComponent<BulletScript>().SetParams(info)); 
    public void ServerPlayerDies(byte playerId)
    {
        System.Random r = new System.Random();
        int respawnIndex = r.Next(0, Networking.NetworkMono.Respawns.Count);
        Vector3 newPosition = Networking.NetworkMono.Respawns[respawnIndex].position;
        if (playerId == 0)
        {
            Networking.NetworkMono.LocalPlayer.GetComponent<Rigidbody>().velocity = Vector3.zero;
            Networking.NetworkMono.LocalPlayer.transform.position = newPosition;
            Networking.PlayerAlive = false;
        }
        else
        {
            Networking.NetworkMono.TcpIO.LWrite();
            Networking.NetworkMono.TcpIO.Writer.Write((byte)TCPMessageType.SetPosition);
            Networking.NetworkMono.TcpIO.WriteVector3(newPosition); // New position
            Networking.NetworkMono.TcpIO.Writer.Write(false); // Is alive
            Networking.Players[playerId].socket.Send(Networking.NetworkMono.TcpIO.WriteStream.ToArray());
            Networking.NetworkMono.TcpIO.WDispose();
        }
        Networking.Players[playerId].Alive = false;
    }
}
