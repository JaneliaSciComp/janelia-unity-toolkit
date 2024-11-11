using System;
using System.Collections.Generic;

// Uses `SocketReader` to provide a higher-level API, with the concept of "messages".
// Avoids creating temporary strings that would trigger garbage collection.

namespace Janelia
{
    public class SocketMessageReader
    {
        // The message delimiter, either a header characterat the start (e.g., the 'F' at the start of a
        // FicTrac message) or a terminator character at the end (e.g., a newline after a string 
        // representing JSON).
        public struct Delimiter
        {
            public enum Type { Header, Terminator };
            public Type type;
            public Byte character;
            public Delimiter(Type t, Byte c)
            {
                type = t;
                character = c;
            }
        }

        public static Delimiter Header(Byte c)
        {
            return new Delimiter(Delimiter.Type.Header, c);
        }

        public static Delimiter Terminator(Byte c)
        {
            return new Delimiter(Delimiter.Type.Terminator, c);
        }

        // Create a reader for messages that start with the specified delimiter.
        public SocketMessageReader(Delimiter delim, string server = "127.0.0.1", int port = 2000, 
                                   int readBufferSizeBytes = 1024, int readBufferCount = 240, bool useUDP = true)
        {
            _delimiter = delim;
            _socketReader = new SocketReader(server, port, readBufferSizeBytes, readBufferCount, useUDP);
            _socketReaderMessage = new Byte[readBufferSizeBytes];
        }

        public void ForwardTo(string hostname = "127.0.0.1", int port = 2100)
        {
            _socketReader.ForwardTo(hostname, port);
        }

        public void Start()
        {
            _socketReader.Start();
        }

        public bool GetNextMessage(ref Byte[] dataFromSocket, ref long dataTimestampMs, ref int indexInData)
        {
            int tmp = 0;
            return GetNextMessage(ref dataFromSocket, ref dataTimestampMs, ref indexInData, ref tmp);
        }

        // Get the next message that has been read from the socket.  The data of the message is returned
        // through the array `dataFromSocket`, and the actual message starts at index `indexInData` in
        // that array.  The timestamp for when the message was read (in milliseconds since the epoch)
        // is returned through `dataTimestampMs`.
        public bool GetNextMessage(ref Byte[] dataFromSocket, ref long dataTimestampMs, ref int indexInData, ref int length)
        {
            if (!_socketReaderHadMessage || (_currentDataIndicesIndex >= _currentDataIndices.Count))
            {
                _socketReaderHadMessage = _socketReader.Take(ref _socketReaderMessage, ref _socketReaderTimestampMs);
                if (!_socketReaderHadMessage)
                {
                    return false;
                }
                SeparateMessages(_socketReaderMessage, ref _currentDataIndices);
                if (_currentDataIndices.Count == 0)
                {
                    _socketReaderHadMessage = false;
                    return false;
                }
                _currentDataIndicesIndex = 0;
            }
            dataFromSocket = _socketReaderMessage;
            dataTimestampMs = _socketReaderTimestampMs;

            indexInData = _currentDataIndices[_currentDataIndicesIndex++];
            if (_currentDataIndicesIndex < _currentDataIndices.Count)
            {
                length = _currentDataIndices[_currentDataIndicesIndex] - indexInData;
            }
            else
            {
                length = dataFromSocket.Length - indexInData;
            }
            return true;
        }

        public void OnDisable()
        {
            _socketReader.OnDisable();
        }

        private void SeparateMessages(Byte[] dataFromSocket, ref List<int> indices)
        {
            indices.Clear();
            for (int i = 0, j = 0; j < dataFromSocket.Length; j++)
            {
                if (_delimiter.type == Delimiter.Type.Header)
                {
                    if (dataFromSocket[j] == _delimiter.character)
                    {
                        indices.Add(j);
                    }
                }
                else
                {
                    if (dataFromSocket[j] == _delimiter.character)
                    {
                        indices.Add(i);
                        i = j + 1;
                    }
                }
            }
        }

        private SocketReader _socketReader;
        private Delimiter _delimiter;
        private bool _socketReaderHadMessage = false;
        private Byte[] _socketReaderMessage;

        private long _socketReaderTimestampMs;
        private List<int> _currentDataIndices = new List<int>();
        private int _currentDataIndicesIndex = -1;
    }
}
