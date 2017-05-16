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

    public class Range<T> where T : IComparable, IComparable<T>
    {
        public T min;
        public T max;

        public Range(T _min, T _max)
        {
            min = _max;
            max = _max;
        }

        public bool Contains(T val)
        {
            return val.CompareTo(min) > 0 && val.CompareTo(max) < 0;
        }
    }

    public class BaseInfo
    {
        public string identifier;
    }


    public class ParameterInfo : BaseInfo
    {
        public Range<float> range = new Range<float>(0f, 1f);
        public float defaultValue = 0f;
    }

    public class ChannelInfo : BaseInfo
    {
		public List<ParameterInfo> parameters = new List<ParameterInfo>();
    }

    public class ComponentInfo : BaseInfo
    {
        public List<ChannelInfo> channels = new List<ChannelInfo>();
    }


    // ----------------------------- MESSAGE CONTAINERS --------------------------------------

    public class Message
    {
        public MessageType type;
    }

    // look into using immutable versions of strings, lists and dictionaries

    public class ChannelMessage : Message
    {
        public string channel;
        public string component;
        public string instance;

        public Dictionary<string, string> properties = new Dictionary<string, string>();

        public Dictionary<string, float[]> parameters = new Dictionary<string, float[]>();
        
    }

    // component declaration message

    public class ComponentMessage : Message
    {
        public List<ComponentInfo> components;


    }

    // ----------------------------- MESSAGE ENCODE/DECODE --------------------------------------

    public class JSONMessagePool
    {
        // have a pool for each message class

        public readonly ObjectPool<ChannelMessage> ChannelMessagePool = new ObjectPool<ChannelMessage>();

        public readonly ObjectPool<ComponentMessage> ComponentMessagePool = new ObjectPool<ComponentMessage>();

    }

    // decoder based on JSONObject not JSONUtility, would be good to check the performance difference

	public class JSONMessageDecoder 
	{
        private JSONMessagePool pool = new JSONMessagePool();

        public Message DecodeMessage(JSONObject message, out MessageType messageType)
        {
            messageType = ExtractMessageType(message);

            switch (messageType)
            {
                case MessageType.Create:
                case MessageType.Destroy:
                case MessageType.Control:
                case MessageType.Event:
                    {
                        return DecodeChannelMessage(message, messageType);
                    }
                default:
                    return null;
            }
        }

        private MessageType ExtractMessageType(JSONObject message) 
		{
            return MessageType.Undefined;
		}

        private string ExtractStringField(JSONObject obj, string key, bool required = true)
        {
            string s = "";
            if (required && !obj.HasField(key))
            {
                throw new JSONDecodeException("expected string field: " + key);
            }
            obj.GetField(ref s, key);
            return s;
        }

        private float ExtractFloatField(JSONObject obj, string key, bool required = true)
        {
            float f = 0f;
            if (required && !obj.HasField(key))
            {
                throw new JSONDecodeException("expected numeric field: " + key);
            }
            obj.GetField(ref f, key);
            return f;
        }

        private ChannelMessage DecodeChannelMessage(JSONObject obj, MessageType messageType)
        {
            ChannelMessage message = pool.ChannelMessagePool.Fetch();

            message.type = messageType;

            message.component = ExtractStringField(obj, "component");
            message.channel = ExtractStringField(obj, "channel");
            message.instance = ExtractStringField(obj, "instance");

            message.properties.Clear();

            // extract properties

            JSONObject propertiesObj = obj.GetField("properties");
            if (propertiesObj != null)
            {
                CheckJSONObjectType(propertiesObj, JSONObject.Type.OBJECT);

                for (int i = 0; i < propertiesObj.list.Count; i++)
                {
                    JSONObject val = propertiesObj.list[i];
                    CheckJSONObjectType(val, JSONObject.Type.STRING);
                    message.properties[propertiesObj.keys[i]] = val.str;
                }
            }

            message.parameters.Clear();
            
            // extract parameters

            JSONObject parametersObj = obj.GetField("parameters");
            if (parametersObj != null)
            {
                CheckJSONObjectType(parametersObj, JSONObject.Type.OBJECT);

                for (int i = 0; i < parametersObj.list.Count; i++)
                {
                    JSONObject val = parametersObj.list[i];
                    CheckJSONObjectType(val, JSONObject.Type.ARRAY);
                    float[] values = val.list.Select(valueObj => valueObj.n).ToArray();
                    message.parameters[parametersObj.keys[i]] = values;
                }
            }

            return message;
        }

        private ComponentMessage DecodeComponentMessage(JSONObject obj)
        {
            ComponentMessage message = pool.ComponentMessagePool.Fetch();

            message.components.Clear();

            JSONObject componentArray = obj.GetField("components");
            if (componentArray != null)
            {
                CheckJSONObjectType(componentArray, JSONObject.Type.ARRAY);

                for (int i = 0; i < componentArray.list.Count; i++)
                {
                    JSONObject componentObj = componentArray.list[i];
                    message.components.Add(ExtractComponentInfo(componentObj));  
                }
            }

            return message;
        }

        private ComponentInfo ExtractComponentInfo(JSONObject obj)
        {
            CheckJSONObjectType(obj, JSONObject.Type.OBJECT);

            ComponentInfo info = new ComponentInfo();

            info.identifier = ExtractStringField(obj, "identifier");

            JSONObject channelArray = obj.GetField("channels");
            if (channelArray != null)
            {
                CheckJSONObjectType(channelArray, JSONObject.Type.ARRAY);

                for (int i = 0; i < channelArray.list.Count; i++)
                {
                    JSONObject channelObj = channelArray.list[i];
                    info.channels.Add(ExtractChannelInfo(channelObj));
                }
            }

            return info;
        }

        private ChannelInfo ExtractChannelInfo(JSONObject obj)
        {
            CheckJSONObjectType(obj, JSONObject.Type.OBJECT);

            ChannelInfo info = new ChannelInfo();

            info.identifier = ExtractStringField(obj, "identifier");

            JSONObject parameterArray = obj.GetField("parameters");
            if (parameterArray != null)
            {
                CheckJSONObjectType(parameterArray, JSONObject.Type.ARRAY);

                for (int i = 0; i < parameterArray.list.Count; i++)
                {
                    JSONObject parameterObj = parameterArray.list[i];
                    info.parameters.Add(ExtractParameterInfo(parameterObj));
                }
            }

            return info;
        }

        private ParameterInfo ExtractParameterInfo(JSONObject obj)
        {
            CheckJSONObjectType(obj, JSONObject.Type.OBJECT);

            ParameterInfo info = new ParameterInfo();

            info.identifier = ExtractStringField(obj, "identifier");

            JSONObject rangeObj = obj.GetField("range");
            info.range = ExtractRange(rangeObj);

            info.defaultValue = ExtractFloatField(obj, "defaultValue");

            return info;
        }

        private Range<float> ExtractRange(JSONObject obj)
        {
            CheckJSONObjectType(obj, JSONObject.Type.OBJECT);

            return new Range<float>(ExtractFloatField(obj, "min"), ExtractFloatField(obj, "max")); 
        }

        private void CheckJSONObjectType(JSONObject obj, JSONObject.Type type)
        {
            if (obj.type != type)
            {
                throw new JSONDecodeException("expected JSON " + type.ToString());
            }
        }

    }

	public class JSONMessageEncoder
	{
        public JSONMessagePool pool = new JSONMessagePool();
    }


}


