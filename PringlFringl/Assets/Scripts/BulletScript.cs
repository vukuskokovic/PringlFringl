using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    bool ParamsSet = false;

    public Collider BulletColider;
    public float BulletSpeed;
    public float BulletDrop;
    private GameObject entity;
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
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (Networking.PlayerAlive)
            Networking.NetworkMono.LocalPlayer.GetComponent<Rigidbody>().AddExplosionForce(250, collision.transform.position, 10);
        
        Destroy(gameObject);
    }
}
