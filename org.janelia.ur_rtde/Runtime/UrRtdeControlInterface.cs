using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Janelia
{
    public class UrRtdeControlInterface
    {
        public UrRtdeControlInterface(string ip = "172.17.0.2", bool verbose = false)
        {
            byte[] ip_c = MakeCString(ip);
            _controlInterface = Ur_rtde_RTDEControlInterface_new(ip_c, verbose);
        }

        ~UrRtdeControlInterface()
        {
            Ur_rtde_RTDEControlInterface_delete(_controlInterface);
        }

        public long InitPeriod()
        {
            return Ur_rtde_RTDEControlInterface_initPeriod(_controlInterface);
        }

        public void WaitPeriod(long tCycleStart)
        {
            Ur_rtde_RTDEControlInterface_waitPeriod(_controlInterface, tCycleStart);
        }

        public void StopScript()
        {
            Ur_rtde_RTDEControlInterface_stopScript(_controlInterface);
        }

        public void StopL(float a = 10.0f, bool asynchronous = false)
        {
            Ur_rtde_RTDEControlInterface_stopL(_controlInterface, a, asynchronous);
        }

        public void StopJ(float a = 2.0f, bool asynchronous = false)
        {
            Ur_rtde_RTDEControlInterface_stopJ(_controlInterface, a, asynchronous);
        }

        // pose is a 6-element vector of joint rotations
        public bool MoveJ(float[] pose, float speed = 1.05f, float acceleration = 1.4f, bool asynchronous = false)
        {
            if (pose.Length < 6) {
                Debug.Log("UrRtdeControlInterface.MoveJ failed: pose has length " + pose.Length + " instead of length 6");
                return false;
            }
            return Ur_rtde_RTDEControlInterface_moveJ(_controlInterface, pose[0], pose[1], pose[2], pose[3], pose[4], pose[5], speed, acceleration, asynchronous);
        }

        // pose is a 6-element vector of joint rotations
        public bool MoveJ_IK(float[] pose, float speed = 1.05f, float acceleration = 1.4f, bool asynchronous = false)
        {
            if (pose.Length < 6) {
                Debug.Log("UrRtdeControlInterface.MoveJ_IK failed: pose has length " + pose.Length + " instead of length 6");
                return false;
            }
            return Ur_rtde_RTDEControlInterface_moveJ_IK(_controlInterface, pose[0], pose[1], pose[2], pose[3], pose[4], pose[5], speed, acceleration, asynchronous);

        }

        // pose is a 6-element vector, with the first 3 elements being tool position (x, y, z) and the last 3 elements being tool rotation (rx, ry, rz)
        public bool MoveL(float[] pose, float speed = 0.25f, float acceleration = 1.2f, bool asynchronous = false)
        {
            if (pose.Length < 6) {
                Debug.Log("UrRtdeControlInterface.MoveL failed: pose has length " + pose.Length + " instead of length 6");
                return false;
            }
            return Ur_rtde_RTDEControlInterface_moveL(_controlInterface, pose[0], pose[1], pose[2], pose[3], pose[4], pose[5], speed, acceleration, asynchronous);
        }

        // pose is a 6-element vector, with the first 3 elements being tool position (x, y, z) and the last 3 elements being tool rotation (rx, ry, rz)
        public bool MoveL_FK(float[] pose, float speed = 0.25f, float acceleration = 1.2f, bool asynchronous = false)
        {
            if (pose.Length < 6) {
                Debug.Log("UrRtdeControlInterface.MoveL failed: pose has length " + pose.Length + " instead of length 6");
                return false;
            }
            return Ur_rtde_RTDEControlInterface_moveL_FK(_controlInterface, pose[0], pose[1], pose[2], pose[3], pose[4], pose[5], speed, acceleration, asynchronous);
        }

        // speeds is a 6-element vector of joint speeds
        // If tool = false, then the base is jogged
        public bool JogStart(float[] speeds, bool tool = true)
        {
            if (speeds.Length < 6) {
                Debug.Log("UrRtdeControlInterface.MoveL failed: speed vector has length " + speeds.Length + " instead of length 6");
                return false;
            }
            return Ur_rtde_RTDEControlInterface_jogStart(_controlInterface, speeds[0], speeds[1], speeds[2], speeds[3], speeds[4], speeds[5], tool);
        }

        public bool JogStop()
        {
            return Ur_rtde_RTDEControlInterface_jogStop(_controlInterface);
        }

        public struct PathEntry
        {
            public enum MoveType
            {
                MoveJ = 0,
                MoveL = 1,
                MoveP = 2,
                MoveC = 3
            }

            public enum PositionType 
            {
                PositionTcpPose = 0,
                PositionJoints = 1
            }

            public MoveType moveType;
            public PositionType positionType;
            public List<float> parameters;
        }

        public class Path
        {
            public void AddEntry(PathEntry entry)
            {
                waypoints.Add(entry);
            }

            public List<PathEntry> waypoints = new List<PathEntry>();
        }

        public bool MovePath(Path path, bool asynchronous = false)
        {
            int parametersCount = 0;
            for (int i = 0; i < path.waypoints.Count; i += 1)
            {
                PathEntry entry = path.waypoints[i];
                int n = entry.parameters.Count;
                if (n != 9)
                {
                    // pose[6] is velocity, pose[7] is acceleration, pose[8] is blend
                    Debug.Log("UrRtdeControlInterface.MovePath failed: waypoint " + i + " parameters has length " + n + " instead of length 9");
                    return false;
                }
                parametersCount += n;
            }
            
            int[] moveTypes = new int[path.waypoints.Count];
            int[] positionTypes = new int[path.waypoints.Count];
            int[] parametersCounts = new int[path.waypoints.Count];
            float[] parameters = new float[parametersCount];

            int j = 0;
            for (int i = 0; i < path.waypoints.Count; i += 1)
            {
                moveTypes[i] = (int) path.waypoints[i].moveType;
                positionTypes[i] = (int) path.waypoints[i].positionType;
                parametersCounts[i] = path.waypoints[i].parameters.Count;
                foreach (float parameter in path.waypoints[i].parameters)
                {
                    parameters[j] = parameter;
                    j += 1;
                }
            }

            return Ur_rtde_RTDEControlInterface_movePath(_controlInterface, path.waypoints.Count, moveTypes, positionTypes, parametersCounts,
                                                         parametersCount, parameters, asynchronous);
        }

        public int GetAsyncOperationProgress()
        {
            return Ur_rtde_RTDEControlInterface_getAsyncOperationProgress(_controlInterface);
        }

        public bool GetInverseKinematics(out float[] resultJoints, float[] x, float[] qnear = null, float maxPositionError = 1e-10f, float maxOrientationError = 1e-10f)
        {
            if (x.Length < 6) {
                Debug.Log("UrRtdeControlInterface.GetInverseKinematics failed: x vector has length " + x.Length + " instead of length 6");
                resultJoints = new float[] { 0, 0, 0, 0, 0, 0 };
                return false;
            }
            if ((qnear != null) && (qnear.Length < 6)) {
                Debug.Log("UrRtdeControlInterface.GetInverseKinematics failed: qnear vector has length " + qnear.Length + " instead of length 6");
                resultJoints = new float[] { 0, 0, 0, 0, 0, 0 };
                return false;
            }
            float res0, res1, res2, res3, res4, res5;
            if (qnear != null)
            {
                Ur_rtde_RTDEControlInterface_getInverseKinematics1(_controlInterface,
                                                                   out res0, out res1, out res2, out res3, out res4, out res5,
                                                                   x[0], x[1], x[2], x[3], x[4], x[5],
                                                                   qnear[0], qnear[1], qnear[2], qnear[3], qnear[4], qnear[5],
                                                                   maxPositionError, maxOrientationError);
            }
            else
            {
                Ur_rtde_RTDEControlInterface_getInverseKinematics2(_controlInterface,
                                                                   out res0, out res1, out res2, out res3, out res4, out res5,
                                                                   x[0], x[1], x[2], x[3], x[4], x[5],
                                                                   maxPositionError, maxOrientationError);
            }
            resultJoints = new float[] { res0, res1, res2, res3, res4, res5 };
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

        private IntPtr _controlInterface;

        //

        [DllImport("ur_rtde_c")]
        private static extern IntPtr Ur_rtde_RTDEControlInterface_new(byte[] ip, bool verbose);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RTDEControlInterface_delete(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern long Ur_rtde_RTDEControlInterface_initPeriod(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RTDEControlInterface_waitPeriod(IntPtr obj, long tCycleStart);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RTDEControlInterface_stopScript(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RTDEControlInterface_stopL(IntPtr obj, float a, bool asynchronous);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RTDEControlInterface_stopJ(IntPtr obj, float a, bool asynchronous);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEControlInterface_moveJ(IntPtr obj, 
                                                                      float r0, float r1, float r2,
                                                                      float r3, float r4, float r5,
                                                                      float speed, float acceleration, bool asynchronous);
        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEControlInterface_moveJ_IK(IntPtr obj, 
                                                                         float r0, float r1, float r2,
                                                                         float r3, float r4, float r5,
                                                                         float speed, float acceleration, bool asynchronous);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEControlInterface_moveL(IntPtr obj, 
                                                                      float x, float y, float z,
                                                                      float rx, float ry, float rz,
                                                                      float speed, float acceleration, bool asynchronous);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEControlInterface_moveL_FK(IntPtr obj, 
                                                                         float x, float y, float z,
                                                                         float rx, float ry, float rz,
                                                                         float speed, float acceleration, bool asynchronous);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEControlInterface_jogStart(IntPtr obj,
                                                                         float s0, float s1, float s2,
                                                                         float s3, float s4, float s5,
                                                                         bool tool);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEControlInterface_jogStop(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RTDEControlInterface_movePath(IntPtr obj,
                                                                         int count, int[] moveTypes, int[] positionTypes, int[] parametersCounts,
                                                                         int parametersCountTotal, float[] parameters, bool asynchronous);

        [DllImport("ur_rtde_c")]
        private static extern int Ur_rtde_RTDEControlInterface_getAsyncOperationProgress(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RTDEControlInterface_getInverseKinematics1(IntPtr obj,
                                                                                      out float result0, out float result1, out float result2, out float result3, out float result4, out float result5,
                                                                                      float x0, float x1, float x2, float x3, float x4, float x5,
                                                                                      float qnear0, float qnear1, float qnear2, float qnear3, float qnear4, float qnear5,
                                                                                      float maxPositionError, float maxOrientationError);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RTDEControlInterface_getInverseKinematics2(IntPtr obj,
                                                                                      out float result0, out float result1, out float result2, out float result3, out float result4, out float result5,
                                                                                      float x0, float x1, float x2, float x3, float x4, float x5,
                                                                                      float maxPositionError, float maxOrientationError);
    }
}
