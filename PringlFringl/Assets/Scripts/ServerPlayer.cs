using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayer : MonoBehaviour
{
    private byte playerId;

    public void SetId(byte id)
    {
        playerId = id;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Respawn")
        {
            Debug.Log("Player " + playerId + " has died");
            Networking.NetworkEvents.ServerPlayerDies(playerId);
        }
    }
}
