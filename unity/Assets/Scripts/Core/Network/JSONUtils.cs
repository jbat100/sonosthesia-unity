using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Sonosthesia
{


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

    public static class JSONUtils
    {
        public static string FirstCharToUpper(string input)
        {
            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        public static JSONObject EncodeStringDictionary(Dictionary<string, string> dict)
        {
            // JSONObject has a handy little constructor
            return JSONObject.Create(dict);
        }


        public static JSONObject EncodeFloatsDictionary(Dictionary<string, IList<float>> dict)
        {
            JSONObject json = new JSONObject(JSONObject.Type.OBJECT);
            json.keys = new List<string>();
            json.list = new List<JSONObject>();
            //Not sure if it's worth removing the foreach here
            foreach (KeyValuePair<string, IList<float>> kvp in dict)
            {
                json.keys.Add(kvp.Key);
                json.list.Add(EncodeFloats(kvp.Value));
            }
            return json;
        }

        public static Dictionary<string, IList<float>> DecodeFloatsDictionary(JSONObject json, Dictionary<string, IList<float>> dict = null)
        {
            if (dict == null) dict = new Dictionary<string, IList<float>>();
            if (json != null && json.type == JSONObject.Type.OBJECT)
            {
                for (int i = 0; i < json.Count; i++)
                {
                    IList<float> enumerable = null;
                    dict.TryGetValue(json.keys[i], out enumerable);
                    List<float> list = enumerable as List<float>;
                    if (list == null) list = new List<float>();
                    dict[json.keys[i]] = DecodeFloats(json.list[i], list);
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

        public static JSONObject EncodeList<T>(List<T> objs) where T : JSONEncodable
        {
            JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
            json.list = (List<JSONObject>)objs.Select(obj => { return obj.ToJSON(); }).ToList();
            return json;
        }

        public static List<T> DecodeList<T>(JSONObject json) where T : JSONDecodable, new()
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

        public static JSONObject EncodeFloats(IEnumerable<float> numbers)
        {
            JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
            json.list = (List<JSONObject>)numbers.Select(number => { return JSONObject.Create(number); });
            return json;
        }

        public static List<float> DecodeFloats(JSONObject json, IEnumerable<float> enumerable = null)
        {
            List<float> list = enumerable as List<float>;
            if (list == null)
            {
                list = new List<float>();
            }
            if (json.type == JSONObject.Type.ARRAY && json.list != null)
            {
                list.AddRange(json.list.Select(obj =>
                {
                    if (obj.type != JSONObject.Type.NUMBER)
                    {
                        throw new JSONDecodeException("expected NUMBER type JSONObject");
                    }
                    return obj.n;
                }));
            }
            // be robust to parameters being expressed as single number rather than number array
            else if (json.type == JSONObject.Type.NUMBER)
            {
                list.Add(json.n);
            }
            else
            {
                throw new JSONDecodeException("expected ARRAY type JSONObject with list property");
            }
            return list;
        }
    }

}