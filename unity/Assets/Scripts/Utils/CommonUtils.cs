using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class CommonUtils
{
    // calls DestroyImmediate in Editor, Destroy in Game 
    public static void AdaptiveDestroy(GameObject gameObject)
    {
        if (!gameObject) return;
#if UNITY_EDITOR
        if (Application.isPlaying == false) MonoBehaviour.Destroy(gameObject);
        else MonoBehaviour.DestroyImmediate(gameObject);
#else
        MonoBehaviour.Destroy(gameObject);
#endif
    }

    public static string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string candidate = ip.ToString();
                if (candidate.StartsWith("192.168.")) // emulator gives strange addresses
                {
                    localIP = candidate;
                    break;
                }
            }
        }
        return localIP;
    }

    // Helper function for getting the command line arguments
    public static string GetStartupParameter(string name)
    {
#if UNITY_EDITOR
        var args = new string[] { "Test.exe",
            //"-server", "192.168.8.145",
            //"-media", "http://clips.vorwaerts-gmbh.de/VfE_html5.mp4",
            //"-hub", "ws://127.0.0.1:54321"
        };
        //var args = new string[] { };
#else
        var args = Environment.GetCommandLineArgs();
#endif
        Debug.Log("GetStartupParameter for " + name + ", args is " + args.ToString());
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                Debug.Log("GetStartupParameter for " + name + " returning " + args[i + 1]);
                return args[i + 1];
            }
        }
        return null;
    }

}