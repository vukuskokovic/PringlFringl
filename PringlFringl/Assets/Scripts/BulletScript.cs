using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    bool positionSet = false;
    public void SetPosition(Vector3 position, Vector3 rotation)
    {
        transform.position = position;
        transform.eulerAngles = rotation + (Vector3.right*90);
        positionSet = true;
    }

    private void Update()
    {
        if (positionSet)
        {
            transform.position += transform.up * Time.deltaTime * 10;
            //transform.eulerAngles = new Vector3(transform.eulerAngles.x - Time.deltaTime * 1000, transform.eulerAngles.y, transform.eulerAngles.z);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        var point = collision.contacts[0];
        Destroy(gameObject);
    }
}
