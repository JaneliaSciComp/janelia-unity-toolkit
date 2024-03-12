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
    public static class SessionParameters
    {
        public const int POST_PROCESS_BUILD_ORDER = 3;

        // Must be called from a function marked `[PostProcessBuildAttribute(N)]`
        // where `N` greater than Janelia.Parameters.POST_PROCESS_BUILD_ORDER.
        // Note that these parameters work better in a standalone executable than in
        // a game running in the editor.  Specifically, the user interface for setting
        // parameters appears only in the launcher script for a standalone executable,
        // not when using the editor.  So a game running in the editor will use the
        // parameter value set when the game last ran as a standalone executable.
        public static void AddStringParameter(string key, string defaultValue)
        {
            if ((_keysValuesFloat.Count == 0) && (_keysValuesString.Count == 0))
            {
                Load(_buildDir);
            }
            if (!_keysValuesString.ContainsKey(key))
            {
                _keysValuesString[key] = defaultValue;
                AddToFile("\"" + key + "\"", "\"" + defaultValue + "\"");
            }

            // Force the log directory to exist, because the launcher script will copy
            // `_filename` from `_buildDir` to the log directory.
            string _ = Logger.logDirectory;
        }

        // See the comments for `AddStringParameter`.
        public static void AddFloatParameter(string key, float defaultValue)
        {
            if ((_keysValuesFloat.Count == 0) && (_keysValuesString.Count == 0))
            {
                Load(_buildDir);
            }
            if (!_keysValuesFloat.ContainsKey(key))
            {
                _keysValuesFloat[key] = defaultValue;
                AddToFile("\"" + key + "\"", defaultValue.ToString());
            }

            // Force the log directory to exist, because the launcher script will copy
            // `_filename` from `_buildDir` to the log directory.
            string _ = Logger.logDirectory;
        }

        public static string GetStringParameter(string key)
        {
            return (_keysValuesString.ContainsKey(key)) ? _keysValuesString[key] : "";
        }

        public static float GetFloatParameter(string key, float defaultValue = 0)
        {
            return (_keysValuesFloat.ContainsKey(key)) ? _keysValuesFloat[key] : defaultValue;
        }

        // Using `BeforeSceneLoad` should make this method get called before methods with
        // no argument to `RuntimeInitializeOnLoadMethod`, which is how methods calling
        // `GetFloatParameter` or `GetStringParameter` should be marked.  Thus, the
        // `Load` call should happen before the `Get` calls, as needed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeMethodLoad()
        {
            Load(Logger.logDirectory);
            Log();
        }

#if UNITY_EDITOR
        // The attribute value orders this function after `Logger.OnPostprocessBuildStart` and
        // before `Logger.OnPostprocessBuildFinish`.
        [PostProcessBuildAttribute(POST_PROCESS_BUILD_ORDER)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Janelia.Parameters.OnPostprocessBuild: " + pathToBuiltProject);

            _buildDir = Path.GetDirectoryName(pathToBuiltProject);

            // Any other code calling `AddFloatParameter()` or `AddStringParameter()` must have
            // a `PostProcessBuildAttribute` value greater than this one.
            _keysValuesFloat.Clear();
            _keysValuesString.Clear();

            string n = System.Environment.NewLine;
            string html =
              "      <div>" + n +
              "        Session parameters:" + n +
              "        <textarea id='id_textareaParameters' rows=5 style='width:100%; overflow-y: scroll'></textarea>" + n +
              "      </div>";
            string scriptBlock =
                "    <script language='javascript'>" + n +
                "      var textareaParameters = document.getElementById('id_textareaParameters');" + n +
                "      var fso = new ActiveXObject('Scripting.FileSystemObject');" + n +
                "      var parametersPath = LogDir() + '\\\\" + _filename + "';" + n +
                "      if (!fso.FileExists(parametersPath)) {" + n +
                "        var buildDir = '" + _buildDir.Replace("\\", "\\\\") + "';" + n +
                "        var parametersAtBuildPath = buildDir + '\\\\" + _filename + "';" + n +
                "        if (fso.FileExists(parametersAtBuildPath)) {" + n +
                "          fso.CopyFile(parametersAtBuildPath, parametersPath);" + n +
                "        } else {" + n +
                "          alert('Missing \"" + _filename + "\".  Please rebuild.');" + n +
                "        }" + n +
                "      }" + n +
                "      if (fso.FileExists(parametersPath)) {" + n +
                "        var tso = fso.OpenTextFile(parametersPath, 1);" + n +
                "        if (tso) {" + n +
                "          var text = tso.ReadAll();" + n +
                "          textareaParameters.value = text;" + n +
                "        }" + n +
                "      }" + n +
                "      function writeParametersFile()" + n +
                "      {" + n +
                "        var tso = fso.OpenTextFile(parametersPath, 2, true);" + n +
                "        if (tso) {" + n +
                "          var text = textareaParameters.value;" + n +
                "          tso.WriteLine(text);" + n +
                "        }" + n +
                "      }" + n +
                "    </script>";
            string onRunAppFuncName = "writeParametersFile";
            Logger.AddLauncherOtherPlugin(html, scriptBlock, onRunAppFuncName);
        }
#endif

        private static void AddToFile(string key, string value)
        {
            string path = Path.Combine(_buildDir, _filename);

            string[] lines;
            if (File.Exists(path))
            {
                lines = File.ReadAllLines(path);
            }
            else
            {
                lines = new string[]
                {
                    "{",
                    "}"
                };
            }
            using (StreamWriter outputFile = new StreamWriter(path))
            {
                for (int i = 0; i < lines.Length; ++i)
                {
                    string line = lines[i];
                    outputFile.WriteLine(line);
                    if (line.Trim() == "{")
                    {
                        string addedLine = "  " + key + ": " + value;
                        string nextLine = (i + 1 < lines.Length) ? lines[i + 1] : "";
                        addedLine += (nextLine.Trim() != "}") ? "," : "";
                        outputFile.WriteLine(addedLine);
                    }
                }
            }
        }

        private static void Load(string dir)
        {
            string path = Path.Combine(dir, _filename);
            if (File.Exists(path))
            {
                using (StreamReader inputFile = new StreamReader(path))
                {
                    string line;
                    while ((line = inputFile.ReadLine()) != null)
                    {
                        line = line.Trim().Trim(',');
                        if ((line == "{") || (line == "}"))
                        {
                            continue;
                        }
                        if (line.Length == 0)
                        {
                            Debug.Log("Skipping blank line in " + _filename);
                            continue;
                        }
                        string[] split = line.Split(new char[] { ':' }, 2).Select(x => x.Trim()).ToArray();
                        if (split.Length != 2)
                        {
                            Debug.LogError("Invalid " + _filename + " line (expecting one ':'): '" + line + "'");
                            continue;
                        }
                        if (!IsJsonString(split[0]))
                        {
                            Debug.LogError("Invalid " + _filename + " line (key must be quoted): '" + line + "'");
                            continue;
                        }
                        string key = split[0].Trim('"');
                        if (IsJsonString(split[1]))
                        {
                            string value = split[1].Trim('"');
                            _keysValuesString.Add(key, value);
                        }
                        else
                        {
                            float value = float.Parse(split[1]);
                            _keysValuesFloat.Add(key, value);
                        }
                    }
                }
            }
        }

        private static bool IsJsonString(string s)
        {
            return ((s.First() == '"') && (s.Last() == '"'));
        }

        private static void Log()
        {
            ParametersLogEntry entry = new ParametersLogEntry();

            entry.sessionParameters = new List<string>();

            foreach (KeyValuePair<string, float> pair in _keysValuesFloat)
            {
                entry.sessionParameters.Add(pair.Key + ": " + pair.Value.ToString());
            }
            foreach (KeyValuePair<string, string> pair in _keysValuesString)
            {
                entry.sessionParameters.Add(pair.Key + ": " + pair.Value);
            }

            Logger.Log(entry);
        }

        [Serializable]
        internal class ParametersLogEntry : Logger.Entry
        {
            public List<string> sessionParameters;
        };

        private const string _filename = "SessionParameters.json";
        private static string _buildDir;
        private static Dictionary<string, string> _keysValuesString = new Dictionary<string, string>();
        private static Dictionary<string, float> _keysValuesFloat = new Dictionary<string, float>();
    }
}
