using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public GameObject Player;
    float lastY;
    Rigidbody PlayerRigidBody;
    Vector2 mouseRotation = new Vector2(0, 0);
    public float Sensitivity = 3.0f;
    public float Speed = 3.0f;
    float sinceJump = 0.0f;
    bool InJump = false;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerRigidBody = GetComponent<Rigidbody>();
       
    }
    void Update()
    {
        if(InJump)
            sinceJump += Time.deltaTime;

        mouseRotation.y += Input.GetAxis("Mouse X") * Sensitivity;
        mouseRotation.x += Input.GetAxis("Mouse Y") * Sensitivity;
        transform.localEulerAngles = new Vector3(-mouseRotation.x, mouseRotation.y, 0);
        if (Input.anyKey)
        {
            Vector3 position = Vector3.zero;
            float SpeedAdd = Speed * Time.deltaTime;
            if (Input.GetKey(KeyCode.W)) position += transform.forward;
            if (Input.GetKey(KeyCode.S)) position -= transform.forward;
            if (Input.GetKey(KeyCode.A)) position -= transform.right;
            if (Input.GetKey(KeyCode.D)) position += transform.right;
            if (Input.GetKey(KeyCode.Space) && !InJump && PlayerRigidBody.velocity.y == 0)
            {
                PlayerRigidBody.AddForce(Vector3.up * 250);
                InJump = true;
            }
            position *= SpeedAdd;
            transform.position += position;
        }
        if (Input.anyKeyDown) 
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        if (Input.GetMouseButton(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (lastY == transform.position.y && sinceJump > 0.5)
        {
            InJump = false;
            sinceJump = 0.0f;
        }
        lastY = transform.position.y;
    }
}
