// Full-screen, no-border game views that run with the Unity editor active,
// and an editor window for managing what cameras are displayed in what views.
// Useful for VR studies that use sets of adjacent monitors to display the world
// to the animal (as opposed to using a projector), as all the editor functionality
// is available for fine-tuning the world while the animal is in it.

// Note that the current code works on Windows platforms only (specifically, the code
// that detects the details of all the monitors).

// The assignment of cameras to displays is persisted across sessions.  This persistence
// can be implemented in either of two ways suggested by Edward Rowe
// (https://blog.redbluegames.com/how-to-handle-data-for-custom-editor-tools-in-unity-6b85e9e17715)
// 1. project specific editor preferences
// 2. shared data stored in resources
// Uncomment one of the following definitions to choose the persistence method.

#define PERSIST_AS_EDITOR_PREFS
// #define PERSIST_AS_RESOURCE

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
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
            if (GUILayout.Button("Show Full-Screen Views"))
            {
                // Avoid showing redundant FullScreenViews by closing all of them before (re)showing all of them.

                List<FullScreenView> views = new List<FullScreenView>(FullScreenView.views);
                foreach (FullScreenView view in views)
                {
                    view.Close();
                }

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

        void OnGUI()
        {
            Event e = Event.current;
            if (e.isMouse && e.button == 0)
            {
                Close();
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
            }
        }

        private void Update()
        {
            if ((_camera != null) && _rendering)
            {
                Repaint();
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
        // the next time the editor is started.  It seems difficult to programmatically close all FullScreenViews
        // when the editor is quitting, so instead, prompt the user to do so.

        internal static bool OnEditorWantsToQuit()
        {
            if (views.Count > 0)
            {
                bool ok = EditorUtility.DisplayDialog("Full Screen Views", "Please manually dismiss all full-screen views before exiting the editor", "OK", "Exit Anyway");
                return !ok;
            }
            return true;
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
