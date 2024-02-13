using System;
using System.Linq;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif
using UnityEngine;

namespace Janelia
{
     public class FicTracSpinThresholder: MonoBehaviour
    {
        // A possible value, determined empirically using `python .\FakeTrac.py -tr 0 -ro 1`, is 400.
        public float threshold = 999999;

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
                _angularSpeed = Mathf.Abs(deltaHeading) / deltaTime;

                if (_recentAngularSpeeds == null)
                {
                    _recentAngularSpeeds = new float[RECENT_COUNT];
                    Application.quitting += Report;
                }

                _recentAngularSpeeds[_recentIndex] = angularSpeed;
                _recentCount = _recentCount + 1;
                _recentIndex = (_recentIndex + 1) % _recentAngularSpeeds.Length;
            }

            _initialized = true;

        }

        private void Report()
        {
            int count = (_recentCount < RECENT_COUNT) ? _recentCount - 1 : RECENT_COUNT - 1;
            ArraySegment<float> slice = new ArraySegment<float>(_recentAngularSpeeds, 0, count);
            Array.Sort(slice.Array, slice.Offset, slice.Count);

            const int PERCENTILES = 1;
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

        private const int RECENT_COUNT = 1000;
        private float[] _recentAngularSpeeds;
        private int _recentIndex = 0;
        private int _recentCount = 0;
    }
}
