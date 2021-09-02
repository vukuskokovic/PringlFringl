using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Transform DirectionTransform;
    public RectTransform JumpProgress;
    public NetworkMono networkMono;


    Rigidbody PlayerRigidBody;
    Vector2 mouseRotation = new Vector2(0, 0);
    public float Sensitivity = 3.0f,
                 Speed = 3.0f;
    public float JumpTimer;
    private float jumpElapsed = 0.0f;
    bool MoveMouse = true;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerRigidBody = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (jumpElapsed > 0) jumpElapsed -= Time.deltaTime;
        if (jumpElapsed < 0) jumpElapsed = 0;

        JumpProgress.sizeDelta = new Vector2(100 - (jumpElapsed/JumpTimer*100), 100);
        DirectionTransform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        if (MoveMouse)
        {
            mouseRotation.y += Input.GetAxis("Mouse X") * Sensitivity;
            mouseRotation.x = Mathf.Clamp(mouseRotation.x + (Input.GetAxis("Mouse Y") * Sensitivity), -70, 90);
            transform.localEulerAngles = new Vector3(-mouseRotation.x, mouseRotation.y, 0);
        }
        if (Input.anyKey)
        {
            Vector3 position = Vector3.zero;
            float SpeedAdd = Speed * Time.deltaTime;
            if (Input.GetKey(KeyCode.W)) position += DirectionTransform.forward;
            if (Input.GetKey(KeyCode.S)) position -= DirectionTransform.forward;
            if (Input.GetKey(KeyCode.A)) position -= DirectionTransform.right;
            if (Input.GetKey(KeyCode.D)) position += DirectionTransform.right;
            if (Input.GetKey(KeyCode.Space) && jumpElapsed == 0)
            {
                jumpElapsed = JumpTimer;
                PlayerRigidBody.AddForce(DirectionTransform.up * 10, ForceMode.VelocityChange);
            }
            position *= SpeedAdd;
            transform.position += position;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            MoveMouse = false;
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (!MoveMouse)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                MoveMouse = true;
            }
            else
            {
                NetworkMono.TcpIO.LWrite();
                NetworkMono.TcpIO.WriteShot(Camera.main.transform.position, Camera.main.transform.eulerAngles, Networking.playerId);
                NetworkMono.NetworkingInterface.SendTcp(NetworkMono.TcpIO.WriteStream.ToArray());
                NetworkMono.TcpIO.WDispose();
                networkMono.SpawnBullet(Camera.main.transform.position, Camera.main.transform.eulerAngles);
            }
        }
    }
}
