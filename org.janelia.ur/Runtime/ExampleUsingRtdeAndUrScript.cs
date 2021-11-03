// #define MEASURE_PERFORMANCE

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Janelia
{
    // An example of using `UrScriptClient` to send commands to change the state
    // of a Universal Robots device (i.e., robotic arm, a.k.a. controller or server),
    // and using `RtdeClient` to get messages about the state back from the device.
    // In particular, the state is the angles for the six joints in the arm.
    // When these joint angles are received, they are applied to the transform hierarchy
    // below the object having this script: joint angle `N` is applied to desendant 
    // `ArmN`.  The joint rotation axes are specified by `jointRotationAxes` list,
    // with the joint angles displaced by `jointAngleOffsets` list and multiplied by
    // the `jointAngleScales` list.  This script can be applied  to multiple 
    // Unity hierarchies, each communicating with a different RTDE server using a 
    // distinct port number.

    // Note that the device (server) must be started first, before the application 
    // using this code starts running.

    public class ExampleUsingRtdeAndUrScript : MonoBehaviour
    {
        // URSim offline simulator, running in Virtual Box VM
        public string serverAddress = "192.168.1.224";
        public int serverRtdePort = 30004;
        public int serverUrScriptPort = 30002;

        // These default values are empirically determined for the Janelia arena model, 
        // ca. Oct. 2021.
        public string jointRotationAxes = "y,y,y,y,y,y";
        public string jointAngleOffsets = "0,-90,0,0,-90,0";
        public string jointAngleScales = "1,1,-1,1,1,1";

        public bool debug = false;

#if MEASURE_PERFORMANCE
        public int maximumFrame = 2000;
#endif

        public void Start()
        {
            _rtdeClient = new RtdeClient(serverAddress, serverRtdePort);
            _rtdeStarted = _rtdeClient.Start();

            if (!_rtdeStarted)
            {
                Debug.Log("RobotRTDEClient: could not make RTDE connection.");
                return;
            }

            _urScriptClient = new UrScriptClient(serverAddress, serverUrScriptPort);
            _urScriptClient.debug = debug;
            _urScriptClient.Start();

            ParseJointRotationAxes();
            ParseJointAngleOffsets();
            ParseJointAngleScales();
            FindArms(transform);

#if MEASURE_PERFORMANCE
            // For performance measurement.

            Resolution current = Screen.currentResolution;
            int refreshRateHz = current.refreshRate;
            _deltaTimeTarget = 1.0f / refreshRateHz;
            _deltaTimePlus1msTarget = _deltaTimeTarget + 0.001f;

            _deltaTimePlus1msTargetExceededCount = 0;
            _deltaTimePlus1msTargetExceededSum = 0;
#endif
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
                    string msg = "As of " + _rtdeMessage.timestampMs + " ms, RTDE joint angles: ";
                    foreach (double angle in _rtdeMessage.jointAngles)
                    {
                        msg += angle + " ";
                    }
                    Debug.Log(msg);
                }
            }
            RotateArms();

            // A cheap-and-cheerful UI for choosing the URScript client to use: press the key
            // for its number (one based).

            for (int i = 0; i < UrScriptClient.Count; ++i)
            {
                int uiCode = i + 1;
                if (Input.GetKeyDown(uiCode.ToString()))
                {
                    Debug.Log("Setting current UrScriptClient to " + uiCode + " - 1 = " + i + " (zero based)");
                    UrScriptClient.SetCurrent(i);
                }
            }

#if MEASURE_PERFORMANCE
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
#else
            if (Input.GetKey("q") || Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }
#endif
        }

        public void SendCmd(string cmd)
        {
            _urScriptClient.Write(cmd);
        }

        public void OnDisable()
        {
            _rtdeClient.OnDisable();
            if (_urScriptClient != null)
            {
                _urScriptClient.OnDisable();
            }
        }

        private void ParseJointRotationAxes()
        {
            string axesNoWhite = Regex.Replace(jointRotationAxes.ToLower(), @"\s+", "");
            string axesNoWhiteOrPunc = Regex.Replace(axesNoWhite, @"[\.,;-]", "");
            string axes = axesNoWhiteOrPunc.PadRight(RtdeClient.JOINT_ANGLE_COUNT, 'x');
            for (int i = 0; i < RtdeClient.JOINT_ANGLE_COUNT; ++i)
            {
                _jointRotationAxes[i] = (axes[i] == 'z') ? RotationAxis.Z : (axes[i] == 'y') ? RotationAxis.Y : RotationAxis.X;
            }
        }

        private void ParseJointAngleOffsets()
        {
            string offsetsNoWhite = Regex.Replace(jointAngleOffsets.ToLower(), @"\s+", "");
            string[] offsets = offsetsNoWhite.Split(",");
            for (int i = 0; i < RtdeClient.JOINT_ANGLE_COUNT; ++i)
            {
                if (i < offsets.Length)
                {
                    _jointAngleOffsets[i] = float.Parse(offsets[i]);
                }
            }
        }

        private void ParseJointAngleScales()
        {
            string scalesNoWhite = Regex.Replace(jointAngleScales.ToLower(), @"\s+", "");
            string[] scales = scalesNoWhite.Split(",");        
            for (int i = 0; i < RtdeClient.JOINT_ANGLE_COUNT; ++i)
            {
                if (i < scales.Length)
                {
                    _jointAngleScales[i] = float.Parse(scales[i]);
                }
            }
        }

        private bool FindArms(Transform parent, int i = 1)
        {
            string targetName = "Arm" + i;
            for (int iChild = 0; iChild < parent.childCount; ++iChild)
            {
                Transform child = parent.GetChild(iChild);
                if (child.name == targetName)
                {
                    _arms[i - 1] = child;
                    if (i == 6)
                    {
                        return true;
                    }
                    return FindArms(child, i + 1);
                }
                if (FindArms(child, i))
                {
                    return true;
                }
            }
            return false;
        }

        private void RotateArms()
        {
            for (int i = 0; i < RtdeClient.JOINT_ANGLE_COUNT; ++i)
            {
                if (_arms[i] != null)
                {
                    float aRad = (float)_rtdeMessage.jointAngles[i];
                    float a = aRad * Mathf.Rad2Deg;

                    // Switch to Unity's left-handed coordinate system.
                    a *= -1;

                    // Apply corrections to the Unity model.
                    a *= _jointAngleScales[i];
                    a += _jointAngleOffsets[i];

                    _arms[i].localEulerAngles = (_jointRotationAxes[i] == RotationAxis.X) ? new Vector3(a, 0, 0)
                        : (_jointRotationAxes[i] == RotationAxis.Y) ? new Vector3(0, a, 0)
                        : new Vector3(0, 0, a);
                }
                else
                {
                    Debug.Log("Error: no arm " + i);
                    return;
                }
            }
        }

        private RtdeClient _rtdeClient;
        private bool _rtdeStarted;
        private RtdeClient.Message _rtdeMessage = new RtdeClient.Message();

        private UrScriptClient _urScriptClient;

        private enum RotationAxis { X, Y, Z };
        private RotationAxis[] _jointRotationAxes = new RotationAxis[RtdeClient.JOINT_ANGLE_COUNT];

        private float[] _jointAngleOffsets = new float[RtdeClient.JOINT_ANGLE_COUNT];
        private float[] _jointAngleScales = new float[RtdeClient.JOINT_ANGLE_COUNT];

        private Transform[] _arms = new Transform[RtdeClient.JOINT_ANGLE_COUNT];

#if MEASURE_PERFORMANCE
        // For performance measurement.

        private float _deltaTimeTarget;
        private float _deltaTimePlus1msTarget;
        private int _deltaTimePlus1msTargetExceededCount = 0;
        private float _deltaTimePlus1msTargetExceededSum = 0;
    #endif
    }
}
