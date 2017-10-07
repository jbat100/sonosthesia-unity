using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

        public void Store(IEnumerable<T> ts)
        {
            foreach (T t in ts)
            {
                _stack.Push(t);
            }
        }
    }
}