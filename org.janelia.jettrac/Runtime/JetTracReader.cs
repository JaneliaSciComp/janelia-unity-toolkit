using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    public class JetTracReader
    {
        public const int RING_BUFFER_COUNT = 2048;

        public bool debug = false;

        public int BallDeviceNumber 
        {
            get => _deviceNumBall;
        }

        public int HeadDeviceNumber 
        {
            get => _deviceNumHead;
        }

        public void Start()
        {
            if (debug)
            {
                Debug.Log(Now() + " JetTracReader.Start");
            }

            if (!JetTracIdentifier.Identify(ref _deviceNumBall, ref _deviceNumHead))
            {
                Debug.Log("JetTracReader: Cannot identify raw HID devices");
                return;
            }

            int rawHidDeviceCount = (_deviceNumBall > _deviceNumHead) ? _deviceNumBall + 1 : _deviceNumHead + 1;
            _rawHidReader = new RawHidReader(rawHidDeviceCount);
            _rawHidReader.debug = debug;

            if (debug)
            {
                Debug.Log(Now() + " JetTracReader.Start: _deviceNumBall " + _deviceNumBall);
                Debug.Log(Now() + " JetTracReader.Start: _deviceNumHead " + _deviceNumHead);
            }

            _rawHidReader.Start();
        }

        public bool GetNextBallMessage(ref JetTracParser.BallMessage message)
        {
            if (debug)
            {
                Debug.Log(Now() + "JetTracReader.GetNextBallMessage: taking from device " + _deviceNumBall);
            }

            long timestampMs = 0;
            if (_rawHidReader.Take(_deviceNumBall, ref _rawHidReaderBufferBall, ref timestampMs))
            {
                JetTracParser.ParseBallMessage(ref message, _rawHidReaderBufferBall, timestampMs);
                return true;
            }

            return false;
        }

        public bool GetNextHeadMessage(ref JetTracParser.HeadMessage message)
        {
            if (debug)
            {
                Debug.Log(Now() + "JetTracReader.GetNextHeadMessage: taking from device " + _deviceNumHead);
            }

            long timestampMs = 0;
            if (_rawHidReader.Take(_deviceNumHead, ref _rawHidReaderBufferHead, ref timestampMs))
            {
                JetTracParser.ParseHeadMessage(ref message, _rawHidReaderBufferHead, timestampMs);
                return true;
            }

            return false;
        }

        public void OnDisable()
        {
            _rawHidReader.OnDisable();
        }

        private string Now()
        {
            if (Application.isEditor)
            {
                return "";
            }
            DateTime n = DateTime.Now;
            return "[" + n.Hour + ":" + n.Minute + ":" + n.Second + ":" + n.Millisecond + "] ";
        }

        private RawHidReader _rawHidReader;
        private int _deviceNumBall;
        private int _deviceNumHead;

        private Byte[] _rawHidReaderBufferBall = new byte[RawHidReader.READ_SIZE_BYTES];

        private Byte[] _rawHidReaderBufferHead = new byte[RawHidReader.READ_SIZE_BYTES];
    }
}
