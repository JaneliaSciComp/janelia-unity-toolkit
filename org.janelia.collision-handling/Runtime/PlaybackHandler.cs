using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Implements the playback of the motion captured in the log of a previous session.
// This playback is controlled by the following command-line arguments to the Unity applicattion:
// `-playback` : plays back the most recently saved log file
// `-playback logFile` : plays back the specified log file from the standard log directory
// `-playback logDir/logFile` : plays back the specified log file

namespace Janelia
{
    // The log entries used for playback should be derived from this class.
    public abstract class PlayableLogEntry : Logger.Entry
    {
        public static Vector3 DEFAULT = new Vector3(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
        public Vector3 worldPosition = DEFAULT;
        public Vector3 worldRotationDegs  = DEFAULT;

        public void Set(PlayableLogEntry other)
        {
            frame = other.frame;
            frameAfterSplash = other.frameAfterSplash;
            worldPosition = other.worldPosition;
            worldRotationDegs = other.worldRotationDegs;
        }

        public bool IsUnset()
        {
            return (worldPosition == DEFAULT && worldRotationDegs == DEFAULT);
        }
    }

    // A `RuntimeInitializeOnLoadMethod` won't work on a generic class like `PlaybackHandler`,
    // so this special class exists to hold the `RuntimeInitializeOnLoadMethod`.
    public static class PlaybackHandlerRuntimeInitializeOnLoad
    {
        // `BeforeSplashScreen` is the earliest runtime initialize method.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void OnRuntimeMethodLoad()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Contains("-playback"))
            {
                Logger.enable = false;
            }
            Debug.Log("PlaybackHandler setting Logger.enable " + Logger.enable);
        }
    }

    // The controller.  The generic argument is the type of log entry from which to read the playback details.
    public class PlaybackHandler<Transformation> where Transformation : PlayableLogEntry
    {
        public bool PlaybackActive
        {
            get => _playbackActive;
        }

        // A base class for code to augment the logfile playback with changes other than
        // those implemented by the standard `KinematicSubject`.
        public abstract class PlaybackAugmenter
        {
            public PlaybackAugmenter() => _playbackAugmenters.Add(this);

            public abstract void LoadLog(string playbackLogFilePath);

            public abstract void AugmentPlayback(int frameCount, Transform transform);
        }

        public void ConfigurePlayback()
        {
            _playbackLogFile = Logger.previousLogFile;
            string[] args = System.Environment.GetCommandLineArgs();
            bool found = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-playback")
                {
                    if (i + 1 < args.Length)
                    {
                        _playbackLogFile = args[i + 1];
                        if (!_playbackLogFile.Contains('/') && !_playbackLogFile.Contains('\\'))
                        {
                            _playbackLogFile = Logger.logDirectory + "/" + _playbackLogFile;
                        }
                        if (File.Exists(_playbackLogFile))
                        {
                            found = true;
                        }
                        else
                        {
                            Debug.Log("Cannot find playback log file '" + _playbackLogFile + "'");
                        }
                    }
                }
            }
            _playbackRequested = found;
        }

        public bool Update(ref Transformation currentTransformation, Transform transform)
        {
            if (!_playbackRequested && !_playbackActive)
            {
                return false;
            }

            if (_playbackRequested)
            {
                if (SplashScreen.isFinished)
                {
                    _playbackRequested = false;
                    _playbackLogEntries = Logger.Read<Transformation>(_playbackLogFile);
                    _playbackLogEntries = Filter(_playbackLogEntries);

                    for (int i = 0; i < _playbackAugmenters.Count; ++i)
                    {
                        _playbackAugmenters[i].LoadLog(_playbackLogFile);
                    }

                    _playbackLogIndex = 0;
                    _playbackStartFrame = Time.frameCount;
                    _playbackActive = true;
                }
            }

            if (_playbackActive)
            {
                Transformation transformation = CurrentPlaybackTransformation();
                if (transformation != null)
                {
                    currentTransformation.Set(transformation);
                    SaveAllFrames.SetFrame((int)transformation.frame);

                    int frame = (int)Mathf.Round(transformation.frame);
                    if (_previousFrame >= 0)
                    {
                        _droppedFrameCount += (frame - _previousFrame - 1);
                    }
                    _previousFrame = frame;

                    transform.position = currentTransformation.worldPosition;
                    transform.eulerAngles = currentTransformation.worldRotationDegs;
                }

                for (int i = 0; i < _playbackAugmenters.Count; ++i)
                {
                    _playbackAugmenters[i].AugmentPlayback(Time.frameCount, transform);
                }
            }

            return true;
        }

        // Removes (filters out) any log entries not relevant to playback.
        private List<Transformation> Filter(List<Transformation> l)
        {
            return l.Where(x => !x.IsUnset()).ToList();
        }

        private Transformation CurrentPlaybackTransformation()
        {
            if (_playbackActive && (_playbackLogEntries != null))
            {
                int adjustedFrame = Time.frameCount - _playbackStartFrame;
                while (_playbackLogIndex < _playbackLogEntries.Count)
                {
                    if (_playbackLogEntries[_playbackLogIndex].frameAfterSplash < adjustedFrame)
                    {
                        _playbackLogIndex++;
                    }
                    else
                    {
                        if (_playbackLogEntries[_playbackLogIndex].frameAfterSplash == adjustedFrame)
                        {
                            return _playbackLogEntries[_playbackLogIndex];
                        }
                        return null;
                    }
                }
            }

            Debug.Log("PlaybackHandler dropped " + _droppedFrameCount + " frame" + (_droppedFrameCount == 1 ? "." : "s."));

            // End the session when the playback ends.
            Application.Quit();
            return null;
        }

        private bool _playbackRequested = false;
        private bool _playbackActive = false;
        private string _playbackLogFile = "";
        private List<Transformation> _playbackLogEntries;
        private int _playbackLogIndex;
        private int _playbackStartFrame;

        private int _previousFrame = -1;
        private int _droppedFrameCount = 0;

        private static List<PlaybackAugmenter> _playbackAugmenters = new List<PlaybackAugmenter>();
    }
}
