using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace Janelia
{
    // Sends URScript commands to a Universal Robots device (i.e., robotic arm, a.k.a. controller 
    // or server) via a TCP connection.

    public class UrScriptClient
    {
        public bool debug = false;

        // Static API for accessing a particular `UrScriptClient` out of a collection 
        // (e.g., to control one of several arms).

        public static int Count
        {
            get => _clients.Count;
        }

        // Zero based
        public static bool SetCurrent(int which)
        {
            if ((0 <= which) && (which < _clients.Count))
            {
                Debug.Log("UrScriptClient.SetCurrent(" + which + ")");

                _current = which;
                return true;
            }
            return false;
        }

        public static UrScriptClient GetCurrent()
        {
            return Get(_current);
        }

        public static UrScriptClient Get(int which = 0)
        {
            return (_clients.Count > which) ? _clients[which] : null;
        }


        // The definition of the `UrScriptClient`.

        public UrScriptClient(string hostname = "192.168.1.224", int port = 30002, int connectRetryMs = 5000)
        {
            _hostname = hostname;
            _port = port;
            _connectRetryMs = connectRetryMs;
        }

        public void Start()
        {
            _clients.Add(this);

            if (debug)
                Debug.Log(Now() + "UrScriptClient.Start() creating socket thread");

            _thread = new System.Threading.Thread(ThreadFunctionTCP) { IsBackground = true };
            _thread.Start();
        }

        public void Write(string toWrite)
        {
            lock (_writeLock)
            {
                _toWrite = toWrite;

                if (debug)
                    Debug.Log(Now() + "SocketReader.Write() Monitor.Pulse");

                System.Threading.Monitor.Pulse(_writeLock);
            }
        }

        public void OnDisable()
        {
            _clients.Remove(this);
            if (_thread != null)
            {
                if (debug)
                    Debug.Log(Now() + "SocketReader.OnDisable() aborting socket thread");

                _thread.Abort();
                _thread = null;
            }
        }

        private void ThreadFunctionTCP()
        {
            while (true)
            {
                try
                {
                    if (debug)
                        Debug.Log(Now() + "UrScriptClient trying to connect to server '" + _hostname + "' port " + _port);

                    _clientSocket = new TcpClient(_hostname, _port);
                    try
                    {
                        using (NetworkStream stream = _clientSocket.GetStream())
                        {
                            if (debug)
                                Debug.Log(Now() + "UrScriptClient got stream connection to server '" + _hostname + "' port " + _port);

                            lock (_writeLock)
                            {
                                // Execution will continue here after the `Monitor.Pulse(_writeLock)`
                                // in the `Write` function on the main thread.
                                System.Threading.Monitor.Wait(_writeLock);

                                if (debug)
                                    Debug.Log(Now() + "SocketReader about to write " + _toWrite.Length + " bytes");

                                stream.Write(System.Text.Encoding.UTF8.GetBytes(_toWrite), 0, _toWrite.Length);
                            }

                        }
                    }
                    catch (SocketException socketException)
                    {
                        if (debug)
                            Debug.Log(Now() + "UrScriptClient reading socket exception: " + socketException);
                    }
                }
                catch (SocketException socketException)
                {
                    if (debug)
                    {
                        Debug.Log(Now() + "UrScriptClient connection socket exception: " + socketException);
                        Debug.Log(Now() + "UrScriptClient sleeping for " + _connectRetryMs + " ms before retrying");
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

        private string _hostname;
        private int _port;
        private int _connectRetryMs;

        private System.Threading.Thread _thread;
        private readonly object _writeLock = new object();

        private string _toWrite;

        private TcpClient _clientSocket;

        private static List<UrScriptClient> _clients = new List<UrScriptClient>();
        private static int _current = 0;
    }
}
