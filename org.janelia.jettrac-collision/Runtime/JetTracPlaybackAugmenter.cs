
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Janelia
{
    public class JetTracPlabackAugmenter : KinematicSubject.PlaybackAugmenter
    {
        public override void LoadLog(string playbackLogFilePath)
        {
            if (File.Exists(playbackLogFilePath))
            {
                _playbackLogEntries = Logger.Read<HeadLog>(playbackLogFilePath);
                _playbackLogEntries = Filter(_playbackLogEntries);
            }
        }
        public override void AugmentPlayback(int frameCount, Transform transform)
        {
            HeadLog headLog = CurrentLogEntry(frameCount);
            if (headLog != null)
            {
                Transform headTransform = transform.Find(headLog.headName);
                if (headTransform != null)
                {
                    _headRotationDegrees = headLog.headAbsoluteRotationDegs;
                    headTransform.eulerAngles = headLog.headAbsoluteRotationDegs;
                }
            }
        }

        public void Log(string headName, Vector3 headAbsoluteRotationDegs)
        {
            _currentHeadLog.headName = headName;
            _currentHeadLog.headAbsoluteRotationDegs = headAbsoluteRotationDegs;
            Logger.Log(_currentHeadLog);
        }

        public Vector3 HeadRotationDegrees()
        {
            return _headRotationDegrees;
        }

        private List<HeadLog> Filter(List<HeadLog> l)
        {
            return l.Where(x => !String.IsNullOrEmpty(x.headName)).ToList();
        }

        private HeadLog CurrentLogEntry(int frameCount)
        {
            if (_playbackLogEntries.Count == 0)
            {
                return null;
            }
            while ((_playbackLogIndex < _playbackLogEntries.Count) &&
                   (_playbackLogEntries[_playbackLogIndex].frame <= frameCount))
            {
                if (_playbackLogEntries[_playbackLogIndex].frame == frameCount)
                {
                    return _playbackLogEntries[_playbackLogIndex];
                }
                ++_playbackLogIndex;
            }
            return _playbackLogEntries[_playbackLogIndex - 1];
        }

        // To make `Janelia.Logger.Log(entry)`'s call to JsonUtility.ToJson() work correctly,
        // the type of `entry` must be marked `[Serlializable]`, but its individual fields need not
        // be marked `[SerializeField]`.  The individual fields must be `public`, though.
        [Serializable]
        internal class HeadLog : Logger.Entry
        {
            public string headName;
            public Vector3 headAbsoluteRotationDegs;
        };

        private HeadLog _currentHeadLog = new HeadLog();

        private List<HeadLog> _playbackLogEntries;
        private int _playbackLogIndex = 0;

        private Vector3 _headRotationDegrees = Vector3.zero;
    }
}
