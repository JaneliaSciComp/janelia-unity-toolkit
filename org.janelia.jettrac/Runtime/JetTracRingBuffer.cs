using System;

namespace Janelia
{
    // The code would be more elegant if this class were a generic (possibly replacing
    // the byte-array-valued `RingBuffer` class from the `org.janelia.io` package),
    // but the following alternative ensures there is no chance that using generics
    // introduces temporary allocation that triggers garbage collection.

    public class JetTracRingBuffer
    {
        // Note that this struct stores the union of the data for a ball message and
        // a head message.  So it is a bit wasteful, but that waste should not be 
        // significant, and again, avoids the uncertainties of generics.
        public struct Item
        {
            public Int32 ballX0;
            public Int32 ballY0;
            public Int32 ballX1;
            public Int32 ballY1;
            public float headAngleDegs;
            public UInt64 readTimestampMs;
            public UInt64 deviceTimestampUs;
            public UInt64 deltaReadTimestampMs;
            public UInt64 deltaDeviceTimestampUs;

            public Item(JetTracParser.BallMessage ball)
            {
                ballX0 = ball.x0;
                ballY0 = ball.y0;
                ballX1 = ball.x1;
                ballY1 = ball.y1;
                headAngleDegs = 0;
                readTimestampMs = ball.readTimestampMs;
                deviceTimestampUs = ball.deviceTimestampUs;
                deltaReadTimestampMs = 0;
                deltaDeviceTimestampUs = 0;
            }

            public Item(JetTracParser.HeadMessage head)
            {
                ballX0 = 0;
                ballY0 = 0;
                ballX1 = 0;
                ballY1 = 0;
                headAngleDegs = head.angleDegs;
                readTimestampMs = head.readTimestampMs;
                deviceTimestampUs = head.deviceTimestampUs;
                deltaReadTimestampMs = 0;
                deltaDeviceTimestampUs = 0;
            }
        };

        public int ItemCount
        {
            get => _count;
        }

        public JetTracRingBuffer(int itemCount, int itemSizeBytes)
        {
            _items = new Item[itemCount];
        }

        public void Clear()
        {
            _iGive = _iTake = _count = 0;
        }

        // Give the specified item to the next available spot in the ring.
        public void Give(Item given)
        {
            _items[_iGive] = given;
            if (_count == _items.Length)
                _iTake = (_iTake + 1) % _items.Length;
            else
                _count++;
            _iGive = (_iGive + 1) % _items.Length;
        }

        // Take the item out of the next available spot in the ring.
        public bool Take(ref Item taken)
        {
            if (_count > 0)
            {
                taken = _items[_iTake];
                _iTake = (_iTake + 1) % _items.Length;
                _count--;
                return true;
            }
            return false;
        }

        public int LatestIndex()
        {
            return (_iGive - 1 >= 0) ? _iGive - 1 : _iGive - 1 + _items.Length;
        }

        public bool Peek(ref Item peeked, int i = 0)
        {
            if (_count > 0)
            {
                int j = LatestIndex() - i;
                if (j < 0)
                    j += _items.Length;
                peeked = _items[j];
                return true;
            }
            return false;
        }

        public bool PeekLatest(ref Item peeked)
        {
            return Peek(ref peeked);
        }

        public bool PeekEarliest(ref Item peeked)
        {
            return Peek(ref peeked, _count - 1);
        }

        private Item[] _items;
        private int _iGive = 0;
        private int _iTake = 0;
        private int _count = 0;
    }
}