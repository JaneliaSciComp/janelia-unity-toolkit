using System;
using UnityEngine;
using static Janelia.NiDaqMx;

public class TalkToNiDaq_LED_CylinderRotation : MonoBehaviour
{
    [Header("Cylinder Reference")]
    [Tooltip("Drag the GameObject with AnimateCylinderTexture here. If left empty, it will be found automatically.")]
    public AnimateCylinderTexture cylinderTexture;

    [Header("LED Angle Ranges (0 to 360 degrees)")]
    [Tooltip("LED is ON when the cylinder azimuth falls within any of these ranges. Wrap-around is supported: e.g., from=330 to=30 means 330° through 0° to 30°.")]
    public AngleRange[] ledOnAngleRanges = new AngleRange[]
    {
        new AngleRange { fromDeg = 330f, toDeg = 30f }
    };

    [Header("Debug")]
    public bool showEachWrite = false;
    public bool showEachRead = false;

    private NiDaqLedLogEntry _currentLogEntry = new NiDaqLedLogEntry();
    private Janelia.NiDaqMx.InputParams _inputParams;
    private Janelia.NiDaqMx.OutputParams _outputParams;
    private double[] _readData;
    private double[] _writeData;
    private bool odd = true;

    [Serializable]
    public struct AngleRange
    {
        [Range(0f, 360f)] public float fromDeg;
        [Range(0f, 360f)] public float toDeg;
    }

    private void Start()
    {
        if (cylinderTexture == null)
        {
            cylinderTexture = FindObjectOfType<AnimateCylinderTexture>();
            if (cylinderTexture == null)
            {
                Debug.LogError("TalkToNiDaq_LED_CylinderRotation: No AnimateCylinderTexture found in scene.");
                return;
            }
        }

        _inputParams = new Janelia.NiDaqMx.InputParams
        {
            ChannelNames = new string[] { "ai0", "ai1", "ai2" }
        };
        _readData = new double[_inputParams.SampleBufferSize];

        if (!Janelia.NiDaqMx.CreateInputs(_inputParams))
        {
            Debug.LogError("Creating input failed");
            Debug.LogError(Janelia.NiDaqMx.GetLatestError());
            return;
        }

        _outputParams = new Janelia.NiDaqMx.OutputParams
        {
            ChannelNames = new string[] { "ao0", "ao1", "ao2" },
            VoltageMin = -5,
            VoltageMax = 5
        };

        if (!Janelia.NiDaqMx.CreateOutputs(_outputParams))
        {
            Debug.LogError("Creating output failed");
            Debug.LogError(Janelia.NiDaqMx.GetLatestError());
            return;
        }

        _writeData = new double[3] {
            _outputParams.VoltageMin,
            _outputParams.VoltageMin,
            _outputParams.VoltageMin
        };
    }

    private void Update()
    {
        if (cylinderTexture == null)
            return;

        // Read NiDaq inputs
        int numReadPerChannel = 0;
        if (Janelia.NiDaqMx.ReadFromInputs(_inputParams, ref _readData, ref numReadPerChannel))
        {
            if (numReadPerChannel > 0)
            {
                for (int i = 0; i < numReadPerChannel; i++)
                {
                    int j = IndexInReadBuffer(0, numReadPerChannel, i);
                    int k = IndexInReadBuffer(1, numReadPerChannel, i);
                    int l = IndexInReadBuffer(2, numReadPerChannel, i);
                    _currentLogEntry.tracePD = _readData[j];
                    _currentLogEntry.imgFrameTrigger = _readData[k];
                    _currentLogEntry.ledTrigger = _readData[l];
                }
            }
        }
        else
        {
            Debug.LogWarning("Read from input failed");
            Debug.LogWarning(Janelia.NiDaqMx.GetLatestError());
        }

        if (showEachRead)
        {
            Debug.Log($"tracePD: {_currentLogEntry.tracePD}, imgFrameTrigger: {_currentLogEntry.imgFrameTrigger}, ledTrigger: {_currentLogEntry.ledTrigger}");
        }

        // Get cylinder azimuth directly in 0-360
        float azimuth = cylinderTexture.AzimuthDeg;

        // Check if azimuth falls within any LED-ON range (supports wrap-around)
        bool ledOn = false;
        for (int i = 0; i < ledOnAngleRanges.Length; i++)
        {
            float from = ledOnAngleRanges[i].fromDeg;
            float to = ledOnAngleRanges[i].toDeg;
            if (from <= to)
            {
                // Normal range: e.g., from=150 to=210
                if (azimuth >= from && azimuth <= to)
                {
                    ledOn = true;
                    break;
                }
            }
            else
            {
                // Wrap-around range: e.g., from=330 to=30 means 330->360 and 0->30
                if (azimuth >= from || azimuth <= to)
                {
                    ledOn = true;
                    break;
                }
            }
        }

        // ao0: toggling photodiode signal
        _writeData[0] = odd ? _outputParams.VoltageMax : _outputParams.VoltageMin;
        odd = !odd;

        // ao1: rotation Y modulation
        double rotationY = transform.rotation.eulerAngles.y;
        double modulator = (_outputParams.VoltageMax - _outputParams.VoltageMin) / 360.0;
        _writeData[1] = rotationY * modulator + _outputParams.VoltageMin;

        // ao2: LED based on cylinder rotation
        _writeData[2] = ledOn ? _outputParams.VoltageMax : _outputParams.VoltageMin;

        // Log
        _currentLogEntry.cylinderAzimuth = azimuth;
        _currentLogEntry.ledState = ledOn ? 1.0 : 0.0;
        Janelia.Logger.Log(_currentLogEntry);

        // Write outputs to DAQ
        int expectedNumWritten = _writeData.Length;
        if (!Janelia.NiDaqMx.WriteToOutputs(_outputParams, _writeData, ref expectedNumWritten))
        {
            Debug.LogError("Write to outputs failed");
            Debug.LogError(Janelia.NiDaqMx.GetLatestError());
        }
        else if (showEachWrite)
        {
            Debug.Log($"Azimuth: {azimuth:F1}° | LED: {(ledOn ? "ON" : "OFF")} | Write: [{_writeData[0]:F2}, {_writeData[1]:F2}, {_writeData[2]:F2}]");
        }
    }

    private void OnDestroy()
    {
        if (_writeData == null || _outputParams == null)
            return;

        double resetValue = _outputParams.VoltageMin;
        int expectedNumWritten = _writeData.Length;

        if (!Janelia.NiDaqMx.WriteToOutputs(_outputParams,
                new double[] { resetValue, resetValue, resetValue },
                ref expectedNumWritten))
        {
            Debug.LogError("Reset write failed on destroy");
            Debug.LogError(Janelia.NiDaqMx.GetLatestError());
        }

        Janelia.NiDaqMx.OnDestroy();
    }

    [Serializable]
    private class NiDaqLedLogEntry : Janelia.Logger.Entry
    {
        public double tracePD;
        public double imgFrameTrigger;
        public double ledTrigger;
        public float cylinderAzimuth;
        public double ledState;
    }
}
