using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{
    public static class IDictionaryUtils
    {
        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return "{" + string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
        }
    }

}
