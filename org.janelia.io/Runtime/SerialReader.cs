using System;
using System.IO;

// To make `System.IO.Ports` available, do the following in the Unity editor:
// In the `Edit` menu, choose `Project Settings...` to raise the `Project Settings` window.
// In the `Player` tab, open the `Other Settings` bellow.
// In the `Configuration` section, set `API Compatibility Level` to `.NET 4.x`.
using System.IO.Ports;

using System.Text;
using System.Threading;
using UnityEngine;

// Reads messages in a serial stream connected to a USB port.

namespace Janelia
{
    public class SerialReader
    {
        public string Portname { get { return _portname; } }

        public bool debug = true;

        // Setting this flag to `true` will reduce performance.,
        public bool debugSlowly = false;

        // Messages are strings of bytes, ending with the `terminator` character.
        public SerialReader(string portname = "", byte terminator = 10, int readBufferSizeBytes = 256, int ringBufferCount = 1024)
        {
            _portname = portname;
            _terminator = terminator;
            _readBufferSizeBytes = readBufferSizeBytes;
            _ringBufferCount = ringBufferCount;
            _ringBuffer = new RingBuffer(_ringBufferCount, _readBufferSizeBytes);
        }

        ~SerialReader()
        {
            OnDisable();
        }

        public void Start()
        {
            if (String.IsNullOrEmpty(_portname))
            {
                string[] portnames = SerialPort.GetPortNames();
                foreach (string portname in portnames)
                {
                    try
                    {
                        SerialPort test = new SerialPort(portname, 9600);
                        test.Open();
                        test.Close();
                        _portname = portname;
                        break;
                    }
                    catch (System.Exception) {}
                }
            }
            if (!String.IsNullOrEmpty(_portname))
            {
                if (debug)
                    Debug.Log(Now() + "SerialReader using port '" + _portname + "'");

                _port = new SerialPort(_portname, 9600);
                if (debug)
                    Debug.Log(Now() + "SerialReader.Start() opening serial port '" + _portname + "'");
                _port.Open();

                if (_port.IsOpen)
                {
                    if (_thread == null)
                    {
                        if (debug)
                            Debug.Log(Now() + "SerialReader.Start() creating thread for '" + _portname + "'");
                        _thread = new Thread(ThreadFunction) { IsBackground = true };
                    }

                    _thread.Start();
                }
                else
                {
                    if (debug)
                        Debug.Log(Now() + "SerialReader.Start() could not open serial port '" + _portname + "'");
                }
            }
        }

        public bool Take(ref Byte[] taken, ref long timestampMs)
        {
            return _ringBuffer.Take(ref taken, ref timestampMs);
        }

        public void OnDisable()
        {
            if (debug)
                Debug.Log(Now() + "SerialReader.OnDisable() for '" + _portname + "'");

            if (_thread != null)
            {
                if (debug)
                    Debug.Log(Now() + "SerialReader.OnDisable() aborting socket thread for '" + _portname + "'");
                _thread.Abort();
                try
                {
                    _port.Close();
                }
                catch (System.Exception) {}
            }
        }

        private void ThreadFunction()
        {
            _readBuffer = new Byte[_readBufferSizeBytes];
            ReadMore();
            Thread.Sleep(Timeout.Infinite);
        }

        // This approach using `BaseStream` is suggested as being more reliable than the higher-level API
        // for `SerialPort`:
        // https://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport
        private void ReadMore()
        {
            int maxToRead = _readBuffer.Length - _readBufferOffset;
            _port.BaseStream.BeginRead(_readBuffer, _readBufferOffset, maxToRead, delegate (IAsyncResult ar)
            {
                try
                {
                    int numBytesRead = _port.BaseStream.EndRead(ar);

                    if (debugSlowly)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < numBytesRead; ++i)
                        {
                            byte b = _readBuffer[_readBufferOffset + i];
                            sb.Append((int)b);
                            sb.Append(" ");
                        }
                        Debug.Log(Now() + "SerialReader.ReadMore read " + numBytesRead + " bytes from '" + _portname + "': " + sb.ToString());
                    }

                    if (numBytesRead > 0)
                    {
                        if (_readBuffer[_readBufferOffset + numBytesRead - 1] == _terminator)
                        {
                            if (debugSlowly)
                                Debug.Log(Now() + "SerialReader.ReadMore storing " + (_readBufferOffset + numBytesRead) + " bytes from '" + _portname + "'");

                            _ringBuffer.Give(_readBuffer);
                            _readBufferOffset = 0;
                            Array.Clear(_readBuffer, _readBufferOffset, numBytesRead);
                        }
                        else
                        {
                            _readBufferOffset += numBytesRead;
                        }
                    }
                }
                catch (IOException exc)
                {
                    Debug.Log(Now() + "SerialReader.ReadMore + '" + _portname + "' exception: '" + exc.Message + "'");
                }

                ReadMore();
            }, null);
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

        private System.Threading.Thread _thread;

        private string _portname;
        private SerialPort _port;

        private Byte _terminator = 10;

        private int _readBufferSizeBytes;
        private Byte[] _readBuffer;
        private int _readBufferOffset = 0;

        private int _ringBufferCount;
        private RingBuffer _ringBuffer;
    }
}
