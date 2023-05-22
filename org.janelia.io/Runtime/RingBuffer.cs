using System;
using UnityEngine;

namespace Janelia
{
    public class RingBuffer
    {
        public RingBuffer(int itemCount, int itemSizeBytes)
        {
            Debug.Log("Creating RingBuffer with " + itemCount + " items of size " + itemSizeBytes + " bytes each");

            _items = new Item[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                _items[i] = new Item
                {
                    bytes = new Byte[itemSizeBytes],
                    timestampMs = 0
                };
            }
        }

        // Give the specified bytes to the next available buffer in the ring.  The data is copied.
        public void Give(Byte[] given)
        {
            lock (_lock)
            {
                Buffer.BlockCopy(given, 0, _items[_iGive].bytes, 0, given.Length);
                _items[_iGive].timestampMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (_count == _items.Length)
                    _iTake = (_iTake + 1) % _items.Length;
                else
                    _count++;
                _iGive = (_iGive + 1) % _items.Length;
            }
        }

        // Take the specified bytes out of the next available buffer int the ring.  The data is copied.
        public bool Take(ref Byte[] taken, ref long timestampMs)
        {
            lock (_lock)
            {
                if (_count > 0)
                {
                    Buffer.BlockCopy(_items[_iTake].bytes, 0, taken, 0, taken.Length);
                    timestampMs = _items[_iTake].timestampMs;
                    _iTake = (_iTake + 1) % _items.Length;
                    _count--;
                    return true;
                }
                return false;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _iGive = _iTake = _count = 0;
            }
        }

        private struct Item
        {
            public long timestampMs;
            public Byte[] bytes;
        }

        private Item[] _items;
        private int _iGive = 0;
        private int _iTake = 0;
        private int _count = 0;
        private readonly object _lock = new object();
    }
}