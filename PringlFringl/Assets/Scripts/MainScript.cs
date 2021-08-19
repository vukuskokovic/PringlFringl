using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class MainScript : MonoBehaviour
{
    public static bool Running = true;
    void Start()
    {
        if (MainMenu.CreateRoom) 
        {
            new Thread( AcceptThread ).Start();
        }    
    }

    private void OnApplicationQuit()
    {
        Running = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    static void AcceptThread() 
    {
        while (Running)
        {
            Socket newUser = MainMenu.Socket.Accept();

        }
    }
}
