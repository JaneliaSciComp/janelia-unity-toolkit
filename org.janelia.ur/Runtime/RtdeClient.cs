using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Janelia
{
    // The RTDE server (i.e., controller, i.e., robot) must be started first,
    // before this class' `startReceiving` method is called.
    public class RtdeClient
    {
        public double updateFrequencyHz = 60.0;

        public RtdeClient(string server = "127.0.0.1", int port = 2000, int bufferSizeBytes = BUFFER_SIZE_BYTES)
        {
            int readBufferCount = 240;
            bool useUDP = false;
            _socketClient = new SocketReader(server, port, bufferSizeBytes, readBufferCount, useUDP);

            _requestBytes = new Byte[bufferSizeBytes];
            _replyBytes = new Byte[bufferSizeBytes];
        }

        public bool Start(bool startReceiving = true)
        {
            _socketClient.Start();
            if (startReceiving)
            {
                if (!StartReceiving())
                {
                    return false;
                }
            }
            return true;
        }

        public bool StartReceiving()
        {
            if (NegotiateProtocolVersion())
            {
                if (SetupControllerOutputs())
                {
                    if (StartControllerOutput())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public const int JOINT_ANGLE_COUNT = 6;
        public const int TCP_POSTION_COUNT = 6;

        public class Message
        {
            public long timestampMs;
            public double[] jointAngles = new double[JOINT_ANGLE_COUNT];
            public double[] tcpPositions = new double[TCP_POSTION_COUNT];
        }

        public bool GetNextMessage(ref Message message)
        {
            const Byte RTDE_DATA_PACKAGE = 85;

            long timestampMs = 0;
            if (_socketClient.Take(ref _replyBytes, ref timestampMs))
            {
                message.timestampMs = timestampMs;

                UInt16 replySize = 0;
                Byte replyCmd = 0;
                UInt16 offset = UnpackReplyStart(_replyBytes, ref replyCmd, ref replySize);
                if (replyCmd != RTDE_DATA_PACKAGE)
                {
                    return false;
                }
                
                Byte replyRecipeId = 0;
                offset = Unpack(_replyBytes, ref replyRecipeId, offset);
                if (replyRecipeId != _recipeId)
                {
                    return false;
                }

                for (int i = 0; i < JOINT_ANGLE_COUNT; ++i)
                {
                    offset = Unpack(_replyBytes, ref message.jointAngles[i], offset);
                }

                for (int i = 0; i < TCP_POSTION_COUNT; ++i)
                {
                    offset = Unpack(_replyBytes, ref message.tcpPositions[i], offset);
                }

                return true;
            }
            return false;
        }

        public void OnDisable()
        {
            _socketClient.OnDisable();
        }

        private bool NegotiateProtocolVersion()
        {
            // This code does not try to minimize temporary allocations, because it
            // will be called only once, at initialization.

            // Check a few times to see if the RTDE connection is ready for writing,
            // but only a few times, to keep the Unity code from freezing at startup.
            for (int i = 0; i < 3; ++i)
            {
                if (_socketClient.ReadyToWrite())
                {
                    break;
                }
                System.Threading.Thread.Sleep(500);
            }
            if (!_socketClient.ReadyToWrite())
            {
                return false;
            }

            const Byte RTDE_REQUEST_PROTOCOL_VERSION = 86;
            const Byte RTDE_GET_URCONTROL_VERSION = 118;

            UInt16 offset = PackRequestStart(RTDE_REQUEST_PROTOCOL_VERSION, ref _requestBytes);
            UInt16 version = 2;
            offset = Pack(version, ref _requestBytes, offset);
            UInt16 requestSize = PackRequestEnd(offset, ref _requestBytes);

            _socketClient.Write(_requestBytes, requestSize);

            ReadSync(ref _replyBytes);

            UInt16 replySize = 0;
            Byte replyCmd = 0;
            bool ok = false;
            offset = UnpackReplyStart(_replyBytes, ref replyCmd, ref replySize);
            offset = Unpack(_replyBytes, ref ok, offset);

            if (!ok)
            {
                return false;
            }

            offset = PackRequestStart(RTDE_GET_URCONTROL_VERSION, ref _requestBytes);
            requestSize = PackRequestEnd(offset, ref _requestBytes);

            _socketClient.Write(_requestBytes, requestSize);

            ReadSync(ref _replyBytes);

            UInt32 replyMajor = 0;
            UInt32 replyMinor = 0;
            UInt32 replyBugfix = 0;
            UInt32 replyBuild = 0;
            offset = UnpackReplyStart(_replyBytes, ref replyCmd, ref replySize);
            offset = Unpack(_replyBytes, ref replyMajor, offset);
            offset = Unpack(_replyBytes, ref replyMinor, offset);
            offset = Unpack(_replyBytes, ref replyBugfix, offset);
            offset = Unpack(_replyBytes, ref replyBuild, offset);

            if ((replyMajor < 3) || ((replyMajor == 3) && (replyMinor < 2)))
            {
                return false;
            }

            return true;
        }

        private bool SetupControllerOutputs()
        {
            const Byte RTDE_CONTROL_PACKAGE_SETUP_OUTPUTS = 79;

            UInt16 offset = PackRequestStart(RTDE_CONTROL_PACKAGE_SETUP_OUTPUTS, ref _requestBytes);
            // Request output of the actual joint rotations (as opposed to the target rotations).
            String varNames = "actual_q,actual_TCP_pose";
            offset = Pack(updateFrequencyHz, ref _requestBytes, offset);
            offset = Pack(varNames, ref _requestBytes, offset);
            UInt16 requestSize = PackRequestEnd(offset, ref _requestBytes);
            _socketClient.Write(_requestBytes, requestSize);

            ReadSync(ref _replyBytes);

            UInt16 replySize = 0;
            Byte replyCmd = 0;
            string varTypes = "";
            offset = UnpackReplyStart(_replyBytes, ref replyCmd, ref replySize);
            offset = Unpack(_replyBytes, ref _recipeId, offset);
            UInt16 varTypesByteCount = (UInt16)(replySize - offset);
            offset = Unpack(_replyBytes, ref varTypes, offset, varTypesByteCount);
            bool ok = ((_recipeId != 0) && (varTypes == "VECTOR6D,VECTOR6D"));
            return ok;
        }

        private bool StartControllerOutput()
        {
            const Byte TDE_CONTROL_PACKAGE_START = 83;

            UInt16 offset = PackRequestStart(TDE_CONTROL_PACKAGE_START, ref _requestBytes);
            UInt16 requestSize = PackRequestEnd(offset, ref _requestBytes);

            _socketClient.Write(_requestBytes, requestSize);

            ReadSync(ref _replyBytes);

            UInt16 replySize = 0;
            Byte replyCmd = 0;
            bool ok = false;
            offset = UnpackReplyStart(_replyBytes, ref replyCmd, ref replySize);
            offset = Unpack(_replyBytes, ref ok, offset);

            return ok;
        }

        private void ReadSync(ref Byte[] taken)
        {
            long timestampMs = 0;
            for (int i = 0; i < 20; ++i)
            {
                if (_socketClient.Take(ref taken, ref timestampMs))
                {
                    return;
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        private UInt16 PackRequestStart(Byte cmd, ref Byte[] dst)
        {
            // Leave room for a UInt16 at the start, to specify the overall size,
            // which will be set when the payload is added.
            UInt16 offset = sizeof(UInt16);
            return Pack(cmd, ref dst, offset);
        }

        private UInt16 PackRequestEnd(UInt16 offsetAfterPayload, ref Byte[] dst)
        {
            Pack(offsetAfterPayload, ref dst, 0);
            return offsetAfterPayload;
        }

        private UInt16 UnpackReplyStart(Byte[] src, ref Byte cmd, ref UInt16 size)
        {
            UInt16 offset = Unpack(src, ref size, 0);
            return Unpack(src, ref cmd, offset);
        }

        private UInt16 Pack(Byte[] src, ref Byte[] dst, UInt16 offset, bool handleEndian = true)
        {
            // The `System.Buffers` assembly has the `Binary.BinaryPrimitives` class
            // with useful functions like `WriteUInt16BigEndian`, to handle the RTDE
            // convention of using big-endian encoding.  But that assembly is not
            // available in Unity.
            if (handleEndian && BitConverter.IsLittleEndian)
                Array.Reverse(src);
            Buffer.BlockCopy(src, 0, dst, offset, src.Length);
            return (UInt16)(offset + src.Length);
        }

        private UInt16 Pack(Byte src, ref Byte[] dst, UInt16 offset)
        {
            dst[offset] = src;
            return (UInt16)(offset + 1);
        }

        private UInt16 Pack(UInt16 src, ref Byte[] dst, UInt16 offset)
        {
            // `BitConverter.GetBytes` creates garbage, and the `NetworkWriter` alternative is deprecated:
            // https://answers.unity.com/questions/1094068/how-to-convert-floatsdoubles-to-raw-bytes-without.html
            // For RTDE, though, this garbage should not be a problem, because `GetBytes` is needed only
            // for "input" (i.e., sending data from Unity to the robot), which is assumed not to repeatedly 
            // in a `GameObject.Update` function the way "output" does.
            return Pack(BitConverter.GetBytes(src), ref dst, offset);
        }

        private UInt16 Pack(string src, ref Byte[] dst, UInt16 offset)
        {
            return Pack(Encoding.ASCII.GetBytes(src), ref dst, offset, false);
        }

        private UInt16 Pack(double src, ref Byte[] dst, UInt16 offset)
        {
            return Pack(BitConverter.GetBytes(src), ref dst, offset);
        }

        private UInt16 Unpack(Byte[] src, ref Byte dst, UInt16 offset)
        {
            dst = src[offset];
            return (UInt16)(offset + 1);
        }

        private UInt16 Unpack(Byte[] src, ref bool dst, UInt16 offset)
        {
            dst = (src[offset] != 0);
            return (UInt16)(offset + 1);
        }

        private UInt16 Unpack(Byte[] src, ref UInt16 dst, UInt16 offset)
        {
            // The `System.Buffers` assembly has the `Binary.BinaryPrimitives` class
            // with useful functions like `ReadUInt16BigEndian`, to handle the RTDE
            // convention of using big-endian encoding.  But that assembly is not
            // available in Unity.  As an alternative, use `BitConverter`, and reverse
            // the `src` bytes in a `_scratch` array (allocated once and reused)
            // if `BitConverter.IsLittleEndian` is true.
            CopyToScratch(src, offset, sizeof(UInt16));
            dst = BitConverter.ToUInt16(_scratch, 0);
            return (UInt16)(offset + 2);
        }

        private UInt16 Unpack(Byte[] src, ref UInt32 dst, UInt16 offset)
        {
            CopyToScratch(src, offset, sizeof(UInt32));
            dst = BitConverter.ToUInt32(_scratch, 0);
            return (UInt16)(offset + 4);
        }

        private UInt16 Unpack(Byte[] src, ref string dst, UInt16 offset, UInt16 count)
        {
            dst = Encoding.ASCII.GetString(src, offset, count);
            return (UInt16)(offset + count);
        }

        private UInt16 Unpack(Byte[] src, ref double dst, UInt16 offset)
        {
            CopyToScratch(src, offset, sizeof(double));
            dst = BitConverter.ToDouble(_scratch, 0);
            return (UInt16)(offset + 8);
        }

        private void CopyToScratch(Byte[] src, UInt16 offset, int length)
        {
            int j = BitConverter.IsLittleEndian ? offset + length - 1 : offset;
            int jChange = BitConverter.IsLittleEndian ? -1 : 1;
            for (int i = 0; i < length; ++i)
            {
                _scratch[i] = src[j];
                j += jChange;
            }
        }

        private const int BUFFER_SIZE_BYTES = 4096;

        private SocketReader _socketClient;

        Byte[] _requestBytes;
        Byte[] _replyBytes;

        private Byte _recipeId = 0;

        private Byte[] _scratch = new byte[8];
    }
}
