using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
     public class FicTracAverager : MonoBehaviour
    {
        public static void StartAveragingHeading(GameObject obj, int windowInFrames)
        {
            FicTracAverager averager = obj.GetComponent<FicTracAverager>();
            if (obj != null)
            {
                averager.StartAveragingHeading(windowInFrames);
            }
        }

        public static float GetAverageHeading(GameObject obj)
        {
            float result = 0;
            FicTracAverager averager = obj.GetComponent<FicTracAverager>();
            if (obj != null)
            {
                result = averager.GetAverageHeading();
            }
            return result;
        }

        // Seems to be called before the event handlers added to `Application.quitting`.
        // The `Logger` class has one of those event handlers to write the log a final time.
        public void OnApplicationQuit()
        {
            // When the session ends, force computation and storage.
            float mean = GetAverageHeading();

            if (_started)
            {
                Debug.Log("Janelia.FicTracAverager stored mean heading " + mean);
            }
        }

        private void StartAveragingHeading(int windowInFrames)
        {
            _window = windowInFrames;
            _headings = new float[_window];
            Array.Clear(_headings, 0, _window);
            _index = 0;
            _started = true;
            _dirty = true;
        }

        internal void RecordHeading(float heading)
        {
            if (_started)
            {
                _headings[_index] = heading;
                _index = (_index + 1) % _window;
                _dirty = true;
            }
        }

        private float GetAverageHeading()
        {
            if (!_dirty)
            {
                return _mean;
            }
            if (!_started)
            {
                _mean = RestoreMeanHeading();
                _dirty = false;
                return _mean;
            }
            float sum = 0;
            foreach (float heading in _headings)
            {
                sum += heading;
            }
            _mean = sum / _window;
            StoreMeanHeading(_mean);
            _dirty = false;
            return _mean;
        }

        private void StoreMeanHeading(float mean)
        {
            string playerPrefsKey = "janelia-unity-toolkit.FicTracAverager." + PathName() + ".meanHeadingDegs";
            PlayerPrefs.SetFloat(playerPrefsKey, mean);
            _currentStored.storedMeanHeadingDegs = mean;
            Logger.Log(_currentStored);
        }

        private float RestoreMeanHeading()
        {
            string playerPrefsKey = "janelia-unity-toolkit.FicTracAverager." + PathName() + ".meanHeadingDegs";
            float mean = PlayerPrefs.GetFloat(playerPrefsKey, 0);
            _currentRestored.restoredMeanHeadingDegs = mean;
            Logger.Log(_currentRestored);
            return mean;
        }

        private string PathName()
        {
            GameObject o = gameObject;
            string path = o.name;
            while (o.transform.parent != null)
            {
                o = o.transform.parent.gameObject;
                path = o.name + "-" + path;
            }
            return path;
        }

        [Serializable]
        internal class Stored : Logger.Entry
        {
            public float storedMeanHeadingDegs;
        };
        private Stored _currentStored = new Stored();

        [Serializable]
        internal class Restored : Logger.Entry
        {
            public float restoredMeanHeadingDegs;
        };
        private Restored _currentRestored = new Restored();

        private int _window;
        private float[] _headings;
        private int _index = 0;
        private bool _started = false;
        private float _mean = 0;
        private bool _dirty = true;
    }
}
