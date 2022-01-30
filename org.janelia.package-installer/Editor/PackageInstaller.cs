using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Janelia
{
    // Installs the package, `P`, chosen by the user, after first installing all the packages (from the
    // same repository) on which `P` depends.  Dependencies are determined using the `"references"` section
    // of a package's .asmdef files in its `Editor` and `Runtime` subdirectories.

    // Also installs a set of packages (and dependencies) specified in a "manifest" file in JSON format.
    // The manifest is a simple JSON array of package paths, or names that are expanded into paths using
    // simple heuristics.  For example, in the following excerpt from a file system, the packages in
    // `manifest1.json` and `manifest2.json` will be resolved:
    // Heuristic 1:
    //   VR/
    //      janelia-unity-toolkit/
    //                            org.janelia.background/
    //                            org.janelia.camera-utilities/
    //                            org.janelia.collision-handling/
    //      manifest1.json ('["org.janelia.background", "org.janelia.collision-handling"])
    //      manifests/
    //                manifest2.json ('["org.janelia.collision-handling", "org.janelia.background"])

    // To make multiple calls to `UnityEditor.PackageManager.Client.Add()`, it appears to be necessary
    // to have a class with an `Update()` method.  Hence this implementation uses an `EditorWindow`
    // when a simple use of `EditorUtility.DisplayDialog()` probably would look nicer.
    // See the comments with `EditorApplication.update`, below.

    public class PackageInstaller : EditorWindow
    {
        [MenuItem("Window/Install Package and Dependencies")]
        public static void MenuItem()
        {
            _pkgNamesAlreadyInstalled.Clear();

            // The Unity documentation on `UnityEditor.PackageManager.Client` has examples that
            // use `EditorApplication.update` as follows, to call the code that checks for
            // `Status == StatusCode.Success`.  This approach works for a `Client.List()` call,
            // as being done here.  But it does not work for multiple calls to `Client.Add()`,
            // as needed further in the code: even when the second call occurs only after
            // `Status == StatusCode.Success` for the first call, the second call produces
            // an error, "Failed to resolve packages: An operation that requires exclusive access
            // to the project is already running and must be completed before another can be started.
            // No packages loaded."  The only solution seems to be to abandon the use of
            // `EditorApplication.update` for multiple `Client.Add()` calls, and instead to use
            // an explicit `Update()` method.  See the code later in this file.

            _listRequest = Client.List();
            EditorApplication.update += UpdateForListRequest;
        }

        private static void OnAssemblyCompilationFinished(string s, CompilerMessage[] compilerMessages)
        {
            bool updatedApiLevel = false;
            foreach (CompilerMessage msg in compilerMessages)
            {
                if (msg.type == CompilerMessageType.Error)
                {
                    // The `org.janelia.io` package uses `System.IO.Ports` to read serial input, and for some reason,
                    // the default configuration of Unity does not include the required assembly.  So just go ahead and
                    // change to the non-default setting that solves the problem.

                    if (msg.message.Contains("CS0234") && msg.message.Contains("Ports") && msg.message.Contains("System.IO") && !updatedApiLevel)
                    {
                        Debug.Log(msg.message);
                        Debug.Log("Fixing by changing 'Player' setting 'API Compatibility Level' to 'ApiCompatibilityLevel.NET_4_6'");
                        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_4_6);

                        updatedApiLevel = true;
                        CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
                    }
                }
            }
        }

        private static void UpdateForListRequest()
        {
            if (_listRequest.IsCompleted)
            {
                EditorApplication.update -= UpdateForListRequest;
                if (_listRequest.Status == StatusCode.Success)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo pkg in _listRequest.Result)
                    {
                        if (pkg.source == PackageSource.Local)
                        {
                            _pkgNamesAlreadyInstalled.Add(pkg.name);
                        }
                    }

                    LaunchWindow();
                }
                else if (_listRequest.Status >= StatusCode.Failure)
                {
                    EditorUtility.DisplayDialog("Error", "Cannot list already installed packages", "OK");
                }
            }
        }

        private static void LaunchWindow()
        {
            string choice = EditorUtility.OpenFilePanel("Install package and dependencies", "", "json");
            if (choice.Length != 0)
            {
                List<string> pkgDirs = PkgDirsFromFilePanelChoice(choice);
                foreach (string pkgDir in pkgDirs)
                {
                    PackageNode unused = new PackageNode(pkgDir);
                }

                List<string> pkgPathsToInstall = PackageNode.TopoSort();
                bool installNeeded = pkgPathsToInstall.Any(p => !_pkgNamesAlreadyInstalled.Contains(Path.GetFileName(p)));
                if (installNeeded)
                {
                    PackageInstaller window = (PackageInstaller)GetWindow(typeof(PackageInstaller));
                    window._pkgPathsToInstall = pkgPathsToInstall;
                    window._state = State.AWAITNG_USER_TRIGGER;
                }
                else
                {
                    EditorUtility.DisplayDialog("Install package and dependencies", "Everything is installed already", "OK");
                }
            }
        }

        private static List<string> PkgDirsFromFilePanelChoice(string choice)
        {
            List<string> result = new List<string>();

            if (Path.GetExtension(choice) == ".json")
            {
                if (Path.GetFileName(choice) == "package.json")
                {
                    // The `choice` can be the `package.json` file in a package directory.
                    result.Add(Path.GetDirectoryName(choice));
                }
                else
                {
                    // The `choice` can be a file containing a simple JSON array of package paths.
                    List<string> paths = ParseSimpleJsonArray(choice);
                    List<string> fullPaths = MakeFullPaths(paths, choice);
                    foreach (string path in fullPaths)
                    {
                        if (Path.GetFileName(choice) == "package.json")
                        {
                            result.Add(Path.GetDirectoryName(path));
                        }
                        else if (File.Exists(Path.Combine(path, "package.json")))
                        {
                            result.Add(path);
                        }
                    }
                }
            }
            else if (File.Exists(Path.Combine(choice, "package.json")))
            {
                // The `choice` can be a package directory, containing a `package.json` file.
                // TODO: Unfortunately, `EditorUtility.OpenFilePanel` cannot support allowing the choice of either
                // a file or a folder, so this option cannot actually be invoked as of now.
                result.Add(choice);
            }

            return result;
        }

        private static List<string> ParseSimpleJsonArray(string path)
        {
            try
            {
                string s1 = File.ReadAllText(path);
                string s2 = "{ \"a\": " + s1 + "}";
                JsonArrayWrapper w = JsonUtility.FromJson<JsonArrayWrapper>(s2);
                return w.a;
            }
            catch
            {
                return new List<string>();
            }
        }

        private static List<string> MakeFullPaths(List<string> paths, string chosenPkgListFile)
        {
            // Try a heuristic for turning package names into full paths: check if the chosen file
            // (listing the packages) is a sibling of the top-level directory for the toolkit source
            // code, and if so, use package directories from that top-level directory.
            string parent = Path.GetDirectoryName(chosenPkgListFile);
            string sibling = Path.Combine(parent, "janelia-unity-toolkit");
            string dir = Directory.Exists(sibling) ? sibling : "";

            // If that does not work, try going up one more level.
            if (dir.Length == 0)
            {
                string grandparent = Path.GetDirectoryName(parent);
                string aunt = Path.Combine(grandparent, "janelia-unity-toolkit");
                dir = Directory.Exists(aunt) ? aunt : "";
            }

            // If that does not work, see if the chosen file is in the some directory within
            // the "janelia-unit-toolkit" repo itself.
            if (dir.Length == 0)
            {
                string[] parts = chosenPkgListFile.Split('/');
                string candidate = "";
                foreach (string part in parts)
                {
                    candidate = Path.Join(candidate, part);
                    if (part == "janelia-unity-toolkit")
                    {
                        dir = candidate;
                        break;
                    }
                }
            }

            List<string> result = new List<string>();
            foreach (string path in paths)
            {
                if (Path.IsPathRooted(path))
                {
                    result.Add(path);
                }
                else
                {
                    result.Add(Path.Combine(dir, path));
                }
            }
            return result;
        }

        [Serializable]
        private class JsonArrayWrapper
        {
            public List<string> a;
        }


        private void OnGUI()
        {
            GUILayout.Label("Install the following packages:");
            foreach (string pkg in _pkgPathsToInstall)
            {
                GUILayout.Label(GetDisplayName(pkg));
            }
            GUI.enabled = (_state == State.AWAITNG_USER_TRIGGER);
            if (GUILayout.Button("Install"))
            {
                if (_state == State.AWAITNG_USER_TRIGGER)
                {
                    _state = State.ADD_STARTING;
                }
            }
            GUI.enabled = true;
            if (_statusMessage.Length > 0)
            {
                GUILayout.Label(_statusMessage);
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case State.ADD_STARTING:
                    Debug.Log("Adding package " + _pkgPathsToInstall[_iInstalling]);

                    CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

                    _addRequests.Add(Client.Add("file:" + _pkgPathsToInstall[_iInstalling]));
                    _state = State.ADD_IN_PROGRESS;

                    _statusMessage = "Adding " + GetDisplayName(_pkgPathsToInstall[_iInstalling]) + "...";
                    break;
                case State.ADD_IN_PROGRESS:
                    if (_addRequests.Last().IsCompleted)
                    {
                        if (_addRequests.Last().Status == StatusCode.Success)
                        {
                            Debug.Log("Successfully added package " + _pkgPathsToInstall[_iInstalling]);

                            _iInstalling++;
                            if (_iInstalling < _pkgPathsToInstall.Count)
                            {
                                _state = State.ADD_STARTING;
                            }
                            else
                            {
                                _statusMessage = "";
                                this.Close();
                            }
                        }
                        else
                        {
                            Error err = _addRequests.Last().Error;
                            if (err != null)
                            {
                                Debug.Log("Error adding package " + _pkgPathsToInstall[_iInstalling] + ": " + err.errorCode + ": " + err.message);
                                EditorUtility.DisplayDialog("Install package and dependencies",
                                    "Error adding package " + _pkgPathsToInstall[_iInstalling] + ":\n" + err.errorCode + ":\n" + err.message,
                                    "OK");
                            }

                            _iInstalling = _pkgPathsToInstall.Count;
                            _state = State.IDLE;
                            this.Close();
                        }
                    }
                    break;
            }
        }

        private string GetDisplayName(string pkg)
        {
            string[] pathElems = pkg.Split(Path.DirectorySeparatorChar);
            int n = pathElems.Length;
            string pkgName = pathElems[n - 1];
            string repoName = pathElems[n - 2];
            string displayName = repoName + Path.DirectorySeparatorChar + pkgName;
            if (_pkgNamesAlreadyInstalled.Contains(pkgName))
            {
                displayName = "(" + displayName + ")";
            }
            return displayName;
        }

        private enum State
        {
            IDLE,
            AWAITNG_USER_TRIGGER,
            ADD_STARTING,
            ADD_IN_PROGRESS,
        }

        private static HashSet<string> _pkgNamesAlreadyInstalled = new HashSet<string>();
        private static ListRequest _listRequest;

        private State _state = State.IDLE;
        private string _statusMessage = "";

        private List<string> _pkgPathsToInstall = new List<string>();
        private int _iInstalling = 0;
        private List<AddRequest> _addRequests = new List<AddRequest>();
    }
}
