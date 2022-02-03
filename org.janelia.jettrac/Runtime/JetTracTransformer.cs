using System;
using UnityEngine;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Microsoft.Win32;
#endif

namespace Janelia
{
    public class JetTracTransformer
    {
        public const int DEVICE_RATE_HZ = 360;

        // This default calibration scale and the corresponding ball diameter are about right
        // for 1 Unity unit being 10 cm (0.1 m).  Note that this Unity unit is different from
        // the standard of 1 Unity unit being 1 m.  The smaller units seem to work better with
        // jETTrac, perhaps because the scale factor is bigger and not as close to the limits
        // of floating point precision.
                
        public const float DEFAULT_CALIBRATION_SCALE = 0.0005f;
        public const float DEFAULT_BALL_DIAMETER = 2.0f;

        // Indices 0 and 1 correspond to 1 and 2, respectively, in the 2010 Seelig et. al. paper.

        // Ball sensor 0 is the sensor with a direct connection to the overall USB output for the device.
        // It produces the `x0` and `y0` values.
        public float ballSensor0YAngleDegs = 135;

        // Ball sensor is the sensor connected to the sensor 2 with an internal cable.
        // It producces the `x1` and `y1` values.
        // 225 degrees corresponds to -135 degrees, as in the 2010 Seelig et. al paper.
        public float ballSensor1YAngleDegs = 225;

        public int SmoothingWindow
        {
            get => _smoothingWindow;
        }

        public JetTracTransformer(float headRotationYDegs0 = 0, int smoothWindow = 3)
        {
            _headRotationYDegs0 = headRotationYDegs0;
            _smoothingWindow = smoothWindow;

            _ballSinceLastUpdateBuffer = new JetTracRingBuffer(DEVICE_RATE_HZ, RawHidReader.READ_SIZE_BYTES);
            _ballSmoothingBuffer = new JetTracRingBuffer(_smoothingWindow, RawHidReader.READ_SIZE_BYTES);

            _headSmoothingBuffer = new JetTracRingBuffer(_smoothingWindow, RawHidReader.READ_SIZE_BYTES);

            // On Windows, retrieve the calibration scale from the registry.  On other platforms,
            // some external code must call `SetCalibrationScale(s)` for the appropriate `s`.s
            GetCalibrationScale();

            Debug.Log("JetTracTransformer calibration scale " + _calibrationScale);
        }

        public void Clear()
        {
            _ballSinceLastUpdateBuffer.Clear();
            _ballSmoothingBuffer.Clear();
            _headSmoothingBuffer.Clear();
            _lastBallReadTimestampMs = 0;
            _lastBallDeviceTimestampUs = 0;
            _lastHeadReadTimestampMs = 0;
            _lastHeadDeviceTimestampUs = 0;
        }

        public void AddInput(JetTracParser.BallMessage ball)
        {
            JetTracRingBuffer.Item item = new JetTracRingBuffer.Item(ball);

            item.deltaReadTimestampMs = item.readTimestampMs - _lastBallReadTimestampMs;
            item.deltaDeviceTimestampUs = item.deviceTimestampUs - _lastBallDeviceTimestampUs;
            _lastBallReadTimestampMs = item.readTimestampMs;
            _lastBallDeviceTimestampUs = item.deviceTimestampUs;

            // The ball sensor values are displacements, so all the values reported since the
            // last `Update` call need to be stored, so they can be summed.
            _ballSinceLastUpdateBuffer.Give(item);
        }

        public void AddInput(JetTracParser.HeadMessage head)
        {
            JetTracRingBuffer.Item item = new JetTracRingBuffer.Item(head);

            item.deltaReadTimestampMs = item.readTimestampMs - _lastHeadReadTimestampMs;
            item.deltaDeviceTimestampUs = item.deviceTimestampUs - _lastHeadDeviceTimestampUs;
            _lastHeadReadTimestampMs = item.readTimestampMs;
            _lastHeadDeviceTimestampUs = item.deviceTimestampUs;

            // The head sensor values are absolute rotations, so they need to be stored only
            // for smoothing (of the latest value, based on some previous values).
            _headSmoothingBuffer.Give(item);

            if (!_headMessageAngleDegs0Set)
            {
                _headMessageAngleDegs0Set = true;
                _headMessageAngleDegs0 = head.angleDegs;
            }
        }

        // Computes the global position and rotation.
        // The arguments are expected to be the current position and rotation.
        // Assumes that the forward axis of the agent is X, and the upward axis is Y..
        public bool Update(ref Vector3 bodyPosition, ref float bodyRotationYDegs, bool smooth = false)
        {
            float ballX0 = 0, ballY0 = 0, ballX1 = 0, ballY1 = 0;
            UInt64 ballDtUs = 0;

            if (GetBallVelocities(ref ballX0, ref ballY0, ref ballX1, ref ballY1, ref ballDtUs, smooth))
            {
                const float Y_ANGLE_FULL_EFFECT_DEGS = 180;
                float gamma0 = Mathf.Abs(Y_ANGLE_FULL_EFFECT_DEGS - ballSensor0YAngleDegs);
                float gamma1 = Mathf.Abs(Y_ANGLE_FULL_EFFECT_DEGS - ballSensor1YAngleDegs);

                float gamma0Rad = gamma0 * Mathf.Deg2Rad;
                float gamma1Rad = gamma1 * Mathf.Deg2Rad;
                float velocityForward = ballY0 * Mathf.Cos(gamma0Rad) + ballY1 * Mathf.Cos(gamma1Rad);
                float velocitySide = ballY0 * Mathf.Sin(gamma0Rad) - ballY1 * Mathf.Sin(gamma1Rad);
                float velocityRotation = (ballX0 + ballX1) / 2;

                velocityForward = -velocityForward;
                velocitySide = -velocitySide;
                velocityRotation = -velocityRotation;

                float uncalibatedBodyRotationYDegs = velocityRotation * ballDtUs;

                // Unity has a left-handed coordinate system.
                uncalibatedBodyRotationYDegs = -uncalibatedBodyRotationYDegs;

                bodyRotationYDegs += _calibrationScale * uncalibatedBodyRotationYDegs;

                float cos = Mathf.Cos(bodyRotationYDegs * Mathf.Deg2Rad);
                float sin = Mathf.Sin(bodyRotationYDegs * Mathf.Deg2Rad);
                float forward = velocityForward * ballDtUs;
                float side = velocitySide * ballDtUs;
                float uncalibratedChangeX = side * sin + forward * cos;
                float uncalibratedChangeZ = side * cos - forward * sin;
                bodyPosition.x += _calibrationScale * uncalibratedChangeX;
                bodyPosition.z += _calibrationScale * uncalibratedChangeZ;
            }

            return true;
        }

        // A version that does not allow the ball to rotate the body (i.e., the jETTrac device's
        // `x0` and `x1` values are ignored).
        public bool Update(ref Vector3 bodyPosition, bool smooth = false)
        {
            float bodyRotationYDegs = 0;
            return Update(ref bodyPosition, ref bodyRotationYDegs, smooth);
        }
        

        // Should be applied to `Transform`'s `eulerAngles` property, which compensates for
        // the parent `Transform`'s rotation.  (Applying to `localEulerAngles` would require
        // subtracting off the parent rotation explicitly.)  Note also, from the Unity documentation
        // on `Transform.eulerAngles`:
        // "Do not set one of the eulerAngles axis separately (eg. eulerAngles.x = 10; ) since
        // this will lead to drift and undesired rotations. When setting them to a new value set them 
        // all at once as shown above. Unity will convert the angles to and from the rotation stored 
        // in Transform.rotation."
        public bool Update(ref float headRotationYDegs, bool smooth = false)
        {
            float headAngleDegs = 0;
            UInt64 headDtUs = 0;

            if (GetHeadVelocities(ref headAngleDegs, ref headDtUs, smooth))
            {
                // Unity has a left-handed coordinate system.
                headAngleDegs = -headAngleDegs;

                headRotationYDegs = _headRotationYDegs0 + headAngleDegs * headDtUs;
            }

            return true;
        }

        public static float GetCalibrationScale()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_calibrationScale == 0.0f)
            {
                _calibrationScale = GetFromRegistry(REGISTRY_KEY_CALIBRATION_SCALE, DEFAULT_CALIBRATION_SCALE);
            }
#endif
            return _calibrationScale;
        }

        public static void SetCalibrationScale(float scale, bool save = true)
        {
            _calibrationScale = scale;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (save)
            {
                SetInRegistry(REGISTRY_KEY_CALIBRATION_SCALE, _calibrationScale);
            }
#endif
        }

        public static float GetBallDiameter()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_ballDiameter == 0.0f)
            {
                _ballDiameter = GetFromRegistry(REGISTRY_KEY_BALL_DIAMETER, DEFAULT_BALL_DIAMETER);
            }
#endif
            return _ballDiameter;
        }

        public static void SetBallDiameter(float diameter, bool save = true)
        {
            _ballDiameter = diameter;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (save)
            {
                SetInRegistry(REGISTRY_KEY_BALL_DIAMETER, _ballDiameter);
            }
#endif
        }

        private bool GetBallVelocities(ref float x0, ref float y0, ref float x1, ref float y1, ref UInt64 dtUs, bool smooth)
        {
            x0 = y0 = x1 = y1 = dtUs = 0;
            if (_ballSinceLastUpdateBuffer.ItemCount == 0)
            {
                return false;
            }

            // The ball sensor values are displacements, so all the values accumulated since the
            // last `Update` call need to be summed.
            dtUs = 0;
            JetTracRingBuffer.Item item = new JetTracRingBuffer.Item();
            while (_ballSinceLastUpdateBuffer.Take(ref item))
            {
                if (smooth)
                {
                    // Smoothing makes the most sense when applied to velocities, not displacements.
                    float x0Vel = 0, y0Vel = 0, x1Vel = 0, y1Vel = 0;
                    float smoothingDtUs = item.deltaDeviceTimestampUs;
                    _ballSmoothingBuffer.Give(item);
                    JetTracRingBuffer.Item smoothingItem = new JetTracRingBuffer.Item();
                    for (int i = 0; i < _ballSmoothingBuffer.ItemCount; ++i)
                    {
                        _ballSmoothingBuffer.Peek(ref smoothingItem, i);
                        x0Vel += ((smoothingItem.ballX0 / (float) smoothingItem.deltaDeviceTimestampUs));
                        y0Vel += ((smoothingItem.ballY0 / (float) smoothingItem.deltaDeviceTimestampUs));
                        x1Vel += ((smoothingItem.ballX1 / (float) smoothingItem.deltaDeviceTimestampUs));
                        y1Vel += ((smoothingItem.ballY1 / (float) smoothingItem.deltaDeviceTimestampUs));
                    }
                    // When smoothing is done, convert from velocities back to displacements.
                    x0 += (x0Vel / (float) _ballSmoothingBuffer.ItemCount * smoothingDtUs);
                    y0 += (y0Vel / (float) _ballSmoothingBuffer.ItemCount * smoothingDtUs);
                    x1 += (x1Vel / (float) _ballSmoothingBuffer.ItemCount * smoothingDtUs);
                    y1 += (y1Vel / (float) _ballSmoothingBuffer.ItemCount * smoothingDtUs);
                }
                else 
                {
                    x0 += item.ballX0;
                    y0 += item.ballY0;
                    x1 += item.ballX1;
                    y1 += item.ballY1;
                }
                dtUs += item.deltaDeviceTimestampUs;
            }
            x0 /= (float) dtUs;
            y0 /= (float) dtUs;
            x1 /= (float) dtUs;
            y1 /= (float) dtUs;

            return true;
        }

        private bool GetHeadVelocities(ref float headAngleDegs, ref UInt64 dtUs, bool smooth)
        {
            headAngleDegs = dtUs = 0;
            if (_headSmoothingBuffer.ItemCount == 0)
            {
                return false;
            }

            // Whether smoothing or not, `_headSmoothingBuffer` has the latest reading.
            JetTracRingBuffer.Item latestItem = new JetTracRingBuffer.Item();
            _headSmoothingBuffer.PeekLatest(ref latestItem);
            dtUs = latestItem.deltaDeviceTimestampUs;

            if (smooth)
            {
                JetTracRingBuffer.Item item = new JetTracRingBuffer.Item();
                for (int i = 0; i < _headSmoothingBuffer.ItemCount; ++i)
                {
                    _headSmoothingBuffer.Peek(ref item, i);
                    // Correct so the angle at start-up would be considered zero.
                    float correctedAngle = item.headAngleDegs - _headMessageAngleDegs0;
                    headAngleDegs += (correctedAngle / item.deltaDeviceTimestampUs);
                }
                headAngleDegs /= _headSmoothingBuffer.ItemCount;
            }
            else
            {
                // Correct so the angle at start-up would be considered zero.
                float correctedAngle = latestItem.headAngleDegs - _headMessageAngleDegs0;
                headAngleDegs = correctedAngle / latestItem.deltaDeviceTimestampUs;
            }

            return true;
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // For now, at least, the code to use the registry will not be a general capability
        // (e.g., some public API in `org.janelia.general`), per this recommenation:
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.setvalue
        // "Caution: Do not expose `RegistryKey` objects in such a way that a malicious program
        // could create thousands of meaningless subkeys or key/value pairs. For example,
        // do not allow callers to enter arbitrary keys or values."

        private static void SetInRegistry(string key, float value)
        {
            RegistryKey regJUT = GetRegistryEntry();
            if (regJUT != null)
            {
                regJUT.SetValue(key, value);
            }
        }

        private static float GetFromRegistry(string key, float defaultValue = 0)
        {
            RegistryKey regJUT = GetRegistryEntry();
            if (regJUT != null)
            {
                object value = regJUT.GetValue(key);
                if (value != null)
                {
                    return float.Parse((string)value);
                }
            }
            return defaultValue;
        }

        private static RegistryKey GetRegistryEntry()
        {
            const string OUTER_NAME = "SOFTWARE";
            const string INNER_NAME = "janelia-unity-toolkit";

            RegistryKey regUser = Registry.CurrentUser;
            string[] regUserSubKeyNames = regUser.GetSubKeyNames();
            // Handle different casing for OUTER_NAME.
            string outerName = Array.Find(regUserSubKeyNames, k => k.ToUpper() == OUTER_NAME);
            if (!string.IsNullOrEmpty(outerName))
            {
                RegistryKey regSoftware = regUser.OpenSubKey(outerName, true);
                string[] regSoftwareSubKeyNames = regSoftware.GetSubKeyNames();
                if (!Array.Exists(regSoftwareSubKeyNames, k => k == INNER_NAME))
                {
                    regSoftware.CreateSubKey(INNER_NAME);
                }
                regSoftwareSubKeyNames = regSoftware.GetSubKeyNames();
                if (Array.Exists(regSoftwareSubKeyNames, k => k == INNER_NAME))
                {
                    RegistryKey regJUT = regSoftware.OpenSubKey(INNER_NAME, true);
                    return regJUT;
                }
            }
            return null;
        }

        private const string REGISTRY_KEY_CALIBRATION_SCALE = "jETTrac_calibration_scale";
        private const string REGISTRY_KEY_BALL_DIAMETER = "jETTrac_ball_diameter";
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private static float _calibrationScale = 0.0f;
        private static float _ballDiameter = 0.0f;
#else
        private static float _calibrationScale = DEFAULT_CALIBRATION_SCALE;
        private static float _ballDiameter = DEFAULT_BALL_DIAMETER;
#endif

        private int _smoothingWindow;

        private JetTracRingBuffer _ballSinceLastUpdateBuffer;
        private JetTracRingBuffer _ballSmoothingBuffer;

        private UInt64 _lastBallReadTimestampMs = 0;
        private UInt64 _lastBallDeviceTimestampUs = 0;

        private float _headMessageAngleDegs0;
        private bool _headMessageAngleDegs0Set = false;

        private float _headRotationYDegs0;

        private JetTracRingBuffer _headSmoothingBuffer;
        private UInt64 _lastHeadReadTimestampMs = 0;
        private UInt64 _lastHeadDeviceTimestampUs = 0;
    }
}
