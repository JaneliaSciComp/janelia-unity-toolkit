using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    // Gets jetTrac input and converts it to kinematic motion, for use by a subclass
    // of `Janelia.KinematicSubject`, from the org.janelia.collision-handling package.
    // The direction for forward motion is the positive local X axis.
    // The up direction is the positive local Y axis.
    // Assumes this script is attached to a `GameObject` with a child named `Head`
    // whose local `Y` rotation is 90 degrees, so that if the main camera is a child
    // of `Head` then it will be looking in the direction of forward motion 
    // (i.e. the camera's positive Z axis will be rotated to match the main `GameObject`'s
    // positive X axis) for the jETTrac's head rotation at start up.

    public class JetTracUpdater : KinematicSubject.IKinematicUpdater
    {    
        public float headRotationYDegs0 = 0;
        public bool readHead = true;
        public Transform headTransform = null;

        public bool allowBodyRotation = true;
        public bool smooth = true;
        public int smoothingWindow = 3;
        public bool logJetTracMessages = false;
        public bool debug = false;

        public void Start()
        {
            _headRotation.y = headRotationYDegs0;
            _transformer = new JetTracTransformer(headRotationYDegs0, smoothingWindow);

            _reader = new JetTracReader();
            _reader.debug = debug;
            _reader.Start();

            LogParameters();
        }

        public void Update()
        {
            _bodyPositionPrev = _bodyPosition;
            _bodyRotationPrev = _bodyRotation;

            JetTracParser.BallMessage ballMessage = new JetTracParser.BallMessage();
            while (_reader.GetNextBallMessage(ref ballMessage))
            {
                if (debug)
                {
                    Debug.Log(Now() + "ball: " + ballMessage.deviceTimestampUs + " us, " + ballMessage.x0 + ", "
                        + ballMessage.y0 + ", " + ballMessage.x1 + ", " + ballMessage.y1 + ", smooth " + smooth);
                }

                _transformer.AddInput(ballMessage);

                if (logJetTracMessages)
                {
                    Log(ballMessage);
                }
            }
            if (allowBodyRotation)
            {
                _transformer.Update(ref _bodyPosition, ref _bodyRotation.y, smooth);
            }
            else
            {
                _transformer.Update(ref _bodyPosition, smooth);
            }

            if (debug)
            {
                Debug.Log(Now() + "body pos " + _bodyPosition + ", body rot " + _bodyRotation.y 
                    + ", head rot " + _headRotation.y);
            }

            if (readHead)
            {
                JetTracParser.HeadMessage headMessage = new JetTracParser.HeadMessage();
                while (_reader.GetNextHeadMessage(ref headMessage))
                {
                    if (debug)
                    {
                        Debug.Log(Now() + "head: " + headMessage.deviceTimestampUs + " us, " 
                            + headMessage.angleDegs);
                    }

                    _transformer.AddInput(headMessage);
                   
                    if (logJetTracMessages)
                    {
                        Log(headMessage);
                    }
                }
                _transformer.Update(ref _headRotation.y, smooth);

                if (debug)
                {
                    Debug.Log(Now() + "body pos " + _bodyPosition + ", body rot " + _bodyRotation.y 
                        + ", head rot " + _headRotation.y);
                }
            }
        }

        public Vector3? Translation()
        {
            return _bodyPosition - _bodyPositionPrev;
        }

        public Vector3? RotationDegrees()
        {
            return _bodyRotation - _bodyRotationPrev;
        }

        // Not part of the standard `KinematicSubject.IKinematicUpdater`.
        public Vector3 HeadRotationDegrees()
        {
            return _headRotation;
        }

        public void OnDisable()
        {
            _reader.OnDisable();
        }

        private void LogParameters()
        {
            _currentJetTracParametersLog.jetTrac_headRotationYDegs0 = headRotationYDegs0;
            _currentJetTracParametersLog.jetTrac_readHead = readHead;
            _currentJetTracParametersLog.jetTrac_allowBodyRotation = allowBodyRotation;
            _currentJetTracParametersLog.jetTrac_smooth = smooth;
            _currentJetTracParametersLog.jetTrac_smoothingWindow = smoothingWindow;
            _currentJetTracParametersLog.jetTrac_ballDeviceNumber = _reader.BallDeviceNumber;
            _currentJetTracParametersLog.jetTrac_headDeviceNumber = _reader.HeadDeviceNumber;
            Logger.Log(_currentJetTracParametersLog);
        }

        private void Log(JetTracParser.BallMessage msg)
        {
            _currentJetTracBallMessageLog.jetTrac_readTimestampMs = msg.readTimestampMs;
            _currentJetTracBallMessageLog.jetTrac_deviceTimestampUs = msg.deviceTimestampUs;
            _currentJetTracBallMessageLog.jetTrac_x0 = msg.x0;
            _currentJetTracBallMessageLog.jetTrac_y0 = msg.y0;
            _currentJetTracBallMessageLog.jetTrac_x1 = msg.x1;
            _currentJetTracBallMessageLog.jetTrac_y1 = msg.y1;
            Logger.Log(_currentJetTracBallMessageLog);
        }

        private void Log(JetTracParser.HeadMessage msg)
        {
            _currentJetTracHeadMessageLog.jetTrac_readTimestampMs = msg.readTimestampMs;
            _currentJetTracHeadMessageLog.jetTrac_deviceTimestampUs = msg.deviceTimestampUs;
            _currentJetTracHeadMessageLog.jetTrac_angleDegs = msg.angleDegs;
            Logger.Log(_currentJetTracHeadMessageLog);

        }

        private string Now()
        {
            if (Application.isEditor)
            {
                return "";
            }
            return "[" + Time.frameCount + ", " + Time.time + "] ";
        }

        private JetTracReader _reader;
        private JetTracTransformer _transformer;
        private Vector3 _bodyPosition;
        private Vector3 _bodyPositionPrev;
        private Vector3 _bodyRotation;
        private Vector3 _bodyRotationPrev;
        private Vector3 _headRotation;
        
        // To make `Janelia.Logger.Log<T>()`'s call to JsonUtility.ToJson() work correctly,
        // the `T` must be marked `[Serlializable]`, but its individual fields need not be
        // marked `[SerializeField]`.  The individual fields must be `public`, though.

        [Serializable]
        private class JetTracParametersLog : Logger.Entry
        {
            public float jetTrac_headRotationYDegs0;
            public bool jetTrac_readHead;
            public bool jetTrac_allowBodyRotation;
            public bool jetTrac_smooth;
            public int jetTrac_smoothingWindow;
            public int jetTrac_ballDeviceNumber;
            public int jetTrac_headDeviceNumber;
        };
        private JetTracParametersLog _currentJetTracParametersLog = new JetTracParametersLog();

        [Serializable]
        private class JetTracBallMessageLog : Logger.Entry
        {
            public UInt64 jetTrac_readTimestampMs;
            public UInt64 jetTrac_deviceTimestampUs;
            public Int32 jetTrac_x0;
            public Int32 jetTrac_y0;
            public Int32 jetTrac_x1;
            public Int32 jetTrac_y1;
        };
        private JetTracBallMessageLog _currentJetTracBallMessageLog = new JetTracBallMessageLog();

        [Serializable]
        private class JetTracHeadMessageLog : Logger.Entry
        {
            public UInt64 jetTrac_readTimestampMs;
            public UInt64 jetTrac_deviceTimestampUs;
            public float jetTrac_angleDegs;
        };
        private JetTracHeadMessageLog _currentJetTracHeadMessageLog = new JetTracHeadMessageLog();
    }
}
