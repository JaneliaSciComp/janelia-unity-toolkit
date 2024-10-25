using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Depends on: org.janelia.camera-utilities for OffAxisPerspectiveCamera.

namespace Janelia
{
    // If an existing object is used for a screen, it is assumed to be a quad, aligned with the XY plane,
    // with unit size (i.e., going from -0.5 to 0.5).

    public class SetupCamerasNGon : EditorWindow
    {
        [MenuItem("Window/Setup Cameras, N-gon")]
        public static void ShowWindow()
        {
            SetupCamerasNGon window = (SetupCamerasNGon)GetWindow(typeof(SetupCamerasNGon));
        }

        public SetupCamerasNGon()
        {
            _cameraScreens = new List<CameraScreen>();
            _rotationY = RotationYCentered();

            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        public static void Setup(int numCameras = 4, int numEmptySides = 1, float screenWidth = 5.8f, float screenHeight = 9.5f,
                                 float fractionalHeight = 0.737f, float rotationY = -18, float offsetX = 0, float offsetZ = 0)
        {
            // Remove the saved UI input values, so `OnEnable` does not load them and set `_cameraScreens`
            // to the wrong size relative to the settings enabled in the rest of this routine.
            AssetDatabase.Refresh();
            bool success = AssetDatabase.DeleteAsset("Assets/Resources/Editor/savedCamerasNGon.asset");
            AssetDatabase.Refresh();

            SetupCamerasNGon window = (SetupCamerasNGon)GetWindow(typeof(SetupCamerasNGon));

            window._numCameras = numCameras;
            window._numEmptySides = numEmptySides;
            window._screenWidth = screenWidth;
            window._screenHeight = screenHeight;
            window._fractionalHeight = fractionalHeight;
            window._rotationY = rotationY;
            window._offsetX = offsetX;
            window._offsetZ = offsetZ;

            window.ReconcileCameraScreens();
            window._rotationY = window.RotationYCentered();

            window.UpdateCameras();
        }

        private GameObject _fly;
        private string _flyComponentExtra;

        [Serializable]
        private class CameraScreen
        {
            public Camera camera;
            public GameObject screen;
        }
        private List<CameraScreen> _cameraScreens;

        // The N in the N-gon is _numCameras + _numEmptySides.

        private int _numCameras = 4;
        private int _numEmptySides = 1;
        private float _screenWidth = 5.8f; // cm
        private float _screenHeight = 9.5f; // cm
        private float _fractionalHeight = 0.737f;
        private float _rotationY = -18;
        private float _offsetX = 0;
        private float _offsetZ = 0;
        private float _tilt = 0;
        private float _near = 0.01f;
        private float _far = 1000.0f;

        // With GameObject.CreatePrimitive(), properties like these must be present somewhere
        // in the code to prevent a crash due to code stripping.  See the note at the bottom here:
        // https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html

        private MeshFilter _preventStrippingMeshFilter;
        private MeshRenderer _preventStrippingMeshRenderer;
        private BoxCollider _preventStrippingBoxCollider;

        public void OnEnable()
        {
            Load();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            _fly = (GameObject)EditorGUILayout.ObjectField("Fly", _fly, typeof(GameObject), true);

            // A script listed here (without the ".cs" suffix) will be added to the created "fly" object,
            // as a convenience.
            _flyComponentExtra = EditorGUILayout.TextField("Extra script for fly", _flyComponentExtra);

            int numCamerasBefore = _numCameras;
            int numEmptySidesBefore = _numEmptySides;
            _numCameras = EditorGUILayout.IntField("Number of cameras", _numCameras);
            _numEmptySides = EditorGUILayout.IntField("Number of empty sides", _numEmptySides);

            _screenWidth = EditorGUILayout.FloatField("Screen width (mm)", _screenWidth);
            _screenHeight = EditorGUILayout.FloatField("Screen height (mm)", _screenHeight);
            _fractionalHeight = EditorGUILayout.FloatField("Fractional height", _fractionalHeight);

            _rotationY = EditorGUILayout.FloatField("Rotation Y (deg)", _rotationY);

            // The created "fly" object will be displaced from the center of the n-gon by this vector.
            _offsetX = EditorGUILayout.FloatField("Offset X (mm)", _offsetX);
            _offsetZ = EditorGUILayout.FloatField("Offset Z (mm)", _offsetZ);

            _tilt = EditorGUILayout.FloatField("Tilt X (deg)", _tilt);

            _near = EditorGUILayout.FloatField("Near", _near);
            _far = EditorGUILayout.FloatField("Far", _far);

            ReconcileCameraScreens();

            for (int i = 0; i < _cameraScreens.Count; i++)
            {
                _cameraScreens[i].camera = (Camera)EditorGUILayout.ObjectField("Camera " + (i + 1), _cameraScreens[i].camera, typeof(Camera), true);
                _cameraScreens[i].screen = (GameObject)EditorGUILayout.ObjectField("Screen " + (i + 1), _cameraScreens[i].screen, typeof(GameObject), true);
            }

            if (GUI.changed)
            {
                if ((_numCameras != numCamerasBefore) || (_numEmptySides != numEmptySidesBefore))
                {
                    // Recompute _rotationY only when a manually set value might no longer make sense.
                    _rotationY = RotationYCentered();
                }
            }

            if (GUILayout.Button("Update"))
            {
                UpdateCameras();
            }

            EditorGUILayout.EndVertical();
        }

        private void ReconcileCameraScreens()
        {
            while (_cameraScreens.Count < _numCameras)
            {
                _cameraScreens.Add(new CameraScreen());
            }

            int failSafe = _cameraScreens.Count;
            while ((_cameraScreens.Count > _numCameras) && (failSafe-- > 0))
            {
                foreach (CameraScreen cameraScreen in _cameraScreens)
                {
                    string suffix = cameraScreen.camera.name.Substring("FlyCamera".Length);
                    int i;
                    if (int.TryParse(suffix, out i))
                    {
                        if (i > _numCameras)
                        {
                            _cameraScreens.Remove(cameraScreen);
                            break;
                        }
                    }
                }
            }
        }

        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            Save();
        }

        private void OnDestroy()
        {
            Save();
        }

        private float RotationYCentered()
        {
            // Rotates the screen so the positive X axis points to:
            // the middle of the middle screen for an odd number of screens
            // the middle edge between screens for an even number of screens
            int numSides = _numCameras + _numEmptySides;
            float fovDeg = 360.0f / numSides;
            return -(_numCameras - 1) * fovDeg / 2.0f + 90.0f;
        }

        private Vector3 ScreenTranslationWithTilt(float fovRad)
        {
            float viewDirTransNoTilt = (_screenWidth / 2.0f) / Mathf.Tan(fovRad / 2.0f);
            float viewDirTrans = viewDirTransNoTilt / Mathf.Cos(Mathf.Deg2Rad * _tilt);
            Vector3 trans1 = new Vector3(0, 0, viewDirTrans);

            float heightDirTransNoTilt = 0.5f * _screenHeight - _fractionalHeight * _screenHeight;
            Vector3 trans2a = new Vector3(0, heightDirTransNoTilt, 0);
            Vector3 trans2b = Quaternion.AngleAxis(-_tilt, Vector3.right) * trans2a;

            Vector3 trans = trans1 + trans2b;
            return trans;
        }

        private void UpdateCameras()
        {
            if (_cameraScreens.Count != _numCameras)
            {
                Debug.Log("Exactly " + _numCameras + " camera/screen pairs are expected.");
                return;
            }

            // Create the objects if they are not specified.

            string name = "Fly";
            if (_fly == null)
            {
                _fly = GameObject.Find(name);
            }
            if (_fly == null)
            {
                _fly = new GameObject(name);
                _fly.transform.localPosition = new Vector3(0, 0.01f, 0);

                // For some reason, creating objects in this routine does not seem to
                // mark the containing scene as dirty, so it is difficult to save the
                // scene.  As a work-around, manually force the dirty marking.
                SetObjectDirty(_fly);

                if ((_flyComponentExtra != null) && (_flyComponentExtra.Length > 0))
                {
                    string fullName = _flyComponentExtra + ",Assembly-CSharp";
                    Type t = Type.GetType(fullName);
                    if (t != null)
                    {
                        _fly.AddComponent(t);
                    }
                    else
                    {
                        Debug.Log("Cannot find extra script of type '" + fullName + "'");
                    }
                }
            }

            for (int i = 0; i < _cameraScreens.Count; i++)
            {
                name = "FlyCamera" + (i + 1);
                if (_cameraScreens[i].camera == null)
                {
                    GameObject obj = GameObject.Find(name);
                    if (obj != null) {
                        _cameraScreens[i].camera = obj.GetComponent<Camera>();
                    }
                }
                if (_cameraScreens[i].camera == null)
                {
                    GameObject cameraObj = new GameObject(name);
                    SetObjectDirty(cameraObj);
                    _cameraScreens[i].camera = cameraObj.AddComponent(typeof(Camera)) as Camera;

                    _cameraScreens[i].camera.targetDisplay = i + 1;
                }
                _cameraScreens[i].camera.transform.localRotation = Quaternion.identity;
                _cameraScreens[i].camera.transform.localPosition = Vector3.zero;
                name = "FlyCamera" + (i + 1) + "Screen";
                if (_cameraScreens[i].screen == null)
                {
                    _cameraScreens[i].screen = GameObject.Find(name);
                }
                if (_cameraScreens[i].screen == null)
                {
                    _cameraScreens[i].screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    SetObjectDirty(_cameraScreens[i].screen);
                    _cameraScreens[i].screen.name = name;
                }
                _cameraScreens[i].screen.transform.localRotation = Quaternion.identity;
                _cameraScreens[i].screen.transform.localPosition = Vector3.zero;

                MeshRenderer renderer = _cameraScreens[i].screen.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                OffAxisPerspectiveCamera offAxisPerspectiveCamera =
                    _cameraScreens[i].camera.gameObject.GetComponent<OffAxisPerspectiveCamera>();
                if (offAxisPerspectiveCamera == null)
                {
                    offAxisPerspectiveCamera =
                        _cameraScreens[i].camera.gameObject.AddComponent<OffAxisPerspectiveCamera>() as OffAxisPerspectiveCamera;
                }
                offAxisPerspectiveCamera.screen = _cameraScreens[i].screen;
            }

            // If displays are rotated 90 degrees, so the image is taller than it is wide,
            // the Windows Display settings should indicate that the displays are in "Portrait" mode
            // (or "Portrait, flipped"). Then the text displayed by Windows will have the correct
            // orientation. Unity's Screen.resolutions is aware of this Windows Display setting.
            // So when the editor is placed on a screen that has been rotated and set to "Portrait",
            // then Screen.resolutions returns values with height greater than width.

            int numSides = _numCameras + _numEmptySides;
            float fovDeg = 360.0f / numSides;
            float fovRad = Mathf.PI * 2.0f / numSides;

            Vector3 screenTrans = ScreenTranslationWithTilt(fovRad);
            float camYRot = _rotationY;

            for (int i = 0; i < _cameraScreens.Count; i++)
            {
                Transform cameraXform = _cameraScreens[i].camera.gameObject.transform;
                cameraXform.SetParent(_fly.transform);

                Transform screenXform = _cameraScreens[i].screen.transform;
                screenXform.SetParent(cameraXform);

                _cameraScreens[i].camera.nearClipPlane = _near;
                _cameraScreens[i].camera.farClipPlane = _far;

                cameraXform.localPosition = new Vector3(0, 0, 0);
                cameraXform.Rotate(_tilt, camYRot, 0);

                screenXform.localPosition = screenTrans;
                screenXform.localEulerAngles = new Vector3(-_tilt, 0, 0);
                screenXform.localScale = new Vector3(_screenWidth, _screenHeight, 1);

                // One way of making the screens invisible to their cameras.
                screenXform.Rotate(0, 180, 0);

                screenXform.position += new Vector3(_offsetX, 0, _offsetZ);

                camYRot += fovDeg;
            }

            Camera[] cameras = _fly.GetComponentsInChildren<Camera>();
            foreach (Camera camera in cameras)
            {
                bool found = false;
                for (int i = 0; i < _cameraScreens.Count; i++)
                {
                    if (_cameraScreens[i].camera == camera)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    DestroyImmediate(camera.gameObject);
                }
            }

            if (!Application.isPlaying)
            {
                // Make sure that even if only parameters were changed but no new objects created,
                // still the scene is marked dirty and in need of saving.
                EditorSceneManager.MarkSceneDirty(_fly.scene);
            }
        }

        // Storing the state across sessions, using resources.

        private CamerasNGonSaved _saved;

        private void Save()
        {
            _saved.flyName = PathName(_fly);
            _saved.cameraNames.Clear();
            _saved.screenNames.Clear();
            foreach (CameraScreen cameraScreen in _cameraScreens)
            {
                GameObject cameraObj = (cameraScreen.camera != null) ? cameraScreen.camera.gameObject : null;
                _saved.cameraNames.Add(PathName(cameraObj));
                _saved.screenNames.Add(PathName(cameraScreen.screen));
            }
            _saved.numEmptySides = _numEmptySides;
            _saved.screenWidth = _screenWidth;
            _saved.screenHeight = _screenHeight;
            _saved.fractionalHeight = _fractionalHeight;
            _saved.rotationY = _rotationY;
            _saved.offsetX = _offsetX;
            _saved.offsetZ = _offsetZ;
            _saved.tilt = _tilt;
            _saved.near = _near;
            _saved.far = _far;

            AssetDatabase.Refresh();
            EditorUtility.SetDirty(_saved);
            AssetDatabase.SaveAssets();
        }

        private void Load()
        {
            _saved = Resources.Load<CamerasNGonSaved>("Editor/savedCamerasNGon");
            if (_saved != null)
            {
                _fly = GameObject.Find(_saved.flyName);
                if (_saved.cameraNames.Count == _saved.screenNames.Count)
                {
                    _cameraScreens.Clear();
                    for (int i = 0; i < _saved.cameraNames.Count; i++)
                    {
                        CameraScreen cameraScreen = new CameraScreen();
                        GameObject cameraObj = GameObject.Find(_saved.cameraNames[i]);
                        cameraScreen.camera = (cameraObj != null) ? cameraObj.GetComponent<Camera>() : null;
                        cameraScreen.screen = GameObject.Find(_saved.screenNames[i]);
                        _cameraScreens.Add(cameraScreen);
                    }
                    _numCameras = _cameraScreens.Count;
                    _numEmptySides = _saved.numEmptySides;
                    _screenWidth = _saved.screenWidth;
                    _screenHeight = _saved.screenHeight;
                    _fractionalHeight = _saved.fractionalHeight;
                    _rotationY = _saved.rotationY;
                    _offsetX = _saved.offsetX;
                    _offsetZ = _saved.offsetZ;
                    _tilt = _saved.tilt;
                    _near = _saved.near;
                    _far = _saved.far;
                }
            }
            else
            {
                _saved = CreateInstance<CamerasNGonSaved>();

                string root = Application.dataPath;
                EnsureDirectory(root + "/Resources");
                EnsureDirectory(root + "/Resources/Editor");

                // Saving and loading work only if the filename has the extension ".asset".

                AssetDatabase.CreateAsset(_saved, "Assets/Resources/Editor/savedCamerasNGon.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Save();
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

        private string PathName(GameObject o)
        {
            if (o == null)
            {
                return "";
            }
            string path = o.name;
            while (o.transform.parent != null)
            {
                o = o.transform.parent.gameObject;
                path = o.name + "/" + path;
            }
            return path;
        }

        private void SetObjectDirty(GameObject obj)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(obj);
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
        }
    }
}
