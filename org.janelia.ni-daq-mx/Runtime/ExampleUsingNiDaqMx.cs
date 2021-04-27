// An example of using `Janelia.NiDaqMx` to read from and write to a
// National Instruments (NI) DAQ device with the NI-DAQmx library.

// 1. In the Unity editor, create a cylinder (e.g., with the menu item
// "GameObject/3D Object/Cylinder"
// 2. With the cylinder selected, use the "Add Component" button in the editor's
// Inspector panel to attach this script to the cylinder.
// 3. Attach the NI DAQ device.
// 4. Run the game.
// 5. When the application is running, the cylinder should rotate based on the inputs
// "Dev1/ai0" and "Dev1/ai3".
// 6. Pressing any key should write a value to outputs "Dev1/ao0" and "Dev1/ao1".
// The value alternates between three values: 0, the configured minimum voltage (-5),
// and the configured maximum (7).
// 7. Check the "Show Each Read Write" box on the cylinder to get more details of
// the DAQ device communication in the editor console.
// 8. When running as a stand-alone application, pressing the escape key quits the application.

// Additional options:

// Uncomment the following to have a key press write two values.
// #define MULTIPLE_WRITE

// Uncomment the following to test detection of the error of changing channel parameters
// after the channel has been created.
// #define TEST_ERROR

using UnityEngine;

namespace Janelia
{
    public class ExampleUsingNiDaqMx : MonoBehaviour
    {
        public bool showEachRead = false;
        public bool showEachWrite = false;

        private void Start()
        {
            // Create parameters that are the default values except the specified value.
            _inputParams = new Janelia.NiDaqMx.InputParams
            {
                ChannelNames = new string[] { "ai0", "ai3" }
            };

            // Create parameters that are the default values except the specified values.
            _outputParams = new Janelia.NiDaqMx.OutputParams()
            {
                ChannelNames = new string[] { "ao0", "ao1" },
                VoltageMin = -5,
                VoltageMax = 7
            };

            // To create parameters that are all the default values, use the following:
            // _outputParams = new Janelia.NiDaqMx.OutputParams();

            if (!Janelia.NiDaqMx.CreateInputs(_inputParams))
            {
                Debug.Log("Creating inputs failed");
                Debug.Log(Janelia.NiDaqMx.GetLatestError());
                return;
            }

            if (!Janelia.NiDaqMx.CreateOutputs(_outputParams))
            {
                Debug.Log("Creating outputs failed");
                Debug.Log(Janelia.NiDaqMx.GetLatestError());
                return;
            }

            _readData = new double[_inputParams.SampleBufferSize];
#if MULTIPLE_WRITE
            _writeData = new double[2];
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }

#if TEST_ERROR
            // Should fail with an exception, because parameters cannot be changed once in use.
            if (Time.frameCount == 10)
            {
                _inputParams.VoltageMax = -8.0;
            }
#endif

            // Reading

            int numReadPerChannel = 0;
            if (!Janelia.NiDaqMx.ReadFromInputs(_inputParams, ref _readData, ref numReadPerChannel))
            {
                Debug.Log("Frame " + Time.frameCount + ": read from input failed");
                Debug.Log(Janelia.NiDaqMx.GetLatestError());
            }
            else
            {
                if (numReadPerChannel > 0)
                {
                    float[] rot = { 0, 0 };
                    for (int iChannel = 0; iChannel < _inputParams.ChannelNames.Length; iChannel++)
                    {
                        double sum = 0;
                        for (int i = 0; i < numReadPerChannel; i++)
                        {
                            int j = Janelia.NiDaqMx.IndexInReadBuffer(iChannel, numReadPerChannel, i);
                            sum += _readData[j];
                        }
                        float mean = (float)(sum / numReadPerChannel);

                        if (showEachRead)
                        {
                            Debug.Log("Frame " + Time.frameCount + ": buffer size " + _readData.Length +
                                ", read " + numReadPerChannel + " values with mean " + mean);
                        }

                        rot[iChannel] = 0.25f * mean * 360.0f;
                    }
                    transform.eulerAngles = new Vector3(rot[0], rot[1], 0);
                }
                else
                {
                    Debug.Log("Frame " + Time.frameCount + ": unexpectedly, read " + numReadPerChannel + " values");
                }
            }

            // Writing

            if (Input.anyKeyDown)
            {
                int numWrittenPerChannel = 0;
#if MULTIPLE_WRITE
                _writeData[0] = (_iWrite % 3 == 0) ? 0 : (_iWrite % 3 == 1) ? _outputParams.VoltageMax : _outputParams.VoltageMin;
                _writeData[1] = (_iWrite % 3 == 0) ? 0 : (_iWrite % 3 == 1) ? _outputParams.VoltageMin : _outputParams.VoltageMax;
                int expectedNumWritten = _writeData.Length;
                if (!Janelia.NiDaqMx.WriteToOutputs(_outputParams, _writeData, ref numWritten))
#else
                int expectedNumWritten = 1;
                double writeValue = (_iWrite % 3 == 0) ? 0 : (_iWrite % 3 == 1) ? _outputParams.VoltageMax : _outputParams.VoltageMin;
                if (!Janelia.NiDaqMx.WriteToOutputs(_outputParams, writeValue, ref numWrittenPerChannel))
#endif
                {
                    Debug.Log("Frame " + Time.frameCount + ": write to outputs failed");
                    Debug.Log(Janelia.NiDaqMx.GetLatestError());
                }
                if (numWrittenPerChannel != expectedNumWritten)
                {
                    Debug.Log("Frame " + Time.frameCount + ": unexpectedly, wrote " + numWrittenPerChannel + " values per channel");
                }
                else if (showEachWrite)
                {
#if MULTIPLE_WRITE
                    Debug.Log("Frame " + Time.frameCount + ": wrote " + numWritten + " value(s): " + _writeData[0] + ", " + _writeData[1]);
#else
                    Debug.Log("Frame " + Time.frameCount + ": wrote " + numWrittenPerChannel + " value(s) per channel: " + writeValue);
#endif
                }
                _iWrite++;
            }
        }

        private void OnDestroy()
        {
            Janelia.NiDaqMx.OnDestroy();
        }

        private Janelia.NiDaqMx.InputParams _inputParams;
        private Janelia.NiDaqMx.OutputParams _outputParams;

        int _iWrite = 0;
        double[] _readData;
#if MULTIPLE_WRITE
        double[] _writeData;
#endif
    }
}
