using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;


namespace Janelia
{
    public class UrRtdeReceiveInterface
    {
        public UrRtdeReceiveInterface(string ip = "172.17.0.2", bool verbose = false)
        {
            byte[] ip_c = MakeCString(ip);
            _receiveInterface = Ur_rtde_RTDEReceiveInterface_new(ip_c, verbose);
        }

        ~UrRtdeReceiveInterface()
        {
            Ur_rtde_RTDEReceiveInterface_delete(_receiveInterface);
        }

        public bool IsConnected()
        {
            return Ur_rtde_RTDEReceiveInterface_isConnected(_receiveInterface);
        }

        // Actual joint positions
        public bool GetActualQ(out float[] pose)
        {
            float a0, a1, a2, a3, a4, a5;
            bool result = Ur_rtde_RTDEReceiveInterface_getActualQ(_receiveInterface, out a0, out a1, out a2, out a3, out a4, out a5);
            pose = result ? new float[] { a0, a1, a2, a3, a4, a5 } : new float[] { 0, 0, 0, 0, 0, 0 };
            return result;
        }

        // Actual Cartesian coordinates of the tool: (x,y,z,rx,ry,rz), where rx, ry and rz is a rotation vector representation of the tool orientation
        public bool GetActualTcpPose(out float[] pose)
        {
            float x, y, z, ax, ay, az;
            bool result = Ur_rtde_RTDEReceiveInterface_getActualTCPPose(_receiveInterface, out x, out y, out z, out ax, out ay, out az);
            pose = result ? new float[] { x, y, z, ax, ay, az } : new float[] { 0, 0, 0, 0, 0, 0 };
            return result;
        }

        public bool IsProtectiveStopped()
        {
            return Ur_rtde_RTDEReceiveInterface_isProtectiveStopped(_receiveInterface);
        }

        public bool IsEmergencyStopped()
        {
            return Ur_rtde_RTDEReceiveInterface_isEmergencyStopped(_receiveInterface);
        }

        private static byte[] MakeCString(string s)
        {
            return Encoding.ASCII.GetBytes(s + '\0');
        }

        private static string MakeCSharpString(byte[] b)
        {
            return Encoding.ASCII.GetString(b);
        }

        private IntPtr _receiveInterface;

        //

        [DllImport("ur_rtde_c")]
        private static extern IntPtr Ur_rtde_RTDEReceiveInterface_new(byte[] ip, bool verbose);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RTDEReceiveInterface_delete(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEReceiveInterface_isConnected(IntPtr obj);


        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEReceiveInterface_getActualQ(IntPtr obj, 
                                                                           out float a0, out float a1, out float a2,
                                                                           out float a3, out  float a4, out float a5);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEReceiveInterface_getActualTCPPose(IntPtr obj, 
                                                                                 out float x, out float y, out float z,
                                                                                 out float rx, out  float ry, out float rz);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEReceiveInterface_isProtectiveStopped(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEReceiveInterface_isEmergencyStopped(IntPtr obj);
    }
}
