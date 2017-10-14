#region License
/*
 * SocketIO.cs
 *
 * The MIT License
 *
 * Copyright (c) 2014 Fabio Panettieri
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

#endregion

//#define SOCKET_IO_DEBUG			// Uncomment this for debug
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;


public class WSMessenger : MonoBehaviour
{

    public string url = "ws://127.0.0.1:3333";
    public bool autoConnect = false;
    public int reconnectDelay = 5;

    public WebSocket socket { get { return ws; } }
    public bool IsConnected { get { return connected; } }



    private volatile bool connected;
    private volatile bool wsConnected;

    private Thread socketThread;
    private WebSocket ws;
        


    public void Awake()
    {


        ws = new WebSocket(url);
        ws.OnOpen += OnOpen;
        ws.OnMessage += OnMessage;
        ws.OnError += OnError;
        ws.OnClose += OnClose;
        wsConnected = false;

        connected = false;

    }

    public void Start()
    {
        if (autoConnect) { Connect(); }
    }

    public void Update()
    {

        if (wsConnected != ws.IsConnected)
        {
            wsConnected = ws.IsConnected;
            if (wsConnected)
            {
                Debug.Log("connect");
            }
            else
            {
                Debug.Log("disconnect");
            }
        }

    }

    public void OnDestroy()
    {
        if (socketThread != null) { socketThread.Abort(); }
    }

    public void OnApplicationQuit()
    {
        Close();
    }



    public void Connect()
    {
        connected = true;

        socketThread = new Thread(RunSocketThread);
        socketThread.Start(ws);

    }

    public void Close()
    {
        connected = false;
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
        Debug.Log("open");
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        Debug.Log("message");
    }
        

    private void OnError(object sender, ErrorEventArgs e)
    {
        Debug.Log("error");
    }

    private void OnClose(object sender, CloseEventArgs e)
    {
        Debug.Log("close");
    }
        
}
