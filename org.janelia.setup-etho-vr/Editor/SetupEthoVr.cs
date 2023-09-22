using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR

namespace Janelia
{
    public class SetupEthoVr : EditorWindow
    {
        [MenuItem("Window/Setup Etho-VR")]
        public static void ShowWindow()
        {
            GetWindow(typeof(SetupEthoVr));
        }

        public void OnGUI()
        {
            EditorGUIUtility.labelWidth = 200;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            _twoScreenRig = EditorGUILayout.Toggle("Use two-screen behavioral rig", _twoScreenRig);
            EditorGUILayout.Space();

            _ficTracIntegratedHeading = EditorGUILayout.Toggle("Use FicTrac integrated heading", _ficTracIntegratedHeading);
            EditorGUILayout.Space();

            if (GUILayout.Button("Setup"))
            {
                Setup(_ficTracIntegratedHeading);
                Close();
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void Setup(bool ficTracIntegratedHeading)
        {
            SetupCameraScreens();
            SetupForceRenderRate();
            SetupAdjoiningDisplaysCamera();
            SetupLogging();
            SetupFicTrac();
            SetupNiDaqMx();
        }

        private void SetupCameraScreens()
        {
            if (_twoScreenRig)
            {
                int numCameras = 2;
                int numEmptySides = 2;
                float screenWidth = 1.0f;
                float screenHeight = 1.72f;
                float fractionalHeight = 0.33f;
                float rotationY = 45;
                float offsetX = -0.32f;
                SetupCamerasNGon.Setup(numCameras, numEmptySides, screenWidth, screenHeight, fractionalHeight, 
                                       rotationY, offsetX);
            }
            else{
                SetupCamerasNGon.Setup();
            }
        }

        private void SetupForceRenderRate()
        {
            GameObject obj = GameObject.Find("ForceRenderRate");
            if (obj == null)
            {
                obj = new GameObject("ForceRenderRate");
            }
            ForceRenderRate forcer = obj.GetComponent<ForceRenderRate>();
            if (forcer == null)
            {
                forcer = obj.AddComponent<ForceRenderRate>();
            }
            forcer.rateHz = 144.0f;
            forcer.framesToAverage = 400;
        }

        private void SetupAdjoiningDisplaysCamera()
        {
            GameObject fly = GameObject.Find("Fly");
            if (fly == null)
            {
                Debug.Log("Cannot find object 'Fly'");
                return;
            }
            Camera[] flyCameras = fly.GetComponentsInChildren<Camera>();
            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera == null)
            {
                Debug.Log("Cannot find object 'Main Camera'");
                return;
            }
            AdjoiningDisplaysCamera adjoiner = mainCamera.GetComponent<AdjoiningDisplaysCamera>();
            if (adjoiner == null)
            {
                adjoiner = mainCamera.AddComponent<AdjoiningDisplaysCamera>();
            }
            adjoiner.displayCameras = flyCameras;
        }

        private void SetupLogging()
        {
            GameObject fly = GameObject.Find("Fly");
            if (fly == null)
            {
                Debug.Log("Cannot find object 'Fly'");
                return;
            }
            LogOptions options = fly.GetComponent<LogOptions>();
            if (options == null)
            {
                options = fly.AddComponent<LogOptions>();
            }
            options.EnableLogging = true;
        }

        private void SetupFicTrac(  )
        {
            GameObject fly = GameObject.Find("Fly");
            if (fly == null)
            {
                Debug.Log("Cannot find object 'Fly'");
                return;
            }
            FicTracSubjectIntegrated subject1 = fly.GetComponent<FicTracSubjectIntegrated>();
            if (subject1 != null)
            {
                DestroyImmediate(subject1);
            }
            FicTracSubject subject2 = fly.GetComponent<FicTracSubject>();
            if (subject2 != null)
            {
                DestroyImmediate(subject2);
            }
            if (_ficTracIntegratedHeading)
            {
                FicTracSubjectIntegrated subject = fly.AddComponent<FicTracSubjectIntegrated>();
                subject.ficTracBallRadius = 0.47f; // cm
                subject.logFicTracMessages = true;
            }
            else
            {
                FicTracSubject subject = fly.AddComponent<FicTracSubject>();
                subject.ficTracBallRadius = 0.47f; // cm
                subject.smoothingCount = 1;
                subject.logFicTracMessages = true;
            }
        }

        private void SetupNiDaqMx()
        {
            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera == null)
            {
                Debug.Log("Cannot find object 'Main Camera'");
                return;
            }
            Talk2NiDaqMx talk = mainCamera.GetComponent<Talk2NiDaqMx>();
            if (talk == null)
            {
                mainCamera.AddComponent<Talk2NiDaqMx>();
            }
        }

        private bool _twoScreenRig = false;
        private bool _ficTracIntegratedHeading = false;
    }
}
#endif
