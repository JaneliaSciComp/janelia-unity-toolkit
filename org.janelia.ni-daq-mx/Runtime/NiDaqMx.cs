// A class for reading from and writing to National Instruments data acquisition (DAQ)
// hardware, using the second-generation NI-DAQmx driver.  Assumes that the driver
// has been downloaded and installed from National Instruments (e.g., from
// https://www.ni.com/en-us/support/downloads/drivers/download.ni-daqmx.html)
// The DLLs (e.g., `nicaiu.dll`) then should be in a standard location (e.g., the
// `C:\Windows\System32` folder) so they will be found by the `DllImport` statements.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Janelia
{
    public static class NiDaqMx
    {
        // Parameters to specify one or more input voltage channels.  Before `CreateInput`
        // is called, any parameter value can be changed from its default by using the
        // public (uppercase) properties accessesors.  But after `CreateInput` is called,
        // parameter values should not be changed, and doing so raises an exception.

        public class InputParams
        {
            internal string deviceName = "Dev1";
            public string DeviceName
            {
                get => deviceName;
                set { Restrict(); deviceName = value; }
            }

            // Specify the channel names with synatx like the following:
            // ip = new Janelia.NiDaqMx.InputParams { ChannelNames = new string[] { "ai0", "ai3" }};
            internal string[] channelNames = { "ai0" };
            public string[] ChannelNames
            {
                get => channelNames;
                set { Restrict(); channelNames = value; }
            }

            public int ChannelIndex(string channelName)
            {
                return Array.IndexOf(channelNames, channelName);
            }

            internal double voltageMin = -10.0;
            public double VoltageMin
            {
                get => voltageMin;
                set { Restrict(); voltageMin = value; }
            }

            internal double voltageMax = 10.0;
            public double VoltageMax
            {
                get => voltageMax;
                set { Restrict(); voltageMax = value; }
            }

            internal double samplesPerSec = 1000.0;
            public double SamplesPerSec
            {
                get => samplesPerSec;
                set { Restrict(); samplesPerSec = value; }
            }

            // The sample buffer size must be big enough for all samples from all channels,
            // since they are all read at once.
            internal ulong sampleBufferSize = 1000;
            public ulong SampleBufferSize
            {
                get => sampleBufferSize;
                set { Restrict(); sampleBufferSize = value; }
            }

            internal double timeoutSecs = 10.0;
            public double TimeoutSecs
            {
                get => timeoutSecs;
                set { Restrict(); timeoutSecs = value; }
            }

            internal bool inUse = false;
            internal void Restrict()
            {
                if (inUse)
                {
                    throw new MemberAccessException("Janelia.NiDaqMx.InputParams are in use and cannot be changed");
                }
            }
        }

        // Parameters to specify one or more output voltage channels.  Before `CreateOutput`
        // is called, any parameter value can be changed from its default by using the
        // public (uppercase) properties accessesors.  But after `CreateOutput` is called, 
        // parameter values should not be changed, and doing so raises an exception.

        public class OutputParams
        {
            public string deviceName = "Dev1";
            public string DeviceName
            {
                get => deviceName;
                set { Restrict(); deviceName = value; }
            }

            // Specify the channel names with synatx like the following:
            // ip = new Janelia.NiDaqMx.OutputParams { ChannelNames = new string[] { "ao0", "ao1" }};
            internal string[] channelNames = { "ao0" };
            public string[] ChannelNames
            {
                get => channelNames;
                set { Restrict(); channelNames = value; }
            }

            public int ChannelIndex(string channelName)
            {
                return Array.IndexOf(channelNames, channelName);
            }

            internal double voltageMin = -10.0;
            public double VoltageMin
            {
                get => voltageMin;
                set { Restrict(); voltageMin = value; }
            }

            internal double voltageMax = 10.0;
            public double VoltageMax
            {
                get => voltageMax;
                set { Restrict(); voltageMax = value; }
            }

            internal bool inUse = false;
            internal void Restrict()
            {
                if (inUse)
                {
                    throw new MemberAccessException("Janelia.NiDaqMx.OutpuParams are in use and cannot be changed");
                }
            }
        }

        // Error reporting.

        public static string GetLatestError()
        {
            return _latestError;
        }

        // Create the input channel(s) from the specified parameters.  Returns `true` if the creation
        // was successful.  If not, `GetLatestError()` will report what went wrong.

        public static bool CreateInputs(InputParams p)
        {
            if (_inputParamsToTaskHandle.ContainsKey(p))
            {
                _latestError = "Input(s) already created for parameters";
                return false;
;           }

            ulong taskHandle = 0;
            if (!Init(p.deviceName, ref taskHandle))
            {
                return false;
            }
            _inputParamsToTaskHandle.Add(p, taskHandle);

            // A list of channels is one string, with list elements separated by ', '.
            // https://zone.ni.com/reference/en-XX/help/370466AH-01/mxcncpts/physchannames/
            string fullChannelNames = String.Join(", ", p.channelNames.Select(c => p.deviceName + "/" + c));

            byte[] physicalChannels = MakeCString(fullChannelNames);
            byte[] namesToAssignToChannels = MakeCString("");
            IntPtr customScaleNames = IntPtr.Zero;
            int status = DAQmxCreateAIVoltageChan(taskHandle, physicalChannels, namesToAssignToChannels, DAQmx_Val_Diff,
                p.voltageMin, p.voltageMax, DAQmx_Val_Volts, customScaleNames);

            if (!StatusIndicatesSuccess(status))
            {
                return false;
            }

            byte[] source = MakeCString("");
            status = DAQmxCfgSampClkTiming(taskHandle, source, p.samplesPerSec, DAQmx_Val_Rising, DAQmx_Val_ContSamps, p.sampleBufferSize);

            if (!StatusIndicatesSuccess(status))
            {
                return false;
            }

            p.inUse = true;

            status = DAQmxStartTask(taskHandle);
            return StatusIndicatesSuccess(status);
        }

        // Read from all input channels created with `CreateInputs`. Returns `true` if the reading
        // was successful.  If not, `GetLatestError()` will report what went wrong.
        // The data from the channels are appended sequentially, not interleaved.

        public static bool ReadFromInputs(InputParams p, ref double[] buffer, ref int numReadPerChannel)
        {
            if (!_inputParamsToTaskHandle.ContainsKey(p))
            {
                _latestError = "Cannot read before inputs are created";
                return false;
            }

            ulong taskHandle = _inputParamsToTaskHandle[p];

            const int ReturnAllAvailable = -1;
            numReadPerChannel = 0;
            int status = DAQmxReadAnalogF64(taskHandle, ReturnAllAvailable, p.timeoutSecs, DAQmx_Val_GroupByChannel, 
                buffer, (uint)buffer.Length, ref numReadPerChannel, IntPtr.Zero);

            return StatusIndicatesSuccess(status);
        }

        // Get the index for a particular channel's data item in the `buffer` produced by `ReadFromInputs`.

        public static int IndexInReadBuffer(int channelIndex, int numReadPerChannel, int index = 0)
        {
            return channelIndex * numReadPerChannel + index;
        }

        // Create the output channel(s) from the specified parameters.  Returns `true` if the creation
        // was successful.  If not, `GetLatestError()` will report what went wrong.

        public static bool CreateOutputs(OutputParams p)
        {
            if (_outputParamsToTaskHandle.ContainsKey(p))
            {
                _latestError = "Input(s) already created for parameters";
                return false;
                ;
            }

            ulong taskHandle = 0;
            if (!Init(p.deviceName, ref taskHandle))
            {
                return false;
            }
            _outputParamsToTaskHandle.Add(p, taskHandle);

            // A list of channels is one string, with list elements separated by ', '.
            // https://zone.ni.com/reference/en-XX/help/370466AH-01/mxcncpts/physchannames/
            string fullChannelNames = String.Join(", ", p.channelNames.Select(c => p.deviceName + "/" + c));

            byte[] physicalChannels = MakeCString(fullChannelNames);
            byte[] namesToAssignToChannels = MakeCString("");
            IntPtr customScaleNames = IntPtr.Zero;
            int status = DAQmxCreateAOVoltageChan(taskHandle, physicalChannels, namesToAssignToChannels, p.voltageMin, p.voltageMax, DAQmx_Val_Volts, customScaleNames);

            if (!StatusIndicatesSuccess(status))
            {
                return false;
            }

            // Note there is no call to `DAQmxCfgSampClkTiming`.
            // So does writing use "on demand" timing?
            // http://zone.ni.com/reference/en-XX/help/370466AH-01/mxcncpts/smpletimingtype/

            p.inUse = true;

            status = DAQmxStartTask(taskHandle);
            return StatusIndicatesSuccess(status);
        }

        // Write to all output channels created with `CreateOutputs`.  The `data` argument has
        // all the data values to write for all the channels, appended successively, so the
        // `N` values to write to the first channel are first, followed by the `N` values to
        // write to the second channel, etc.  Note that the same number of values is written to
        // each channel.  See the convenience function, `IndexInWriteData`.  Returns `true` if
        // the writing was successful.  If not, `GetLatestError()` will report what went wrong.

        public static bool WriteToOutputs(OutputParams p, double[] data, ref int numWritten)
        {
            if (!_outputParamsToTaskHandle.ContainsKey(p))
            {
                _latestError = "Cannot write before outputs are created";
                return false;
            }

            ulong taskHandle = _outputParamsToTaskHandle[p];

            numWritten = 0;
            IntPtr reserved = IntPtr.Zero;
            int numSampsPerChan = data.Length / p.channelNames.Length;
            int status = DAQmxWriteAnalogF64(taskHandle, numSampsPerChan, false, 0, DAQmx_Val_GroupByChannel, data, ref numWritten, reserved);

            return StatusIndicatesSuccess(status);
        }

        // Get the index for a particular channel's data item in the `data` argument to `WriteToOutputs`.

        public static int IndexInWriteData(int channelIndex, int numToWritePerChannel, int index = 0)
        {
            return channelIndex * numToWritePerChannel + index;
        }

        // A simplified way to write the same data value to all the output channels.

        public static bool WriteToOutputs(OutputParams p, double data, ref int numWritten)
        {
            if (_dataOneDoublePerChannel == null)
            {
                _dataOneDoublePerChannel = new double[p.channelNames.Length];
            }
            for (int i = 0; i < _dataOneDoublePerChannel.Length; ++i)
            {
                _dataOneDoublePerChannel[i] = data;
            }
            return WriteToOutputs(p, _dataOneDoublePerChannel, ref numWritten);
        }

        public static void OnDestroy()
        {
            foreach (ulong taskHandle in _inputParamsToTaskHandle.Values)
            {
                Terminate(taskHandle);
            }
            foreach (ulong taskHandle in _outputParamsToTaskHandle.Values)
            {
                Terminate(taskHandle);
            }
        }

        //

        private static bool Init(string deviceName, ref ulong taskHandle)
        {
            int status = 0;

            if (!_resetDevices.Contains(deviceName))
            {
                _resetDevices.Add(deviceName);

                byte[] deviceNameC = MakeCString(deviceName);
                status = DAQmxResetDevice(deviceNameC);
                if (!StatusIndicatesSuccess(status))
                {
                    return false;
                }
            }

            long unixMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            string taskName = "Task" + unixMs.ToString();
            byte[] taskNameC = MakeCString(taskName);
            status = DAQmxCreateTask(taskNameC, ref taskHandle);

            return StatusIndicatesSuccess(status);
        }

        private static bool Terminate(ulong taskHandle)
        {
            if (taskHandle != 0)
            {
                int status = DAQmxStopTask(taskHandle);
                if (!StatusIndicatesSuccess(status))
                {
                    return false;
                }

                status = DAQmxClearTask(taskHandle);
                if (!StatusIndicatesSuccess(status))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool StatusIndicatesSuccess(int status)
        {
            if (status != 0)
            {
                DAQmxGetExtendedErrorInfo(_errorBuffer, ErrorBufferSize);
                _latestError = MakeCSharpString(_errorBuffer);
                return false;
            }
            _latestError = NoError;
            return true;
        }

        private static byte[] MakeCString(string s)
        {
            return Encoding.ASCII.GetBytes(s + '\0');
        }

        private static string MakeCSharpString(byte[] b)
        {
            return Encoding.ASCII.GetString(b);
        }

        private static HashSet<string> _resetDevices = new HashSet<string>();

        // Using `InputParams` or `OutputParams` as the dictionary key should not cause problems with
        // heap allocation and garbage collection.  A `Dictionary<K,V>` uses the `K.GetHashCode()` method
        // and the default `Object.GetHashCode` should follow the .NET guideline for not allocating memory:
        // https://docs.microsoft.com/en-us/visualstudio/profiling/da0010-expensive-gethashcode
        private static Dictionary<InputParams, ulong> _inputParamsToTaskHandle = new Dictionary<InputParams, ulong>();
        private static Dictionary<OutputParams, ulong> _outputParamsToTaskHandle = new Dictionary<OutputParams, ulong>();

        private static double[] _dataOneDoublePerChannel;

        private const uint ErrorBufferSize = 1024;
        private const string NoError = "No error";
        private static byte[] _errorBuffer = new byte[ErrorBufferSize];
        private static string _latestError = NoError;

        //

        // From `C:\Program Files (x86)\National Instruments\NI-DAQ\DAQmx ANSI C Dev\include\NIDAQmx.h`

        // private static const int DAQmx_Val_Cfg_Default = -1;
        private const int DAQmx_Val_Diff = 10106;

        private const int DAQmx_Val_Volts = 10348;

        private const int DAQmx_Val_Rising = 10280;
        // private const int DAQmx_Val_FiniteSamps = 10178;
        private const int DAQmx_Val_ContSamps = 10123;

        private const uint DAQmx_Val_GroupByChannel = 0;

        // Return value 0 means no error.

        [DllImport("nicaiu")]
        private static extern int DAQmxResetDevice(byte[] deviceName);

        [DllImport("nicaiu")]
        private static extern int DAQmxGetErrorString(int errorCode, byte[] errorString, uint bufferSize);

        [DllImport("nicaiu")]
        private static extern int DAQmxGetExtendedErrorInfo(byte[] errorString, uint bufferSize);

        [DllImport("nicaiu")]
        private static extern int DAQmxCreateTask(byte[] taskName, ref ulong taskHandle);

        [DllImport("nicaiu")]
        private static extern int DAQmxStartTask(ulong taskHandle);

        [DllImport("nicaiu")]
        private static extern int DAQmxStopTask(ulong taskHandle);

        [DllImport("nicaiu")]
        private static extern int DAQmxClearTask(ulong taskHandle);

        [DllImport("nicaiu")]
        private static extern int DAQmxIsTaskDone(ulong taskHandle, ref uint isTaskDone);

        [DllImport("nicaiu")]
        private static extern int DAQmxCreateAIVoltageChan(ulong taskHandle, byte[] physicalChannel, byte[] nameToAssignToChannel, int terminalConfig,
            double minVal, double maxVal, int units, IntPtr customScaleName);

        [DllImport("nicaiu")]
        private static extern int DAQmxCfgSampClkTiming(ulong taskHandle, byte[] source, double rate, int activeEdge, int sampleMode, ulong sampsPerChanToAcquire);

        [DllImport("nicaiu")]
        private static extern int DAQmxReadAnalogF64(ulong taskHandle, int numSampsPerChan, double timeout, uint fillMode, double[] readArray, uint arraySizeInSamps, ref int sampsPerChanRead, IntPtr reserved);

        [DllImport("nicaiu")]
        private static extern int DAQmxCreateAOVoltageChan(ulong taskHandle, byte[] physicalChannel, byte[] nameToAssignToChannel, double minVal, double maxVal, int units, IntPtr customScaleName);

        [DllImport("nicaiu")]
        private static extern int DAQmxWriteAnalogF64(ulong taskHandle, int numSampsPerChan, bool autoStart, double timeout, uint dataLayout, double[] writeArray, ref int sampsPerChanWritten, IntPtr reserved);
    }
}