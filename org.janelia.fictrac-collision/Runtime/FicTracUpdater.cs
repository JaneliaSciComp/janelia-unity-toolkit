﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    // Gets FicTrac input and converts it to kinematic motion, for use by a subclass
    // of `Janelia.KinematicSubject`, from the org.janelia.collision-handling package.
    // The direction for forward motion is the positive local X axis.
    // The up direction is the positive local Y axis.

    public class FicTracUpdater : KinematicSubject.IKinematicUpdater
    {
        public string ficTracServerAddress = "127.0.0.1";
        public int ficTracServerPort = 2000;

        public float ficTracBallRadius = 0.5f;
        public int smoothingCount = 3;

        // The size in bytes of one item in the buffer of FicTrac messages.
        public int ficTracBufferSize = 1024;
        // The number of items in the buffer of FicTrac messages.
        public int ficTracBufferCount = 240;

        // For detecting periods of chaotic motion from FicTrac, when the heading changes
        // with an angular speed above a threshold.
        public FicTracSpinThresholder thresholder;

        public bool logFicTracMessages = false;

        // Setting this flag to `true` will reduce performance.
        public bool debugSlowly = false;

        public void Start()
        {
            _dataForSmoothing = new Vector3[smoothingCount];

            _currentFicTracParametersLog.ficTracServerAddress = ficTracServerAddress;
            _currentFicTracParametersLog.ficTracServerPort = ficTracServerPort;
            _currentFicTracParametersLog.ficTracBallRadius = ficTracBallRadius;
            _currentFicTracParametersLog.ficTracSmoothingCount = smoothingCount;
            Logger.Log(_currentFicTracParametersLog);

            _socketMessageReader = new SocketMessageReader(HEADER, ficTracServerAddress, ficTracServerPort,
                                                           ficTracBufferSize, ficTracBufferCount);
            _socketMessageReader.Start();
        }

        public void Update()
        {
            _deltaRotationVectorLabUpdated = Vector3.zero;

            // Each message from FicTrac is a string of data values ("columns") separted by commas.
            // The standard C# code for reading messages from a socket and extracting some of the
            // columns generates a lot of temporary strings, and thus triggers garbarge collection.
            // As an alternative, `SocketMessageReader` provides a `GetNextMessage` routine that reuses
            // an internal `Byte[]` and sets the `ref i0` argument to the index where the next
            // message begins.

            Byte[] dataFromSocket = null;
            long dataTimestampMs = 0;
            int i0 = -1;
            while (_socketMessageReader.GetNextMessage(ref dataFromSocket, ref dataTimestampMs, ref i0))
            {
                bool valid = true;

                // Then the indices, relative to `i0`, of individual data columns can be found
                // with `IoUtilities.NthSplit`, and the numerical values can be parsed
                // the indices using `IoUtilities.ParseDouble` and `IoUtilities.ParseLong`.
                // The FicTrac documentation indicates the columns of interest.

                // https://github.com/rjdmoore/fictrac/blob/master/doc/data_header.txt
                // COL     PARAMETER                       DESCRIPTION
                // 1       frame counter                   Corresponding video frame(starts at #1).
                // 6-8     delta rotation vector (lab)     Change in orientation since last frame,
                //                                         represented as rotation angle / axis(radians)
                //                                         in laboratory coordinates(see
                //                                         * configImg.jpg).

                int i6 = 0, len6 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 6, ref i6, ref len6);
                float a = (float)IoUtilities.ParseDouble(dataFromSocket, i6, len6, ref valid);
                if (!valid)
                    break;

                int i7 = 0, len7 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 7, ref i7, ref len7);
                float b = (float)IoUtilities.ParseDouble(dataFromSocket, i7, len7, ref valid);
                if (!valid)
                    break;

                int i8 = 0, len8 = 0;
                IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 8, ref i8, ref len8);
                float c = (float)IoUtilities.ParseDouble(dataFromSocket, i8, len8, ref valid);
                if (!valid)
                    break;

                float s = Mathf.Rad2Deg;
                float heading = c * s;
                thresholder.UpdateRelative(heading, Time.deltaTime);
                if (thresholder.angularSpeed < thresholder.threshold)
                {
                    _deltaRotationVectorLabToSmooth.Set(a, b, c);
                    Smooth();
                    _deltaRotationVectorLabUpdated += _deltaRotationVectorLabToSmooth;
                }
                else
                {
                     thresholder.Log();
                }

                if (logFicTracMessages)
                {
                    int i22 = 0, len22 = 0;
                    IoUtilities.NthSplit(dataFromSocket, SEPARATOR, i0, 22, ref i22, ref len22);
                    long timestampWrite = IoUtilities.ParseLong(dataFromSocket, i22, len22, ref valid);
                    if (!valid)
                        break;

                    _currentFicTracMessageLog.ficTracTimestampWriteMs = timestampWrite;
                    _currentFicTracMessageLog.ficTracTimestampReadMs = dataTimestampMs;
                    _currentFicTracMessageLog.ficTracDeltaRotationVectorLab.Set(a, b, c);
                    Logger.Log(_currentFicTracMessageLog);
                }
            }
        }

        // https://www.researchgate.net/figure/Visual-output-from-the-FicTrac-software-see-supplementary-video-a-A-segment-of-the_fig2_260044337
        // Rotation about `a_x` is sideways translation
        // Rotation about `a_y` is forward/backward translation
        // Rotation about `a_z` is heading change

        public Vector3? Translation()
        {
            float s = ficTracBallRadius;
            float forward = _deltaRotationVectorLabUpdated[1] * s;
            float sideways = _deltaRotationVectorLabUpdated[0] * s;
            return new Vector3(forward, 0, sideways);
        }

        public Vector3? RotationDegrees()
        {
            float s = Mathf.Rad2Deg;
            float heading = _deltaRotationVectorLabUpdated[2] * s;
            return new Vector3(0, -heading, 0);
        }

        public void OnDisable()
        {
            _socketMessageReader.OnDisable();
        }

        private void Smooth()
        {
            _dataForSmoothing[_dataForSmoothingOldestIndex] = _deltaRotationVectorLabToSmooth;
            _dataForSmoothingOldestIndex = (_dataForSmoothingOldestIndex + 1) % smoothingCount;

            _deltaRotationVectorLabToSmooth = Vector3.zero;
            foreach (Vector3 value in _dataForSmoothing)
            {
                _deltaRotationVectorLabToSmooth += value;
            }
            _deltaRotationVectorLabToSmooth /= smoothingCount;
        }

        private SocketMessageReader.Delimiter HEADER = SocketMessageReader.Header((Byte)'F');
        private const Byte SEPARATOR = (Byte)',';    
        SocketMessageReader _socketMessageReader;

        private Vector3[] _dataForSmoothing;
        private int _dataForSmoothingOldestIndex = 0;
        private Vector3 _deltaRotationVectorLabToSmooth = new Vector3();

        private Vector3 _deltaRotationVectorLabUpdated = new Vector3();

        // To make `Janelia.Logger.Log<T>()`'s call to JsonUtility.ToJson() work correctly,
        // the `T` must be marked `[Serlializable]`, but its individual fields need not be
        // marked `[SerializeField]`.  The individual fields must be `public`, though.

        [Serializable]
        private class FicTracParametersLog : Logger.Entry
        {
            public string ficTracServerAddress;
            public int ficTracServerPort;
            public float ficTracBallRadius;
            public int ficTracSmoothingCount;
        };
        private FicTracParametersLog _currentFicTracParametersLog = new FicTracParametersLog();

        [Serializable]
        private class FicTracMessageLog : Logger.Entry
        {
            public long ficTracTimestampWriteMs;
            public long ficTracTimestampReadMs;
            public Vector3 ficTracDeltaRotationVectorLab;
        };
        private FicTracMessageLog _currentFicTracMessageLog = new FicTracMessageLog();
    }
}
