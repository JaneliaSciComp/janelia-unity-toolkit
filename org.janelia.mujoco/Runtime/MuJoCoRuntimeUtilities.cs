using System;
using System.IO;
using UnityEngine;

namespace Janelia
{
    public static class MuJoCoRuntimeUtilities
    {
        public static Vector3 UnityVector3(float[] qpos, int iSource)
        {
            Vector3 mjVec = new Vector3(x: qpos[iSource + 0], y: qpos[iSource + 1], z: qpos[iSource + 2]);
            return Mujoco.MjEngineTool.UnityVector3(mjVec);
        }

        // See MjEngineTool.cs Mujoco.MjEngineTool.SetMjVector3(double* mjTarget, Vectore unityVec)
        public static void SetMjVector3(float[] mjTarget, int iTarget, Vector3 unityVec) 
        {
            Vector3 mjVec = Mujoco.MjEngineTool.MjVector3(unityVec);
            mjTarget[iTarget + 0] = mjVec[0];
            mjTarget[iTarget + 1] = mjVec[1];
            mjTarget[iTarget + 2] = mjVec[2];
        }

        // See MjEngineTool.cs Mujoco.MjEngineTool.UnityQuaternion(double* mjQuat)
        public static Quaternion UnityQuaternion(float[] qpos, int iSource)
        {
            Quaternion q = new Quaternion(
                x:  qpos[iSource + 1], 
                y:  qpos[iSource + 3], 
                z:  qpos[iSource + 2], 
                w: -qpos[iSource + 0]
            );
            return q;
        }

        // See MjEngineTool.cs Mujoco.MjEngineTool.SetMjQuaternion(double* mjTarget, Quaternion unityQuat)
        public static void SetMjQuaternion(float[] mjTarget, int iTarget, Quaternion unityQuat)
        {
            Quaternion mjQuat = Mujoco.MjEngineTool.MjQuaternion(unityQuat);
            mjTarget[iTarget + 0] = mjQuat.w;
            mjTarget[iTarget + 1] = mjQuat.x;
            mjTarget[iTarget + 2] = mjQuat.y;
            mjTarget[iTarget + 3] = mjQuat.z;
        }

        // The default `i0 = 0` specifies a top free joint, giving the overall position and orientation of the body.
        public static void AddFreeJointPose(Vector3 position, Quaternion rotation, float[] animationFrame, int i0 = 0)
        {
            Vector3 p = Mujoco.MjEngineTool.MjVector3(position);
            for (int i = 0; i < 3; ++i)
            {
                animationFrame[i0 + i] += p[i];
            }

            Quaternion q0 = UnityQuaternion(animationFrame, i0 + 3);
            Quaternion q1 = rotation * q0;
            SetMjQuaternion(animationFrame, i0 + 3, q1);
        }

        public static float[][] LoadAnimation(string filePath)
        {
            // Parse a file of this format:
            // [[0, 1, 2], [3, 4, 5], [6, 7, 8]]
            
            float[][] result;
            if (File.Exists(filePath))
            {
                string s0 = File.ReadAllText(filePath);
                string s1 = s0.Replace(" ", "").Replace("\t", "").Replace("\n", "");
                if ((s1[0] == '[') && (s1[s1.Length - 1] == ']'))
                {
                    string s2 = s1.Substring(1, s1.Length - 2);
                    string[] items = s2.Split("]");
                    result = new float[items.Length - 1][];
                    for (int i = 0; i < items.Length - 1; ++i)
                    {
                        string inner0 = items[i];
                        string inner1 = (inner0[0] == ',') ? inner0.Substring(1, inner0.Length - 1) : inner0;
                        if (inner1[0] == '[')
                        {
                            string inner2 = inner1.Substring(1, inner1.Length - 1);
                            string[] innerItems = inner2.Split(",");
                            result[i] = new float[innerItems.Length];
                            for (int j = 0; j < innerItems.Length; ++j)
                            {
                                float value = 0, parsed = 0;
                                if (Single.TryParse(innerItems[j], out parsed))
                                {
                                    value = parsed;
                                }
                                result[i][j] = value;
                            }
                        }
                    }
                    return result;
                }
            }

            result = new float[1][];
            return result;
        }

    }
}