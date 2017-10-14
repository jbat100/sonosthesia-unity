
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;


namespace Sonosthesia
{
    // inspired by SocketIOComponent

    public class WSJSONMessenger : SocketJSONMessenger
    {
        public string url = "ws://127.0.0.1:3000";
        public int reconnectDelay = 5;
        public bool sendInBinary = false;
        
        public bool IsConnected { get { return connected; } }

        private volatile bool connected;
        private volatile string errorMessage;

        private Thread socketThread;
        private WebSocket webSocket;

        protected override void Awake()
        {
            base.Awake();

            // media can be overriden with media argument
            string urlParam = CommonUtils.GetStartupParameter("-hub");
            if (urlParam != null)
            {
                Debug.LogWarning("WSJSONMessenger ws address set to " + urlParam);
                if (urlParam.StartsWith("ws://") == false)
                {
                    Debug.LogWarning("WSJSONMessenger will most likely fail, url should start with ws://");
                }
                url = urlParam;
            }

            try
            {
                webSocket = new WebSocket(url);
                webSocket.OnOpen += OnOpen;
                webSocket.OnMessage += OnMessage;
                webSocket.OnError += OnError;
                webSocket.OnClose += OnClose;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            connected = false;

        }

        public void Start()
        {
            if (autoConnect) { Connect(); }
        }


        // taken from MessageEventArgs
        private static string ConvertToString(Opcode opcode, byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public void Connect()
        {
            //Status = SocketStatus.DISCONNECTED;
            connected = true;
            socketThread = new Thread(RunSocketThread);
            socketThread.Start(webSocket);

        }

        public void Close()
        {
            connected = false;
            Status = SocketStatus.DISCONNECTED;   
        }

        public void OnDestroy()
        {
            if (socketThread != null) { socketThread.Abort(); }
            //Close();
        }

        public void OnApplicationQuit()
        {
            Close();
        }

        public override void SendString(string str)
        {
            if (webSocket != null && webSocket.IsConnected)
            {
                if (sendInBinary)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(str);
                    webSocket.SendAsync(bytes, completed => { });
                }
                else
                {
                    webSocket.SendAsync(str, completed => { });
                }
            }
        }


        private void RunSocketThread(object obj)
        {
            WebSocket webSocket = (WebSocket)obj;
            while (connected)
            {
                if (webSocket.IsConnected)
                {
                    //Debug.Log("WebsocketClientTest Thread Sleep " + reconnectDelay);
                    Thread.Sleep(reconnectDelay * 1000);
                }
                else
                {
                    //Debug.Log("WebsocketClientTest Connect Start");
                    webSocket.Connect();
                    //Debug.Log("WebsocketClientTest Connect End");
                }
            }
            //Debug.Log("WebsocketClientTest Close");
            webSocket.Close();
        }


        private void OnOpen(object sender, EventArgs e)
        {
            Debug.Log("WebsocketClientTest OnOpen");
            Status = SocketStatus.CONNECTED;
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            Debug.Log("WebsocketClientTest OnMessage " + e.Type);
            JSONObject json = JSONSocketUtils.MessageEventArgsToJSONObject(e);
            InternalEnqueueMessage(json);
        }


        private void OnError(object sender, ErrorEventArgs e)
        {
            Debug.Log("WebsocketClientTest OnError " + e.Message);
            errorMessage = e.Message;
            Status = SocketStatus.ERROR;
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            Debug.Log("WebsocketClientTest OnClose " + e.Reason);
            Status = SocketStatus.DISCONNECTED;
        }

    }
}

