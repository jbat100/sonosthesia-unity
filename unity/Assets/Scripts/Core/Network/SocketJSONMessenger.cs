using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Net;
using UnityEngine;

namespace Sonosthesia
{

    public class JSONSocketUtils
    {

        // taken from MessageEventArgs
        private static string BinaryToString(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public static JSONObject MessageEventArgsToJSONObject(MessageEventArgs e)
        {

            JSONObject json = null;
            
            if (e.Type == Opcode.Text)
            {
                //Debug.Log("JSONSocketUtils text ");
                json = new JSONObject(e.Data);
            }
            else if (e.Type == Opcode.Binary)
            {
                string text = BinaryToString(e.RawData);
                //Debug.Log("JSONSocketUtils converted binary to text ");
                json = new JSONObject(text);
            }

            return json;
        }
    }

    public enum SocketStatus
    {
        UNDEFINED,
        DISCONNECTED,
        CONNECTING,
        CONNECTED,
        ERROR
    }

    public struct SocketStatusEventArgs
    {
        public SocketStatus status;
        public SocketStatus previous;

        public SocketStatusEventArgs(SocketStatus _status, SocketStatus _previous = SocketStatus.UNDEFINED)
        {
            status = _status;
            previous = _previous;
        }
    }

    public delegate void SocketStatusEventHandler(object sender, SocketStatusEventArgs args);

    // this class is meant as an abstraction which works for any JSONObject sender/receiver
    // including websocket and socket.io

    public delegate void JSONMessageEventHandler(object sender, List<JSONObject> jsonObjects);

    abstract public class SocketJSONMessenger : MonoBehaviour
    {
        
        public bool push = false;
        public bool autoConnect = true;
        public bool logJSON = false;

        public event JSONMessageEventHandler JSONMessageEvent;

        public event SocketStatusEventHandler StatusEvent;

        public SocketStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    SocketStatus previous = _status;
                    _status = value;
                    if (StatusEvent != null)
                    {
                        StatusEvent(this, new SocketStatusEventArgs(_status, previous));
                    }
                }
            }
        }

        private SocketStatus _status = SocketStatus.UNDEFINED;
        private object _jsonQueueLock;
        private List<JSONObject> _jsonQueue;

        protected virtual void Awake()
        {
            _jsonQueueLock = new object();
            _jsonQueue = new List<JSONObject>();
        }


        abstract public void SendString(string str);

        public virtual void SendMessage(JSONObject json)
        {
            string str = json.Print();

            if (logJSON)
            {
                Debug.Log(GetType().Name + " sending JSON : " + str);
            }

            if (Status == SocketStatus.CONNECTED)
            {
                SendString(str);
            }
            else
            {
                Debug.LogWarning("Cannot send message, socket is not connected");
            }
            
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
