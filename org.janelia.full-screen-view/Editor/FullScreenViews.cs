// Full-screen, no-border game views that run with the Unity editor active,
// and an editor window for managing what cameras are displayed in what views.
// Useful for VR studies that use sets of adjacent monitors to display the world
// to the animal (as opposed to using a projector), as all the editor functionality
// is available for fine-tuning the world while the animal is in it.

// To achieve a high frame rate (e.g., 240 Hz), it may help to close as much of the
// standard Unity user interface as possible.  This code redraws the game views in
// the same loop that redraws the standard user interface, so it is helpful to make
// that loop as streamlined as possible.

// Note that the current code works on Windows platforms only (specifically, the code
// that detects the details of all the monitors).

// The assignment of cameras to displays is persisted across sessions.  This persistence
// can be implemented in either of two ways suggested by Edward Rowe
// (https://blog.redbluegames.com/how-to-handle-data-for-custom-editor-tools-in-unity-6b85e9e17715)
// 1. project specific editor preferences
// 2. shared data stored in resources
// Uncomment one of the following definitions to choose the persistence method.

// #define PERSIST_AS_EDITOR_PREFS
#define PERSIST_AS_RESOURCE

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

#if PERSIST_AS_RESOURCE
using System.IO;
#endif

namespace Janelia
{
    //
    // The editor window that manages what cameras are displayed in what views.
    //

    public class FullScreenViewManager : EditorWindow
    {
        [MenuItem("Window/Manage Full Screen Views")]
        public static void ShowWindow()
        {
            FullScreenViewManager window = (FullScreenViewManager)GetWindow(typeof(FullScreenViewManager));
            if (window._monitors.Count == 0)
            {
                window._monitors = Monitor.EnumeratedMonitors();
                window.LoadCameras();
            }
        }

        public FullScreenViewManager()
        {
            _monitors = new List<Monitor>();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            OnGUIRefreshMonitorPositions();
            OnGUICameraObjectFields();
            OnGUIShowFullScreenViews();
            EditorGUILayout.EndVertical();
        }

        private void OnGUIRefreshMonitorPositions()
        {
            if (GUILayout.Button("Refresh Monitor Positions"))
            {
                List<Monitor> refreshedMonitors = Monitor.EnumeratedMonitors();
                for (int i = 0; i < refreshedMonitors.Count; i++)
                {
                    if (i < _monitors.Count)
                    {
                        refreshedMonitors[i].cameraInstanceId = _monitors[i].cameraInstanceId;
                    }
                }
                _monitors = refreshedMonitors;
            }
        }

        private void OnGUICameraObjectFields()
        {
            for (int i = 0; i < _monitors.Count; i++)
            {
                Monitor monitor = _monitors[i];
                EditorGUILayout.LabelField("Monitor " + (i + 1).ToString() + " at (" + monitor.left + ", " + monitor.top + ")");
                Camera oldCamera = (Camera)EditorUtility.InstanceIDToObject(monitor.cameraInstanceId);
                Camera newCamera = (Camera)EditorGUILayout.ObjectField("Camera", oldCamera, typeof(Camera), true);
                if (newCamera != null)
                {
                    // Make sure the camera is viewed only once.

                    int instanceId = newCamera.GetInstanceID();
                    bool alreadyUsed = false;
                    foreach (Monitor other in _monitors)
                    {
                        if (other.cameraInstanceId == instanceId)
                        {
                            alreadyUsed = true;
                            break;
                        }
                    }
                    if (!alreadyUsed)
                    {
                        monitor.cameraInstanceId = instanceId;
                    }
                }
                else
                {
                    monitor.cameraInstanceId = 0;
                }
            }
        }

        private void OnGUIShowFullScreenViews()
        {
            _progressBoxLocation = 
                (ProgressBoxLocation)EditorGUILayout.EnumPopup("Progress box", _progressBoxLocation);
            _progressBoxScreen = EditorGUILayout.IntField("Progress box screen", _progressBoxScreen);
            _progressBoxScreen = Math.Max(1, Math.Min(_progressBoxScreen, _monitors.Count));
            _progressBoxSize = EditorGUILayout.IntField("Progress box size (px)", _progressBoxSize);
            _progressBoxSize = Math.Max(5, Math.Min(_progressBoxSize, 100));

            if (GUILayout.Button("Show Full-Screen Views"))
            {
                // Avoid showing redundant FullScreenViews by closing all of them before (re)showing all of them.

                List<FullScreenView> views = new List<FullScreenView>(FullScreenView.views);
                foreach (FullScreenView view in views)
                {
                    view.Close();
                }

                int i = 1;
                foreach (Monitor monitor in _monitors)
                {
                    Camera camera = (Camera)EditorUtility.InstanceIDToObject(monitor.cameraInstanceId);
                    if (camera != null)
                    {
                        FullScreenView window = EditorWindow.CreateInstance<FullScreenView>();

                        // Negative coordinates must be scaled by the pixelsPerPoint for the target monitor, but
                        // positive coordinates must be scaled by the pixelsPerPoint of the main monitor.

                        float pixelsPerPointX = (monitor.left < 0) ? monitor.pixelsPerPoint : _monitors[0].pixelsPerPoint;
                        int x = (int)(monitor.left / pixelsPerPointX);
                        float pixelsPerPointY = (monitor.top < 0) ? monitor.pixelsPerPoint : _monitors[0].pixelsPerPoint;
                        int y = (int)(monitor.top / pixelsPerPointY);

                        int width = (int)(monitor.width / monitor.pixelsPerPoint);
                        int height = (int)(monitor.height / monitor.pixelsPerPoint);

                        window.position = new Rect(x, y, width, height);
                        window.cameraInstanceId = camera.GetInstanceID();

                        if (i++ == _progressBoxScreen)
                        {
                            window.progressBoxLocation = _progressBoxLocation;
                            window.progressBoxSize = _progressBoxSize;
                        }

                        // Using ShowPopup() eliminates all borders and window decorations.

                        window.ShowPopup();
                    }
                }
            }
        }

        private void OnDestroy()
        {
            SaveCameras();
        }

#if PERSIST_AS_EDITOR_PREFS
        private void SaveCameras()
        {
            EditorPrefs.SetInt(NumMonitorsPersistenceKey(), _monitors.Count);
            for (int i = 0; i < _monitors.Count; i++)
            {
                Camera camera = (Camera)EditorUtility.InstanceIDToObject(_monitors[i].cameraInstanceId);
                string path = (camera != null) ? PathName(camera.gameObject) : "";
                EditorPrefs.SetString(CameraNamePersistenceKey(i), path);
            }
            EditorPrefs.SetInt(ProgressBoxLocationPersistenceKey(), (int)_progressBoxLocation);
            EditorPrefs.SetInt(ProgressBoxScreenPersistenceKey(), _progressBoxScreen);
            EditorPrefs.SetInt(ProgressBoxSizePersistenceKey(), _progressBoxSize);
        }

        private void LoadCameras()
        {
            int count = EditorPrefs.GetInt(NumMonitorsPersistenceKey());
            for (int i = 0; i < count; i++)
            {
                if (i < _monitors.Count)
                {
                    string path = EditorPrefs.GetString(CameraNamePersistenceKey(i));
                    GameObject obj = GameObject.Find(path);
                    if (obj != null)
                    {
                        Camera camera = obj.GetComponent<Camera>();
                        if (camera != null)
                        {
                            _monitors[i].cameraInstanceId = camera.GetInstanceID();
                        }
                    }
                }
            }
            _progressBoxLocation =
                (ProgressBoxLocation)EditorPrefs.GetInt(ProgressBoxLocationPersistenceKey());
            _progressBoxScreen = EditorPrefs.GetInt(ProgressBoxScreenPersistenceKey());
            _progressBoxSize = EditorPrefs.GetInt(ProgressBoxSizePersistenceKey());
        }

        private string PersistenceKeyPrefix()
        {
            return PlayerSettings.companyName + "." + PlayerSettings.productName + ".FullScreenViewManager";
        }

        private string CameraNamePersistenceKey(int i)
        {
            return PersistenceKeyPrefix() + ".monitor." + i.ToString() + ".cameraName";
        }

        private string NumMonitorsPersistenceKey()
        {
            return PersistenceKeyPrefix() + ".numMonitors";
        }

        private string ProgressBoxLocationPersistenceKey()
        {
            return PersistenceKeyPrefix() + ".progressBoxLocation";
        }

        private string ProgressBoxScreenPersistenceKey()
        {
            return PersistenceKeyPrefix() + ".progressBoxScreen";
        }

        private string ProgressBoxSizePersistenceKey()
        {
            return PersistenceKeyPrefix() + ".progressBoxSize";
        }

#elif PERSIST_AS_RESOURCE
        private FullScreenViewsSaved _savedFullScreenViews;

        private void SaveCameras()
        {
            _savedFullScreenViews.cameraNames.Clear();
            for (int i = 0; i < _monitors.Count; i++)
            {
                Camera camera = (Camera)EditorUtility.InstanceIDToObject(_monitors[i].cameraInstanceId);
                string path = (camera != null) ? PathName(camera.gameObject) : "";
                _savedFullScreenViews.cameraNames.Add(path);
            }
            _savedFullScreenViews.progressBoxLocation = (int)_progressBoxLocation;
            _savedFullScreenViews.progressBoxScreen = _progressBoxScreen;
            _savedFullScreenViews.progressBoxSize = _progressBoxSize;

            AssetDatabase.Refresh();
            EditorUtility.SetDirty(_savedFullScreenViews);
            AssetDatabase.SaveAssets();
        }

        private void LoadCameras()
        {
            _savedFullScreenViews = Resources.Load<FullScreenViewsSaved>("Editor/savedFullScreenViews");

            if (_savedFullScreenViews != null)
            {
                for (int i = 0; i < _savedFullScreenViews.cameraNames.Count; i++)
                {
                    if (i < _monitors.Count)
                    {
                        string path = _savedFullScreenViews.cameraNames[i];
                        GameObject obj = GameObject.Find(path);
                        if (obj != null)
                        {
                            Camera camera = obj.GetComponent<Camera>();
                            if (camera != null)
                            {
                                _monitors[i].cameraInstanceId = camera.GetInstanceID();
                            }
                        }
                    }
                }

                _progressBoxLocation = (ProgressBoxLocation)_savedFullScreenViews.progressBoxLocation;
                _progressBoxScreen = _savedFullScreenViews.progressBoxScreen;
                _progressBoxSize = _savedFullScreenViews.progressBoxSize;
            }
            else
            {
                _savedFullScreenViews = CreateInstance<FullScreenViewsSaved>();

                string root = Application.dataPath;
                EnsureDirectory(root + "/Resources");
                EnsureDirectory(root + "/Resources/Editor");

                // Saving and loading work only if the filename has the extension ".asset".

                AssetDatabase.CreateAsset(_savedFullScreenViews, "Assets/Resources/Editor/savedFullScreenViews.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception e)
                {
                    Debug.Log("Cannot create " + path + ": " + e.ToString());
                }
            }
        }
#endif

        private string PathName(GameObject o)
        {
            string path = o.name;
            while (o.transform.parent != null)
            {
                o = o.transform.parent.gameObject;
                path = o.name + "/" + path;
            }
            return path;
        }

        [SerializeField]
        private List<Monitor> _monitors;

        public enum ProgressBoxLocation
        {
            NONE = 0,
            UPPER_LEFT = 1,
            UPPER_RIGHT = 2,
            LOWER_LEFT = 3,
            LOWER_RIGHT = 4
        }
        private int _progressBoxScreen = 1;
        private ProgressBoxLocation _progressBoxLocation;
        private int _progressBoxSize = 20;
    }

    //
    // The full-screen, no-border game view.
    //

    public class FullScreenView : EditorWindow
    {
        public static List<FullScreenView> views;

        public int cameraInstanceId;
        private Camera _camera;
        private bool _rendering = false;

        public FullScreenViewManager.ProgressBoxLocation progressBoxLocation =
            FullScreenViewManager.ProgressBoxLocation.NONE;
        public int progressBoxSize = 20;
        private int _frameCounter = 0;
        private static Texture2D _progressTextureEven;
        private static Texture2D _progressTextureOdd;

        static FullScreenView()
        {
            if (views == null)
            {
                views = new List<FullScreenView>();
            }
        }

        private void Awake()
        {
            views.Add(this);

            EditorApplication.wantsToQuit -= OnEditorWantsToQuit;
            EditorApplication.wantsToQuit += OnEditorWantsToQuit;
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e.isMouse && e.button == 0)
            {
                Close();
            }
            else if (e.type != EventType.Repaint)
            {
                // OnGUI seems to be called at least twice for each Update.
                // So do no camera rendering except when triggered by Update's Repaint.

                return;
            }

            if (_camera == null)
            {
                _camera = (Camera)EditorUtility.InstanceIDToObject(cameraInstanceId);
                if (_camera)
                {
                    _camera.enabled = false;
                    int width = (int)position.width;
                    int height = (int)position.height;
                    _camera.targetTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    _rendering = true;
                }
            }
            if (_rendering)
            {
                _camera.Render();

                // Disable alpha blending when drawing the texture, so black really is black.
                bool alphaBlend = false;
                GUI.DrawTexture(new Rect(0, 0, position.width, position.height), _camera.targetTexture,
                                ScaleMode.ScaleToFit, alphaBlend);

                if (progressBoxLocation != FullScreenViewManager.ProgressBoxLocation.NONE)
                {
                    InitializeProgressTexturesIfNeeded(progressBoxSize);

                    Texture2D progressTexture = (_frameCounter % 2 == 0) ? _progressTextureEven : _progressTextureOdd;
                    int displayWidth = (int)position.width;
                    int displayHeight = (int)position.height;
                    float x, y;
                    switch (progressBoxLocation)
                    {
                        case FullScreenViewManager.ProgressBoxLocation.UPPER_LEFT:
                            x = 0;
                            y = 0;
                            break;
                        case FullScreenViewManager.ProgressBoxLocation.UPPER_RIGHT:
                            x = displayWidth - progressTexture.width;
                            y = 0;
                            break;
                        case FullScreenViewManager.ProgressBoxLocation.LOWER_LEFT:
                            x = 0;
                            y = displayHeight - progressTexture.height;
                            break;
                        default:
                            x = displayWidth - progressTexture.width;
                            y = displayHeight - progressTexture.height;
                            break;
                    }
                    GUI.DrawTexture(new Rect(x, y, progressTexture.width, progressTexture.height), progressTexture, ScaleMode.ScaleToFit, false);
                }
            }
        }

        private void Update()
        {
            if ((_camera != null) && _rendering)
            {
                Repaint();
                _frameCounter++;
            }

#if false
            Debug.Log("View [y " + position.yMin + "]: Update delta time " + Time.deltaTime + " ms (" + 1.0 / Time.deltaTime + " FPS) playing " + Application.isPlaying);
#endif
        }

        private void OnDestroy()
        {
            _rendering = false;
            _camera.targetTexture = null;
            _camera.enabled = true;
            views.Remove(this);
        }

        // Any FullScreenView that is displayed when the editor is quit will appear as a blank, un-closeable window
        // the next time the editor is started.  So close all FullScreenViews when the editor is quitting.

        internal static bool OnEditorWantsToQuit()
        {
            while (views.Count > 0)
            {
                FullScreenView view = views[0];
                views.RemoveAt(0);
                view.Close();
            }
            return true;
        }

        private static void InitializeProgressTexturesIfNeeded(int size)
        {
            if ((_progressTextureEven == null) || (_progressTextureEven.width != size))
            {
                _progressTextureEven = MakeProgressTexture(size, size, true);
            }
            if ((_progressTextureOdd == null) || (_progressTextureOdd.width != size))
            {
                _progressTextureOdd = MakeProgressTexture(size, size, false);
            }
        }

        private static Texture2D MakeProgressTexture(int width, int height, bool even)
        {
            Texture2D result = new Texture2D(width, height);
            Color color = even ? new Color(0, 0, 0, 1) : new Color(1, 1, 1, 1);
            Color[] pixels = Enumerable.Repeat(color, width * height).ToArray();
            result.SetPixels(pixels);
            result.Apply();

            // The texture must be saved as an asset or the reference to the texture will be lost.
            // https://forum.unity.com/threads/unity-randomly-loses-references.356082/

            string name = even ? "TmpProgressTextureEven.asset" : "TmpProgressTextureOdd.asset";
            AssetDatabase.CreateAsset(result, "Assets/" + name);

            return result;
        }
    }

    //
    // The details of a particular display monitor, incuding the scaling factor that Unity uses
    // for the position and dimensions of editor windows on that display.
    // Includes Windows-specific functionality for enumerating all the available displays and
    // the location in an extended desktop.
    //

    [Serializable]
    internal class Monitor
    {
        public int left;
        public int top;
        public int width;
        public int height;
        public float pixelsPerPoint;
        public int cameraInstanceId;

        static public List<Monitor> EnumeratedMonitors()
        {
            List<Monitor> result = new List<Monitor>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdc, ref RectApi prect, IntPtr dwData)
                {
                    result.Add(new Monitor(prect.left, prect.top, prect.width, prect.height));
                    return true;
                },
                0);

            // Sort the monitors because EnumDisplayMonitors does not seem to return them in a
            // consistent order across sessions.

            result.Sort(delegate (Monitor x, Monitor y)
            {
                // The monitor at (0, 0) is the main monitor, which should always be first.
                if ((x.left == 0) && (x.top == 0))
                {
                    return -1;
                }
                else if ((y.left == 0) && (y.top == 0))
                {
                    return 1;
                }

                // Other monitors should be sorted in left-to-right order based on their left edges.
                if (x.left == y.left)
                {
                    return (x.top - y.top);
                }
                return (x.left - y.left);
            });

            foreach (Monitor monitor in result)
            {
                MonitorTester tester = EditorWindow.CreateInstance<MonitorTester>();

                // These coordinates seem to work, even though they don't have the tricky scaling done when
                // showing a FullScreenView, above.

                tester.position = new Rect(monitor.left, monitor.top, 20, 20);
                tester.monitor = monitor;

                // Using ShowPopup() displays the EditorWindow without decorations.

                tester.ShowPopup();
            }

            return result;
        }

        private Monitor(int l, int t, int w, int h)
        {
            left = l;
            top = t;
            width = w;
            height = h;
            pixelsPerPoint = 1.0f;
            cameraInstanceId = 0;
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RectApi pRect, IntPtr dwData);
        [DllImport("user32")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct RectApi
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public int width { get { return right - left; } }
            public int height { get { return bottom - top; } }
        }

        private class MonitorTester : EditorWindow
        {
            internal Monitor monitor;

            private void OnGUI()
            {
                monitor.pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
                Close();
            }
        }
    }
}
