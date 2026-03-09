using System;
using System.IO;
using UnityEngine;
using static Janelia.NiDaqMx;

public class TalkToNiDaq_LED_MovieProtocol : MonoBehaviour
{
    [Header("LED Trigger: Specific Filenames")]
    [Tooltip("LED turns ON when the displayed PNG matches any of these filenames (e.g., \"1.png\", \"special.png\").")]
    public string[] ledOnTextureNames = new string[0];

    [Header("LED Trigger: Numeric Ranges")]
    [Tooltip("LED turns ON when the numeric part of the PNG filename falls within any of these ranges (inclusive).")]
    public TextureNumberRange[] ledOnTextureRanges = new TextureNumberRange[0];

    [Header("Debug")]
    public bool showEachWrite = false;
    public bool showEachRead = false;

    private MovieProtocolLedLogEntry _currentLogEntry = new MovieProtocolLedLogEntry();
    private Janelia.NiDaqMx.InputParams _inputParams;
    private Janelia.NiDaqMx.OutputParams _outputParams;
    private double[] _readData;
    private double[] _writeData;
    private bool odd = true;

    [Serializable]
    public struct TextureNumberRange
    {
        [Tooltip("First PNG number in range (inclusive), e.g., 500")]
        public int fromNumber;
        [Tooltip("Last PNG number in range (inclusive), e.g., 600")]
        public int toNumber;
    }

    private void Start()
    {
        _inputParams = new Janelia.NiDaqMx.InputParams
        {
            ChannelNames = new string[] { "ai0", "ai1", "ai2" }
        };
        _readData = new double[_inputParams.SampleBufferSize];

        if (!Janelia.NiDaqMx.CreateInputs(_inputParams))
        {
            Debug.LogError("TalkToNiDaq_LED_MovieProtocol: Creating input failed");
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
            Debug.LogError("TalkToNiDaq_LED_MovieProtocol: Creating output failed");
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

        // Get the currently displayed texture name from BackgroundChanger
        string currentTexture = Janelia.BackgroundChanger.CurrentTextureName;

        // Check if LED should be ON
        bool ledOn = false;
        if (!string.IsNullOrEmpty(currentTexture))
        {
            ledOn = IsTextureInNameList(currentTexture) || IsTextureInNumberRange(currentTexture);
        }

        // ao0: toggling photodiode signal
        _writeData[0] = odd ? _outputParams.VoltageMax : _outputParams.VoltageMin;
        odd = !odd;

        // ao1: rotation Y modulation
        double rotationY = transform.rotation.eulerAngles.y;
        double modulator = (_outputParams.VoltageMax - _outputParams.VoltageMin) / 360.0;
        _writeData[1] = rotationY * modulator + _outputParams.VoltageMin;

        // ao2: LED based on current texture
        _writeData[2] = ledOn ? _outputParams.VoltageMax : _outputParams.VoltageMin;

        // Log
        _currentLogEntry.currentTexture = currentTexture;
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
            Debug.Log($"Texture: {currentTexture} | LED: {(ledOn ? "ON" : "OFF")} | Write: [{_writeData[0]:F2}, {_writeData[1]:F2}, {_writeData[2]:F2}]");
        }
    }

    /// <summary>
    /// Check if the texture filename matches any entry in the explicit name list.
    /// </summary>
    private bool IsTextureInNameList(string textureName)
    {
        for (int i = 0; i < ledOnTextureNames.Length; i++)
        {
            if (string.Equals(textureName, ledOnTextureNames[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if the numeric part of the texture filename falls within any configured range.
    /// E.g., "523.png" → 523 → checked against ranges like {500, 600}.
    /// </summary>
    private bool IsTextureInNumberRange(string textureName)
    {
        if (ledOnTextureRanges.Length == 0)
            return false;

        // Strip extension and try to parse the filename as a number
        string nameWithoutExt = Path.GetFileNameWithoutExtension(textureName);
        if (!int.TryParse(nameWithoutExt, out int textureNumber))
            return false;

        for (int i = 0; i < ledOnTextureRanges.Length; i++)
        {
            if (textureNumber >= ledOnTextureRanges[i].fromNumber &&
                textureNumber <= ledOnTextureRanges[i].toNumber)
            {
                return true;
            }
        }
        return false;
    }

    private void OnDestroy()
    {
        if (_writeData == null || _outputParams == null)
            return;

        try
        {
            double resetValue = _outputParams.VoltageMin;
            int expectedNumWritten = _writeData.Length;

            Janelia.NiDaqMx.WriteToOutputs(_outputParams,
                    new double[] { resetValue, resetValue, resetValue },
                    ref expectedNumWritten);
        }
        catch (Exception) { }

        try
        {
            Janelia.NiDaqMx.OnDestroy();
        }
        catch (Exception) { }
    }

    [Serializable]
    private class MovieProtocolLedLogEntry : Janelia.Logger.Entry
    {
        public double tracePD;
        public double imgFrameTrigger;
        public double ledTrigger;
        public string currentTexture;
        public double ledState;
    }
}
