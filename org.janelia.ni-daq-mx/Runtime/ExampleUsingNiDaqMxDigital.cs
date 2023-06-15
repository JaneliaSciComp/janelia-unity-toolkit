// An example of using `Janelia.NiDaqMx` to write to the digital output of a
// National Instruments (NI) DAQ device with the NI-DAQmx library.

// 1. In the Unity editor, create a cylinder (e.g., with the menu item
// "GameObject/3D Object/Cylinder"
// 2. With the cylinder selected, use the "Add Component" button in the editor's
// Inspector panel to attach this script to the cylinder.
// 3. Attach the NI DAQ device.
// 4. Run the game.
// 5. Press any key to send values to the NI DAQ device's digital output (assumed to have 8 ports).
// 6. When running as a stand-alone application, pressing the escape key quits the application.

// Additional options:

// Uncomment the following to test detection of the error of changing channel parameters
// after the channel has been created.
// #define TEST_ERROR

using UnityEngine;

namespace Janelia
{
    public class ExampleUsingNiDaqMxDigital : MonoBehaviour
    {
        public bool showEachWrite = true;

        private void Start()
        {
            // Create parameters that are the default values except the specified values.
            _outputParams = new Janelia.NiDaqMx.OutputParams()
            {
                ChannelNames = new string[] { "port1/line0", "port1/line1" },
            };

            // To create parameters that are all the default values, use the following:
            // _outputParams = new Janelia.NiDaqMx.OutputParams();

            if (!Janelia.NiDaqMx.CreateDigitalOutputs(_outputParams))
            {
                Debug.Log("Creating outputs failed");
                Debug.Log(Janelia.NiDaqMx.GetLatestError());
                return;
            }
            Debug.Log("Creating outputs succeeded");

            _writeData = new byte[2];
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

            // Writing

            if (Input.anyKeyDown)
            {
                int numWrittenPerChannel = 0;
                _writeData[0] = (byte)((_iWrite % 3 == 0) ? 0 : (_iWrite % 3 == 1) ? 100 : 200);
                _writeData[1] = (byte)((_iWrite % 3 == 0) ? 1 : (_iWrite % 3 == 1) ? 101 : 201);
                int expectedNumWrittenPerChannel = _writeData.Length / _outputParams.ChannelNames.Length;
                if (!Janelia.NiDaqMx.WriteToDigitalOutputs(_outputParams, _writeData, ref numWrittenPerChannel))
                {
                    Debug.Log("Frame " + Time.frameCount + ": write to outputs failed");
                    Debug.Log(Janelia.NiDaqMx.GetLatestError());
                }
                if (numWrittenPerChannel != expectedNumWrittenPerChannel)
                {
                    Debug.Log("Frame " + Time.frameCount + ": unexpectedly, wrote " + numWrittenPerChannel + " values per channel");
                }
                else if (showEachWrite)
                {
                    int n = numWrittenPerChannel * _outputParams.ChannelNames.Length;
                    Debug.Log("Frame " + Time.frameCount + ": wrote " + n + " value(s): " + _writeData[0] + ", " + _writeData[1]);
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
        byte[] _writeData;
    }
}
