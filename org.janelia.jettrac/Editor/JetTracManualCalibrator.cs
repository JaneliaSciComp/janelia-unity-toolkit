// Manages a pre-experiment calibration session in the editor, which determines how
// many units of motion in Unity are produced by rotations of of the jETTrac ball.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Janelia
{
    public class JetTracManualCalibrator : EditorWindow
    {
        [MenuItem("Window/jETTrac/jETTrac Manual Calibration")]
        public static void ShowWindow()
        {
            JetTracManualCalibrator window = (JetTracManualCalibrator)GetWindow(typeof(JetTracManualCalibrator));
            window._calibrating = false;

            float calibrationScale = JetTracTransformer.GetCalibrationScale();
            if (calibrationScale != 0)
            {
                window._calibrationScale = calibrationScale;
            }

            float ballDiameter = JetTracTransformer.GetBallDiameter();
            if (ballDiameter != 0)
            {
                window._ballDiameter = ballDiameter;
            }
            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_SMOOTH))
            {
                window._smooth = EditorPrefs.GetBool(EDITOR_PREF_KEY_SMOOTH);
            }
            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_SMOOTHING_WINDOW))
            {
                window._smoothingWindow = EditorPrefs.GetInt(EDITOR_PREF_KEY_SMOOTHING_WINDOW);
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();
            float calibrationScale = EditorGUILayout.DelayedFloatField("Latest calibration scale", _calibrationScale);
            if (EditorGUI.EndChangeCheck())
            {
                if (EditorUtility.DisplayDialog("Override calibration scale?", "Override the computed calibration scale and save it for the future?",
                        "OK", "Cancel"))
                {
                    _calibrationScale = calibrationScale;
                    JetTracTransformer.SetCalibrationScale(_calibrationScale);
                }
            }

            EditorGUI.BeginChangeCheck();
            _ballDiameter = EditorGUILayout.DelayedFloatField("Ball diameter (Unity units)", _ballDiameter);
            if (EditorGUI.EndChangeCheck())
            {
                JetTracTransformer.SetBallDiameter(_ballDiameter);
            }

            EditorGUI.BeginChangeCheck();
            _smooth = EditorGUILayout.Toggle("Use smoothing", _smooth);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(EDITOR_PREF_KEY_SMOOTH, _smooth);
            }

            EditorGUI.BeginChangeCheck();
            _smoothingWindow = EditorGUILayout.DelayedIntField("Smoothing window", _smoothingWindow);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt(EDITOR_PREF_KEY_SMOOTHING_WINDOW, _smoothingWindow);
            }

            if (!_calibrating)
            {
                if (GUILayout.Button("Start calibration"))
                {
                    StartCalibrating();
                }
                if (_resultText.Length > 0)
                {
                    GUILayout.Label(_resultText);
                }
            }
            else
            {
                GUILayout.Label("Roll the ball once around\nas smoothly and directly as possible.");
                if (GUILayout.Button("Complete calibration"))
                {
                    EndCalibrating();
                }
                if (GUILayout.Button("Cancel calibration"))
                {
                    if (EditorUtility.DisplayDialog("Cancel calibration?", "Cancel calibration and keep the current scale?",
                        "Yes", "No"))
                    {
                        CancelCalibrating();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        // The Unity documentation says this function is "called multiple times per second
        // on all visibe windows."
        public void Update()
        {
            if (_calibrating)
            {
                JetTracParser.BallMessage ballMessage = new JetTracParser.BallMessage();
                while (_reader.GetNextBallMessage(ref ballMessage))
                {
                    _transformer.AddInput(ballMessage);

                    Vector3 updatedBodyPosition = _bodyPosition;
                    Vector3 updateBodyRotation = _bodyRotation;
                    _transformer.Update(ref updatedBodyPosition, ref updateBodyRotation.y, _smooth);

                    Vector3 pathStep = updatedBodyPosition - _bodyPosition;
                    _bodyPath.Add(pathStep);

                    _bodyPathLength += pathStep.magnitude;

                    _bodyPosition += pathStep;
                    _bodyRotation = updateBodyRotation;

                    Debug.Log("Path length so far: " + _bodyPathLength + "; current position: " + _bodyPosition + "; rotation: " + _bodyRotation);
                }
            }
        }

        public void OnDisable()
        {
            if (_reader != null)
            {
                _reader.OnDisable();
            }
        }

        private void StartCalibrating()
        {
            _calibrating = true;
            _reader = new JetTracReader();
            _transformer = new JetTracTransformer(_smoothingWindow);

            JetTracTransformer.SetCalibrationScale(1, false);

            _bodyPosition = Vector3.zero;
            _bodyRotation = Vector3.zero;

            _bodyPositionStart = _bodyPosition;
            _bodyRotationStart = _bodyRotation;

            _bodyPath.Clear();
            _bodyPathLength = 0;

            try
            {
                _reader.Start();
            }
            catch (Exception e)
            {
                Debug.Log("Could not start reading the ball tracker: " + e);
            }
        }

        private void EndCalibrating()
        {
            _calibrating = false;
            _reader.OnDisable();

            // TODO: Is there any statistical analysis to do on the line segments in `_bodyPath`?

            if (_bodyPathLength > 0)
            {
                float radius = _ballDiameter / 2;
                float circumference = 2 * Mathf.PI * radius;
                _calibrationScale = circumference / _bodyPathLength;

                JetTracTransformer.SetCalibrationScale(_calibrationScale);

                float directPathLength = Vector3.Distance(_bodyPosition, _bodyPositionStart);

                _resultText = "New calibration scale factor: " + _calibrationScale + "\n" +
                    "Manual path length: " + _bodyPathLength + "\n" +
                    "Direct path length: " + directPathLength + "\n" +
                    "Manual path is " + (100 * _bodyPathLength / directPathLength) + "% of direct path\n" +
                    "Initial rotation (degrees): " + _bodyRotationStart + "\n" +
                    "Final rotation (degrees): " + _bodyRotation + "\n";
            }
            else
            {
                _resultText = "Calibration skipped: ball was not rotated";
            }
        }

        private void CancelCalibrating()
        {
            _calibrating = false;
            _reader.OnDisable();
            _resultText = "Calibration canceled";
        }

        private float _calibrationScale = JetTracTransformer.DEFAULT_CALIBRATION_SCALE;

        private float _ballDiameter = JetTracTransformer.DEFAULT_BALL_DIAMETER;

        private bool _smooth = true;
        private const string EDITOR_PREF_KEY_SMOOTH = "JetTracManualCalibratorSmooth";

        private int _smoothingWindow = 3;
        private const string EDITOR_PREF_KEY_SMOOTHING_WINDOW = "JetTracManualCalibratorSmoothingWindow";

        private JetTracReader _reader;
        private JetTracTransformer _transformer;

        private bool _calibrating;

        Vector3 _bodyPosition = new Vector3();
        Vector3 _bodyRotation = new Vector3();

        Vector3 _bodyPositionStart;
        Vector3 _bodyRotationStart;

        float _bodyPathLength;
        List<Vector3> _bodyPath = new List<Vector3>();

        string _resultText = "";
    }
}
