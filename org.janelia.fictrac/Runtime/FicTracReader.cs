using System;
using System.Collections.Generic;

namespace Janelia
{
    public class FicTracReader
    {
        public FicTracReader(string server = "127.0.0.1", int port = 2000, int readBufferSize = 1024)
        {
            _socketReader = new SocketReader(server, port, readBufferSize);
            _socketReaderMessage = new Byte[readBufferSize];
        }

        public void Start()
        {
            _socketReader.Start();
        }

        public bool GetNextMessage(ref Byte[] dataFromSocket, ref long dataTimestampMs, ref int indexInData)
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
                    return false;
                }
                _currentDataIndicesIndex = 0;
            }
            dataFromSocket = _socketReaderMessage;
            dataTimestampMs = _socketReaderTimestampMs;
            indexInData = _currentDataIndices[_currentDataIndicesIndex++];
            return true;
        }

        public void OnDisable()
        {
            _socketReader.OnDisable();
        }

        private void SeparateMessages(Byte[] dataFromSocket, ref List<int> indices)
        {
            indices.Clear();
            for (int i = 0; i < dataFromSocket.Length; i++)
            {
                // Each FicTrac message starts with "FT, ".
                if (dataFromSocket[i] == 'F')
                {
                    indices.Add(i);
                }
            }
        }

        private SocketReader _socketReader;
        private bool _socketReaderHadMessage = false;
        private Byte[] _socketReaderMessage;
        private long _socketReaderTimestampMs;
        private List<int> _currentDataIndices = new List<int>();
        private int _currentDataIndicesIndex = -1;
    }
}
