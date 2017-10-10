﻿
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

            ws = new WebSocket(url);
            ws.OnOpen += OnOpen;
            ws.OnMessage += OnMessage;
            ws.OnError += OnError;
            ws.OnClose += OnClose;
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
            Status = SocketStatus.CONNECTING;
            connected = true;
            socketThread = new Thread(RunSocketThread);
            socketThread.Start(ws);
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
            if (wsConnected != ws.IsConnected)
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

            JSONObject json = null;

            if (e.Type == Opcode.Text)
            {
                //Debug.Log("WebsocketClientTest text " + e.Data);
                json = new JSONObject(e.Data);
            }
            else if (e.Type == Opcode.Binary)
            {
                string text = ConvertToString(e.Type, e.RawData);
                //Debug.Log("WebsocketClientTest converted binary to text " + text);
                json = new JSONObject(text);
            }


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

