using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

// A lower-level reader.
// `SocketMessageReader` is higher level, using the current class and adding notion of messages,
// with a designated delimiter (header or terminator).

namespace Janelia
{
    public class SocketReader
    {
        // Supports UDP (the default) or TCP.
        public readonly bool usingUDP;
        public bool debug = true;

        // Setting this flag to `true` will reduce performance.,
        public bool debugSlowly = false;

        // Only TCP needs `connectRetryMs`.
        public SocketReader(string hostname = "127.0.0.1", int port = 2000, int readBufferSizeBytes = 1024, int readBufferCount = 240, bool useUDP = true, int connectRetryMs = 5000)
        {
            usingUDP = useUDP;
            _hostname = hostname;
            _port = port;
            _connectRetryMs = connectRetryMs;
            _readBufferSizeBytes = readBufferSizeBytes;
            _readBufferCount = readBufferCount;
            _ringBuffer = new RingBuffer(_readBufferCount, _readBufferSizeBytes);
        }

        ~SocketReader()
        {
            OnDisable();
        }

        public void Start()
        {
            if (_thread == null)
            {
                if (debug)
                    Debug.Log(Now() + "SocketReader.Start() creating socket thread");

                _thread = usingUDP ?
                    new System.Threading.Thread(ThreadFunctionUDP) { IsBackground = true } :
                    new System.Threading.Thread(ThreadFunctionTCP) { IsBackground = true };

                _thread.Start();
            }
        }

        public bool Take(ref Byte[] taken, ref long timestampMs)
        {
            return _ringBuffer.Take(ref taken, ref timestampMs);
        }

        public void OnDisable()
        {
            if (_thread != null)
            {
                if (debug)
                    Debug.Log(Now() + "SocketReader.OnDisable() aborting socket thread");

                _thread.Abort();
            }
        }

        private void ThreadFunctionUDP()
        {
            if (debug)
                Debug.Log(Now() + "SocketReader using UDP");

            // The `UdpClient` class does not seem to support a version of `Receive` that will reuse a
            // `byte []` buffer passed in as argument.  So, to avoid possible problems with garbage collection,
            // use the lower-level `Socket` approach.
            Socket socket;

            try
            {
                if (debug)
                    Debug.Log(Now() + "SocketReader setting up socket for host '" + _hostname + "' port " + _port);

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);

                socket.Bind(new IPEndPoint(IPAddress.Parse(_hostname), _port));
            }
            catch (SocketException socketException)
            {
                if (debug)
                    Debug.Log(Now() + "SocketReader cannot set up socket: " + socketException);
                return;
            }

            Byte[] readBuffer = new Byte[_readBufferSizeBytes];
            while (true)
            {
                try
                {
                    if (debugSlowly)
                        Debug.Log(Now() + "SocketReader waiting to receive UDP datagram");

                    int length = socket.Receive(readBuffer);

                    if (debugSlowly)
                        Debug.Log("SocketReader read " + length + " bytes");

                    _ringBuffer.Give(readBuffer);
                    Array.Clear(readBuffer, 0, length);

                    if (debugSlowly)
                        Debug.Log("SocketReader added " + length + " bytes to the ring buffer");
                }
                catch (SocketException socketException)
                {
                    if (debug)
                        Debug.Log(Now() + "SocketReader exception when during receive: " + socketException);
                }
            }
        }

        private void ThreadFunctionTCP()
        {
            if (debug)
                Debug.Log(Now() + "SocketReader using TCP");

            Byte[] readBuffer = new Byte[_readBufferSizeBytes];
            while (true)
            {
                try
                {
                    if (debug)
                        Debug.Log(Now() + "SocketReader trying to connect to server '" + _hostname + "' port " + _port);

                    _clientSocket = new TcpClient(_hostname, _port);
                    try
                    {
                        using (NetworkStream stream = _clientSocket.GetStream())
                        {
                            if (debug)
                                Debug.Log(Now() + "SocketReader got stream connection to server '" + _hostname + "' port " + _port);

                            int length;
                            while ((length = stream.Read(readBuffer, 0, readBuffer.Length)) != 0)
                            {
                                if (debugSlowly)
                                    Debug.Log("SocketReader read " + length + " bytes");

                                _ringBuffer.Give(readBuffer);
                                Array.Clear(readBuffer, 0, length);

                                if (debugSlowly)

                                    Debug.Log("SocketReader added " + length + " bytes to the ring buffer");
                            }
                        }
                    }
                    catch (SocketException socketException)
                    {
                        if (debug)
                            Debug.Log(Now() + "SocketReader reading socket exception: " + socketException);
                    }
                }
                catch (SocketException socketException)
                {
                    if (debug)
                    {
                        Debug.Log(Now() + "SocketReader connection socket exception: " + socketException);
                        Debug.Log(Now() + "SocketReader sleeping for " + _connectRetryMs + " ms before retrying");
                    }

                    System.Threading.Thread.Sleep(_connectRetryMs);
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

        private static System.Threading.Thread _thread;

        private string _hostname;
        private int _port;
        private int _connectRetryMs;
        private int _readBufferSizeBytes;
        private int _readBufferCount;

        private TcpClient _clientSocket;

        private RingBuffer _ringBuffer;
    }
}
