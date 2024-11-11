// Manages a log of application activity.  When the application starts, a
// new log is created, and code in the application can add JSON objects
// to the log at any point.  Code can also force the log to be written
// to a file, or that writing happens automatically when the application
// quits.  The log is stored in the same directory as the standard
// log of strings that Unity creates for calls to `Debug.Log()`.

// Also creates a launcher script, which first presents a dialog box for configuring
// the application and then runs the application when the user closes the dialog.
// The launcher script is created in the main project directory and its name has the
// suffix "Launcher.hta".  The dialog appears on the "console" display (i.e., the
// display where the script is run, not the external displays where the application's
// content appears).  By default, the dialog contains a text input for adding
// "header notes" to be saved at the beginning of the log.  Optionally, other packages
// can add other user interface to the launcher with the `Logger.AddLauncherRadioButtonPlugin`
// and `Logger.AddLauncherOtherPlugin` functions (see below).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace Janelia
{
    // A static class to support logging.  There is no need to explicitly create an instance of this class
    // or start the logging, or end the logging: that functionality happens automatically.  Also has a
    // post-process build function to create the launcher script.
    public class Logger
    {
        public static bool enable = true;

        public static int maxLogEntries = 4096;

        public static string logDirectory
        {
            get { return getLogDirectory(); }
        }

        public static string currentLogFile
        {
            get { return _currentLogFile; }
        }

        public static string previousLogFile
        {
            get { return _previousLogFile; }
        }

        // Base class for log entries.
        [Serializable]
        public class Entry
        {
            // The elapsed time since the application started.
            [SerializeField]
            public float timeSecs;

            // The current frame (starting at 1).
            [SerializeField]
            public float frame;

            // The elapsed time since the end of the "Made with Unity" splash screen,
            // which appears when a standalone executable starts running.
            [SerializeField]
            public float timeSecsAfterSplash;

            // The number of frames since the end of the splash screen (starting at 0).
            [SerializeField]
            public float frameAfterSplash;
        }

        // Adds a log entry, saved as JSON.  The `entry` should be an instance of
        // a class derived from `Entry`, adding the data to be logged.
        public static void Log(Entry entry)
        {
            if (enable)
            {
                InitIfNeeded();

                entry.timeSecs = Time.time;
                entry.frame = Time.frameCount;

                if (!_splashIsFinished)
                {
                    _splashIsFinished = SplashScreen.isFinished;
                    if (_splashIsFinished)
                    {
                        _timeSecsSplashFinished = Time.time;
                        _frameSplashFinished = Time.frameCount;
                    }
                }
                entry.timeSecsAfterSplash = entry.timeSecs - _timeSecsSplashFinished;
                entry.frameAfterSplash = entry.frame - _frameSplashFinished;

                // This `JsonUtility.ToJson` function is supposed to be the most efficient way to produce
                // a JSON string in Unity, but it does do a heap allocation (i.e., GC memory) for the string
                // it returns.  Thus logging will eventually trigger garbage collection, but in practice,
                // profiling indicates the time involved seems to be only a millisecond or so.
                string s = JsonUtility.ToJson(entry, true);
                _entries[_iEntries++] = s;
                // Subtract 1 because `Write()` logs one item of its own.
                if (_iEntries == _entries.Length - 1)
                {
                    Write();
                }
            }
        }

        // Force the entries currently in the log to be written to a file.  The log is
        // then reset, so those entries will not be written again.
        public static void Write()
        {
            if (enable && !_writing)
            {
                _writing = true;

                if (_iEntries > 0)
                {
                    _wroteIndicator.numberOfEntriesWritten = _iEntries;
                    Log(_wroteIndicator);

                    for (int i = 0; i < _iEntries; i++)
                    {
                        if (_firstWrite)
                        {
                            _firstWrite = false;
                        }
                        else
                        {
                            _writer.Write(",\n");
                        }
                        _writer.Write(_entries[i]);
                    }
                    _writer.Flush();
                    // Don't actually clear the array, so its current entries become garbage
                    // more gradually, which may make garbage collection performance more consistent.
                    _iEntries = 0;
                }

                _writing = false;
            }
        }

        public static List<T> Read<T>(string path) where T : Entry
        {
            List<T> result = new List<T>();
            if (File.Exists(path))
            {
                try
                {
                    string s = File.ReadAllText(path);
                    int i0 = 1;
                    int i1 = 1;
                    bool keepGoing = true;
                    while (keepGoing)
                    {
                        if (FindTopLevelObject(s, ref i0, out i1))
                        {
                            string s1 = s.Substring(i0, i1 - i0 + 1);
                            i0 = i1 + 1;
                            T entry = JsonUtility.FromJson<T>(s1);
                            result.Add(entry);
                        }
                        else
                        {
                            keepGoing = false;
                        }
                    }
                    return result;
                }
                catch (System.IO.IOException e)
                {
                    Debug.Log("Logger.Read failed: '" + e.Message + "'");
                }
            }
            return result;
        }

        // Plugs in some launcher capabilities.  The launcher will have a new radio button with label text `radioButtonLabel`.
        // Optionally, next to this radio button will be the additional components specified by `radioButtonOtherHTML`.
        // When the launcher dialog `Continue` button is pressed with this radio button selected, then the function named
        // `radioButtonFuncName` will be called.  That function should be defined in the Javascript code block
        // `scriptBlockWithRadioButtonFunc`.  That function can run the application by calling `runApp(extraArgs)` where
        // `extraArgs` is a string defining any extra command-line arguments.
        public static void AddLauncherRadioButtonPlugin(string radioButtonLabel, string radioButtonOtherHTML, string radioButtonFuncName,
            string scriptBlockWithRadioButtonFunc)
        {
            _launcherRadioButtonPlugins.Add(new LauncherRadioButtonPlugin
            {
                radioButtonLabel = radioButtonLabel,
                radioButtonOtherHTML = radioButtonOtherHTML,
                radioButtonFuncName = radioButtonFuncName,
                scriptBlockWithRadioButtonFunc = scriptBlockWithRadioButtonFunc
            });
        }

        public static void AddLauncherOtherPlugin(string html, string scriptBlock, string onRunAppFuncName)
        {
            _launcherOtherPlugins.Add(new LauncherOtherPlugin
            {
                html = html,
                scriptBlock = scriptBlock,
                onRunAppFuncName = onRunAppFuncName
            });
        }

        private static string getLogDirectory()
        {
            if (_logDirectory == null)
            {
                string[] path = {Environment.GetEnvironmentVariable("AppData"), @"..\", getLogDirectorySuffix()};
                _logDirectory = Path.GetFullPath(Path.Combine(path));
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            return _logDirectory;
        }

        private static string getLogDirectorySuffix()
        {
          if (_logDirectorySuffix == null)
          {
            string[] path = {"LocalLow", Application.companyName, Application.productName };
            _logDirectorySuffix = Path.Combine(path);
          }
          return _logDirectorySuffix;
        }

        // `AfterSceneLoad` is the latest runtime initialize method.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnRuntimeMethodLoad()
        {
            Debug.Log("Application run as: " + System.Environment.CommandLine);

            InitIfNeeded();
            LogUtilities.LogCurrentResolution();
        }

        private static void InitIfNeeded()
        {
            if (_entries != null)
            {
                return;
            }

            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Contains("-noLog") || args.Contains("-nolog"))
            {
                enable = false;
            }

            LogOptions options = LogUtilities.GetOptions();
            if (options != null)
            {
                enable = options.EnableLogging;
            }
            if (enable)
            {
                Debug.Log("Logger: starting");

                _entries = new string[maxLogEntries];

                _previousLogFile = "";
                DirectoryInfo dirInfo = new DirectoryInfo(logDirectory);
                System.IO.FileInfo[] files = dirInfo.GetFiles("*.json");
                if (files.Length > 0)
                {
                    _previousLogFile = files.OrderByDescending(f => f.LastWriteTime).First().FullName;
                }

                string path = logDirectory + "/" + logFilename();
                _currentLogFile = path;

                _writer = new StreamWriter(path);

                _writer.Write("[\n");
                _writer.Flush();

                Application.quitting += ApplicationQuitting;

                string argsStr = String.Join(" ", System.Environment.GetCommandLineArgs());
                Debug.Log("Logger: executable run as: " + argsStr);

                AddLogHeader();
            }
        }

        private static string logFilename()
        {
            string extra = "";
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "-logFilenameExtra") && (i + 1 < args.Length))
                {
                    extra = args[i + 1];
                    string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                    foreach (char c in invalid)
                    {
                        extra = extra.Replace(c.ToString(), "");
                    }
                    if (!extra.StartsWith("_"))
                    {
                      extra = "_" + extra;
                    }
                    break;
                }
            }

            DateTime now = DateTime.Now;
            string filename = "Log_" + now.ToString("yyyy") + "-" + now.ToString("MM") + "-" +
                now.ToString("dd") + "_" + now.ToString("HH") + "-" + now.ToString("mm") + "-" +
                now.ToString("ss") + extra + ".json";

            return filename;
        }

        private static void ApplicationQuitting()
        {
            if (enable)
            {
                Debug.Log("Logger: stopping");

                Write();
                _writer.Write("\n]\n");
                _writer.Flush();
                _writer.Dispose();
            }
        }

        private static bool FindTopLevelObject(string s, ref int i0, out int i1)
        {
            i1 = i0;
            while (s[i0] != '{')
            {
                if (++i0 == s.Length)
                {
                    return false;
                }
            }
            i1 = i0;
            int n = 1;
            while (n != 0)
            {
                if (++i1 == s.Length)
                {
                    return false;
                }
                n += (s[i1] == '{') ? 1 : (s[i1] == '}') ? -1 : 0;
            }
            return true;
        }

#if UNITY_EDITOR
        // The attribute value orders this function first among the scripts run after building,
        // so plugin support can be initialized.
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuildStart(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Janelia.Logger.OnPostprocessBuildStart: " + pathToBuiltProject);

            _launcherRadioButtonPlugins = new List<LauncherRadioButtonPlugin>();
            _launcherOtherPlugins = new List<LauncherOtherPlugin>();
        }

        // The attribute value orders this function last among the scripts run after building,
        // so capabilities added by plugins can be incorporated.
        [PostProcessBuildAttribute(999)]
        public static void OnPostprocessBuildFinish(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Janelia.Logger.OnPostprocessBuildFinish: " + pathToBuiltProject);

            string standaloneNameNoExt = Path.GetFileNameWithoutExtension(pathToBuiltProject);
            string projectDir = Directory.GetCurrentDirectory();
            string scriptPath = Path.Combine(projectDir, standaloneNameNoExt + "Launcher.hta");
            Debug.Log("scriptPath: " + scriptPath);

            string scriptTemplatePath = Path.GetFullPath("Packages/org.janelia.logging/Editor/launcherScriptTemplate.hta");

            string standalonePath = pathToBuiltProject;

            // TODO: Rather than using this cheap and cheerful approach to find the shortcut file created
            // by org.janelia.camera-utilities' `AdjoiningDisplaysCameraBuilder`, it might be better to
            // check all files in `projectDir` for a shortcut with `Arguments` mentioning the executable.
            string shortcutPath = Path.Combine(projectDir, standaloneNameNoExt + ".lnk");
            Debug.Log("Checking for shortcut: " + shortcutPath);
            // TODO: Unfortunately, initially creating the shortcut file seems to take some time, and it
            // may not be finished when this code is run.  So if no shortcut is found, sleep for a few
            // seconds and check again.  Is there a better solution?
            if (!File.Exists(shortcutPath)) {
                Thread.Sleep(2000);
            }
            if (File.Exists(shortcutPath)) {
                Debug.Log("Using shortcut");
            }

            MakeLauncherScript(scriptTemplatePath, scriptPath, shortcutPath, standalonePath);
        }

        // The launcher script is a Microsoft "HTML Application" ("HTA"), with HTML and
        // JScript (Javascript) code that gets run by Internet Explorer:
        // https://en.wikipedia.org/wiki/HTML_Application
        // This implementation has the advantage that it runs on any modern Windows system
        // without requiring the installation of any additional software.
        // TODO: The disadvantage is that this implementation works on Windows only, so
        // there should be an alternative implementation is other platforms are to be supported.

        private static void MakeLauncherScript(string scriptTemplatePath, string scriptPath, string shortcutPath, string standalonePath)
        {
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }

            string logDirSuffixStr = getLogDirectorySuffix().Replace("\\", "\\\\");
            string shortcutPathStr = shortcutPath.Replace("\\", "\\\\");
            string standalonePathStr = standalonePath.Replace("\\", "\\\\");

            // The HTML and JScript code is in a separate "template" file, which has
            // special directives that get replaced with application-specific details
            // like the location of the log directory.  There are also directives for
            // plugin code.

            List<string> lines = new List<string>();
            using (StreamReader inputFile = new StreamReader(scriptTemplatePath))
            {
                string line;
                while ((line = inputFile.ReadLine()) != null)
                {
                    if (line.Contains("PLUGIN_RADIO_BUTTONS"))
                    {
                        line = MakeLauncherPluginRadioButtons();
                    }
                    else if (line.Contains("CALL_PLUGIN_FUNCTIONS"))
                    {
                        line = MakeLauncherPluginFunctionCalls();
                    }
                    else if (line.Contains("PLUGIN_SCRIPT_BLOCKS"))
                    {
                        line = MakeLauncherPluginScriptBlocks();
                    }
                    else if (line.Contains("PLUGIN_OTHER_UI"))
                    {
                        line = MakeLauncherPluginOtherUI();
                    }
                    else if (line.Contains("CALL_PLUGIN_OTHER_FUNCTIONS"))
                    {
                        line = MakeLauncherPluginOtherFunctionCalls();
                    }
                    else if (line.Contains("LOG_DIR_SUFFIX"))
                    {
                        line = line.Replace("LOG_DIR_SUFFIX", logDirSuffixStr);
                    }
                    else if (line.Contains("SHORTCUT_PATH"))
                    {
                        line = line.Replace("SHORTCUT_PATH", shortcutPathStr);
                    }
                    else if (line.Contains("STANDALONE_PATH"))
                    {
                        line = line.Replace("STANDALONE_PATH", standalonePathStr);
                    }
                    if (line.Length > 0)
                    {
                        lines.Add(line);
                    }
                }
            }

            using (StreamWriter outputFile = new StreamWriter(scriptPath))
            {
                foreach (string line in lines)
                    outputFile.WriteLine(line);
            }
        }

        private static string MakeLauncherPluginRadioButtons()
        {
            string result = "";
            string n = System.Environment.NewLine;
            int index = 2;
            foreach (LauncherRadioButtonPlugin plugin in _launcherRadioButtonPlugins)
            {
               result += (!String.IsNullOrEmpty(result)) ? n : "";
               result +=
                    "      <div>" + n +
                    "        <label>" + n +
                    "          <input type='radio' name='radios' id='" + index + "' />" + n +
                    "          " + plugin.radioButtonLabel + n +
                    "        </label>" + n;
                if (!String.IsNullOrEmpty(plugin.radioButtonOtherHTML))
                {
                    result +=
                        "        <div style='padding-left:30px'>" + n +
                                 plugin.radioButtonOtherHTML + n +
                        "        </div>" + n;
                }
                result +=
                    "      </div>";
                index += 1;
            }
            return result;
        }

        private static string MakeLauncherPluginFunctionCalls()
        {
            string result = "";
            string n = System.Environment.NewLine;
            int index = 2;
            foreach (LauncherRadioButtonPlugin plugin in _launcherRadioButtonPlugins)
            {
                result += (!String.IsNullOrEmpty(result)) ? n : "";
                result +=
                    "        else if (r[" + index + "].checked)" + n +
                    "          " + plugin.radioButtonFuncName + "();";
                index += 1;
            }
            return result;
        }

        private static string MakeLauncherPluginScriptBlocks()
        {
            string result = "";
            string n = System.Environment.NewLine;
            foreach (LauncherRadioButtonPlugin plugin in _launcherRadioButtonPlugins)
            {
                result += (!String.IsNullOrEmpty(result)) ? n : "";
                result += plugin.scriptBlockWithRadioButtonFunc;
            }
            foreach (LauncherOtherPlugin plugin in _launcherOtherPlugins)
            {
                result += (!String.IsNullOrEmpty(result)) ? n : "";
                result += plugin.scriptBlock;
            }
            return result;
        }

        private static string MakeLauncherPluginOtherUI()
        {
            string result = "";
            string n = System.Environment.NewLine;
            foreach (LauncherOtherPlugin plugin in _launcherOtherPlugins)
            {
                result += (!String.IsNullOrEmpty(result)) ? n : "";
                result += plugin.html;
            }
            return result;
        }

        private static string MakeLauncherPluginOtherFunctionCalls()
        {
            string result = "";
            string n = System.Environment.NewLine;
            foreach (LauncherOtherPlugin plugin in _launcherOtherPlugins)
            {
                result += (!String.IsNullOrEmpty(result)) ? n : "";
                result += "        " + plugin.onRunAppFuncName + "();";
            }
            return result;
        }
#endif

        private static void AddLogHeader()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-addLogHeader")
                {
                    string logHeaderPath = Path.Combine(logDirectory, "logHeader.txt");
                    using (StreamReader inputFile = new StreamReader(logHeaderPath))
                    {
                        _logHeader.headerNotes = "";
                        string line;
                        while ((line = inputFile.ReadLine()) != null)
                        {
                            _logHeader.headerNotes += line + " ";
                        }
                        Log(_logHeader);
                    }
                }
            }
        }

        private static bool _splashIsFinished = false;
        private static float _timeSecsSplashFinished;
        private static float _frameSplashFinished;

        private static StreamWriter _writer;
        private static bool _firstWrite = true;

        // A special log entry added each time the log is written, indicating how many
        // elements were written.
        [Serializable]
        private class WroteIndicator : Entry
        {
            public int numberOfEntriesWritten;
        };
        static private WroteIndicator _wroteIndicator = new WroteIndicator();

        private static string[] _entries;
        private static int _iEntries = 0;
        private static bool _writing = false;

        private static string _logDirectory;
        private static string _logDirectorySuffix;
        private static string _currentLogFile;
        private static string _previousLogFile;

        [Serializable]
        private class LogHeader : Entry
        {
            public string headerNotes;
        };
        static private LogHeader _logHeader = new LogHeader();

        private struct LauncherRadioButtonPlugin
        {
            public string radioButtonLabel;
            public string radioButtonOtherHTML;
            public string radioButtonFuncName;
            public string scriptBlockWithRadioButtonFunc;
        }
        private static List<LauncherRadioButtonPlugin> _launcherRadioButtonPlugins;

        private struct LauncherOtherPlugin
        {
            public string html;
            public string scriptBlock;
            public string onRunAppFuncName;
        }
        private static List<LauncherOtherPlugin> _launcherOtherPlugins;
    }
}
