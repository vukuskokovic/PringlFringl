using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public Collider BulletColider;
    public float BulletSpeed;
    public float BulletDrop;
    public float Range;
    public float Force;
    private GameObject entity;
    private bool ParamsSet = false;
    public void SetParams(ProjectileInfo info)
    {
        transform.position = info.origin;
        transform.eulerAngles = info.rotation + (Vector3.right*90);
        entity = info.id == Networking.LocalPlayerId ? Networking.NetworkMono.LocalPlayer : Networking.Players[info.id].Entity;
        name = "Projectile";
        gameObject.layer = 6;
        Physics.IgnoreCollision(BulletColider, entity.gameObject.GetComponent<Collider>());
        BulletColider.enabled = true;
        ParamsSet = true;
    }

    private void Update()
    {
        if (ParamsSet)
        {
            transform.position += transform.up * Time.deltaTime * BulletSpeed;
            //transform.Rotate(Vector3.up * Time.deltaTime * BulletSpeed * 10);
            transform.Rotate(Vector3.right * Time.deltaTime * BulletDrop);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (Networking.PlayerAlive)
        {
            float distance = Vector3.Distance(collision.contacts[0].point, Networking.NetworkMono.LocalPlayer.transform.position);
            if(distance < Range)
            {
                float amplifier = (1 - (distance / Range)) * Force;
                Vector3 forceFrom = Networking.NetworkMono.LocalPlayer.transform.position - collision.contacts[0].point;
                forceFrom.Normalize();
                Networking.NetworkMono.LocalPlayer.GetComponent<Rigidbody>().AddForce(forceFrom * amplifier);
            }
        }
        
        Destroy(gameObject);
    }
}
