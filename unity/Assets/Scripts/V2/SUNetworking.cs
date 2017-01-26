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

//Should be abstract but is instantiated by Unity FromJson only to know the type when deserializing
public class SUMessage
{
    public string path;

    public SUMessage(string path)
    {
        this.path = path;
    }

    public virtual void Process() { }
    public virtual SUMessage CreateFromJson(string cmdString) { return null; }
}

[Serializable]
public class SUStatusRequestMessage : SUMessage
{
    public SUStatusRequestMessage() : base("application/identification/request") { }
    public override SUMessage CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUStatusRequestMessage>(cmdString);
    }
    public override void Process()
    {
        SUStatusProcessMsg msg = new SUStatusProcessMsg();
        SUUServerSingleton.Instance._outgoingMsgQueue.Enqueue(msg);
    }
}

[Serializable]
public class SUStatusProcessMsg : SUMessage
{
    [Serializable]
    public struct Data
    {
        public int pid;
        public string identifier;
    }

    public Data content;
    public SUStatusProcessMsg() : base(SUUServerSingleton.IdMsgId) {
        SUUServerSingleton server = SUUServerSingleton.Instance;
        content.pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        content.identifier = server.DigiscapeIdentifier;
    }

    public override SUMessage CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUStatusProcessMsg>(cmdString);
    }

    public override void Process()
    {
    }
}


[Serializable]
public class SUControlCommandMsg : SUMessage
{
    [Serializable]
    public struct Data
    {
        public string application;
        public string path;
        public bool mirror;
        public string command;
    }

    public Data content;
    public SUControlCommandMsg() : base(SUUServerSingleton.CmdMsgId) { }


    public SUControlCommandMsg(SUUCommand command, string path) : base(SUUServerSingleton.CmdMsgId)
    {
        //Content.Application = str.Application;
        content.command = JsonUtility.ToJson(command);
        content.path = path;
    }

    public override SUMessage CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUControlCommandMsg>(cmdString);
    }

    public override void Process()
    {
        try
        {
            //if (content.mirror)
            //{

                SUUServerSingleton server = SUUServerSingleton.Instance;

                //Get the type of the command
                SUUCommand newCmd = JsonUtility.FromJson<SUUCommand>(content.command);

                //Create the command 
                newCmd = server.CreateCommand(newCmd.Type, content.command);

                newCmd.Apply();
            //}
        }
        catch (Exception Ex)
        {
            Debug.LogError("Process " + Ex.Message + " " + content.command);
        }
    }
}


public class SUUCommand
{
    public string Type; //WARNING non public member are not serialized by JsonUtility.FromJson!

    public SUUCommand (string type)
    {
        this.Type = type;
    }

    public virtual void Apply() { }
    public virtual SUUCommand CreateFromJson(string cmdString) { return null; }

}



[Serializable]
public class SUUMoveCommand : SUUCommand
{
    [Serializable]
    public struct MoveStruct
    {
        public string NetId;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public MoveStruct[] Args;
    public SUUMoveCommand() : base(SUUServerSingleton.MoveCmdId) {} //Used to get the command type when unserializing 
    public SUUMoveCommand(MoveStruct[] args) : base(SUUServerSingleton.MoveCmdId) //Used to construct the command 
    {
        this.Args = args;
    }

    public override SUUCommand CreateFromJson(string cmdString) //Used to unserialize once the type is known
    {
       return JsonUtility.FromJson<SUUMoveCommand>(cmdString);
    }

    public override void Apply()
    {
        foreach (SUUMoveCommand.MoveStruct str in Args)
        {
            GameObject currentObject;
            if (SUUServerSingleton.Instance._netIdDict.TryGetValue(str.NetId, out currentObject))
            {
                Transform tf = currentObject.GetComponent<Transform>();
                tf.localPosition = str.Position;
                tf.rotation = str.Rotation;
                tf.localScale = str.Scale;
            }
            else
            {
                //DebugConsole.Log("NO Game object with net Id "+ str.NetId);
            }
        }
    }
}

[Serializable]
public class SUUSpawnCommand : SUUCommand
{
    public string PrefabName;
    public string Name;
    public string NetId;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public SUUServerSingleton.ClusterSpawnMode Mode;
    public SUUSpawnCommand() : base(SUUServerSingleton.SpawnCmdId) { }
    public SUUSpawnCommand(string prefabName, string name, string netId, Transform transform, SUUServerSingleton.ClusterSpawnMode mode) : base(SUUServerSingleton.SpawnCmdId)
    {
        this.PrefabName = prefabName;
        this.Name = name;
        this.NetId = netId;
        this.Position = transform.position;
        this.Rotation = transform.rotation;
        this.Scale = transform.localScale;
        this.Mode = mode;
    }

    public override SUUCommand CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUUSpawnCommand>(cmdString);
    }

    public override void Apply()
    {
        Debug.Log("SpawnCommand Apply " + Name+ " netId "+NetId);
        SUUServerSingleton.Instance.LocalSpawn(PrefabName, Name, NetId, Position, Rotation, Scale,Mode);
    }
}



[Serializable]
public class SUUDestroyCommand : SUUCommand
{
    public string NetId;

    public SUUDestroyCommand() : base(SUUServerSingleton.DestroyCmdId) { }
    public SUUDestroyCommand(string netId) : base(SUUServerSingleton.DestroyCmdId)
    {
        this.NetId = netId;
    }

    public override SUUCommand CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUUDestroyCommand>(cmdString);
    }

    public override void Apply()
    {
        GameObject currentObject;
        if (SUUServerSingleton.Instance._netIdDict.TryGetValue(NetId, out currentObject))
        {
            GameObject.Destroy(currentObject.gameObject);
        }
    }
}


[Serializable]
public class SUUResynchCommand : SUUCommand
{
    [Serializable]
    public struct ResynchStruct
    {
        public string PrefabName;
        public string Name;
        public string NetId;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public SUUServerSingleton.ClusterSpawnMode Mode; //TODO Handle this
    }
    public ResynchStruct[] Args;

    public SUUResynchCommand() : base(SUUServerSingleton.ResynchCmdId) { }

    public SUUResynchCommand(ResynchStruct[] args) : base(SUUServerSingleton.ResynchCmdId)
    {
        this.Args = args;
    }

    public override SUUCommand CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUUResynchCommand>(cmdString);
    }

    public override void Apply()
    {
        SUUServerSingleton.Instance.LocalDestroyAll();

        foreach (ResynchStruct str in Args)
        {
            SUUServerSingleton.Instance.LocalSpawn(str.PrefabName, str.Name, str.NetId, str.Position, str.Rotation, str.Scale, str.Mode);
        }
    }
}


[Serializable]
public class SUUClearCommand : SUUCommand
{
    public SUUClearCommand() : base(SUUServerSingleton.ClearCmdId) { }

    public override SUUCommand CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUUClearCommand>(cmdString);
    }

    public override void Apply()
    {
        SUUServerSingleton.Instance.LocalDestroyAll();
    }
}

[Serializable]
public class SUUTranslationRequestCommand : SUUCommand
{

    public string NetId;
    public Vector3 Translation;
    public SUUTranslationRequestCommand() : base(SUUServerSingleton.TranslationRequestCmdId) { }
    public SUUTranslationRequestCommand(string netId, Vector3 translation) : base(SUUServerSingleton.TranslationRequestCmdId)
    {
        this.NetId = netId;
        this.Translation = translation;
    }

    public override SUUCommand CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUUTranslationRequestCommand>(cmdString);
    }

    public override void Apply()
    {
        //Get object using netID
        GameObject localObject = SUUServerSingleton.Instance.getMasterObjNetId(NetId); 
        if (localObject)
        {
            if (SUUServerSingleton.Instance.IsSUUMaster)
                localObject.transform.Translate(Translation);
        }
        else
        {
            //DebugConsole.Log("Cannot find object from NetId " + NetId);
        }
    }
}


[Serializable]
public class SUUPositionRequestCommand : SUUCommand
{

    public string NetId;
    public Vector3 Position;
    public SUUPositionRequestCommand() : base(SUUServerSingleton.PositionRequestCmdId) { }
    public SUUPositionRequestCommand(string netId, Vector3 position) : base(SUUServerSingleton.PositionRequestCmdId)
    {
        this.NetId = netId;
        this.Position = position;
    }

    public override SUUCommand CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUUPositionRequestCommand>(cmdString);
    }

    public override void Apply()
    {
        //Get object using netID
        GameObject localObject = SUUServerSingleton.Instance.getMasterObjNetId(NetId);
        if (SUUServerSingleton.Instance.IsSUUMaster)
            localObject.transform.localPosition = Position;
    }
}


[Serializable]
public class SUURotationRequestCommand : SUUCommand
{

    public string NetId;
    public Quaternion Rotation;
    public SUURotationRequestCommand() : base(SUUServerSingleton.RotationRequestCmdId) { }
    public SUURotationRequestCommand(string netId, Quaternion rotation) : base(SUUServerSingleton.RotationRequestCmdId)
    {
        this.NetId = netId;
        this.Rotation = rotation;
    }

    public override SUUCommand CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUURotationRequestCommand>(cmdString);
    }

    public override void Apply()
    {
        //Get object using netID
        GameObject localObject = SUUServerSingleton.Instance.getMasterObjNetId(NetId);
        if (SUUServerSingleton.Instance.IsSUUMaster)
            localObject.transform.localRotation = Rotation;
    }
}


[Serializable]
public class SUUEulerRequestCommand : SUUCommand
{

    public string NetId;
    public Vector3 Rotation;
    public SUUEulerRequestCommand() : base(SUUServerSingleton.EulerRequestCmdId) { }
    public SUUEulerRequestCommand(string netId, Vector3 rotation) : base(SUUServerSingleton.EulerRequestCmdId)
    {
        this.NetId = netId;
        this.Rotation = rotation;
    }

    public override SUUCommand CreateFromJson(string cmdString)
    {
        return JsonUtility.FromJson<SUUEulerRequestCommand>(cmdString);
    }

    public override void Apply()
    {
        //Get object using netID
        GameObject localObject = SUUServerSingleton.Instance.getMasterObjNetId(NetId);
        if (SUUServerSingleton.Instance.IsSUUMaster)
            localObject.transform.eulerAngles = Rotation;
    }
}

public class SUUServerSingleton : Singleton<SUUServerSingleton>
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
    internal System.Object _ingoingMsgQueueLock = new System.Object(); 
    internal Queue<SUMessage> _ingoingMsgQueue = new Queue<SUMessage>(); //Queue of the received commands to be computed
    internal byte[] _delimiter;
    internal string _delimiterString = "__tcp_json_delimiter__";
    internal int _socketBufferSize = 8192;
    internal Dictionary<string, GameObject> _netIdDict;
    internal Dictionary<string, Type> _factoryDict = new Dictionary<string, Type>();
    internal List<SUUObject> _objects = new List<SUUObject>();
    internal Queue<SUUCommand> _internalCommands = new Queue<SUUCommand>();
    internal System.Object _internalCommandsQueueLock = new System.Object(); //Lock for the access to the thread shared command queue (ex:lock(cmdQueueLock) {})


    public bool IsSUUMaster; //Is this instance the master app ?
    public string DigiscapeIdentifier="default";

    public static string SpawnCmdId = "SPWN";
    public static string MoveCmdId = "MOVE";
    public static string ClearCmdId = "CLEA";
    public static string DestroyCmdId = "DEST";
    public static string ResynchCmdId = "RSYN";
    public static string TranslationRequestCmdId = "TRAR";
    public static string PositionRequestCmdId = "POSR";
    public static string RotationRequestCmdId = "ROTR";
    public static string EulerRequestCmdId = "EULR";

    public static string IdMsgId = "application/identification";
    public static string CmdMsgId = "application/control/cmd";

    protected SUUServerSingleton()
    {
        _delimiter = System.Text.Encoding.UTF8.GetBytes(_delimiterString);
        _netIdDict = new Dictionary<string, GameObject>();
        //Register basic commands
        this.RegisterCommand<SUUSpawnCommand>(SpawnCmdId);
        this.RegisterCommand<SUUMoveCommand>(MoveCmdId);
        this.RegisterCommand<SUUClearCommand>(ClearCmdId);
        this.RegisterCommand<SUUDestroyCommand>(DestroyCmdId);
        this.RegisterCommand<SUUResynchCommand>(ResynchCmdId);
        this.RegisterCommand<SUUTranslationRequestCommand>(TranslationRequestCmdId);




        //Register basic SU message
        this.RegisterMessage<SUControlCommandMsg>(CmdMsgId);
        this.RegisterMessage<SUStatusProcessMsg>(IdMsgId);



    } // guarantee this will be always a singleton only - can't use the constructor!

    //Factory command constructor
    public SUUCommand CreateCommand(string id, string json)
    {
        Type type = null;
        if (_factoryDict.TryGetValue(id, out type))
        {
            SUUCommand cmd = (SUUCommand)Activator.CreateInstance(type);
            return cmd.CreateFromJson(json);

        }

        throw new ArgumentException("No Command registered for this id");
    }

    //Factory Message constructor
    public SUMessage CreateMessage(string id, string json)
    {
        Type type = null;
        if (_factoryDict.TryGetValue(id, out type))
        {
            SUMessage msg = (SUMessage)Activator.CreateInstance(type);
            return msg.CreateFromJson(json);
        }

        throw new ArgumentException("No Message registered for this id");
    }



    //Factory 
    public void RegisterCommand<Tderived>(string id) where Tderived : SUUCommand
    {
        var type = typeof(Tderived);

        if (type.IsInterface || type.IsAbstract)
            throw new ArgumentException("Cannnot register interfaces nor abstract classes.");

        _factoryDict[id] =  type;
    }

    public void RegisterMessage<Tderived>(string id) where Tderived : SUMessage
    {
        var type = typeof(Tderived);

        if (type.IsInterface || type.IsAbstract)
            throw new ArgumentException("Cannnot register interfaces nor abstract classes.");

        _factoryDict[id] = type;
    }

    internal void AddObject(SUUObject newObject)
    {
        _objects.Add(newObject);
    }

    internal void RemoveObject(SUUObject oldObject)
    {
        _objects.Remove(oldObject);

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

            //Send SUStatusProcessMessage
            SUStatusProcessMsg msg = new SUStatusProcessMsg();
            _outgoingMsgQueue.Enqueue(msg);


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
            _clientSocket.Send(_cmdOutBuffer,size, SocketFlags.None);
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
        lock (_ingoingMsgQueueLock)
        {
            //Copy the socket buffer at the end of the cmd buffer
            if (_msgInBufferSize+ size >= _msgInBuffer.Length)
            {
                Debug.Log("The size of the ingoing command buffer is too small. This command will be ignored");
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
                        if (i-msgBeginIndex<=_delimiter.Length)
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

                                //Create the new message
                                newMsg = CreateMessage(newMsg.path, msgString);

                                _ingoingMsgQueue.Enqueue(newMsg);

                            }
                            catch (Exception Ex)
                            {
                                Debug.LogError(Ex.Message + " " + msgString);

                            }
                        }
                        msgBeginIndex = i+ _delimiter.Length;//Begin of the next command

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

    //Apply directly on the unity thread if called on the uniscope master, send otherwise
    public void SendOrApplyCommand(SUUCommand cmd)
    {
        if (IsSUUMaster)
        {
            lock(_internalCommandsQueueLock)
            {
                _internalCommands.Enqueue(cmd);
            }            
        }
        else
            SendCommand(cmd);
    }

    public void SendCommand(SUUCommand cmd)
    {
        SendMessage(new SUControlCommandMsg(cmd, "/default"));
    }

    public enum UniscopeSpawnMode { MASTERONLY, SERVERONLY,EVERYWHERE};
    public enum ClusterSpawnMode { MASTERONLY, EVERYWHERE };

    public void Spawn(GameObject prefab, string name, Transform tf, UniscopeSpawnMode uniscopeSpawnMode, ClusterSpawnMode clusterSpawnMode)
    {
        DebugConsole.Log("Spawning " + name);


        GameObject gameObject = null;
        if (uniscopeSpawnMode != UniscopeSpawnMode.MASTERONLY || IsSUUMaster)
        {
            //Spawn in the local scene
            gameObject = (GameObject)Instantiate(prefab, tf.position, tf.rotation);
            if (name.Length > 0)
                gameObject.name = name;
            if (clusterSpawnMode == ClusterSpawnMode.EVERYWHERE)
            {
                //Spawn in LAN
                NetworkServer.Spawn(gameObject);
            }

        }
        if (uniscopeSpawnMode == UniscopeSpawnMode.EVERYWHERE || (uniscopeSpawnMode == UniscopeSpawnMode.EVERYWHERE && IsSUUMaster))
        {
            //If the object has been created localy, keep its prefab name
            string netId = "";
            if (gameObject)
            {
                gameObject.GetComponent<SUUObject>().setPrefabName(prefab.name);
                netId = gameObject.GetComponent<NetworkIdentity>().netId.ToString();
            }
            //Send the SPAWN command to the remote SUUServer
            SUUSpawnCommand cmd = new SUUSpawnCommand(prefab.name, name,netId, tf,clusterSpawnMode);
            SendCommand(cmd);
        }
    }

    internal void LocalSpawn(string prefabName, string name, string netId, Transform tf, SUUServerSingleton.ClusterSpawnMode mode)
    {
        LocalSpawn(prefabName, name, netId, tf.position, tf.rotation, tf.localScale, mode);
    }

    internal void LocalSpawn(string prefabName, string name, string netId, Vector3 position, Quaternion rotation, Vector3 scale, SUUServerSingleton.ClusterSpawnMode mode)
    {
        DebugConsole.Log("LocalSpawn " + prefabName + " " + name + " "+netId);

        //Get Prefab
        GameObject preFab = null;

        foreach (GameObject currentPreFab in NetworkManager.singleton.spawnPrefabs)
        {
            if (currentPreFab.name.Equals(prefabName))
            {
                preFab = currentPreFab;
                break;
            }
        }

        if (preFab != null)
        {
            //local spawn
            var gameObject = (GameObject)GameObject.Instantiate(preFab, position, rotation);
            if (name.Length > 0)
                gameObject.name = name;

            if (mode == SUUServerSingleton.ClusterSpawnMode.EVERYWHERE)
            {
                //LAN Spawn
                Rigidbody rigidBody = gameObject.GetComponent<Rigidbody>();
                if (rigidBody) rigidBody.isKinematic = true;

                NetworkServer.Spawn(gameObject);

                //Keep prefabName for resync
                gameObject.GetComponent<SUUObject>().setPrefabName(prefabName);
                //Add an entry in the netId table
                SUUServerSingleton.Instance._netIdDict[netId] = gameObject;
            }
            else
            {
                //TODO keep track of local only object created remotly
            }
        }
        else
        {
            Debug.LogError("Can't find preFab " + prefabName + ". Unable to spawn GameObject");
        }
    }

    public void RemoteDestroyAll()
    {
        if (IsSUUMaster)
        { 
            //Send the CLEAR command
            SUUClearCommand cmd = new SUUClearCommand();
            SendCommand(cmd);
            //And destroy local objects
            LocalDestroyAll();
        }
    }

    internal void LocalDestroyAll()
    {
        foreach (SUUObject currentObject in _objects)
        {
            NetworkServer.Destroy(currentObject.gameObject); //Destroy - local client
            Destroy(currentObject.gameObject); //Destroy - current scene
        }
    }


    public void TranslateRequest (GameObject gameObject, Vector3 translation)
    {
        if (IsSUUMaster)
            gameObject.transform.Translate(translation);
        else
        {
            //Parse the netId dict to find the netId corresponding to the local gameobject

            foreach (KeyValuePair<string, GameObject> entry in _netIdDict)
            {
                // do something with entry.Value or entry.Key
                if (entry.Value ==gameObject)
                {
                    //Send the TRAR command
                    SUUTranslationRequestCommand cmd = new SUUTranslationRequestCommand(entry.Key, translation);
                    SendCommand(cmd);
                    return;
                }
            }

            DebugConsole.Log("TranslateRequest: Cannot find netId for " + gameObject.name);
        }
    }


    public void SetPositionRequest(GameObject gameObject, Vector3 position)
    {
        if (IsSUUMaster)
            gameObject.transform.localPosition = position;
        else
        {
            //Send the POSR command
            SUUPositionRequestCommand cmd = new SUUPositionRequestCommand(gameObject.GetComponent<NetworkIdentity>().netId.ToString(), position);
            SendCommand(cmd);
        }
    }


    public void SetRotationRequest(GameObject gameObject, Quaternion rotation)
    {
        if (IsSUUMaster)
            gameObject.transform.localRotation = rotation;
        else
        {
            //Send the POSR command
            SUURotationRequestCommand cmd = new SUURotationRequestCommand(gameObject.GetComponent<NetworkIdentity>().netId.ToString(), rotation);
            SendCommand(cmd);
        }
    }

    public void SetEulerRequest(GameObject gameObject, Vector3 rotation)
    {
        if (IsSUUMaster)
            gameObject.transform.eulerAngles = rotation;
        else
        {
            //Send the POSR command
            SUUEulerRequestCommand cmd = new SUUEulerRequestCommand(gameObject.GetComponent<NetworkIdentity>().netId.ToString(), rotation);
            SendCommand(cmd);
        }
    }


    public GameObject getMasterObjNetId(string clientObjectNetId)
    {
        //Get the netId of the object in the master app scene
        //TO DO: keep a dict for faster lookup ? 
        foreach (SUUObject currentGameObject in _objects)
        {
            if (currentGameObject.GetComponent<NetworkIdentity>().netId.ToString().Equals(clientObjectNetId))
                return currentGameObject.gameObject;
        }

        return null;
    }


    public void RemoteResynch()
    {
        if (IsSUUMaster)
        {
            //Send the RESYNCH command for each SUUObject
            SUUResynchCommand.ResynchStruct[] structs = new SUUResynchCommand.ResynchStruct[_objects.Count];

            int i = 0;
            foreach (SUUObject currentObject in _objects)
            {
                Transform tf = currentObject.GetComponent<Transform>();
                SUUResynchCommand.ResynchStruct str = new SUUResynchCommand.ResynchStruct();
                str.NetId = currentObject.GetComponent<NetworkIdentity>().netId.ToString();
                str.Position = tf.position;
                str.Rotation = tf.rotation;
                str.Scale = tf.localScale;
                str.PrefabName = currentObject.GetComponent<SUUObject>().getPrefabName();
                str.Name = currentObject.name;
                structs[i] = str;
                i++;
            }

            SUUResynchCommand cmd = new SUUResynchCommand(structs);
            SendCommand(cmd);
        }
    }

}


public struct SUUServerParameters
{
    public bool isApp;
    public bool isMasterApp;
    public string SUAddress;
    public int SUPort;
    public string identifier;
}


public class SUUServer : MonoBehaviour {

    private SUUServerSingleton server;

    public string IpAddress="192.168.2.31";
    public int IpPort=8000;
    public int SocketBufferSize= 1048576;
    public int CommandBufferSize = 16777216;
    public bool IsSUUMaster = true;
    public float SynchroIntervalInSecond = 0.0666666666666667f;

    private float _timeElaspedSinceLastSynchro=0.0f;

    void Awake ()
    {
        string[] args = Environment.GetCommandLineArgs();

        bool useDigiscape = true;

        string DigiscapeIdentifier="";

        if (args.Length >= 2 && !Application.isEditor)
        {
            SUUServerParameters parameters;
            try
            {
                string jsonText = System.IO.File.ReadAllText(args[1]);


                parameters = JsonUtility.FromJson<SUUServerParameters>(jsonText);
                DigiscapeIdentifier = parameters.identifier;
                if (parameters.isMasterApp && parameters.isApp)
                {
                    useDigiscape = true;
                    IsSUUMaster = true;
                    DebugConsole.Log("SERVER");
                    DebugConsole.Log("SU Server IP: " + parameters.SUAddress);
                    DebugConsole.Log("SU Server Port: " + parameters.SUPort);
                }
                else if (!parameters.isMasterApp && parameters.isApp)
                {
                    useDigiscape = true;
                    IsSUUMaster = false;
                    DebugConsole.Log("LOCALSERVER");
                    DebugConsole.Log("SU Server IP: " + parameters.SUAddress);
                    DebugConsole.Log("SU Server Port: " + parameters.SUPort);
                }
                else
                {
                    useDigiscape = false;
                    IsSUUMaster = false;
                    DebugConsole.Log("CLIENT");
                }

                IpAddress = parameters.SUAddress;
                IpPort = parameters.SUPort;

            }
            catch (Exception E)
            {
                DebugConsole.Log("first parameter must be a valid json file path");
                DebugConsole.Log(E.Message);
            }

        }

        if (useDigiscape || Application.isEditor)
        {
            server = SUUServerSingleton.Instance;
            server.IsSUUMaster = IsSUUMaster;
            server.setSocketBufferSize(SocketBufferSize);
            server.setCmdBufferSize(CommandBufferSize);
            server.DigiscapeIdentifier = DigiscapeIdentifier;


            //Connect to the Digiscape Local server
            Connect();

        }
        else
            this.enabled = false;


    }

    void Update ()
    {

    }




    void LateUpdate()
    {
        _timeElaspedSinceLastSynchro += Time.deltaTime;
        lock (server._outgoingMsgQueueLock)
        { 
            //TODO add a network "frame" rate
            if (server.IsSUUMaster)
            {
                if (true)
//            if (timeElaspedSinceLastSynchro > synchroIntervalInSecond)
                {
                    _timeElaspedSinceLastSynchro = _timeElaspedSinceLastSynchro % SynchroIntervalInSecond;
                    //Update DigiObjects position
                    //Can't convert ArrayList to strongly typed array (needed for JSon seralization)
                    //So we need the number of object
                    int changedObjectNumber = 0;
                    foreach (SUUObject currentObject in server._objects)
                    {
                        Transform tf = currentObject.GetComponent<Transform>();
                        if (tf.hasChanged)
                        {
                            changedObjectNumber++;
                        }
                    }

                    if (changedObjectNumber > 0)
                    {
                        SUUMoveCommand.MoveStruct[] structs = new SUUMoveCommand.MoveStruct[changedObjectNumber];
                        int changedObjectIndex = 0;
                        foreach (SUUObject currentObject in server._objects)
                        {
                            Transform tf = currentObject.GetComponent<Transform>();
                            if (tf.hasChanged)
                            {
                                SUUMoveCommand.MoveStruct str = new SUUMoveCommand.MoveStruct();
                                str.NetId = currentObject.GetComponent<NetworkIdentity>().netId.ToString();
                                str.Position = tf.localPosition;
                                str.Rotation = tf.localRotation;
                                str.Scale = tf.localScale;

                                structs[changedObjectIndex] = str;
                                changedObjectIndex++;
                            }
                        }

                        SUUMoveCommand moveCmd = new SUUMoveCommand(structs);
                        server._outgoingMsgQueue.Enqueue(new SUControlCommandMsg(moveCmd, "/default"));
                    }
                }
            }
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


        //Process the local commands
        lock (server._internalCommandsQueueLock)
        {
            if (server._internalCommands.Count > 0)
            {
                do
                {
                    SUUCommand cmd = (SUUCommand)server._internalCommands.Dequeue();
                    cmd.Apply();

                }
                while (server._internalCommands.Count > 0);
            }
        }






        //Apply the received commands by the master or the slaves
        lock (server._ingoingMsgQueueLock)
        {
            //Process the received messages
            if (server._ingoingMsgQueue.Count <= 0) return;
            do
            {
                SUMessage msg = (SUMessage)server._ingoingMsgQueue.Dequeue();
                msg.Process();
            }
            while (server._ingoingMsgQueue.Count > 0);
        }
    }

    private void Connect()
    {
        Debug.Log("CONNECT: "+IpAddress+" : "+IpPort);

        server.Connect(IpAddress, IpPort);
    }
}
