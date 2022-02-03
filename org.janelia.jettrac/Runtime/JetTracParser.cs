using System;
using UnityEngine;

namespace Janelia
{
    public static class JetTracParser
    {
        public struct BallMessage
        {
            public UInt64 readTimestampMs;
            public UInt64 deviceTimestampUs;
            public Int32 x0;
            public Int32 y0;
            public Int32 x1;
            public Int32 y1;

            public override string ToString() =>
                $"ms: {readTimestampMs}; us: {deviceTimestampUs}; x0 {x0}; y0 {y0}; x1 {x1}; y1 {y1}";
        }

        public static bool ParseBallMessage(ref BallMessage message, Byte[] readData, long readTimestampMs)
        {
            UInt64 deviceTimestampUs;
            Int32 x0, y0, x1, y1;
            int offset = 0;

            Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, HEADER_SIZE_BYTES);
            if (!HasBallHeader(_bitConverterBuffer))
            {
                return false;
            }
            offset += HEADER_SIZE_BYTES;

            if (BitConverter.IsLittleEndian)
            {
                Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, sizeof(UInt64));
                FastReverse(ref _bitConverterBuffer, 0, sizeof(UInt64));
                deviceTimestampUs = BitConverter.ToUInt64(_bitConverterBuffer, 0);

                offset += sizeof(UInt64);

                Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, sizeof(Int32));
                FastReverse(ref _bitConverterBuffer, 0, sizeof(Int32));
                x0 = BitConverter.ToInt32(_bitConverterBuffer, 0);

                offset += sizeof(Int32);

                Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, sizeof(Int32));
                FastReverse(ref _bitConverterBuffer, 0, sizeof(Int32));
                y0 = BitConverter.ToInt32(_bitConverterBuffer, 0);

                offset += sizeof(Int32);

                Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, sizeof(Int32));
                FastReverse(ref _bitConverterBuffer, 0, sizeof(Int32));
                x1 = BitConverter.ToInt32(_bitConverterBuffer, 0);

                offset += sizeof(Int32);

                Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, sizeof(Int32));
                FastReverse(ref _bitConverterBuffer, 0, sizeof(Int32));
                y1 = BitConverter.ToInt32(_bitConverterBuffer, 0);
            }
            else
            {
                deviceTimestampUs = BitConverter.ToUInt64(readData, 0);
                offset += sizeof(UInt64);

                x0 = BitConverter.ToInt32(readData, offset);
                offset += sizeof(Int32);

                y0 = BitConverter.ToInt32(readData, offset);
                offset += sizeof(UInt64);

                x1 = BitConverter.ToInt32(readData, offset);
                offset += sizeof(Int32);

                y1 = BitConverter.ToInt32(readData, offset);
            }

            message.readTimestampMs = (UInt64)readTimestampMs;
            message.deviceTimestampUs = deviceTimestampUs;
            message.x0 = x0;
            message.y0 = y0;
            message.x1 = x1;
            message.y1 = y1;

            return true;
        }

        public struct HeadMessage
        {
            public UInt64 readTimestampMs;
            public UInt64 deviceTimestampUs;
            public float angleDegs;

            public override string ToString() =>
                $"ms: {readTimestampMs}; us: {deviceTimestampUs}; angleDegs {angleDegs}";
        }

        public static bool ParseHeadMessage(ref HeadMessage message, Byte[] readData, long readTimestampMs)
        {
            UInt64 deviceTimestampUs;
            float angleDegs;
            int offset = 0;

            Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, HEADER_SIZE_BYTES);
            if (!HasHeadHeader(_bitConverterBuffer))
            {
                return false;
            }
            offset += HEADER_SIZE_BYTES;

            if (BitConverter.IsLittleEndian)
            {
                Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, sizeof(UInt64));
                FastReverse(ref _bitConverterBuffer, 0, sizeof(UInt64));
                deviceTimestampUs = BitConverter.ToUInt64(_bitConverterBuffer, 0);

                offset += sizeof(UInt64);

                Buffer.BlockCopy(readData, offset, _bitConverterBuffer, 0, sizeof(float));
                FastReverse(ref _bitConverterBuffer, 0, sizeof(float));
                angleDegs = BitConverter.ToSingle(_bitConverterBuffer, 0);
            }
            else
            {
                deviceTimestampUs = BitConverter.ToUInt64(readData, 0);
                angleDegs = BitConverter.ToSingle(readData, sizeof(UInt64));
            }

            message.readTimestampMs = (UInt64)readTimestampMs;
            message.deviceTimestampUs = deviceTimestampUs;
            message.angleDegs = angleDegs;

            return true;
        }

        static private bool HasBallHeader(byte[] data)
        {
            return ((data[0] == (byte)'J') && (data[1] == (byte)'T') && (data[2] == (byte)'B') && (data[3] == (byte)0));
        }

        static private bool HasHeadHeader(byte[] data)
        {
            return ((data[0] == (byte)'J') && (data[1] == (byte)'T') && (data[2] == (byte)'H') && (data[3] == (byte)0));
        }
        
        // Does not create temporary memory allocations (garbage) like `Array.Reverse`.
        static private void FastReverse(ref byte[] data, int start, int length)
        {
            int n = length / 2;
            int i0 = start, i1 = start + length - 1;
            for (int i = 0; i < n; ++i, ++i0, --i1)
            {
                byte t = data[i0];
                data[i0] = data[i1];
                data[i1] = t;
            }
        }

        private const int HEADER_SIZE_BYTES = 4;

        static private byte[] _bitConverterBuffer = new byte[sizeof(UInt64)];
    }
}
