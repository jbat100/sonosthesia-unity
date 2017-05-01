using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;
using System;
using UnityEngine.Networking;
using UnityEngine.EventSystems;


namespace Sonosthesia
{


    public class SUServerSingleton : Singleton<SUServerSingleton>
    {
        private Socket _clientSocket;
        private IPEndPoint _ipEnd;
        private Thread _dataReceived;
        private byte[] _socketOutBuffer = null;
        private byte[] _socketInBuffer = null;
        internal byte[] _cmdOutBuffer = null;
        internal byte[] _msgInBuffer = null;
        internal int _msgInBufferSize = 0;
        internal int _cmdInBufferIndex = 0;
        internal System.Object _outgoingMsgQueueLock = new System.Object(); //Lock for the access to the thread shared command queue (ex:lock(cmdQueueLock) {})
        internal Queue<SUMessage> _outgoingMsgQueue = new Queue<SUMessage>(); //Queue of the message to be sent
        internal System.Object _incomingMsgQueueLock = new System.Object();
        internal Queue<SUMessage> _incomingMsgQueue = new Queue<SUMessage>(); //Queue of the received commands to be computed
        internal byte[] _delimiter;
        internal string _delimiterString = "__tcp_json_delimiter__";
        internal int _socketBufferSize = 8192;

        public string ComponentIdentifier = "default";

        // protected guarantees this will be always a singleton only - can't use the constructor!
        protected SUServerSingleton()
        {
            _delimiter = System.Text.Encoding.UTF8.GetBytes(_delimiterString);


        }

        internal void Connect(string ipAddress, int port)
        {
            try
            {
                _ipEnd = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Set the receive buffer size to 8k
                //clientSocket.ReceiveBufferSize = socketBufferSize; //TODO tune
                _clientSocket.Connect(_ipEnd);
            }
            catch (SocketException E)
            {
                Debug.Log(E.Message);
            }

            // Start the received Thread even if the connection failed, we will try to reconnect later
            _dataReceived = new Thread(new ThreadStart(CheckData));
            _dataReceived.IsBackground = true;
            _dataReceived.Start();
        }

        private void CheckData()
        {
            while (true)
            {
                if (_clientSocket.Connected)
                {
                    // If there is so data
                    if (_clientSocket.Available > 0)
                    {
                        // We read the data
                        try
                        {
                            int size = _clientSocket.Receive(_socketInBuffer);
                            if (size > 0) //
                                BytesToCommands(size);
                            else Debug.Log("No data received");


                        }
                        catch (EndOfStreamException EdosE)
                        {
                            Debug.Log("[CheckData] EndOfStreamException error: " + EdosE.Message);
                        }
                        catch (SerializationException E)
                        {
                            Debug.Log("[CheckData] Deserialization error: " + E.Message);
                        }
                        catch (Exception Ex)
                        {
                            Debug.Log(Ex.Message);
                        }

                    }
                    else
                    {
                        // We verify that the socket is still connected
                        if (_clientSocket.Poll(1, SelectMode.SelectRead) && _clientSocket.Available == 0)
                        {
                            Debug.Log("[CheckData] Socket has been disconnected !");
                        }
                    }
                }
                else
                {
                    // If disconnected, try to reconnect
                    Debug.Log("[CheckData] Try to reconnect...");
                    try
                    {
                        _clientSocket.Close();
                        _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _clientSocket.Connect(_ipEnd);


                    }
                    catch (SocketException E)
                    {
                        Debug.Log("[CheckData] Reconnection error: " + E.Message);
                    }

                    Thread.Sleep(5000); // Try to reconnect each 5 seconds
                }
                Thread.Sleep(1); // This is usefull ! 
            }
        }

        public void Send(int size)
        {
            try
            {
                _clientSocket.Send(_cmdOutBuffer, size, SocketFlags.None);
            }
            catch (SocketException E)
            {
                Debug.Log("[Send] Error while sending: " + E.Message);
            }
        }

        internal void setSocketBufferSize(int bufferSize)
        {
            if (_socketOutBuffer != null)
            {
                _socketOutBuffer = null;
                _socketInBuffer = null;
                GC.Collect();
            }

            _socketOutBuffer = new byte[bufferSize];
            _socketInBuffer = new byte[bufferSize];

        }

        internal void setCmdBufferSize(int bufferSize)
        {
            if (_cmdOutBuffer != null)
            {
                _cmdOutBuffer = null;
                _msgInBuffer = null;
                GC.Collect();
            }

            _cmdOutBuffer = new byte[bufferSize];
            _msgInBuffer = new byte[bufferSize];
        }

        internal void BytesToCommands(int size)
        {
            //Split the buffer using the limiter because the command could have been cut
            //by the server so we must fill the command buffer first
            //Find the first delimiter, if not found copy the content to the cmd buffer
            //cmdInBufferIndex
            lock (_incomingMsgQueueLock)
            {
                //Copy the socket buffer at the end of the cmd buffer
                if (_msgInBufferSize + size >= _msgInBuffer.Length)
                {
                    Debug.Log("The size of the incoming command buffer is too small. This command will be ignored");
                    _cmdInBufferIndex = 0;
                }


                Array.Copy(_socketInBuffer, 0, _msgInBuffer, _msgInBufferSize, size);
                _msgInBufferSize += size;

                //Parse the buffer in search of the delimiter byte array
                int msgBeginIndex = 0;
                bool found1stDelimiter = false;
                for (int i = 0; i < _msgInBufferSize; i++)
                {
                    int commonBytes = 0;
                    for (int j = 0; j < _delimiter.Length; j++)
                    {
                        if (i + j < _msgInBufferSize && _msgInBuffer[i + j] == _delimiter[j]) commonBytes++;
                        else break; //Not found

                    }
                    if (commonBytes == _delimiter.Length)
                    {
                        //Found the delimiter
                        if (!found1stDelimiter)
                        {
                            if (i == 0)
                            {
                                //First delimiter, beginning of a command
                                found1stDelimiter = true;
                                msgBeginIndex = 0;
                            }
                            else
                            {
                                //first incomplete command, throw away
                            }

                        }
                        else
                        {
                            if (i - msgBeginIndex <= _delimiter.Length)
                            {
                                //empty command 
                                Debug.LogError("Empty command");
                            }
                            else
                            {
                                //We have a second delimiter and a complete command
                                //Unserialize it to know the type
                                string msgString = System.Text.Encoding.UTF8.GetString(_msgInBuffer, msgBeginIndex + _delimiter.Length, i - (msgBeginIndex + _delimiter.Length));
                                try
                                {
                                    Debug.Log("Uniscope message received" + msgString);

                                    //Get the type of the SU message
                                    SUMessage newMsg = JsonUtility.FromJson<SUMessage>(msgString);

                                    // TODO Create the new message
                                    // newMsg = CreateMessage(newMsg.path, msgString);
                                    //_incomingMsgQueue.Enqueue(newMsg);

                                }
                                catch (Exception Ex)
                                {
                                    Debug.LogError(Ex.Message + " " + msgString);

                                }
                            }
                            msgBeginIndex = i + _delimiter.Length;//Begin of the next command

                        }
                        i += _delimiter.Length;
                    }
                }
                if (!found1stDelimiter)
                {
                    //No delimiter, the command is incomplete, discard it
                    _msgInBufferSize = msgBeginIndex = 0;
                    Debug.Log("Discard Message");

                }
                else
                {
                    //Shift what is left to the beginning of the buffer
                    Array.Copy(_msgInBuffer, msgBeginIndex, _msgInBuffer, 0, _msgInBufferSize - msgBeginIndex);
                    _msgInBufferSize = _msgInBufferSize - msgBeginIndex;
                    msgBeginIndex = 0;
                }
            }
        }

        public void SendMessage(SUMessage msg)
        {
            lock (_outgoingMsgQueueLock)
            {
                _outgoingMsgQueue.Enqueue(msg);
            }
        }


    }


    public struct SUServerParameters
    {
        public string SUAddress;
        public int SUPort;
    }


    public class TCPConnection : MonoBehaviour
    {

        private SUServerSingleton server;

        public string IpAddress = "192.168.2.31";
        public int IpPort = 8000;
        public int SocketBufferSize = 1048576;
        public int CommandBufferSize = 16777216;

        void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();

            server = SUServerSingleton.Instance;
            server.setSocketBufferSize(SocketBufferSize);
            server.setCmdBufferSize(CommandBufferSize);

            Connect();
        }

        void Update()
        {

        }

        void LateUpdate()
        {
            lock (server._outgoingMsgQueueLock)
            {
                //Send the commands
                int bufferIndex = 0;
                string jsonMsg = server._delimiterString;
                if (server._outgoingMsgQueue.Count > 0)
                {
                    do
                    {
                        SUMessage msg = (SUMessage)server._outgoingMsgQueue.Dequeue();
                        jsonMsg += JsonUtility.ToJson(msg) + server._delimiterString;
                        int msgSizeInBytes = System.Text.Encoding.UTF8.GetByteCount(jsonMsg);
                        if (bufferIndex + msgSizeInBytes >= CommandBufferSize)
                        {
                            Debug.LogError("The size of the message buffer is too small. Doubling it.Lost messages in the process..");
                            server.setCmdBufferSize(CommandBufferSize * 2);
                        }
                        System.Text.Encoding.UTF8.GetBytes(jsonMsg, 0, jsonMsg.Length, server._cmdOutBuffer, bufferIndex);
                        bufferIndex += msgSizeInBytes;
                        jsonMsg = server._delimiterString;
                    }
                    while (server._outgoingMsgQueue.Count > 0);
                    if (bufferIndex > 0) server.Send(bufferIndex);
                }
            }

            //Apply the received commands by the master or the slaves
            lock (server._incomingMsgQueueLock)
            {
                //Process the received messages
                if (server._incomingMsgQueue.Count <= 0) return;
                do
                {
                    SUMessage msg = (SUMessage)server._incomingMsgQueue.Dequeue();
                    // TODO trigger message event
                }
                while (server._incomingMsgQueue.Count > 0);
            }
        }

        private void Connect()
        {
            Debug.Log("CONNECT: " + IpAddress + " : " + IpPort);
            server.Connect(IpAddress, IpPort);
        }
    }


}