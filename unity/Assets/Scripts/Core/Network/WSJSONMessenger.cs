
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

        public WebSocket socket { get { return ws; } }
        public bool IsConnected { get { return connected; } }

        private volatile bool connected;
        private volatile bool wsConnected;
        private volatile string errorMessage;

        private Thread socketThread;
        private WebSocket ws;

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
                ws = new WebSocket(url);
                ws.OnOpen += OnOpen;
                ws.OnMessage += OnMessage;
                ws.OnError += OnError;
                ws.OnClose += OnClose;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            wsConnected = false;
            connected = false;

        }

        // taken from MessageEventArgs
        private static string ConvertToString(Opcode opcode, byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public override void Connect()
        {
            if (ws != null)
            {
                Status = SocketStatus.CONNECTING;
                connected = true;
                socketThread = new Thread(RunSocketThread);
                socketThread.Start(ws);
            }
        }

        public override void Close()
        {
            Debug.Log("Closing socket");
            socket.Close();
            connected = false;
            Status = SocketStatus.DISCONNECTED;
        }

        protected override void Update()
        {
            if (ws != null && wsConnected != ws.IsConnected)
            {
                wsConnected = ws.IsConnected;
                if (wsConnected)
                {
                    Debug.Log(GetType().Name + " Connect");
                }
                else
                {
                    Debug.Log(GetType().Name + " Disconnect");
                }
            }

            base.Update();
        }

        private void OnDestroy()
        {
            if (socketThread != null) { socketThread.Abort(); }
        }

        private void OnApplicationQuit()
        {
            Close();
        }

        public override void SendString(string str)
        {
            if (ws != null && ws.IsConnected)
            {
                if (sendInBinary)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(str);
                    ws.SendAsync(bytes, completed => { });
                }
                else
                {
                    ws.SendAsync(str, completed => { });
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
                    Thread.Sleep(reconnectDelay);
                }
                else
                {
                    webSocket.Connect();
                }
            }
            webSocket.Close();
        }


        private void OnOpen(object sender, EventArgs e)
        {
            Debug.Log("WebsocketClientTest OnOpen");
            Status = SocketStatus.CONNECTED;
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            //Debug.Log("WebsocketClientTest OnMessage " + e.Type);

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

