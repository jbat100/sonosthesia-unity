﻿using System;
using System.Collections.Generic;

namespace com.spacepuppy.Collections
{

    /// <summary>
    /// Represents a queue of static length. As entries are pushed onto the collection and reach the length of the queue, 
    /// they are automatically removed from the collection. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CircularQueue<T> : IEnumerable<T>, ICollection<T>
    {

        #region Fields

        private T[] _values;
        private int _count;
        private int _head;
        private int _rear;

        private IEqualityComparer<T> _comparer;
        private int _version;

        #endregion

        #region CONSTRUCTOR

        public CircularQueue(int size)
        {
            if (size < 0) throw new System.ArgumentException("Size must be non-negative.", "size");
            _values = new T[size];
            _count = 0;
            _head = 0;
            _rear = 0;
            _comparer = EqualityComparer<T>.Default;
        }

        public CircularQueue(int size, IEqualityComparer<T> comparer)
        {
            if (size < 0) throw new System.ArgumentException("Size must be non-negative.", "size");
            _values = new T[size];
            _count = 0;
            _head = 0;
            _rear = 0;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        #endregion

        #region Properties

        public int Size { get { return _values.Length; } }

        public int Count { get { return _count; } }

        #endregion

        #region Methods

        public void Enqueue(T value)
        {
            if (_values.Length == 0) return;

            _values[_rear] = value;

            if (_count == _values.Length)
                _head = (_head + 1) % _values.Length;
            _rear = (_rear + 1) % _values.Length;
            _count = Math.Min(_count + 1, _values.Length);
            _version++;
        }

        public T Dequeue()
        {
            if (_count == 0) throw new InvalidOperationException("CircularQueue is empty.");

            T result = _values[_head];
            _values[_head] = default(T);
            _head = (_head + 1) % _values.Length;
            _count--;
            _version++;

            return result;
        }

        public T Peek()
        {
            if (_count == 0) throw new InvalidOperationException("CircularQueue is empty.");

            return _values[_head];
        }

        public T PopLast()
        {
            if (_count == 0) throw new InvalidOperationException("CircularQueue is empty.");

            _rear = (_rear > 0) ? _rear - 1 : _values.Length - 1;
            T result = _values[_rear];
            _values[_rear] = default(T);
            _count--;
            _version++;

            return result;
        }

        public T PeekLast()
        {
            if (_count == 0) throw new InvalidOperationException("CircularQueue is empty.");

            int index = (_rear > 0) ? _rear - 1 : _values.Length - 1;
            return _values[index];
        }

        public void Resize(int size)
        {
            if (size < 0) throw new System.ArgumentException("Size must be non-negative.", "size");

            if(size > _values.Length && _head < _rear)
            {
                //if growing, and the queue doesn't wrap, we can just resize
                System.Array.Resize(ref _values, size);
            }
            else if(size < _values.Length && _head < _rear && size > _rear)
            {
                //if shrinking, and the queue doesn't wrap, and the size we're resizing to still fits the queue, we can just resize
                System.Array.Resize(ref _values, size);
            }
            else
            {
                T[] arr = new T[size];
                int index = _head;
                _count = Math.Min(size, _count);
                for (int i = 0; i < _count; i++)
                {
                    arr[i] = _values[index];
                    index = (index + 1) % _values.Length;
                }
                _values = arr;
                _head = 0;
                _rear = _count;
            }

            _version++;
        }

        public void Clear()
        {
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = default(T);
            }
            _count = 0;
            _head = 0;
            _rear = 0;
            _version++;
        }

        #endregion

        #region IEnumerable Interface

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region ICollection Interface

        void ICollection<T>.Add(T item)
        {
            this.Enqueue(item);
        }

        public bool Contains(T item)
        {
            int index = _head;
            for (int i = 0; i < _count; i++)
            {
                if (_comparer.Equals(_values[index], item)) return true;
                index = (index + 1) % _values.Length;
            }

            return false;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            int index = _head;
            for(int i = 0; i < _count; i++)
            {
                array[arrayIndex + i] = _values[index];
                index = (index + 1) % _values.Length;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new System.NotSupportedException();
        }

        #endregion

        #region Special Types

        public struct Enumerator : IEnumerator<T>
        {

            #region Fields

            private CircularQueue<T> _que;
            private int _index;
            private int _ver;
            private T _current;

            #endregion

            #region CONSTRUCTOR

            public Enumerator(CircularQueue<T> que)
            {
                _que = que;
                _index = que._head;
                _ver = que._version;
                _current = default(T);
            }

            #endregion

            #region IEnumerator Interface

            public T Current
            {
                get { return _current; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return _current; }
            }

            public bool MoveNext()
            {
                if (_ver != _que._version) throw new System.InvalidOperationException("CirculeQueue was modified while enumerating.");
                if (_index >= _que._count)
                {
                    _current = default(T);
                    return false;
                }
                _current = _que._values[_index];
                _index = (_index + 1) % _que._values.Length;
                return true;
            }

            public void Reset()
            {
                if (_ver != _que._version) throw new System.InvalidOperationException("CirculeQueue was modified while enumerating.");
                _index = _que._head;
                _current = default(T);
            }

            public void Dispose()
            {
            }

            #endregion

        }

        #endregion

    }
}
