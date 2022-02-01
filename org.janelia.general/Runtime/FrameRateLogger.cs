// Basic logging of the average frame rate, to the log file maintained by
// org.janelia.logging.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Janelia
{
    public class FrameRateLogger : MonoBehaviour
    {
        public int secsToAverage = 1;
        public bool roundAverageToMs = true;
        public bool useDebugLog = false;

        void Start()
        {
            _deltaTimesSum = 0;
            _deltaTimesCount = 0;
        }

        void Update()
        {
            _deltaTimesSum += Time.deltaTime;
            ++_deltaTimesCount;

            // Ensure the user-supplied number of frames to average is reasonable.
            secsToAverage = Mathf.Max(1, secsToAverage);

            if (_deltaTimesSum >= secsToAverage)
            {
                float deltaTimeAverage = _deltaTimesSum / _deltaTimesCount;
                if (roundAverageToMs)
                {
                    deltaTimeAverage = (float)Math.Round(deltaTimeAverage, 3);
                }
                float fpsAverage = 1.0f / deltaTimeAverage;
                fpsAverage = (float)Math.Round(fpsAverage, 2);
                
                _currentFrameRateLog.averageFrameRateHz = fpsAverage;
                _currentFrameRateLog.secsAveragedOver = _deltaTimesSum;
                Logger.Log(_currentFrameRateLog);

                if (useDebugLog)
                {
                    Debug.Log(Now() + ": average frame rate: " + fpsAverage + " Hz over last " + _deltaTimesSum + " secs");
                }

                _deltaTimesCount = 0;
                _deltaTimesSum = 0.0f;
            }
        }

        private string Now()
        {
            if (Application.isEditor)
            {
                return "";
            }
            return "[" + Time.frameCount + ", " + Time.time + "] ";
        }

        private int _deltaTimesCount = 0;
        private float _deltaTimesSum = 0.0f;

        [Serializable]
        private class FrameRateLog : Logger.Entry
        {
            public float averageFrameRateHz;
            public float secsAveragedOver;
        };
        private FrameRateLog _currentFrameRateLog = new FrameRateLog();

    }
}
