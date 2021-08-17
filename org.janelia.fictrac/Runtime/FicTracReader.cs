using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    public class FicTracReader
    {
        // https://github.com/rjdmoore/fictrac/blob/master/doc/data_header.txt
        public struct Message
        {
            public long frame;
            public Vector3 deltaRotCam;
            public float deltaRotErr;
            public Vector3 deltaRotLab;
            public Vector3 absRotCam;
            public Vector3 absRotLab;
            public Vector2 integrPosLab;
            public float integrAnimalHeadingLab;
            public float animalMoveDirLab;
            public float animalMoveSpeed;
            public Vector2 integrMove;
            public long timestamp;
            public long seqCounter;
            public long deltaTimestamp;
            public long altTimestamp;
        }

        public FicTracReader(string server = "127.0.0.1", int port = 2000, int readBufferSizeBytes = 1024)
        {
            _socketMessageReader = new SocketMessageReader(HEADER, server, port, readBufferSizeBytes);
        }

        public void Start()
        {
            _socketMessageReader.Start();
        }

        public bool GetNextMessage(ref Message message)
        {
            Byte[] dataFromSocket = null;
            long dataTimestampMs = 0;
            int i0 = -1;
            if (_socketMessageReader.GetNextMessage(ref dataFromSocket, ref dataTimestampMs, ref i0))
            {
                bool valid = true;

                int i1 = 0, len1 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 1, ref i1, ref len1);
                long v1 = IoUtilities.ParseLong(dataFromSocket, i1, len1, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 1");
                    return false;
                }

                message.frame = v1;

                int i2 = 0, len2 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 2, ref i2, ref len2);
                float v2 = (float)IoUtilities.ParseDouble(dataFromSocket, i2, len2, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 2");
                    return false;
                }

                int i3 = 0, len3 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 3, ref i3, ref len3);
                float v3 = (float)IoUtilities.ParseDouble(dataFromSocket, i3, len3, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 3");
                    return false;
                }

                int i4 = 0, len4 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 4, ref i4, ref len4);
                float v4 = (float)IoUtilities.ParseDouble(dataFromSocket, i4, len4, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 4");
                    return false;
                }

                message.deltaRotCam = new Vector3(v2, v3, v4);

                int i5 = 0, len5 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 5, ref i5, ref len5);
                float v5 = (float)IoUtilities.ParseDouble(dataFromSocket, i5, len5, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 5");
                    return false;
                }

                message.deltaRotErr = v5;

                int i6 = 0, len6 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 6, ref i6, ref len6);
                float v6 = (float)IoUtilities.ParseDouble(dataFromSocket, i6, len6, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 6");
                    return false;
                }

                int i7 = 0, len7 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 7, ref i7, ref len7);
                float v7 = (float)IoUtilities.ParseDouble(dataFromSocket, i7, len7, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 7");
                    return false;
                }

                int i8 = 0, len8 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 8, ref i8, ref len8);
                float v8 = (float)IoUtilities.ParseDouble(dataFromSocket, i8, len8, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 8");
                    return false;
                }

                message.deltaRotLab = new Vector3(v6, v7, v8);

                int i9 = 0, len9 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 9, ref i9, ref len9);
                float v9 = (float)IoUtilities.ParseDouble(dataFromSocket, i9, len9, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 9");
                    return false;
                }

                int i10 = 0, len10 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 10, ref i10, ref len10);
                float v10 = (float)IoUtilities.ParseDouble(dataFromSocket, i10, len10, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 10");
                    return false;
                }

                int i11 = 0, len11 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 11, ref i11, ref len11);
                float v11 = (float)IoUtilities.ParseDouble(dataFromSocket, i11, len11, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 11");
                    return false;
                }

                message.absRotCam = new Vector3(v9, v10, v11);

                int i12 = 0, len12 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 12, ref i12, ref len12);
                float v12 = (float)IoUtilities.ParseDouble(dataFromSocket, i12, len12, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 12");
                    return false;
                }

                int i13 = 0, len13 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 13, ref i13, ref len13);
                float v13 = (float)IoUtilities.ParseDouble(dataFromSocket, i13, len13, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 13");
                    return false;
                }

                int i14 = 0, len14 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 14, ref i14, ref len14);
                float v14 = (float)IoUtilities.ParseDouble(dataFromSocket, i14, len14, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 14");
                    return false;
                }

                message.absRotLab = new Vector3(v12, v13, v14);

                int i15 = 0, len15 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 15, ref i15, ref len15);
                float v15 = (float)IoUtilities.ParseDouble(dataFromSocket, i15, len15, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 15");
                    return false;
                }

                int i16 = 0, len16 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 16, ref i16, ref len16);
                float v16 = (float)IoUtilities.ParseDouble(dataFromSocket, i16, len16, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 16");
                    return false;
                }

                message.integrPosLab = new Vector2(v15, v16);

                int i17 = 0, len17 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 17, ref i17, ref len17);
                float v17 = (float)IoUtilities.ParseDouble(dataFromSocket, i17, len17, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 17");
                    return false;
                }

                message.integrAnimalHeadingLab = v17;

                int i18 = 0, len18 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 18, ref i18, ref len18);
                float v18 = (float)IoUtilities.ParseDouble(dataFromSocket, i18, len18, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 18");
                    return false;
                }

                message.animalMoveDirLab = v18;

                int i19 = 0, len19 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 19, ref i19, ref len19);
                float v19 = (float)IoUtilities.ParseDouble(dataFromSocket, i19, len19, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 19");
                    return false;
                }

                message.animalMoveSpeed = v19;

                int i20 = 0, len20 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 20, ref i20, ref len20);
                float v20 = (float)IoUtilities.ParseDouble(dataFromSocket, i20, len20, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 20");
                    return false;
                }

                int i21 = 0, len21 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 21, ref i21, ref len21);
                float v21 = (float)IoUtilities.ParseDouble(dataFromSocket, i21, len21, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 21");
                    return false;
                }

                message.integrMove = new Vector2(v20, v21);

                int i22 = 0, len22 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 22, ref i22, ref len22);
                long v22 = IoUtilities.ParseLong(dataFromSocket, i22, len22, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 22");
                    return false;
                }

                message.timestamp = v22;

                int i23 = 0, len23 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 23, ref i23, ref len23);
                long v23 = IoUtilities.ParseLong(dataFromSocket, i23, len23, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 23");
                    return false;
                }

                message.seqCounter = v23;

                int i24 = 0, len24 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 24, ref i24, ref len24);
                long v24 = IoUtilities.ParseLong(dataFromSocket, i24, len24, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 24");
                    return false;
                }

                message.deltaTimestamp = v24;

                int i25 = 0, len25 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 25, ref i25, ref len25);
                long v25 = IoUtilities.ParseLong(dataFromSocket, i25, len25, ref valid);
                if (!valid)
                {
                    Debug.Log("FicTracReader.GetNextMessage() failed to parse column 25");
                    return false;
                }

                message.altTimestamp = v25;

                return true;
            }
            return false;
        }

        public void OnDisable()
        {
            _socketMessageReader.OnDisable();
        }

        private SocketMessageReader.Delimiter HEADER = SocketMessageReader.Header((Byte)'F');
        private const Byte SEPARATOR = (Byte)',';

        private SocketMessageReader _socketMessageReader;
    }
}
