using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;

public class SocketIOJSONMessenger : SocketJSONMessenger
{

    private SocketIOComponent _socket;
    
    protected override void Awake ()
    {

        base.Awake();

        _socket = GetComponent<SocketIOComponent>();

        _socket.On("open", OnOpen);
        _socket.On("error", OnError);
        _socket.On("close", OnClose);
        _socket.On("message", OnMessage);
    }


    private void OnOpen(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
    }

    private void OnError(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
    }

    private void OnClose(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
    }

    private void OnTime(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Time received: " + e.name + " " + e.data);
    }

    private void OnMessage(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Message received: " + e.name + " " + e.data);

        if (e.data == null)
        {
            Debug.LogError("SocketIOResponder entity expected data");
            return;
        }
        else
        {
            // this should already be on the main thread so there is no need to enqueue
            BroadcastIncomingMessage(e.data);
        }
        

    }
}
