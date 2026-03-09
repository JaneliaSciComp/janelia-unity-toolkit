using System;
using UnityEngine;
using static Janelia.NiDaqMx;

public class TalkToNiDaq_LED_CylinderRotation : MonoBehaviour
{
    [Header("Cylinder Reference")]
    [Tooltip("Drag the GameObject with AnimateCylinderTexture here. If left empty, it will be found automatically.")]
    public AnimateCylinderTexture cylinderTexture;

    [Header("LED Angle Ranges (signed degrees, -180 to 180)")]
    [Tooltip("LED is ON when the cylinder azimuth falls within any of these ranges.")]
    public AngleRange[] ledOnAngleRanges = new AngleRange[]
    {
        new AngleRange { minAngle = 150f, maxAngle = 180f },
        new AngleRange { minAngle = -180f, maxAngle = -150f }
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
        [Range(-180f, 180f)] public float minAngle;
        [Range(-180f, 180f)] public float maxAngle;
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

        // Get cylinder azimuth and convert from 0-360 to signed -180 to 180
        float azimuth0to360 = cylinderTexture.AzimuthDeg;
        float signedAzimuth = azimuth0to360 > 180f ? azimuth0to360 - 360f : azimuth0to360;

        // Check if azimuth falls within any LED-ON range
        bool ledOn = false;
        for (int i = 0; i < ledOnAngleRanges.Length; i++)
        {
            float min = ledOnAngleRanges[i].minAngle;
            float max = ledOnAngleRanges[i].maxAngle;
            if (signedAzimuth >= min && signedAzimuth <= max)
            {
                ledOn = true;
                break;
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
        _currentLogEntry.cylinderAzimuth = signedAzimuth;
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
            Debug.Log($"Azimuth: {signedAzimuth:F1}° | LED: {(ledOn ? "ON" : "OFF")} | Write: [{_writeData[0]:F2}, {_writeData[1]:F2}, {_writeData[2]:F2}]");
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
