#region License
/*
 * TestSocketIO.cs
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;

// ------------------------------------------ LINE DATA STRUCTURES ------------------------------------

// not using Line as it is likely to conflict with geometric interpretation in unity
public class TerrainLineDescription
{
    public string id;
    public string creatorId;
    public string name;
    public ThalesLineType type;
    public List<UMTCoordinate> coordinates;

    public string uid
    {
        get { return ThalesUtils.MakeUID(id, creatorId); }
    }
}

public delegate void RegisterLineEventHandler(object sender, TerrainLineDescription line);
public delegate void RemoveLineEventHandler(object sender, string uid);

public class TerrainLineManager
{
    public event RegisterLineEventHandler RegisterLineEvent;
    public event RemoveLineEventHandler RemoveLineEvent;

    // string key is uid
    private Dictionary<string, TerrainLineDescription> _descriptions = new Dictionary<string, TerrainLineDescription>();

    public void RegisterLine(TerrainLineDescription line)
    {
        if (CheckNoLine(line.uid))
        {
            _descriptions[line.uid] = line;
            Debug.Log("TerrainLineManager registered line " + line.uid);
            if (RegisterLineEvent != null) RegisterLineEvent(this, line);
        }
    }

    public void RemoveLine(string uid)
    {
        if (CheckLine(uid))
        {
            _descriptions.Remove(uid);
            Debug.Log("TerrainLineManager removed line" + uid);
            if (RemoveLineEvent != null) RemoveLineEvent(this, uid);
        }
    }

    private bool CheckLine(string uid)
    {
        if (!_descriptions.ContainsKey(uid))
        {
            Debug.LogError("TerrainLineManager line with uid " + uid + " is not registered");
            return false;
        }
        return true;
    }

    private bool CheckNoLine(string uid)
    {
        if (_descriptions.ContainsKey(uid))
        {
            Debug.LogError("TerrainLineManager line with uid " + uid + " is already registered");
            return false;
        }
        return true;
    }
}

// ----------------------------------------- ENTITY DATA STRUCTURES -----------------------------------

public class EntityDetails
{
    public string id;
    public string creatorId;
    public string name;
    public string type;
    public string master;

    public string uid
    {
        get { return ThalesUtils.MakeUID(id, creatorId); }
    }
}

public class EntityStatus
{
    public Attitude attitude;
    public UMTCoordinate position;
}

public class Entity
{
    public EntityDetails details;
    public EntityStatus status;
}

public delegate void RegisterEntityEventHandler(object sender, Entity entity);
public delegate void UpdateEntityEventHandler(object sender, Entity entity);
public delegate void RemoveEntityEventHandler(object sender, string uid);

public class EntityManager
{
    public event RegisterEntityEventHandler RegisterEntityEvent;
    public event UpdateEntityEventHandler UpdateEntityEvent;
    public event RemoveEntityEventHandler RemoveEntityEvent;

    // string key is uid
    private Dictionary<string, Entity> _descriptions = new Dictionary<string, Entity>();

    public void RegisterEntity(EntityDetails details)
    {
        if (CheckNoEntity(details.uid))
        {
            Entity entity = new Entity();
            entity.details = details;
            _descriptions[details.uid] = entity;
            Debug.Log("EntityManager registered entity " + details.uid);

            if (RegisterEntityEvent != null) RegisterEntityEvent(this, entity);
        }
    }

    public void UpdateEntityStatus(string uid, EntityStatus status)
    {
        if (CheckEntity(uid))
        {
            Entity entity = _descriptions[uid];
            entity.status = status;
            Debug.Log("EntityManager updated entity status" + uid);

            if (UpdateEntityEvent != null) UpdateEntityEvent(this, entity);
        }
    }

    public void RemoveEntity(string uid)
    {
        if (CheckEntity(uid))
        {
            _descriptions.Remove(uid);
            Debug.Log("EntityManager removed entity" + uid);

            if (RemoveEntityEvent != null) RemoveEntityEvent(this, uid);
        }
    }

    private bool CheckEntity(string uid)
    {
        if (!_descriptions.ContainsKey(uid))
        {
            Debug.LogError("EntityManager entity with uid " + uid + " is not registered");
            return false;
        }
        return true;
    }

    private bool CheckNoEntity(string uid)
    {
        if (_descriptions.ContainsKey(uid))
        {
            Debug.LogError("EntityManager entity with uid " + uid + " is already registered");
            return false;
        }
        return true;
    }
}


// ----------------------------------------- DATA PARSING -----------------------------------

// https://msdn.microsoft.com/en-us/library/87cdya3t(v=vs.110).aspx

public class JSONMessageException : Exception
{
    // is it actually useful to define these if we are not adding behaviour? get an error when not defining them...

    public JSONMessageException()
    {
    }

    public JSONMessageException(string message) : base(message)
    {
    }

    public JSONMessageException(string message, Exception inner) : base(message, inner)
    {
    }
}

enum Topic
{
    UNKNOWN,
    ENTITY_REGISTER,
    ENTITY_STATUS,
    ENTITY_REMOVE,
    LINE_REGISTER,
    LINE_REMOVE
}

class MessageParser
{
    private const string _unexpectedMessage = "unexpected message format";

    private Dictionary<string, Topic> _topicMap = new Dictionary<string, Topic>()
    {
        { "ENTITY_REGISTER", Topic.ENTITY_REGISTER },
        { "ENTITY_STATUS", Topic.ENTITY_STATUS },
        { "ENTITY_REMOVE", Topic.ENTITY_REMOVE },
        { "LINE_REGISTER", Topic.LINE_REGISTER },
        { "LINE_REMOVE", Topic.LINE_REMOVE },
    };

    private Dictionary<string, ThalesLineType> _lineTypeMap = new Dictionary<string, ThalesLineType>()
    {
        { "LIMA", ThalesLineType.LIMA },
        { "BORDER_BRIGADE", ThalesLineType.BORDER_BRIGADE },
        { "BORDER_GROUP", ThalesLineType.BORDER_GROUP },
        { "BORDER_BATTALION", ThalesLineType.BORDER_BATTALION },
        { "BORDER_COMPANY", ThalesLineType.BORDER_COMPANY },
        { "BORDER_PLATOON", ThalesLineType.BORDER_PLATOON }
    };

    public Topic ExtractTopic(JSONObject message)
    {
        if (!ObjectHasField(message, "topic"))
        {
            throw new JSONMessageException("no message topic");
        }

        string topicStr = "";
        message.GetField(ref topicStr, "topic");

        Topic topic = Topic.UNKNOWN;
        if (!_topicMap.TryGetValue(topicStr, out topic))
        {
            throw new JSONMessageException("invalid message topic : " + topicStr);
        }
        return topic;
    }

    // get the message field object
    public JSONObject ExtractContent(JSONObject message)
    {
        if (!ObjectHasField(message, "message"))
        {
            throw new JSONMessageException("no message field");
        }

        return message.GetField("message");
    }

    // content extractors

    public string ExtractUID(JSONObject content)
    {
        string ID = ExtractID(content);
        string creatorID = ExtractCreatorID(content);
        return ThalesUtils.MakeUID(ID, creatorID);
    }

    public string ExtractID(JSONObject content)
    {
        int ID = -1;
        content.GetField(ref ID, "ID");
        return ID.ToString();
    }

    public string ExtractCreatorID(JSONObject content)
    {
        int creatorID = -1;
        content.GetField(ref creatorID, "creatorID");
        return creatorID.ToString();
    }

    public TerrainLineDescription ExtractTerrainLine(JSONObject content)
    {
        TerrainLineDescription line = new TerrainLineDescription();

        line.id = ExtractID(content);
        line.creatorId = ExtractCreatorID(content);

        content.GetField(ref line.name, "name");

        string lineTypeStr = "";
        content.GetField(ref lineTypeStr, "type");

        ThalesLineType lineType = ThalesLineType.UNKNOWN;
        if (!_lineTypeMap.TryGetValue(lineTypeStr, out lineType))
        {
            Debug.LogError("MessageParser unknown line type : " + lineTypeStr);
        }

        line.type = lineType;
        line.coordinates = new List<UMTCoordinate>();

        JSONObject coordinates = content.GetField("coordinates");

        if (coordinates.type == JSONObject.Type.ARRAY)
        {
            foreach(JSONObject coordinate in coordinates.list)
            {
                line.coordinates.Add(ExtractCoordinate(coordinate));
            }
        }
        else
        {
            throw new JSONMessageException("expected coordinate array");
        }

        return line;
    }

    public EntityDetails ExtractEntityDetails(JSONObject content)
    {
        EntityDetails details = new EntityDetails();

        details.id = ExtractID(content);
        details.creatorId = ExtractCreatorID(content);
        
        content.GetField(ref details.name, "name");
        content.GetField(ref details.type, "type");
        content.GetField(ref details.master, "master");

        // TODO: check if extraction succeeded
        return details; 
    }

    public EntityStatus ExtractEntityStatus(JSONObject content)
    {
        EntityStatus status = new EntityStatus();

        status.position = ExtractCoordinate(content.GetField("position"));
        status.attitude = ExtractAttitude(content.GetField("attitude"));

        return status;
    }

    // sub content extractors

    private UMTCoordinate ExtractCoordinate(JSONObject obj)
    {
        UMTCoordinate position = new UMTCoordinate();

        obj.GetField(ref position.northing, "northing");
        obj.GetField(ref position.easting, "easting");
        obj.GetField(ref position.zone, "zoneNB");
        obj.GetField(ref position.north, "emisphereIsNorth");
        obj.GetField(ref position.altitude, "altitude");

        return position;
    }

    private Attitude ExtractAttitude(JSONObject obj)
    {
        Attitude attitude = new Attitude();

        obj.GetField(ref attitude.bearing, "bearing");
        obj.GetField(ref attitude.pitch, "pitch");
        obj.GetField(ref attitude.roll, "roll");

        return attitude;
    }

    private bool ObjectHasField(JSONObject obj, string name)
    {
        return obj.IsObject && obj.HasField("topic");
    }
}


// ------------------------------------------------------------------------------------

[RequireComponent(typeof(WSJSONMessenger))]
public class ThalesJSONResponder : MonoBehaviour
{
    public EntityManager EntityManager { get { return _entityManager; } }
    public TerrainLineManager TerrainLineManager { get { return _terrainLineManager; } }

    private EntityManager _entityManager = new EntityManager();
    private TerrainLineManager _terrainLineManager = new TerrainLineManager();

    private SocketJSONMessenger _messenger;
    private MessageParser _messageParser = new MessageParser();

    private void Awake()
    {
        _messenger = GetComponent<SocketJSONMessenger>();
    }

    private void OnEnable()
    {
        _messenger.JSONMessageEvent += OnJSONMessage;
    }

    private void OnDisable()
    {
        _messenger.JSONMessageEvent -= OnJSONMessage;
    }

    void OnJSONMessage(object sender, JSONObject json)
    {
        try
        {
            Topic topic = _messageParser.ExtractTopic(json);
            JSONObject content = _messageParser.ExtractContent(json);

            switch (topic)
            {
                // ENTITIES

                case Topic.ENTITY_REGISTER:
                    {
                        EntityDetails details = _messageParser.ExtractEntityDetails(content);
                        _entityManager.RegisterEntity(details);
                    }
                    break;

                case Topic.ENTITY_STATUS:
                    {
                        string uid = _messageParser.ExtractUID(content);
                        EntityStatus status = _messageParser.ExtractEntityStatus(content);
                        _entityManager.UpdateEntityStatus(uid, status);
                    }
                    break;

                case Topic.ENTITY_REMOVE:
                    {
                        string uid = _messageParser.ExtractUID(content);
                        _entityManager.RemoveEntity(uid);
                    }
                    break;

                // LINES

                case Topic.LINE_REGISTER:
                    {
                        TerrainLineDescription line = _messageParser.ExtractTerrainLine(content);
                        _terrainLineManager.RegisterLine(line);
                    }
                    break;

                case Topic.LINE_REMOVE:
                    {
                        string uid = _messageParser.ExtractUID(content);
                        _terrainLineManager.RemoveLine(uid);
                    }
                    break;

                default:
                    {

                    }
                    break;
            }
        }
        catch (JSONMessageException exception)
        {
            Debug.LogError("JSONMessageException : " + exception.Message);
        }
    }

}
