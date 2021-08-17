using System;
using System.Text;
using UnityEngine;

namespace Janelia
{
    public class ExampleReadingSocket : MonoBehaviour
    {
        public string serverAddress = "127.0.0.1";
        public int serverPort = 2000;
        public int readBufferSizeBytes = 1024;
        public int readBufferCount = 240;
        public bool useUDP = true;
        public bool useJson = false;
        public int maximumFrame = 10000;

        public void Start()
        {
            // JSON messages are terminated by newline characters (ASCII 10), but the ad hoc messages
            // have the header character 'J'.
            SocketMessageReader.Delimiter delim = (useJson) ?
                SocketMessageReader.Terminator((Byte)10) : 
                SocketMessageReader.Header((Byte)'J');
            _socketMessageReader = new SocketMessageReader(delim, serverAddress, serverPort, 
                                                           readBufferSizeBytes, readBufferCount, useUDP);
            _socketMessageReader.Start();

            Resolution current = Screen.currentResolution;
            int refreshRateHz = current.refreshRate;
            _deltaTimeTarget = 1.0f / refreshRateHz;
            _deltaTimePlus1msTarget = _deltaTimeTarget + 0.001f;

            _deltaTimePlus1msTargetExceededCount = 0;
            _deltaTimePlus1msTargetExceededSum = 0;
        }

        public void Update()
        {
            // The standard C# code for reading messages from a socket and extracting some of the
            // columns generates temporary strings, and thus triggers garbarge collection.
            // As an alternative, `SocketMessageReader` provides a `GetNextMessage` routine that reuses
            // an internal `Byte[]` and sets the `ref i0` argument to the index where the next
            // message begins.

            // `SocketMessageReader` has a thread that reads messages from the sender as fast as it can,
            // and puts them in a ring buffer.  Using a `while` loop with `GetNextMessage` gets all the
            // unprocessed messages in the buffer.  In this particular example, the messages contain
            // absolute positions and rotations, so all that is really needed is the most recent
            // unprocessed message.  But in cases where the messages contain relative positions and
            // rotations, the loop would need to sum all the unprocessed messages.

            Byte[] dataFromSocket = null;
            long dataTimestampMs = 0;
            int i0 = -1, len = -1;
            while (_socketMessageReader.GetNextMessage(ref dataFromSocket, ref dataTimestampMs, ref i0, ref len))
            {
                if (useJson)
                {
                    ParseJson(dataFromSocket, i0, len);
                }
                else
                {
                    ParseAdHoc(dataFromSocket, i0);
                }

                transform.position = _position;
                transform.eulerAngles = _rotation;
                transform.localScale = _scale;
            }

            if (Time.deltaTime > _deltaTimePlus1msTarget)
            {
                _deltaTimePlus1msTargetExceededCount++;
                _deltaTimePlus1msTargetExceededSum += (Time.deltaTime - _deltaTimeTarget);
            }

            bool quitting = (Time.frameCount >= maximumFrame) || Input.GetKey("q") || Input.GetKey(KeyCode.Escape);

            if (Input.anyKey || quitting)
            {
                Debug.Log("Time: " + Time.time + "; frame: " + Time.frameCount + "; target deltaTime: " + _deltaTimeTarget);
                Debug.Log("Count exceeding target + 1 ms: " + _deltaTimePlus1msTargetExceededCount);
                float avg = _deltaTimePlus1msTargetExceededSum / _deltaTimePlus1msTargetExceededCount;
                Debug.Log("Sum excess: " + _deltaTimePlus1msTargetExceededSum + " sec");
                Debug.Log("Average excess: " + avg + " sec");
            }

            if (quitting)
            {
                Application.Quit();
            }
        }

        public void OnDisable()
        {
            _socketMessageReader.OnDisable();
        }
        private bool ParseJson(Byte[] dataFromSocket, int i0, int len)
        {
            string jsonStr = _asciiEncoding.GetString(dataFromSocket, i0, len);

            // Unity's JSON utilities cannot deserialize an arbitrary set of keys and values.
            // So the approach is to require that every type of message have a `type` string,
            // obtained by an initial deserialization.  Then, based on the value of `type,
            // a second deserialization recovers the message with a class of the appropriate
            // format.

            JsonUtility.FromJsonOverwrite(jsonStr, _jsonMessageType);
            switch (_jsonMessageType.type)
            {
                case "pose":
                    JsonUtility.FromJsonOverwrite(jsonStr, _jsonMessagePose);
                    _position = _jsonMessagePose.pos;
                    _rotation = _jsonMessagePose.rot;
                    return true;
                case "scale":
                    JsonUtility.FromJsonOverwrite(jsonStr, _jsonMessageScale);
                    _scale.x = _jsonMessageScale.scale;
                    return true;
                default:
                    return false;
            }
        }

        private bool ParseAdHoc(Byte[] dataFromSocket, int i0)
        {
            bool valid = true;

            // Then the indices, relative to `i0`, of individual data columns can be found
            // with `IoUtilities.NthSplit`, and the numerical values can be parsed
            // the indices using `IoUtilities.ParseDouble` and `IoUtilities.ParseLong`.

            // The first data column is a timestamp, in milliseconds since the epoch.

            int whichTimestamp = 1, iTimestamp = 0, lenTimestamp = 0;
            IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, whichTimestamp, ref iTimestamp, ref lenTimestamp);
            long writeTimestampMs = IoUtilities.ParseLong(dataFromSocket, iTimestamp, lenTimestamp, ref valid);
            if (!valid)
                return false;

            // Then there is the position, in world space.

            int whichPosX = 2, iPosX = 0, lenPosX = 0;
            IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, whichPosX, ref iPosX, ref lenPosX);
            float posX = (float)IoUtilities.ParseDouble(dataFromSocket, iPosX, lenPosX, ref valid);
            if (!valid)
                return false;

            int whichPosY = 3, iPosY = 0, lenPosY = 0;
            IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, whichPosY, ref iPosY, ref lenPosY);
            float posY = (float)IoUtilities.ParseDouble(dataFromSocket, iPosY, lenPosY, ref valid);
            if (!valid)
                return false;

            int whichPosZ = 4, iPosZ = 0, lenPosZ = 0;
            IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, whichPosZ, ref iPosZ, ref lenPosZ);
            float posZ = (float)IoUtilities.ParseDouble(dataFromSocket, iPosZ, lenPosZ, ref valid);
            if (!valid)
                return false;

            // Then there is the rotation, in degrees.

            int whichRotX = 5, iRotX = 0, lenRotX = 0;
            IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, whichRotX, ref iRotX, ref lenRotX);
            float rotX = (float)IoUtilities.ParseDouble(dataFromSocket, iRotX, lenRotX, ref valid);
            if (!valid)
                return false;

            int whichRotY = 6, iRotY = 0, lenRotY = 0;
            IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, whichRotY, ref iRotY, ref lenRotY);
            float rotY = (float)IoUtilities.ParseDouble(dataFromSocket, iRotY, lenRotY, ref valid);
            if (!valid)
                return false;

            int whichRotZ = 7, iRotZ = 0, lenRotZ = 0;
            IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, whichRotZ, ref iRotZ, ref lenRotZ);
            float rotZ = (float)IoUtilities.ParseDouble(dataFromSocket, iRotZ, lenRotZ, ref valid);
            if (!valid)
                return false;

            _position.Set(posX, posY, posZ);
            _rotation.Set(rotX, rotY, rotZ);

            return true;
        }

        private SocketMessageReader _socketMessageReader;
        private const Byte SEPARATOR = (Byte)' ';

        // Used to parse just the `type` field of a JSON message.
        [Serializable]
        private class JsonMessageType
        {
            public string type;
        }
        private JsonMessageType _jsonMessageType = new JsonMessageType();


        [Serializable]
        private class JsonMessagePose
        {
            public string type;
            public long timestampMs;
            public Vector3 pos;
            public Vector3 rot;
        }
        private JsonMessagePose _jsonMessagePose = new JsonMessagePose();


        [Serializable]
        private class JsonMessageScale
        {
            public string type;
            public long timestampMs;
            public float scale;
        }
        private JsonMessageScale _jsonMessageScale = new JsonMessageScale();

        private ASCIIEncoding _asciiEncoding = new ASCIIEncoding();

        private Vector3 _position = new Vector3();
        private Vector3 _rotation = new Vector3();
        private Vector3 _scale = Vector3.one;

        private float _deltaTimeTarget;
        private float _deltaTimePlus1msTarget;
        private int _deltaTimePlus1msTargetExceededCount = 0;
        private float _deltaTimePlus1msTargetExceededSum = 0;
    }
}
