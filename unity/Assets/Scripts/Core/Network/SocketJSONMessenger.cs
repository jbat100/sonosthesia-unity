using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{


    // this class is meant as an abstraction which works for any JSONObject sender/receiver
    // including websocket and socket.io

    public delegate void JSONMessagetHandler(object sender, List<JSONObject> jsonObjects);

    abstract public class SocketJSONMessenger : MonoBehaviour
    {
        public event JSONMessagetHandler JSONMessageEvent;
        public bool push = false;
        public bool autoConnect = true;

        private object _jsonQueueLock;
        private List<JSONObject> _jsonQueue;

        protected virtual void Awake()
        {
            _jsonQueueLock = new object();
            _jsonQueue = new List<JSONObject>();
        }

        public void OnEnable()
        {
            if (autoConnect) { Connect(); }
        }

        public void OnDisable()
        {
            Close();
        }

        abstract public void Connect();

        abstract public void Close();

        public virtual void SendMessage(JSONObject json)
        {
            // subclasses should implement this
        }

        public List<JSONObject> DequeueMessages()
        {
            if (push)
            {
                throw new Exception("cannot dequeue messages in push mode");
            }
            else
            {
                return InternalDequeueMessages();
            }
        }

        // this should be called on the main thread, not on some background networking thread
        protected void BroadcastIncomingMessage(JSONObject json)
        {
            if (push)
            {
                InternalEnqueueMessage(json);
            }
            else
            {
                PushJSONObject(json);
            }
        }

        protected virtual void Update()
        {
            if (push)
            {
                PushJSONObjects(InternalDequeueMessages());
            }
        }

        protected void PushJSONObject(JSONObject json)
        {
            if (JSONMessageEvent != null)
            {
                JSONMessageEvent(this, new List<JSONObject> { json });
            }
        }

        protected void PushJSONObjects(List<JSONObject> jsons)
        {
            if (JSONMessageEvent != null)
            {
                JSONMessageEvent(this, jsons);
            }
        }

        // this should be called from a background (likely networking) thread, the enqueued messages
        // will be dequeued and broadcast on the main thread on the next Update call 
        protected void InternalEnqueueMessage(JSONObject json)
        {
            lock (_jsonQueueLock)
            {
                _jsonQueue.Add(json);
            }
        }

        protected List<JSONObject> InternalDequeueMessages()
        {
            List<JSONObject> temp = new List<JSONObject>();

            lock (_jsonQueueLock)
            {
                temp.AddRange(_jsonQueue);
                _jsonQueue.Clear();
            }

            return temp;
        }
    }


}
