using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;


namespace Janelia
{
    public class UrRtdeRobotiqGripper
    {
        public UrRtdeRobotiqGripper(string ip = "172.17.0.2", int port = 63352, bool verbose = false)
        {
            byte[] ip_c = MakeCString(ip);
            _gripperInterface = Ur_rtde_RobotiqGripper_new(ip_c, port, verbose);
        }

        ~UrRtdeRobotiqGripper()
        {
            Ur_rtde_RobotiqGripper_delete(_gripperInterface);
        }

        public void Connect(int timeout_ms = 2000)
        {
            Ur_rtde_RobotiqGripper_connect(_gripperInterface, timeout_ms);
        }

        public void Disconnect()
        {
            Ur_rtde_RobotiqGripper_disconnect(_gripperInterface);
        }

        public bool IsConnected()
        {
            return Ur_rtde_RobotiqGripper_isConnected(_gripperInterface);
        }

        public void Activate(bool autoCalibrate = false)
        {
            Ur_rtde_RobotiqGripper_activate(_gripperInterface, autoCalibrate);
        }

        public bool IsActive()
        {
            return Ur_rtde_RobotiqGripper_isActive(_gripperInterface);
        }

        public float GetOpenPosition()
        {
            return Ur_rtde_RobotiqGripper_getOpenPosition(_gripperInterface);
        }

        public float GetClosedPosition()
        {
            return Ur_rtde_RobotiqGripper_getClosedPosition(_gripperInterface);
        }

        public bool IsOpen()
        {
            return Ur_rtde_RobotiqGripper_isOpen(_gripperInterface);
        }

        public bool IsClosed()
        {
            return Ur_rtde_RobotiqGripper_isClosed(_gripperInterface);
        }

        public enum MoveMode
        {
            StartMove = 0,
            WaitFinished = 1
        }

        public int Move(float position, float speed = -1.0f, float force = -1.0f, MoveMode mode = MoveMode.StartMove)
        {
            return Ur_rtde_RobotiqGripper_move(_gripperInterface, position, speed, force, (int) mode);
        }

        public int Open(float speed = -1.0f, float force = -1.0f, MoveMode mode = MoveMode.StartMove)
        {
            return Ur_rtde_RobotiqGripper_open(_gripperInterface, speed, force, (int) mode);
        }

        public int Close(float speed = -1.0f, float force = -1.0f, MoveMode mode = MoveMode.StartMove)
        {
            return Ur_rtde_RobotiqGripper_close(_gripperInterface, speed, force, (int) mode);
        }

        public enum PositionId
        {
            Open = 0,
            Close = 1
        }

        public void EmergencyRelease(PositionId direction, MoveMode mode = MoveMode.WaitFinished)
        {
            Ur_rtde_RobotiqGripper_emergencyRelease(_gripperInterface, (int) direction, (int) mode);
        }

        public enum MoveParameter
        {
            Position = 0,
            Speed = 1,
            Force = 2
        }

        public enum Unit
        {
            Device = 0,
            Normalized = 1,
            Percent = 2,
            Mm = 3
        }

        public void SetUnit(MoveParameter param, Unit unit)
        {
            Ur_rtde_RobotiqGripper_setUnit(_gripperInterface, (int) param, (int) unit);
        }

        public void SetPositionRange_mm(int range)
        {
            Ur_rtde_RobotiqGripper_setPositionRange_mm(_gripperInterface, range);   
        }

        public float SetSpeed(float speed)
        {
            return Ur_rtde_RobotiqGripper_setSpeed(_gripperInterface, speed);
        }

        public float SetForce(float force)
        {
            return Ur_rtde_RobotiqGripper_setForce(_gripperInterface, force);
        }

        public enum ObjectStatus
        {
            Moving = 0,
            StoppedOuterObject = 1,
            StoppedInnerObect = 2,
            AtDest = 3
        }

        public ObjectStatus ObjectDetectionStatus()
        {
            int result = Ur_rtde_RobotiqGripper_objectDetectionStatus(_gripperInterface);
            return (ObjectStatus) result;
        }

        public ObjectStatus WaitForMotionComplete()
        {
            int result = Ur_rtde_RobotiqGripper_waitForMotionComplete(_gripperInterface);
            return (ObjectStatus) result;
        }

        private static byte[] MakeCString(string s)
        {
            return Encoding.ASCII.GetBytes(s + '\0');
        }

        private static string MakeCSharpString(byte[] b)
        {
            return Encoding.ASCII.GetString(b);
        }

        private IntPtr _gripperInterface;

        //

        [DllImport("ur_rtde_c")]
        private static extern IntPtr Ur_rtde_RobotiqGripper_new(byte[] ip, int port, bool verbose);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RobotiqGripper_delete(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RobotiqGripper_connect(IntPtr obj, int timeout_ms);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RobotiqGripper_disconnect(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RobotiqGripper_isConnected(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RobotiqGripper_activate(IntPtr obj, bool autoCalibrate);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RobotiqGripper_isActive(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern float Ur_rtde_RobotiqGripper_getOpenPosition(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern float Ur_rtde_RobotiqGripper_getClosedPosition(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RobotiqGripper_isOpen(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern bool Ur_rtde_RobotiqGripper_isClosed(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern int Ur_rtde_RobotiqGripper_move(IntPtr obj, float position, float speed, float force, int mode);

        [DllImport("ur_rtde_c")]
        private static extern int Ur_rtde_RobotiqGripper_open(IntPtr obj, float speed, float force, int mode);

        [DllImport("ur_rtde_c")]
        private static extern int Ur_rtde_RobotiqGripper_close(IntPtr obj, float speed, float force, int mode);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RobotiqGripper_emergencyRelease(IntPtr obj, int direction, int mode);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RobotiqGripper_setUnit(IntPtr obj, int param, int unit);

        [DllImport("ur_rtde_c")]
        private static extern void Ur_rtde_RobotiqGripper_setPositionRange_mm(IntPtr obj, int range);
 
        [DllImport("ur_rtde_c")]
        private static extern float Ur_rtde_RobotiqGripper_setSpeed(IntPtr obj, float speed);

        [DllImport("ur_rtde_c")]
        private static extern float Ur_rtde_RobotiqGripper_setForce(IntPtr obj, float force);

        [DllImport("ur_rtde_c")]
        private static extern int Ur_rtde_RobotiqGripper_objectDetectionStatus(IntPtr obj);

        [DllImport("ur_rtde_c")]
        private static extern int Ur_rtde_RobotiqGripper_waitForMotionComplete(IntPtr obj);
     }
}
