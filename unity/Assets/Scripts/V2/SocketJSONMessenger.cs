using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this class is meant as an abstraction which works for any JSONObject sender/receiver
// including websocket and socket.io

public delegate void JSONMessagetHandler(object sender, JSONObject json);

public class SocketJSONMessenger : MonoBehaviour
{
    public event JSONMessagetHandler JSONMessageEvent;

    private object _jsonQueueLock;
    private Queue<JSONObject> _jsonQueue;

    protected virtual void Awake()
    {
        _jsonQueueLock = new object();
        _jsonQueue = new Queue<JSONObject>();
    }

    public virtual void SendMessage(JSONObject json)
    {
        // subclasses should implement this
    }

    // this should be called on the main thread, not on some background networking thread
    protected void BroadcastIncomingMessage(JSONObject json)
    {
        if (JSONMessageEvent != null)
        {
            JSONMessageEvent(this, json);
        }
    }

    // this should be called from a background (likely networking) thread, the enqueued messages
    // will be dequeued and broadcast on the main thread on the next Update call 
    protected void EnqueueIncomingMessage(JSONObject json)
    {
        lock (_jsonQueueLock)
        {
            _jsonQueue.Enqueue(json);
        }
    }

    protected virtual void Update()
    {
        lock (_jsonQueueLock)
        {
            while (_jsonQueue.Count > 0)
            {
                BroadcastIncomingMessage(_jsonQueue.Dequeue());
            }
        }
    }
}
