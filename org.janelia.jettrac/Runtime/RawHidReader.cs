using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Janelia
{
    class RawHidReader
    {
        public const int READ_SIZE_BYTES = 64;
        public const int ALL_RAW_HID_DEVICES = 256;
        
        public bool debug = false;

        public RawHidReader(int deviceCount = ALL_RAW_HID_DEVICES, float deviceFrequencyHz = 360)
        {
            _deviceCount = deviceCount;
            _deviceFreqHz = deviceFrequencyHz;
            // Defined in org.janelia.io.
            _ringBuffers = new RingBuffer[_deviceCount];
        }

        // Returns the number of raw HID devices acutally started.
        public int Start()
        {
            // For Teensy in raw HID mode.
            int vid = 0x16C0;
            int pid = 0x0486;
            int usagePage = 0xFFAB;
            int usage = 0x0200;

            int openedCount = rawhid_open(_deviceCount, vid, pid, usagePage, usage);
            if (_deviceCount == ALL_RAW_HID_DEVICES)
            {
                _deviceCount = openedCount;
            }
            else if (openedCount != _deviceCount)
            {
                Debug.Log("RawHidReader.Start: rawhid_open attempted " + _deviceCount + " but got "
                    + openedCount);
                return 0;
            }

            for (int i = 0; i < _deviceCount; ++i)
            {
                _ringBuffers[i] = new RingBuffer(BUFFER_COUNT, READ_SIZE_BYTES);
            }
                
            _thread = new Thread(ThreadFunction);
            _thread.Start();

            return _deviceCount;
        }

        public bool Take(int deviceNum, ref Byte[] taken, ref long timestampMs)
        {
            if ((0 <= deviceNum) && (deviceNum < _deviceCount))
            {
                return _ringBuffers[deviceNum].Take(ref taken, ref timestampMs);
            }
            return false;
        }

        public void Clear(int deviceNum)
        {
            _ringBuffers[deviceNum].Clear();
        }

        public void OnDisable()
        {
            for (int i = 0; i < _deviceCount; ++i)
            {
                rawhid_close(i);
            }
            if (_thread != null)
            {
                _thread.Abort();
            }
        }

        private void ThreadFunction()
        {
            if (debug)
            {
                Debug.Log(Now() + "RawHidReade.ThreadFunction: starting for  " + _deviceCount + " devices");
            }

            byte[] recvBuffer = new byte[READ_SIZE_BYTES];
            int recvTimeoutMs = Mathf.RoundToInt(1 / _deviceFreqHz / 2);

            while (true)
            {
                for (int deviceNum = 0; deviceNum < _deviceCount; ++deviceNum)
                {
                    int dataLength = rawhid_recv(deviceNum, recvBuffer, READ_SIZE_BYTES, recvTimeoutMs);
                    if (dataLength > 0)
                    {
                         _ringBuffers[deviceNum].Give(recvBuffer);
                    }
                }
            }
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

        private const int BUFFER_COUNT = 720;

        private int _deviceCount;
        private float _deviceFreqHz;

        private Thread _thread;

        private RingBuffer[] _ringBuffers;

        [DllImport("SimpleRawHid")]
        private static extern int rawhid_open(int max, int vid, int pid, int usage_page, int usage);

        [DllImport("SimpleRawHid")]
        private static extern int rawhid_recv(int num, byte[] buf, int len, int timeout);

        [DllImport("SimpleRawHid")]
        private static extern void rawhid_close(int num);
    }
}