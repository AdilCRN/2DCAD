using System;
using System.Collections.Generic;

namespace MarkGeometriesLib.Classes.Generics
{
    public class CachedLoader<T>
    {
        private Dictionary<object, T> _buffer;
        private List<object> _capacityTracker;

        public int Capacity { get; private set; }
        public int Count => _capacityTracker.Count;

        public CachedLoader(int capacity = 10)
        {
            // create buffer to store items
            _buffer = new Dictionary<object, T>();
            _capacityTracker = new List<object>();

            // capacity must be greater than 1
            Capacity = Math.Max(capacity, 1);
        }

        public void Clear()
        {
            _buffer.Clear();
        }

        /// <summary>
        ///     Attempt to load from cache if it exists else load using getter
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public T TryGet(object tag, Func<T> getter)
        {
            // if item does not exist in buffer
            if (_buffer.ContainsKey(tag) == false)
            {
                // if count is greater than capacity
                if (Count >= Capacity && Capacity > 0)
                {
                    // remove item from buffer
                    _buffer.Remove(_capacityTracker[0]);

                    // stop tracking tag
                    _capacityTracker.RemoveAt(0);
                }

                // add item to buffer and track tag
                _buffer[tag] = getter();
                _capacityTracker.Add(tag);
            }

            return _buffer[tag];
        }
    }
}
