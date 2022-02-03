using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Janelia
{
    public class SetupCamerasBox : EditorWindow
    {
        [MenuItem("Window/Setup Cameras, Box with Floor")]
        public static void ShowWindow()
        {
            SetupCamerasBox window = (SetupCamerasBox)GetWindow(typeof(SetupCamerasBox));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_JSON_PATH))
            {
                _jsonPath = EditorPrefs.GetString(EDITOR_PREF_KEY_JSON_PATH);
            }
            _jsonPath = EditorGUILayout.TextField("JSON spec file", _jsonPath);
            if (GUILayout.Button("Choose JSON spec file"))
            {
                _jsonPath = EditorUtility.OpenFilePanel("JSON spec file", ".", "json");
            }
            EditorPrefs.SetString(EDITOR_PREF_KEY_JSON_PATH, _jsonPath);

            if (GUILayout.Button("Create camera rig"))
            {
                SetupCameras();
            }

            if (GUILayout.Button("Delete camera rig"))
            {
                DeleteCameras();
            }

            EditorGUILayout.EndVertical();
        }

        private void SetupCameras()
        {
            if (LoadJson())
            {
                FindSubject();
                DeleteCameras();
                CreateSideCameras();
                CreateFloorCameras();
                AddScripts();
            }
        }

        private bool LoadJson()
        {
            if (!File.Exists(_jsonPath))
            {
                EditorUtility.DisplayDialog("Error", "Cannot find spec file '" + _jsonPath + "'.", "OK");
                return false;
            }

            string json = File.ReadAllText(_jsonPath);
            _spec = new Spec();
            JsonUtility.FromJsonOverwrite(json, _spec);

            if ((_spec.side.screenWidth == 0) || (_spec.side.screenHeight == 0))
            {
                EditorUtility.DisplayDialog("Error", "The JSON 'side' must have 'screenWidth' and 'screenHeight'.", "OK");
                return false;
            }
            if (_spec.side.displayIDs.Min() < 1)
            {
                EditorUtility.DisplayDialog("Error", "The JSON 'side' must have 'displayIDs' of at least 1.", "OK");
                return false;
            }
            if (_spec.side.displayIDs.Max() > 7)
            {
                EditorUtility.DisplayDialog("Error", "The JSON 'side' must have 'displayIDs' of at most 7.", "OK");
                return false;
            }
            if (_spec.floor.displayIDs.Min() < 1)
            {
                EditorUtility.DisplayDialog("Error", "The JSON 'floor' must have 'displayIDs' of at least 1.", "OK");
                return false;
            }
            if (_spec.floor.displayIDs.Max() > 7)
            {
                EditorUtility.DisplayDialog("Error", "The JSON 'floor' must have 'displayIDs' of at most 7.", "OK");
                return false;
            }
            return true;
        }

        private void FindSubject()
        {
            _subject = GameObject.Find(_spec.subjectName);
            if (_subject == null)
            {
                _subject = new GameObject(_spec.subjectName);
                _subject.transform.localPosition = Vector3.zero;

                // For some reason, creating objects in this routine does not seem to
                // mark the containing scene as dirty, so it is difficult to save the
                // scene.  As a work-around, manually force the dirty marking.
                SetObjectDirty(_subject);
            }
        }

        private void DeleteCameras()
        {
            _firstSideTag = "";
            _firstSideAngleY = 0;

            // Of all the objects with the main camera tag,...
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(MAIN_TAG);
            foreach (GameObject obj in tagged)
            {
                Transform xform = obj.transform;
                while (xform != null)
                {
                    // ...find the first that is a descendant of the subject...
                    if (xform.gameObject == _subject)
                    {
                        // ...and remember this tag, so it can be given to one of the new
                        // cameras...
                        _firstSideTag = obj.tag;
                        _firstSideAngleY = obj.transform.eulerAngles[1];
                        break;
                    }
                    if (_firstSideTag.Length > 0)
                    {
                        break;
                    }
                    xform = xform.parent;
                }
            }

            // ...once this descendant of the subject is deleted along with all the other
            // descendant cameras.
            Camera[] camerasToDelete = _subject.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in camerasToDelete)
            {
                if (camera.gameObject)
                {
                    DestroyImmediate(camera.gameObject);
                }
            }
        }

        private void CreateSideCameras()
        {
            float fovDeg = 360.0f / SIDE_COUNT;
            float fovRad = Mathf.PI * 2.0f / SIDE_COUNT;

            // The `screenBottom` value is in world coordinates.  We want to find a `heightDirTrans` such that
            // `_spec.side.screenBottom = _spec.side.cameraY - _spec.side.screenHeight / 2 + heightDirTrans`.
            float heightDirTrans = _spec.side.screenBottom + _spec.side.screenHeight / 2.0f - _spec.side.cameraY;
            float viewDirTrans = (_spec.side.screenWidth / 2.0f) / Mathf.Tan(fovRad / 2.0f);
            
            if ((_firstSideTag.Length == 0) && (GameObject.FindGameObjectWithTag(MAIN_TAG) == null))
            {
                _firstSideTag = MAIN_TAG;
            }

            float cameraAngleY = _firstSideAngleY;

            for (int i = 0; i < SIDE_COUNT; ++i)
            {
                string name = SIDE_CAMERA_NAME_PREFIX + (i + 1);
                int targetDisplay = _spec.side.displayIDs[i];

                GameObject cameraObject = CreateCameraScreen(name, targetDisplay, fovDeg, COLORS[i], _spec.near, _spec.far);
                GameObject screen = cameraObject.transform.GetChild(0).gameObject;

                cameraObject.transform.parent = _subject.transform;

                if ((i == 0) && (_firstSideTag.Length > 0))
                {
                    cameraObject.tag = _firstSideTag;
                }

                cameraObject.transform.localPosition = new Vector3(0, _spec.side.cameraY, 0);
                cameraObject.transform.localEulerAngles = new Vector3(0, cameraAngleY, 0);

                screen.transform.localPosition = new Vector3(0, heightDirTrans, viewDirTrans);
                screen.transform.localScale = new Vector3(_spec.side.screenWidth, _spec.side.screenHeight, 1);

                cameraAngleY += fovDeg;
                if (cameraAngleY > 360)
                {
                    cameraAngleY -= 360;
                }
            }
        }

        private void CreateFloorCameras()
        {
            if ((_spec.floor.screenWidth == 0) || (_spec.floor.screenHeight == 0))
            {
                // The floor is optional, and should be skipped if not specified.
                return;
            }

            string name1 = FLOOR_CAMERA_NAME_PREFIX + "1";
            int targetDisplay1 = _spec.floor.displayIDs[0];

            float d1 = _subject.transform.position.y + _spec.floor.camera1Y - _spec.side.screenBottom;
            float fovRad1 = 2 * Mathf.Atan2(_spec.floor.screenHeight / 2, d1);
            float fovDeg1 = Mathf.Rad2Deg * fovRad1;

            string color1 = COLORS[SIDE_COUNT];

            GameObject cameraObject1 = CreateCameraScreen(name1, targetDisplay1, fovDeg1, color1, _spec.near, _spec.far);
            GameObject screen1 = cameraObject1.transform.GetChild(0).gameObject;

            cameraObject1.transform.parent = _subject.transform;

            cameraObject1.transform.localPosition = new Vector3(_spec.floor.camera1X, _spec.floor.camera1Y, _spec.floor.camera1Z);
            Vector3 z1 = Vector3.down;
            Vector3 y1 = new Vector3(_spec.floor.camera1X, 0, _spec.floor.camera1Z);
            cameraObject1.transform.localRotation = Quaternion.LookRotation(z1, y1);
        
            screen1.transform.localPosition = new Vector3(0, 0, d1);
            screen1.transform.localScale = new Vector3(_spec.floor.screenWidth, _spec.floor.screenHeight, 1);

            string name2 = FLOOR_CAMERA_NAME_PREFIX + "2";
            int targetDisplay2 = _spec.floor.displayIDs[1];

            float d2 = _subject.transform.position.y + _spec.floor.camera2Y - _spec.side.screenBottom;
            float fovRad2 = 2 * Mathf.Atan2(_spec.floor.screenHeight / 2, d2);
            float fovDeg2 = Mathf.Rad2Deg * fovRad2;

            string color2 = COLORS[SIDE_COUNT + 1];

            GameObject cameraObject2 = CreateCameraScreen(name2, targetDisplay2, fovDeg2, color2, _spec.near, _spec.far);
            GameObject screen2 = cameraObject2.transform.GetChild(0).gameObject;

            cameraObject2.transform.parent = _subject.transform;

            cameraObject2.transform.localPosition = new Vector3(_spec.floor.camera2X, _spec.floor.camera2Y, _spec.floor.camera2Z);
            Vector3 z2 = Vector3.down;
            Vector3 y2 = new Vector3(_spec.floor.camera2X, 0, _spec.floor.camera2Z);
            cameraObject2.transform.localRotation = Quaternion.LookRotation(z2, y2);

            screen2.transform.localPosition = new Vector3(0, 0, d2);
            screen2.transform.localScale = new Vector3(_spec.floor.screenWidth, _spec.floor.screenHeight, 1);
        }

        private void AddScripts()
        {
            if (_subject != null)
            {
                ActivateMultiDisplay activate = _subject.GetComponent<ActivateMultiDisplay>() as ActivateMultiDisplay;
                if (activate == null)
                {
                    activate = _subject.AddComponent<ActivateMultiDisplay>() as ActivateMultiDisplay;
                }
                if (activate == null)
                {
                    Debug.Log("Failed to add script 'ActivateMultiDisplay'.");
                }
            }
        }


        // Returns the GameObject for the camera, with the screen as a child.
        private GameObject CreateCameraScreen(string cameraName, int targetDisplay, float fovDeg, string color, 
            float near, float far)
        {
            GameObject cameraObject = new GameObject(cameraName);
            SetObjectDirty(cameraObject);

            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = cameraObject.name + "Screen";
            SetObjectDirty(screen);

            screen.transform.parent = cameraObject.transform;

            Camera camera = cameraObject.AddComponent(typeof(Camera)) as Camera;
            // The `Camera.targetDisplay` field is 0-based, while the display IDs are 1-based in the 
            // user interface (e.g., the Windows "Detect and identify displays" settings).
            camera.targetDisplay = targetDisplay - 1;
            camera.fieldOfView = fovDeg;
            camera.nearClipPlane = near;
            camera.farClipPlane = far;

            CreateMaterial(screen, color);
            MeshRenderer renderer = screen.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // One way of making a sceen invisible to its camera is to have the camera look
            // at the screen's back face.
            screen.transform.Rotate(0, 180, 0);

            OffAxisPerspectiveCamera offAxisPerspectiveCamera =
                    cameraObject.AddComponent<OffAxisPerspectiveCamera>() as OffAxisPerspectiveCamera;
            offAxisPerspectiveCamera.screen = screen;

            return cameraObject;
        }

        private void SetObjectDirty(GameObject obj)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(obj);
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
        }

        private void CreateMaterial(GameObject obj, string colorStr)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            Material mat = new Material(Shader.Find("Standard"));
            string path = "Assets/Materials/" + obj.name + ".mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);

            Color color;
            ColorUtility.TryParseHtmlString(colorStr, out color);
            color.a = 0.35f;

            mat.SetColor("_Color", color);

            // Enable "blend mode" "Transparent".
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            mr.material = mat;
        }

        [Serializable]
        private class SpecSide
        {
            public float screenWidth = 0;
            public float screenHeight = 0;
            public float screenBottom = 0;
            public float cameraX = 0;
            public float cameraY = 0.5f;
            public float cameraZ = 0;
            public int[] displayIDs = new int[SIDE_COUNT] { 2, 3, 4, 5 };
        }

        [Serializable]
        private class SpecFloor
        {
            public float screenWidth = 0;
            public float screenHeight = 0;
            public float camera1X = 0;
            public float camera1Y = 0;
            public float camera1Z = 0;
            public float camera2X = 0;
            public float camera2Y = 0;
            public float camera2Z = 0;
            public int[] displayIDs = new int[FLOOR_COUNT] { 6, 7 };
        }

        [Serializable]
        private class Spec
        {
            public string subjectName = "Subject";
            public SpecSide side;
            public SpecFloor floor;
            public float near = 500;
            public float far = 0.1f;
        }

        private string _jsonPath;
        private const string EDITOR_PREF_KEY_JSON_PATH = "SetupCameraBoxJsonPath";

        private Spec _spec;

        private GameObject _subject;

        private const string MAIN_TAG = "MainCamera";
        private string _firstSideTag = "";
        private float _firstSideAngleY = 0;

        private const int SIDE_COUNT = 4;
        private const int FLOOR_COUNT = 2;

        private static readonly string[] COLORS = new string[SIDE_COUNT + FLOOR_COUNT] {
            "#ff0000", "#ff7f00", "#ffff00", "#00ff00", "#0000ff", "#9400d3"
        };

        const string SIDE_CAMERA_NAME_PREFIX = "SideCamera";
        const string FLOOR_CAMERA_NAME_PREFIX = "FloorCamera";

        // With GameObject.CreatePrimitive(), properties like these must be present somewhere
        // in the code to prevent a crash due to code stripping.  See the note at the bottom here:
        // https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html

        private MeshFilter _preventStrippingMeshFilter;
        private MeshRenderer _preventStrippingMeshRenderer;
        private BoxCollider _preventStrippingBoxCollider;
    }
}
