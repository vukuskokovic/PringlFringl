using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayer : MonoBehaviour
{
    public byte pId;

    public void SetId(byte id)
    {
        pId = id;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Respawn")
        {
            Networking.NetworkEvents.ServerPlayerDies(pId);
        }
    }
}
