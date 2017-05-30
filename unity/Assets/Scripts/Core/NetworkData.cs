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
        public static JSONObject EncodeStringDictionary(Dictionary<string, string> dic)
        {
            // JSONObject has a handy little constructor
            return JSONObject.Create(dic);
        }

        public static JSONObject EncodeFloatArrayDictionary(Dictionary<string, float[]> dic)
        {
            JSONObject json = new JSONObject(JSONObject.Type.OBJECT);
            json.keys = new List<string>();
            json.list = new List<JSONObject>();
            //Not sure if it's worth removing the foreach here
            foreach (KeyValuePair<string, float[]> kvp in dic)
            {
                json.keys.Add(kvp.Key);
                json.list.Add(EncodeFloatList(kvp.Value.ToList()));
            }
            return json;
        }

        public static string ExtractStringField(JSONObject obj, string key, bool required = true)
        {
            string s = "";
            if (required && !obj.HasField(key))
            {
                throw new JSONDecodeException("expected string field: " + key);
            }
            obj.GetField(ref s, key);
            return s;
        }

        public static float ExtractFloatField(JSONObject obj, string key, bool required = true)
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
                return (List<float>)json.list.Select(obj => obj.n);
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
            min = JSONUtils.ExtractFloatField(json, "min");
            max = JSONUtils.ExtractFloatField(json, "max");
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
            identifier = JSONUtils.ExtractStringField(json, "identifier");
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
            defaultValue = JSONUtils.ExtractFloatField(json, "defaultValue");
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
            JSONObject json = new JSONObject();
            json.AddField("type", type.ToString().ToLower());
            return json;
        }

        public virtual void ApplyJSON(JSONObject json)
        {
            type = (MessageType)Enum.Parse(typeof(MessageType), JSONUtils.ExtractStringField(json, "type"));
        }
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


