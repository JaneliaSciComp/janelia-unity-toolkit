// A subject (GameObject representing a moving agent) that supports
// kinematic motion, kinematic collision detection, and logging.
// The kinematic motion is provided by the `updater` field, an object
// that must conform to the `Janelia.KinematicSubject.IKinematicUpdater` 
// interface.

// An application using a `Janelia.KinematicSubject` can play back the motion 
// captured in the log of a previous session.  This playback is controlled by the '
// following command-line arguments
// `-playback` : plays back the most recently saved log file
// `-playback logFile` : plays back the specified log file from the standard log directory
// `-playback logDir/logFile` : plays back the specified log file

// This class has a post-build step that creates a simple "launcher script", to give a
// simple user interface for the command-line arguments for playback.  The user interface
// appears on the primary display, before the application starts running; this approach
// is preferable to an "in-game" UI on one of the secondary displays, since they may be
// set up for an animal participating in an experiment.  This launcher script is
// a Windows Script File (.wsf file) so double-clicking on it runs it on any modern 
// version of Windows, with no need to install any additional software.  The trade-off 
// is that the user interface is crude and looks dated, because it is implemented with 
// Visual Basic and JScript.

// In addition to logging the motion (rotation and translation) at each frame, this class
// also logs the time spent processing each frame, and also logs some details of all
// the meshes as of the start of the application (if `LOG_ALL_MESHES` is defined).

#define SUPPORT_COMMANDLINE_ARGUMENTS
// #define SUPPORT_KEYBOARD_SHORTCUTS
#define LOG_ALL_MESHES

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif
using UnityEngine;

namespace Janelia
{
    // The `DefaultExecutionOrder` attribute needs to specify a value lower than the default of 0,
    // so `Update()` will execute before the cameras rendered by `Janelia.AdjoningDisplaysCamera`.
    // Otherwise, some of those cameras may be a frame behind, producing vertical "tearing".
    [DefaultExecutionOrder(-100)]
    public class KinematicSubject : MonoBehaviour
    {
        // A class conforming to this interface provides the kinematic motion (translation,
        // rotation) for this subject object.
        public interface IKinematicUpdater
        {
            // With C# 8.0, interfaces will be able to have default implementations.  But
            // as of 2020, Unity is not using C# 8.0.  So classes implementing this interface
            // must have a `Start()` method even if it does nothing.
            void Start();
            void Update();

            // These values are relative (displacements).
            Vector3? Translation();
            Vector3? RotationDegrees();
        };

        // The object that provides the kinematic motion, to be set by the constructor of
        // the subclass of this class.
        public IKinematicUpdater updater;

        // Parameters of the collision detection supported by this class.
        public float collisionRadius = 1.0f;
        public Vector3? collisionPlaneNormal = null;

        // Parameters to control when the log is written.  The goal is to avoid writing
        // either too frequently or not frequently enough.  A heuristic is to write when
        // the subject is "still", that is, when the `IKinematicUpdater` has not given any
        // motion for a small number of frames.
        public bool writeLogWhenStill = true;
        public int stillFrames = 5;

        // That heuristic is further refined so that writing when "still" will not happen
        // before a minimum number of frames have passed since the last writing, and writing
        // will not wait more than a maximum number of frames since the last writing, even
        // if not "still".
        public int minWriteInterval = 100;
        public int maxWriteInterval = 200;

        public bool debug = false;

        public void Start()
        {
            if (updater == null)
            {
                Debug.LogError("Janelia.KinematicSubject.updater must be set.");
                Application.Quit();
            }

            // Set up the collision handler to act on this `GameObject`'s transform.
            _collisionHandler = new KinematicCollisionHandler(transform, collisionPlaneNormal, collisionRadius);

            if (ConfigurePlayback())
            {
                StartPlayback();
            }

#if LOG_ALL_MESHES
            LogUtilities.LogAllMeshes();
#endif
        }

        public void Update()
        {
#if SUPPORT_KEYBOARD_SHORTCUTS
            if (Input.GetKey("l"))
            {
                StartPlayback();
            }
#endif

            _framesBeingStill++;

            _currentTransformation.actualTranslation = new Vector3();
            _currentTransformation.actualTranslation = new Vector3();
            _currentTransformation.rotationDegs = new Vector3();
            bool addToLog = false;

            if (!_playbackActive)
            {
                updater.Update();

                Vector3? translation = updater.Translation();
                if (translation != null)
                {
                    // Let the collision handler correct the translation, with approximated sliding contact,
                    // and apply it to this `GameObject`'s transform.  The corrected translation is returned.
                    Vector3 actualTranslation = _collisionHandler.Translate((Vector3)translation);
                    _currentTransformation.attemptedTranslation = (Vector3)translation;
                    _currentTransformation.actualTranslation = actualTranslation;

                    if (debug)
                    {
                        Debug.Log("frame " + Time.frameCount + ": translation " + translation + " becomes " + actualTranslation);
                    }

                    addToLog = true;
                    _framesBeingStill = 0;
                }

                Vector3? rotation = updater.RotationDegrees();
                if (rotation != null)
                {
                    transform.Rotate((Vector3)rotation);
                    _currentTransformation.rotationDegs = (Vector3)rotation;

                    addToLog = true;
                    _framesBeingStill = 0;
                }
            }
            else
            {
                Transformation? transformation = CurrentPlaybackTransformation();
                if (transformation != null)
                {
                    _currentTransformation = (Transformation)transformation;

                    transform.position = _currentTransformation.worldPosition;
                    transform.eulerAngles = _currentTransformation.worldRotationDegs;

                    addToLog = true;
                    _framesBeingStill = 0;
                }
            }

            if (addToLog)
            {
                _currentTransformation.worldPosition = transform.position;
                _currentTransformation.worldRotationDegs = transform.eulerAngles;
                Logger.Log(_currentTransformation);
            }

            _framesSinceLogWrite++;
            bool writeLog = false;
            if (writeLogWhenStill)
            {
                if ((_framesBeingStill >= stillFrames) && (_framesSinceLogWrite > minWriteInterval))
                {
                    writeLog = true;
                    if (debug)
                    {
                        Debug.Log("Frame " + Time.frameCount + ", writing log: still for " + _framesBeingStill + " frames, and " +
                            _framesSinceLogWrite + " frames since last write");
                    }
                }
            }
            if (_framesSinceLogWrite >= maxWriteInterval)
            {
                writeLog = true;
                if (debug)
                {
                    Debug.Log("Frame " + Time.frameCount + ", writing log: " + _framesSinceLogWrite + " frames since last write");
                }
            }

            LogUtilities.LogDeltaTime();

            if (writeLog)
            {
                Logger.Write();
                _framesSinceLogWrite = 0;
                _framesBeingStill = 0;
            }
        }

#if UNITY_EDITOR
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Janelia.KinematicSubject.OnPostprocessBuild: " + pathToBuiltProject);

            string builtNoExt = pathToBuiltProject.Substring(0, pathToBuiltProject.Length - 4);
            string[] builtNoExtSplit = builtNoExt.Split('/');
            int length = builtNoExtSplit.Length;
            string scriptPath = builtNoExtSplit[0];
            for (int i = 1; i < length - 2; i++)
            {
                scriptPath += "\\" + builtNoExtSplit[i];
            }
            string standaloneNameNoExt = builtNoExtSplit[length - 1];
            scriptPath += "\\" + standaloneNameNoExt + "Playback.wsf";
            Debug.Log("Script: " + scriptPath);

            MakeLauncherScript(scriptPath, standaloneNameNoExt);
        }

        private static void MakeLauncherScript(string scriptPath, string standaloneName)
        {
            string[] lines = {
                "<job id='standalonePlayback'>",
                "  <script language='VBScript'>",
                "    Function WSHInputBox(Message, Title, Value)",
                "      WSHInputBox = InputBox(Message, Title, Value)",
                "    End Function",
                "  </script>",
                "  <script language='JScript'>",
                "    var fileSys = new ActiveXObject('Scripting.FileSystemObject');",
                "    var path = fileSys.GetParentFolderName(WScript.ScriptFullName);",
                "    var standalone = path + '\\\\" + standaloneName + ".lnk';",
                "    if (!fileSys.FileExists(standalone)) {",
                "      standalone = path + '\\\\Build\\\\" + standaloneName + ".exe';",
                "    }",
                "    var shell = WScript.CreateObject('WScript.Shell');",
                "    var title = 'Replay from a log file?';",
                "    var prompt = 'Log file: ';",
                "    var result = WSHInputBox(prompt, title, '[previous]');",
                "    if (result != null) {",
                "      var args = ' -playback';",
                "      if (result != '[previous]') {",
                "        args += ' ' + result;",
                "      }",
                "      shell.Run(standalone + args);",
                "    }",
                "  </script>",
                "</job>"
            };

            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }

            using (StreamWriter outputFile = new StreamWriter(scriptPath))
            {
                foreach (string line in lines)
                    outputFile.WriteLine(line);
            }
        }
#endif

        private bool ConfigurePlayback()
        {
#if SUPPORT_COMMANDLINE_ARGUMENTS
            _playbackLogFile = Logger.previousLogFile;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-playback")
                {
                    if (i + 1 < args.Length)
                    {
                        _playbackLogFile = args[i + 1];
                        if (!_playbackLogFile.Contains('/') && !_playbackLogFile.Contains('\\'))
                        {
                            _playbackLogFile = Logger.logDir + "/" + _playbackLogFile;
                        }
                        if (!File.Exists(_playbackLogFile))
                        {
                            Debug.Log("Cannot find playback log file '" + _playbackLogFile + "'");
                            return false;
                        }
                    }
                    return true;
                }
            }
#endif
            return false;
        }

        private void StartPlayback()
        {
            if (!_playbackActive)
            {
                Debug.Log("Playing back log file '" + _playbackLogFile + "'");

                _playbackLogEntries = Logger.Read<Transformation>(_playbackLogFile);
                _playbackLogEntries = Filter(_playbackLogEntries);

                _playbackLogIndex = 0;
                _playbackStartFrame = Time.frameCount;
                _playbackActive = true;
            }
        }

        private List<Logger.Entry<Transformation>> Filter(List<Logger.Entry<Transformation>> l)
        {
            return l.Where(x => x.data.attemptedTranslation != Vector3.zero || x.data.rotationDegs != Vector3.zero).ToList();
        }

        private Transformation? CurrentPlaybackTransformation()
        {
            if (_playbackActive && (_playbackLogEntries != null))
            {
                int adjustedFrame = Time.frameCount - _playbackStartFrame;
                while (_playbackLogIndex < _playbackLogEntries.Count)
                {
                    if (_playbackLogEntries[_playbackLogIndex].frame < adjustedFrame)
                    {
                        _playbackLogIndex++;
                    }
                    else
                    {
                        if (_playbackLogEntries[_playbackLogIndex].frame == adjustedFrame)
                        {
                            return _playbackLogEntries[_playbackLogIndex].data;
                        }
                        return null;
                    }
                }
            }
            _playbackActive = false;
            return null;
        }

        private KinematicCollisionHandler _collisionHandler;

        // To make `Janelia.Logger.Log<T>()`'s call to JsonUtility.ToJson() work correctly,
        // the `T` must be marked `[Serlializable]`, but its individual fields need not be
        // marked `[SerializeField]`.  The individual fields must be `public`, though.
        [Serializable]
        internal struct Transformation
        {
            public Vector3 attemptedTranslation;
            public Vector3 actualTranslation;
            public Vector3 worldPosition;
            public Vector3 rotationDegs;
            public Vector3 worldRotationDegs;
        };

        private Transformation _currentTransformation;

        private int _framesSinceLogWrite = 0;
        private int _framesBeingStill = 0;

        private bool _playbackActive = false;
        private string _playbackLogFile = "";
        private List<Logger.Entry<Transformation>> _playbackLogEntries;
        private int _playbackLogIndex;
        private int _playbackStartFrame;
    }
}
