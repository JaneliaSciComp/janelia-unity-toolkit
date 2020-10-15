using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Janelia
{
    // Installs the package, P, chosen by the user, after first installing all the packages (from the
    // same repository) on which P depends.  Dependencies are determined using the `"references"` section
    // of a package's .asmdef files in its "Editor" and "Runtime" subdirectories.

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
            string pkgJsonPath = EditorUtility.OpenFilePanel("Install package and dependencies", "", "json");
            if (pkgJsonPath.Length != 0)
            {
                string pkgDir = Path.GetDirectoryName(pkgJsonPath);
                PackageNode root = new PackageNode(pkgDir);

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