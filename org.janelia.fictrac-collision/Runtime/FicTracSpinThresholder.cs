using System;
using System.Linq;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif
using UnityEngine;

// Tracks the low-frequency component (i.e., average over a window of recent frames) of the
// FicTrac heading's angular speed, so it can be compared to a threshold.  Being above threshold
// indicates that the FicTrac trackball is spinning freely without input from the animal, as if
// the animal has lifted its legs off the trackball.

namespace Janelia
{
     public class FicTracSpinThresholder: MonoBehaviour
    {
        // The threshold to compare against. This default value will be replaced by the value of
        // the session parameter "ficTracSpinThreshold", if specified.
        public float threshold = 999999;

        // The window (i.e., number of frames) over which to average. This default value will be 
        // replaced by the value of the session parameter "ficTracSpinThreshold", if specified.
        public int window = 10;

        // The low-frequency (i.e., averaged) heading angular speed.
        public float angularSpeed
        {
            get => _angularSpeed;
        }

#if UNITY_EDITOR
        [PostProcessBuildAttribute(SessionParameters.POST_PROCESS_BUILD_ORDER + 10)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Janelia.FicTracSpinThresholder.OnPostprocessBuild: " + pathToBuiltProject);

            SessionParameters.AddFloatParameter("ficTracSpinThreshold", 0);
            // TODO: SessionParameters should support integers, but for now, use a float.
            SessionParameters.AddFloatParameter("ficTracSpinWindow", 0);
        }
#endif

        public void Start()
        {
            _currentLog = new AboveThresholdAngularSpeedLog();

            float t = SessionParameters.GetFloatParameter("ficTracSpinThreshold");
            if (t > 0)
            {
                threshold = t;
            }

            float w = SessionParameters.GetFloatParameter("ficTracSpinWindow");
            if (w > 0)
            {
                window = (int)w;
            }

            _initialized = false;
        }

        public void UpdateAbsolute(float heading, float deltaTime)
        {
            float deltaHeading = heading - _previousHeading;
            _previousHeading = heading;
            Record(deltaHeading, deltaTime);
        }

        public void UpdateRelative(float deltaHeading, float deltaTime)
        {
            Record(deltaHeading, deltaTime);
        }

        public void Log()
        {
            _currentLog.aboveThresholdAngularSpeed = _angularSpeed; 
            _currentLog.threshold = threshold;
            Logger.Log(_currentLog);
        }

        private void Record(float deltaHeading, float deltaTime)
        {
            if (_initialized && (deltaTime > 1e-7f))
            {
                float angularSpeed = Mathf.Abs(deltaHeading) / deltaTime;

                if (_angularSpeedsToAverage == null)
                {
                    _angularSpeedsToAverage = new float[window];
                    Array.Clear(_angularSpeedsToAverage, 0, window);
                }
                _angularSpeedsToAverage[_angularSpeedsToAverageIndex] = angularSpeed;
                _angularSpeedsToAverageIndex = (_angularSpeedsToAverageIndex + 1) % _angularSpeedsToAverage.Length;

                // Average the angular speeds within the window, to do approximate low-pass filtering.
                // The reason is that when the FicTrac ball is spinning freely, its angular speed is 
                // rather constant and high, so its low-frequency component is rather high.  But when
                // a fly is walking on the ball, there are periods of change to the angular speed
                // interspersed with periods of no change, so the low-frequency component is lower.
                _angularSpeed = Average(_angularSpeedsToAverage);

                if (_recentAngularSpeeds == null)
                {
                    _recentAngularSpeeds = new float[RECENT_COUNT];
                    Application.quitting += Report;
                }

                _recentAngularSpeeds[_recentIndex] = _angularSpeed;
                _recentCount = _recentCount + 1;
                _recentIndex = (_recentIndex + 1) % _recentAngularSpeeds.Length;
            }

            _initialized = true;

        }

        private float Average(float[] a)
        {
            float sum = 0;
            foreach (float f in a)
                sum += f;
            return sum / a.Length;
        }

        private void Report()
        {
            int count = (_recentCount < RECENT_COUNT) ? _recentCount - 1 : RECENT_COUNT - 1;
            ArraySegment<float> slice = new ArraySegment<float>(_recentAngularSpeeds, 0, count);
            Array.Sort(slice.Array, slice.Offset, slice.Count);

            const int PERCENTILES = 10;
            float[] low = new float[PERCENTILES];
            float[] high = new float[PERCENTILES];
            for (int i = 0; i < PERCENTILES; ++i)
            {
                float fracLow = i * 0.01f + 0.005f;
                int indexLow = (int)(RECENT_COUNT * fracLow);
                low[i] = _recentAngularSpeeds[indexLow];

                float fracHigh = 1.0f - PERCENTILES * 0.01f + i * 0.01f + 0.005f;
                int indexHigh = (int)(RECENT_COUNT * fracHigh);
                high[i] = _recentAngularSpeeds[indexHigh];
            }
            if (PERCENTILES == 1)
            {
                Debug.Log("FicTrac spin (heading) angular speed, 1st percentile: " + low[0].ToString("F1"));
                Debug.Log("FicTrac spin (heading) angular speed, 99th percentile: " + high[0].ToString("F1"));
            }
            else
            {
                string lowStr = string.Join(", ", low.Select(x => x.ToString("F1")));
                string highStr = string.Join(", ", high.Select(x => x.ToString("F1")));
                Debug.Log("FicTrac spin (heading) angular speed, percentiles 1 to " + PERCENTILES + ": " + lowStr);
                Debug.Log("FicTrac spin (heading) angular speed, percentiles " + (100 - PERCENTILES) + " to 99: " + highStr);
            }
        }

        [Serializable]
        internal class AboveThresholdAngularSpeedLog : Logger.Entry
        {
            public float aboveThresholdAngularSpeed;
            public float threshold;
        };

        float _previousHeading;
        float _angularSpeed;

        bool _initialized;

        private AboveThresholdAngularSpeedLog _currentLog;

        private float[] _angularSpeedsToAverage;
        private int _angularSpeedsToAverageIndex = 0;

        private const int RECENT_COUNT = 1000;
        private float[] _recentAngularSpeeds;
        private int _recentIndex = 0;
        private int _recentCount = 0;
    }
}
