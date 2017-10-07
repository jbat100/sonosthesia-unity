using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;


namespace Sonosthesia
{
    
    // ----------------------------- MESSAGE TYPES --------------------------------------

    public enum MessageType
    {
        Undefined,
        Component,
        Event,
        Action,
        Control,
        Create,
        Destroy
    }

    // ----------------------------- INFO CONTAINERS --------------------------------------

    // http://stackoverflow.com/questions/3329576/generic-constraint-to-match-numeric-types

    public class RangeInfo : JSONDecodable, JSONEncodable
    {
        public float min;
        public float max;

        public RangeInfo(float _min, float _max)
        {
            min = _max;
            max = _max;
        }

        public bool Contains(float val)
        {
            return val > min && val < max;
        }

        public JSONObject ToJSON()
        {
            JSONObject json = new JSONObject();
            json.AddField("min", min);
            json.AddField("max", max);
            return json;
        }

        public void ApplyJSON(JSONObject json)
        {
            min = JSONUtils.DecodeFloatField(json, "min");
            max = JSONUtils.DecodeFloatField(json, "max");
        }
    }

    public class BaseInfo : JSONDecodable, JSONEncodable
    {
        public string identifier;

        public virtual JSONObject ToJSON()
        {
            JSONObject json = new JSONObject();
            json.AddField("identifier", identifier);
            return json;
        }

        public virtual void ApplyJSON(JSONObject json)
        {
            identifier = JSONUtils.DecodeStringField(json, "identifier");
        }
    }


    public class ParameterInfo : BaseInfo
    {
        public RangeInfo range = new RangeInfo(0f, 1f);
        public float defaultValue = 0f;

        public override JSONObject ToJSON()
        {
            JSONObject json = base.ToJSON();
            json.AddField("range", range.ToJSON());
            json.AddField("defaultValue", defaultValue);
            return json;
        }

        public override void ApplyJSON(JSONObject json)
        {
            base.ApplyJSON(json);
            range.ApplyJSON(json.GetField("range"));
            defaultValue = JSONUtils.DecodeFloatField(json, "defaultValue");
        }
    }

    public class ChannelInfo : BaseInfo
    {
        public List<ParameterInfo> parameters = new List<ParameterInfo>();

        public override JSONObject ToJSON()
        {
            JSONObject json = base.ToJSON();
            json.AddField("parameters", JSONUtils.EncodeList(parameters));
            return json;
        }

        public override void ApplyJSON(JSONObject json)
        {
            base.ApplyJSON(json);
            parameters = JSONUtils.DecodeList<ParameterInfo>(json.GetField("parameters"));
        }
    }

    public class ComponentInfo : BaseInfo
    {
        public List<ChannelInfo> channels = new List<ChannelInfo>();

        public override JSONObject ToJSON()
        {
            JSONObject json = base.ToJSON();
            json.AddField("channels", JSONUtils.EncodeList(channels));
            return json;
        }

        public override void ApplyJSON(JSONObject json)
        {
            base.ApplyJSON(json);
            channels = JSONUtils.DecodeList<ChannelInfo>(json.GetField("channels"));
        }
    }


    // ----------------------------- MESSAGE CONTAINERS --------------------------------------

    public class Message : JSONEncodable, JSONDecodable
    {
        public static MessageType DecodeMessageType(JSONObject json)
        {
            //return (MessageType)Enum.Parse(typeof(MessageType), DecodeStringField(json, "type").ToUpper());
            return (MessageType)Enum.Parse(typeof(MessageType), JSONUtils.FirstCharToUpper(JSONUtils.DecodeStringField(json, "type")));
        }

        public static JSONObject EncodeMessageType(MessageType type, JSONObject json = null)
        {
            if (json == null) json = new JSONObject();
            json.AddField("type", type.ToString().ToLower());
            return json;
        }

        public MessageType type;

        public virtual JSONObject ToJSON()
        {
            return EncodeMessageType(type);
        }

        public virtual void ApplyJSON(JSONObject json)
        {
            type = DecodeMessageType(json);
        }
    }

    // look into using immutable versions of strings, lists and dictionaries

    public struct ChannelInstanceKey : IEquatable<ChannelInstanceKey>
    {
        public string component;
        public string channel;
        public string instance;

        public static ChannelInstanceKey Create(string component, string channel, string instance = null)
        {
            ChannelInstanceKey key;
            key.component = component;
            key.channel = channel;
            key.instance = instance;
            return key;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + component.GetHashCode();
            hash = hash * 31 + channel.GetHashCode();
            if (instance != null) hash = hash * 31 + instance.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            return other is ChannelInstanceKey ? Equals((ChannelInstanceKey)other) : false;
        }

        public bool Equals(ChannelInstanceKey other)
        {
            return component == other.component && channel == other.channel && instance == other.instance;
        }

        public override string ToString()
        {
            return GetType().Name + " (component: " + component + ", channel: " + channel + ((instance != null) ? (", instance: " + instance) : "") + ")";
        }
    }

    public struct ChannelKey : IEquatable<ChannelKey>
    {
        public string component;
        public string channel;

        public static ChannelKey Create(string component, string channel)
        {
            ChannelKey key;
            key.component = component;
            key.channel = channel;
            return key;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + component.GetHashCode();
            hash = hash * 31 + channel.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            return other is ChannelKey ? Equals((ChannelKey)other) : false;
        }

        public bool Equals(ChannelKey other)
        {
            return component == other.component && channel == other.channel;
        }

        public override string ToString()
        {
            return GetType().Name + " (component: " + component + ", channel: " + channel + ")";
        }
    }


    public struct ComponentKey : IEquatable<ComponentKey>
    {
        public string component;

        public static ComponentKey Create(string component)
        {
            ComponentKey key;
            key.component = component;
            return key;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + component.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            return other is ComponentKey ? Equals((ComponentKey)other) : false;
        }

        public bool Equals(ComponentKey other)
        {
            return component == other.component;
        }

        public override string ToString()
        {
            return GetType().Name + " (component: " + component + ")";
        }
    }

    public class ChannelMessage : Message
    {
        //------------------ public properties for performance, be responsible... ---------------------

        public ChannelInstanceKey key;

        // for now properties are not taken into account by the JSON encode/decode
        public Dictionary<string, string> properties = new Dictionary<string, string>();

        public Dictionary<string, IList<float>> parameters = new Dictionary<string, IList<float>>();

        // a key for the component and channel pair (without the instance)
        public ChannelKey ChannelKey { get { return ChannelKey.Create(key.component, key.channel); } }

        // a key for just the component (without the instance or the channel)
        public ComponentKey ComponentKey { get { return ComponentKey.Create(key.component); } }

        //------------------ Static constructors ---------------------

        public static ChannelMessage Create(MessageType type, ChannelInstanceKey key)
        {
            ChannelMessage message = new ChannelMessage();
            message.type = type;
            message.key = key;
            return message;
        }

        //-----------------------------------------------------------

        public override string ToString()
        {
            return GetType().Name + " type: " + type + " " + key + ", parameters: " + parameters.ToDebugString();
        }

        //------------------ JSON encode/decode ---------------------

        public override JSONObject ToJSON()
        {
            JSONObject json = base.ToJSON();
            json.AddField("component", key.component);
            json.AddField("channel", key.channel);
            if (key.instance != null) json.AddField("instance", key.instance);
            json.AddField("paramaters", JSONUtils.EncodeFloatsDictionary(parameters));
            return json;
        }

        public override void ApplyJSON(JSONObject json)
        {
            base.ApplyJSON(json);

            JSONObject content = json.GetField("content");

            key = ChannelInstanceKey.Create(
                JSONUtils.DecodeStringField(content, "component"),
                JSONUtils.DecodeStringField(content, "channel"),
                JSONUtils.DecodeStringField(content, "instance", false)
                );

            parameters.Clear();
            parameters = JSONUtils.DecodeFloatsDictionary(content.GetField("parameters"), parameters);
        }

        //------------------ Agglomeration ---------------------

        // push a more recent matching message on top of this one 
        public void Push(ChannelMessage message)
        {
            if (message.key.Equals(key) == false || message.type != type)
            {
                throw new Exception("message key and type should match for push");
            }

            parameters.Combine(message.parameters);
            properties.Combine(message.properties);
        }

    }

    // buffer to be used with a connection
    public class ChannelMessageBuffer
    {
        private ObjectPool<ChannelMessage> _messagePool;

        private Dictionary<ChannelInstanceKey, ChannelMessage> _createMessages = new Dictionary<ChannelInstanceKey, ChannelMessage>();
        private Dictionary<ChannelInstanceKey, ChannelMessage> _controlMessages = new Dictionary<ChannelInstanceKey, ChannelMessage>();
        private Dictionary<ChannelInstanceKey, ChannelMessage> _destroyMessages = new Dictionary<ChannelInstanceKey, ChannelMessage>();


        public ChannelMessageBuffer(ObjectPool<ChannelMessage> messagePool)
        {
            _messagePool = messagePool;
        }

        public void EnqueueMessage(ChannelMessage message)
        {
            switch (message.type)
            {
                case MessageType.Create:
                    {
                        InternalRemoveMessage(_destroyMessages, message.key);
                        InternalRemoveMessage(_controlMessages, message.key);
                        InternalPushMessage(_createMessages, message);
                    }
                    break;
                case MessageType.Control:
                    {
                        InternalPushMessage(_controlMessages, message);
                    }
                    break;
                case MessageType.Destroy:
                    {
                        InternalRemoveMessage(_createMessages, message.key);
                        InternalRemoveMessage(_controlMessages, message.key);
                        InternalPushMessage(_destroyMessages, message);
                    }
                    break;
            }
        }

        public List<ChannelMessage> DequeueMessages()
        {
            List<ChannelMessage> dequeuedList = new List<ChannelMessage>();

            dequeuedList.AddRange(_createMessages.Values);
            dequeuedList.AddRange(_controlMessages.Values);
            dequeuedList.AddRange(_destroyMessages.Values);

            _createMessages.Clear();
            _controlMessages.Clear();
            _destroyMessages.Clear();

            return dequeuedList;
        }

        private void InternalRemoveMessage(Dictionary<ChannelInstanceKey, ChannelMessage> dict, ChannelInstanceKey key)
        {
            ChannelMessage message;
            if (dict.TryGetValue(key, out message))
            {
                dict.Remove(key);
                _messagePool.Store(message);
            }
        }

        private void InternalPushMessage(Dictionary<ChannelInstanceKey, ChannelMessage> dict, ChannelMessage message)
        {
            ChannelMessage old;
            if (dict.TryGetValue(message.key, out old))
            {
                old.Push(message);
                _messagePool.Store(message);
            }
            else
            {
                dict[message.key] = message;
            }
        }
    }

    // component declaration message

    public class ComponentMessage : Message
    {
        public List<ComponentInfo> components;

        //------------------ JSON encode/decode ---------------------

        public override JSONObject ToJSON()
        {
            JSONObject json = base.ToJSON();
            json.AddField("components", JSONUtils.EncodeList(components));
            return json;
        }

        public override void ApplyJSON(JSONObject json)
        {
            base.ApplyJSON(json);

            JSONObject content = json.GetField("content");

            components = JSONUtils.DecodeList<ComponentInfo>(content.GetField("components"));
        }

        public override string ToString()
        {
            return GetType().Name;
        }

    }

} // namespace Sonosthesia


