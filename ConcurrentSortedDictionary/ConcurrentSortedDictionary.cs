using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryCache
{
    public class ConcurrentSortedDictionary<Tkey, TValue>
        : IDictionary<Tkey, TValue>, IDictionary
        where Tkey : notnull
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">Only When get</exception>
        public TValue this[Tkey key]
        {
            get
            {
                if (TryGetValue(key, out TValue? value))
                {
                    return value;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
            set
            {
                lock (_dataWritingLock)
                {
                    WaitForReading();
                    _isWriting = true;
                    _dictionary[key] = value;
                    _isWriting = false;
                }
            }
        }
        public ICollection<Tkey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;


        private readonly SortedDictionary<Tkey, TValue> _dictionary;

        private volatile int _readingThreads;
        private volatile int _pendingWritingThreads;
        private volatile bool _isWriting;
        private readonly object _dataWritingLock;


        public ConcurrentSortedDictionary()
        {
            _dictionary = new();

            _readingThreads = 0;
            _pendingWritingThreads = 0;
            _isWriting = false;
            _dataWritingLock = new object();
        }


        /// <summary>
        /// Adds or throws exception.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException">An element with the same key already exists.</exception>
        public void Add(Tkey key, TValue value)
        {
            WaitForWriting();
            lock (_dataWritingLock)
            {
                WaitForReading();
                _isWriting = true;
                _dictionary.Add(key, value);
                _isWriting = false;
            }
        }

        /// <summary>
        /// Adds or throws exception.
        /// </summary>
        /// <param name="keyValuePair"></param>
        /// <exception cref="ArgumentException">An element with the same key already exists.</exception>
        public void Add(KeyValuePair<Tkey, TValue> keyValuePair)
        {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// Determines whether this contains an element with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(Tkey key)
        {
            WaitForWriting();
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether this contains an element equals to a specific value.
        /// </summary>
        /// <param name="keyValuePair"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Contains(KeyValuePair<Tkey, TValue> keyValuePair)
        {
            ArgumentNullException.ThrowIfNull(keyValuePair);

            WaitForWriting();
            if (_dictionary.TryGetValue(keyValuePair.Key, out TValue? value))
            {
                if (keyValuePair.Value == null)
                {
                    return value == null;
                }
                else
                {
                    return keyValuePair.Value.Equals(value);
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(Tkey key, [MaybeNullWhen(false)] out TValue value)
        {
            WaitForWriting();
            try
            {
                Interlocked.Increment(ref _readingThreads);
                return _dictionary.TryGetValue(key, out value);
            }
            finally
            {
                Interlocked.Decrement(ref _readingThreads);
            }
        }

        /// <summary>
        /// By the definition of IDictionary.Remove(), this returns true if removed; otherwise(including key not found), false.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(Tkey key)
        {
            lock (_dataWritingLock)
            {
                _isWriting = true;
                bool success = _dictionary.Remove(key);
                _isWriting = false;
                return success;
            }
        }

        /// <summary>
        /// Removes all items.
        /// </summary>
        public void Clear()
        {
            lock (_dataWritingLock)
            {
                _isWriting = true;
                _dictionary.Clear();
                _isWriting = false;
            }
        }

        public void CopyTo

        private void WaitForWriting()
        {
            var spinner = new SpinWait();
            while (_isWriting)
            {
                spinner.SpinOnce();
            }
        }

        private void WaitForReading()
        {
            var spinner = new SpinWait();
            while (_readingThreads > 0)
            {
                spinner.SpinOnce();
            }
        }
    }
}
