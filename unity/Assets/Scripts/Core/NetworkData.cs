using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;


namespace Sonosthesia
{

    // ----------------------------- UTILITIES --------------------------------------

    // https://docs.microsoft.com/en-us/dotnet/articles/csharp/language-reference/keywords/new-constraint
    // this is a very minimalist implementation, waiting for a good library to come out... 
    public class ObjectPool<T> where T : new()
    {
        private Stack<T> _stack = new Stack<T>();

        public T Fetch()
        {
            if (_stack.Count == 0)
                return new T();
            return _stack.Pop();
        }

        public void Store(T t)
        {
            _stack.Push(t);
        }
    }

    // https://msdn.microsoft.com/en-us/library/87cdya3t(v=vs.110).aspx

    public class JSONDecodeException : Exception
    {
        public JSONDecodeException() { }

        public JSONDecodeException(string message) : base(message) { }

        public JSONDecodeException(string message, Exception inner) : base(message, inner) { }
    }

    // ----------------------------- INTERFACES --------------------------------------

    public interface JSONEncodable
    {
        JSONObject ToJSON();
    }

    public interface JSONDecodable
    {
        void ApplyJSON(JSONObject json);
    }

    public class JSONUtils
    {
        public static JSONObject EncodeMessageType(MessageType type, JSONObject json = null)
        {
            if (json == null) json = new JSONObject();
            json.AddField("type", type.ToString().ToLower());
            return json;
        }

        public static MessageType DecodeMessageType(JSONObject json)
        {
            return (MessageType)Enum.Parse(typeof(MessageType), DecodeStringField(json, "type"));
        }

        public static JSONObject EncodeStringDictionary(Dictionary<string, string> dict)
        {
            // JSONObject has a handy little constructor
            return JSONObject.Create(dict);
        }

        public static JSONObject EncodeFloatArrayDictionary(Dictionary<string, float[]> dict)
        {
            JSONObject json = new JSONObject(JSONObject.Type.OBJECT);
            json.keys = new List<string>();
            json.list = new List<JSONObject>();
            //Not sure if it's worth removing the foreach here
            foreach (KeyValuePair<string, float[]> kvp in dict)
            {
                json.keys.Add(kvp.Key);
                json.list.Add(EncodeFloatList(kvp.Value.ToList()));
            }
            return json;
        }

        public static Dictionary<string, float[]> DecodeFloatArrayDictionary(JSONObject json, Dictionary<string, float[]> dict = null)
        {
            if (dict == null) dict = new Dictionary<string, float[]>();
            if (json != null && json.type == JSONObject.Type.OBJECT)
            {
                for(int i = 0; i > json.Count; i++)
                {
                    dict[json.keys[i]] = DecodeFloatList(json.list[i]).ToArray();
                }
            }
            return dict;
        }

        public static string DecodeStringField(JSONObject obj, string key, bool required = true)
        {
            string s = "";
            if (required && !obj.HasField(key))
            {
                throw new JSONDecodeException("expected string field: " + key);
            }
            obj.GetField(ref s, key);
            return s;
        }

        public static float DecodeFloatField(JSONObject obj, string key, bool required = true)
        {
            float f = 0f;
            if (required && !obj.HasField(key))
            {
                throw new JSONDecodeException("expected numeric field: " + key);
            }
            obj.GetField(ref f, key);
            return f;
        }

        public static JSONObject EncodeList<T> (List<T> objs) where T : JSONEncodable
        {
            JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
            json.list = (List<JSONObject>)objs.Select(obj => { return obj.ToJSON(); });
            return json;
        } 

        public static List<T> DecodeList<T> (JSONObject json) where T : JSONDecodable, new()
        {
            if (json.type == JSONObject.Type.ARRAY && json.list != null)
            {
                return (List<T>)json.list.Select(obj =>
                {
                    T t = new T();
                    t.ApplyJSON(obj);
                    return t;
                });
            }
            else
            {
                throw new JSONDecodeException("expected ARRAY type JSONObject with list property");
            }
        }

        public static JSONObject EncodeFloatList(List<float> numbers)
        {
            JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
            json.list = (List<JSONObject>)numbers.Select(number => { return JSONObject.Create(number); });
            return json;
        }

        public static List<float> DecodeFloatList(JSONObject json)
        {
            if (json.type == JSONObject.Type.ARRAY && json.list != null)
            {
                return (List<float>)json.list.Select(obj =>
                {
                    if (obj.type != JSONObject.Type.NUMBER)
                    {
                        throw new JSONDecodeException("expected NUMBER type JSONObject");
                    }
                    return obj.n;
                });
            }
            // be robust to parameters being expressed as single number rather than number array
            else if (json.type == JSONObject.Type.NUMBER)
            {
                return new List<float>() { json.n };
            }
            else
            {
                throw new JSONDecodeException("expected ARRAY type JSONObject with list property");
            }
        }
    }
    

    // ----------------------------- MESSAGE TYPES --------------------------------------

    public enum MessageType
    {
        Undefined,
        Component,
        Event,
        Control,
        Create,
        Destroy
    }

    // ----------------------------- INFO CONTAINERS --------------------------------------

    // http://stackoverflow.com/questions/3329576/generic-constraint-to-match-numeric-types

    public class Range : JSONDecodable, JSONEncodable 
    {
        public float min;
        public float max;

        public Range(float _min, float _max)
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
        public Range range = new Range(0f, 1f);
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
        public MessageType type;

        public virtual JSONObject ToJSON()
        {
            return JSONUtils.EncodeMessageType(type);
        }

        public virtual void ApplyJSON(JSONObject json)
        {
            type = JSONUtils.DecodeMessageType(json);
        }
    }

    // look into using immutable versions of strings, lists and dictionaries

    public struct ChannelMessageKey : IEquatable<ChannelMessageKey>
    {
        public string component;
        public string channel;
        public string instance;

        public static ChannelMessageKey Create(string component, string channel, string instance = null)
        {
            ChannelMessageKey key;
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
            return other is ChannelMessageKey ? Equals((ChannelMessageKey)other) : false;
        }

        public bool Equals(ChannelMessageKey other)
        {
            return component == other.component && channel == other.channel && instance == other.instance;
        }
    }

    public class ChannelMessage : Message
    {
        //------------------ public properties for performance, be responsible... ---------------------

        public ChannelMessageKey key;

        // for now properties are not taken into account by the JSON encode/decode
        public Dictionary<string, string> properties = new Dictionary<string, string>();

        public Dictionary<string, float[]> parameters = new Dictionary<string, float[]>();

        //------------------ Static constructors ---------------------

        public static ChannelMessage Create(MessageType type, ChannelMessageKey key)
        {
            ChannelMessage message = new ChannelMessage();
            message.type = type;
            message.key = key;
            return message;
        }

        //------------------ JSON encode/decode ---------------------

        public override JSONObject ToJSON()
        {
            JSONObject json = base.ToJSON();
            json.AddField("component", key.component);
            json.AddField("channel", key.channel);
            if (key.instance != null) json.AddField("instance", key.instance);
            json.AddField("paramaters", JSONUtils.EncodeFloatArrayDictionary(parameters));
            return json;
        }

        public override void ApplyJSON(JSONObject json)
        {
            base.ApplyJSON(json);
            key = ChannelMessageKey.Create(
                JSONUtils.DecodeStringField(json, "component"), 
                JSONUtils.DecodeStringField(json, "channel"), 
                JSONUtils.DecodeStringField(json, "instance", false)
                );
            parameters.Clear();
            parameters = JSONUtils.DecodeFloatArrayDictionary(json.GetField("parameters"), parameters);
        }

        //------------------ Agglomeration ---------------------

        // push a more recent matching message on top of this one 
        public void Push(ChannelMessage message)
        {
            if (message.key.Equals(key) == false || message.type != type)
            {
                throw new Exception("message key and type should match for push");
            }

            message.properties.ToList().ForEach(kvp => properties[kvp.Key] = kvp.Value);
            message.parameters.ToList().ForEach(kvp => parameters[kvp.Key] = kvp.Value);
        }

    }

    // buffer to be used with a connection
    public class ChannelMessageBuffer
    {
        private ObjectPool<ChannelMessage> _messagePool;

        private Dictionary<ChannelMessageKey, ChannelMessage> _createMessages;
        private Dictionary<ChannelMessageKey, ChannelMessage> _controlMessages;
        private Dictionary<ChannelMessageKey, ChannelMessage> _destroyMessages;
        

        public ChannelMessageBuffer(ObjectPool<ChannelMessage> messagePool)
        {
            _messagePool = messagePool;
        }

        public void EnqueueMessage(ChannelMessage message)
        {
            switch(message.type)
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

        private void InternalRemoveMessage(Dictionary<ChannelMessageKey, ChannelMessage> dict, ChannelMessageKey key)
        {
            ChannelMessage message;
            if (dict.TryGetValue(key, out message))
            {
                dict.Remove(key);
                _messagePool.Store(message);
            }
        }

        private void InternalPushMessage(Dictionary<ChannelMessageKey, ChannelMessage> dict, ChannelMessage message)
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
            components = JSONUtils.DecodeList<ComponentInfo>(json.GetField("components"));
        }

    }

    

}


