using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Janelia
{
    // An example of how to use `RtdeClient` to get messages from a Universal Robots
    // device (i.e., robotic arm, a.k.a. controller or server).  In particular, the
    // messages are requested to include the angles for the six joints in the arm.
    // Then the joint angles are applied to the transform hierarchy below the object
    // having this script: joint angle `N` is applied to desendant `ArmNRotateTransform`.
    // The rotation axes are deterimed by `rotationAxes`.  This script can be applied
    // to multiple Unity hierarchies, each communicating with a different RTDE server
    // using a distinct port number.

    // Note that the RTDE server must be started first, before the application using
    // this code starts running.

    public class ExampleUsingRtde : MonoBehaviour
    {
        public string rtdeServerAddress = "127.0.0.1";
        public int rtdeServerPort = 2000;
        public string rotationAxes = "x,x,x,z,x,x";
        public int maximumFrame = 2000;
        public bool debug = false;

        public void Start()
        {
            _rtdeClient = new RtdeClient(rtdeServerAddress, rtdeServerPort);
            _rtdeStarted = _rtdeClient.Start();

            if (!_rtdeStarted)
            {
                Debug.Log("ExampleUsingRtde: could not make RTDE connection.");
            }

            string axesNoWhite = Regex.Replace(rotationAxes.ToLower(), @"\s+", "");
            string axesNoWhiteOrPunc = Regex.Replace(axesNoWhite, @"[\.,;-]", "");
            string axes = axesNoWhiteOrPunc.PadRight(RtdeClient.JOINT_ANGLE_COUNT, 'x');
            for (int i = 0; i < RtdeClient.JOINT_ANGLE_COUNT; ++i)
            {
                _rotationAxes[i] = (axes[i] == 'z') ? RotationAxis.Z : (axes[i] == 'y') ? RotationAxis.Y : RotationAxis.X; 
            }

            // For performance measurement.

            Resolution current = Screen.currentResolution;
            int refreshRateHz = current.refreshRate;
            _deltaTimeTarget = 1.0f / refreshRateHz;
            _deltaTimePlus1msTarget = _deltaTimeTarget + 0.001f;

            _deltaTimePlus1msTargetExceededCount = 0;
            _deltaTimePlus1msTargetExceededSum = 0;
        }

        public void Update()
        {
            // Apply the joint angles read with RTDE.

            if (!_rtdeStarted)
            {
                return;
            }

            while (_rtdeClient.GetNextMessage(ref _rtdeMessage))
            {
                if (debug)
                {
                    string msg = "As of " + _rtdeMessage.timestampMs + "ms, RTDE joint angles: ";
                    foreach (double angle in _rtdeMessage.jointAngles)
                    {
                        msg += angle + " ";
                    }
                    Debug.Log(msg);
                }
            }
            Rotate(transform);

            // Measure performance, specifically, some statistics about the frames more than 1 ms slower
            // than the target frame time.

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
            _rtdeClient.OnDisable();
        }

        private void Rotate(Transform parent, int i = 0)
        {
            if (i < 6)
            {
                string childName = "Arm" + (i + 1) + "RotateTransform";
                Transform child = parent.Find(childName);
                if (child != null)
                {
                    float a = (float)_rtdeMessage.jointAngles[i];
                    child.localEulerAngles = (_rotationAxes[i] == RotationAxis.X) ? new Vector3(a, 0, 0)
                        : (_rotationAxes[i] == RotationAxis.Y) ? new Vector3(0, a, 0) 
                        : new Vector3(0, 0, a);
                    Rotate(child, i + 1);
                }
            }
        }

        private RtdeClient _rtdeClient;
        private bool _rtdeStarted;
        private RtdeClient.Message _rtdeMessage = new RtdeClient.Message();
        private enum RotationAxis { X, Y, Z };
        private RotationAxis[] _rotationAxes = new RotationAxis[RtdeClient.JOINT_ANGLE_COUNT];

        // For performance measurement.

        private float _deltaTimeTarget;
        private float _deltaTimePlus1msTarget;
        private int _deltaTimePlus1msTargetExceededCount = 0;
        private float _deltaTimePlus1msTargetExceededSum = 0;
    }
}
