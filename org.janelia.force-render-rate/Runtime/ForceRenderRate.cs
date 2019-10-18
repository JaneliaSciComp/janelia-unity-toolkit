// Adapted from:
// https://github.com/Unity-Technologies/GenLockProofOfConcept/blob/master/Assets/ForceRenderRate.cs
// (which has no license or copyright)

// Note that the maximum achievable frame rate will be lower when running in the editor.
// Note also this maximum will be significantly lower when using "multi display" capabilities.

using System.Collections;
using System.Threading;
using UnityEngine;

namespace Janelia
{
    public class ForceRenderRate : MonoBehaviour
    {
        public float rateHz = 360.0f;
        private float _currentFrameTime;

        // For reporting the current frame rate, averaged to avoid excesses reporting overhead.
        public bool reportAverageRate = true;
        public int framesToAverage = 500;
        private int _deltaTimesCount = 0;
        private float _deltaTimesSum = 0.0f;


        private void Start()
        {
            Debug.Log("ForceRenderRate: Rate (FPS) " + rateHz);

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 9999;
            _currentFrameTime = Time.realtimeSinceStartup;
            StartCoroutine("WaitForNextFrame");
        }

        private IEnumerator WaitForNextFrame()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                _currentFrameTime += 1.0f / rateHz;
                var t = Time.realtimeSinceStartup;
                var sleepTime = _currentFrameTime - t - 0.01f;
                if (sleepTime > 0)
                    Thread.Sleep((int)(sleepTime * 1000));
                while (t < _currentFrameTime)
                    t = Time.realtimeSinceStartup;
            }
        }

        private void Update()
        {
            _deltaTimesSum += Time.deltaTime;

            // Ensure the user-supplied number of frames to average is reasonable.
            framesToAverage = Mathf.Max(1, Mathf.Min(10000, framesToAverage));

            if (reportAverageRate && (++_deltaTimesCount >= framesToAverage))
            {
                float deltaTimeAverage = _deltaTimesSum / framesToAverage;
                float fpsAverage = 1.0f / deltaTimeAverage;
                Debug.Log("Frame rate " + fpsAverage.ToString("n2") + " Hz (" + 1000 * deltaTimeAverage + 
                    " ms / frame) averaged over " + framesToAverage + " frames");
                _deltaTimesCount = 0;
                _deltaTimesSum = 0.0f;
            }
        }

    }
}