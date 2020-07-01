// Manages a log of application activity.  When the application starts, a
// new log is created, and code in the application can add JSON objects 
// to the log at any point.  Code can also force the log to be written
// to a file, or that writing happens automatically when the application
// quits.  The log is stored in the same directory as the standard
// log of strings that Unity creates for calls to `Debug.Log()`.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Janelia
{
    public class Logger
    {
        public static string logDir;

        public static string currentLogFile
        {
            get { return _currentLogFile; }
        }
        public static string previousLogFile
        {
            get { return _previousLogFile; }
        }

        // A log entries consists of an instance of this class for general information,
        // with an embeded instance of the struct/class `T` for specific information.
        // Due to the way Unity serializes JSON, `T` cannot itself be a generic.
        [Serializable]
        public class Entry<T>
        {
            // The elapsed time since the application started.
            [SerializeField]
            public float timeSecs;

            // The current frame (starting at 1).
            [SerializeField]
            public float frame;

            [SerializeField]
            public T data;
        }

        // Add some specific information to the log as of the current frame, where the
        // type `T` describes the specific information.  Due to the way Unity serializes
        // objects as JSON, `T` cannot be a generic (i.e., a class with a `<U>` of its own).
        public static void Log<T>(T data)
        {
            Entry<T> entry = new Entry<T>();
            entry.timeSecs = Time.time;
            entry.frame = Time.frameCount;
            entry.data = data;

            string s = JsonUtility.ToJson(entry, true);
            _entries.Add(s);
        }

        // Force the entries currently in the log to be written to a file.  The log is
        // then cleared, so those entries will not be written again.
        public static void Write()
        {
            if (_entries.Count > 0)
            {
                _wroteIndicator.numberOfEntriesWritten = _entries.Count;
                Log(_wroteIndicator);

                for (int i = 0; i < _entries.Count; i++)
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

                _entries.Clear();
            }
        }

        // Read the log from the file at `path`, and return each entry interpreted as an
        // instance of the type, `T`.  Any entry that is not really an instance of `T`
        // will become an instance of the default value of `T`.
        public static List<Entry<T>> Read<T>(string path)
        {
            List<Entry<T>> result = new List<Entry<T>>();
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
                            Entry<T> entry = JsonUtility.FromJson<Entry<T>>(s1);
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

        // Executed after `Awake` methods and before `Start` methods.
        [RuntimeInitializeOnLoadMethod]
        private static void OnRuntimeMethodLoad()
        {
            Debug.Log("Logger: starting");

            _entries = new List<string>(4096);

            DateTime now = DateTime.Now;
            logDir = Environment.GetEnvironmentVariable("AppData") + "/" +
                "../LocalLow/" + Application.companyName + "/" + Application.productName;
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            _previousLogFile = "";
            DirectoryInfo dirInfo = new DirectoryInfo(logDir);
            System.IO.FileInfo[] files = dirInfo.GetFiles("*.json");
            if (files.Length > 0)
            {
                _previousLogFile = files.OrderByDescending(f => f.LastWriteTime).First().FullName;
            }

            string filename = "Log_" + now.Year + "-" + now.Month + "-" + now.Day + "_" +
                now.Hour + "-" + now.Minute + "-" + now.Second + ".json";
            string path = logDir + "/" + filename;

            _currentLogFile = path;

            _writer = new StreamWriter(path);

            _writer.Write("[\n");
            _writer.Flush();

            Application.quitting += ApplicationQuitting;
        }

        private static void ApplicationQuitting()
        {
            Debug.Log("Logger: stopping");

            Write();
            _writer.Write("\n]\n");
            _writer.Flush();
            _writer.Dispose();
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

        private static StreamWriter _writer;
        private static bool _firstWrite = true;

        // A special log entry added each time the log is written, indicating how many
        // elements were written.
        [Serializable]
        private struct WroteIndicator
        {
            public int numberOfEntriesWritten;
        };
        static private WroteIndicator _wroteIndicator = new WroteIndicator();

        private static List<string> _entries;

        private static string _currentLogFile;
        private static string _previousLogFile;
    }
}
