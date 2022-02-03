using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Janelia
{
    public class SetupJetTracSubject : EditorWindow
    {

        [MenuItem("Window/jETTrac/Setup jETTrac Collision Subject")]
        public static void ShowWindow()
        {
            SetupJetTracSubject window = (SetupJetTracSubject)GetWindow(typeof(SetupJetTracSubject));
        }

        public void OnEnable()
        {   
            Load();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            _subjectName = EditorGUILayout.TextField("Subject name", _subjectName);
            _headName = EditorGUILayout.TextField("Head name", _headName);

            string headRotationLabel = "Head rotation (degs)";
            string headRotationTooltip = "90 degrees makes the camera look down the subject's X axis";
            GUIContent content = new GUIContent(headRotationLabel, headRotationTooltip);
            _headRotation = EditorGUILayout.FloatField(content, _headRotation);
            _headHeight = EditorGUILayout.FloatField("Head height", _headHeight);

            if (GUILayout.Button("Create"))
            {
                DestroyHierarchy();
                CreateHierarchy();
                AddScripts();

                // TODO: Add simple head and body geometry, with head geometry showing the forward direction,
                // and both geometries being invisible from cameras.
            }

            EditorGUILayout.EndVertical();
        }
        
        private void OnDestroy()
        {
            Save();
        }

        private void DestroyHierarchy()
        {
            GameObject subject = GameObject.Find(_subjectName);
            if (subject != null)
            {
                Camera[] cameras = subject.GetComponentsInChildren<Camera>();
                foreach (Camera camera in cameras)
                {
                    if (camera.name == "Main Camera")
                    {
                        camera.gameObject.transform.parent = null;
                        break;
                    }
                }

                while (subject.transform.childCount > 0)
                {
                    DestroyImmediate(subject.transform.GetChild(0).gameObject);
                }
                DestroyImmediate(subject);
            }
        }

        private void CreateHierarchy()
        {
            // The subject, which is the root of the hierarchy, and to which motion
            // will be applied.

            GameObject subject = new GameObject(_subjectName);
            subject.transform.localPosition = Vector3.zero;
            subject.transform.localEulerAngles = Vector3.zero;
            subject.transform.localScale = Vector3.one;

            // For some reason, creating objects in this routine does not seem to
            // mark the containing scene as dirty, so it is difficult to save the
            // scene.  As a work-around, manually force the dirty marking.

            SetObjectDirty(subject);

            // Add a head as a child of the subject.  This object is useful primarily 
            // for visualizing the jETTrac head encoder when viewing the scene from
            // an additional observational camera.

            GameObject head = new GameObject(_headName);
            head.transform.SetParent(subject.transform);
            head.transform.localPosition = new Vector3(0, _headHeight, 0);
            head.transform.Rotate(0, _headRotation, 0);
            subject.transform.localScale = Vector3.one;

            SetObjectDirty(head);

            GameObject camera = GameObject.Find("Main Camera");
            if (camera != null)
            {
                camera.transform.SetParent(head.transform);
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localEulerAngles = Vector3.zero;
                subject.transform.localScale = Vector3.one;
            }
        }

        private void AddScripts()
        {
            GameObject subject = GameObject.Find(_subjectName);
            if (subject != null)
            {
                JetTracSubject jetTracSubject = subject.GetComponent<JetTracSubject>() as JetTracSubject;
                if (jetTracSubject == null)
                {
                    jetTracSubject = subject.AddComponent<JetTracSubject>() as JetTracSubject;
                }
                if (jetTracSubject != null)
                {
                    jetTracSubject.headName = _headName;
                }
                else
                {
                    Debug.Log("Failed to add script 'JetTracSubject'.");
                }

                FrameRateLogger frameRateLogger = subject.GetComponent<FrameRateLogger>() as FrameRateLogger;
                if (frameRateLogger == null)
                {
                    frameRateLogger = subject.AddComponent<FrameRateLogger>() as FrameRateLogger;
                }
                if (jetTracSubject == null)
                {
                    Debug.Log("Failed to add script 'FrameRateLogger'.");
                }

            }
        }

        // Storing the UI state across sessions, using resources.

        private SetupJetTracSubjectSaved _saved;

        private void Save()
        {
            _saved.subjectName = _subjectName;
            _saved.headName = _headName;
            _saved.headRotation = _headRotation;
            _saved.headHeight = _headHeight;

            AssetDatabase.Refresh();
            EditorUtility.SetDirty(_saved);
            AssetDatabase.SaveAssets();
        }

        private void Load()
        {
            _saved = Resources.Load<SetupJetTracSubjectSaved>("Editor/savedSetupJetTracSubject");
            if (_saved != null)
            {
                _subjectName = _saved.subjectName;
                _headName = _saved.headName;
                _headRotation = _saved.headRotation;
                _headHeight = _saved.headHeight;
            }
            else
            {
                _saved = CreateInstance<SetupJetTracSubjectSaved>();

                string root = Application.dataPath;
                EnsureDirectory(root + "/Resources");
                EnsureDirectory(root + "/Resources/Editor");

                // Saving and loading work only if the filename has the extension ".asset".

                AssetDatabase.CreateAsset(_saved, "Assets/Resources/Editor/savedSetupJetTracSubject.asset");
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

        private void SetObjectDirty(GameObject obj)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(obj);
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
        }

        private string _subjectName = "Subject";
        private string _headName = "Head";
        private float _headRotation = 90.0f;
        private float _headHeight = 0.5f;
    }
}
