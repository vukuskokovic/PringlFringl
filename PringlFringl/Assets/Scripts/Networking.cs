using Newtonsoft.Json;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public static class Networking 
{
    public static T DecodeMessage<T>(byte[] buffer, int recevied)
    {
        string Json = Encoding.UTF8.GetString(buffer, 0, recevied);
        return JsonConvert.DeserializeObject<T>(Json);
    }

    public static byte[] EncodeMessage<T>(T obj)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
    }

    public static string GetString(byte[] buffer, int rec) {
        return Encoding.UTF8.GetString(buffer, 0, rec);
    }

    public static bool SendMessage<T>(T obj, Socket s) 
    {
        byte[] buffer = EncodeMessage(obj);
        s.Send(buffer);
        return true;
    }

    public static Vector3 ReadVec(ref BinaryReader reader)
    {
        Vector3 v = Vector3.one;
        v.x = reader.ReadSingle();
        v.y = reader.ReadSingle();
        v.z = reader.ReadSingle();
        return v;
    }
}