// Utilities to simplify certain common types of logging.

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Janelia
{
    public static class LogUtilities
    {
        public static LogOptions GetOptions()
        {
            LogOptions[] paramObjs = UnityEngine.Object.FindObjectsOfType<LogOptions>();
            if (paramObjs.Length > 0)
            {
                if (paramObjs.Length > 1)
                {
                    Debug.LogError("One LogOptions object expected, " + paramObjs.Length + " found; using first.");
                }
                return paramObjs[0];
            }
            return null;
        }

        public static void LogAllMeshes()
        {
            MeshFilter[] meshes = (MeshFilter[])Resources.FindObjectsOfTypeAll(typeof(MeshFilter));
            foreach (MeshFilter mesh in meshes)
            {
                GameObject obj = mesh.gameObject;
                if ((obj != null) && (obj.hideFlags == HideFlags.None))
                {
                    _meshLog.meshGameObjectPath = PathName(obj);
                    Collider collider = obj.GetComponent<Collider>();
                    _meshLog.colliderType = (collider != null) ? collider.GetType().Name : "NA";
                    _meshLog.worldPosition = obj.transform.position;
                    _meshLog.worldRotationDegs = obj.transform.eulerAngles;
                    _meshLog.worldScale = obj.transform.lossyScale;
                    Logger.Log(_meshLog);
                }
            }
        }

        public static void LogDeltaTime()
        {
            int pos = 0;
            WriteString(_deltaTimeBuf, ref pos, DELTA_TIME_FIELD);
            WriteFixed6(_deltaTimeBuf, ref pos, Time.deltaTime);
            Logger.Log(_deltaTimeBuf, pos);
        }

        public static void LogCurrentResolution()
        {
            Resolution current = Screen.currentResolution;
            int pos = 0;
            WriteString(_currentResolutionBuf, ref pos, REFRESH_RATE_FIELD);
            WriteInt(_currentResolutionBuf, ref pos, current.refreshRate);
            WriteString(_currentResolutionBuf, ref pos, COMMA_NEWLINE);
            WriteString(_currentResolutionBuf, ref pos, WIDTH_PIXELS_FIELD);
            WriteInt(_currentResolutionBuf, ref pos, current.width);
            WriteString(_currentResolutionBuf, ref pos, COMMA_NEWLINE);
            WriteString(_currentResolutionBuf, ref pos, HEIGHT_PIXELS_FIELD);
            WriteInt(_currentResolutionBuf, ref pos, current.height);
            Logger.Log(_currentResolutionBuf, pos);
        }

        public static void WriteString(char[] buf, ref int pos, string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                buf[pos++] = s[i];
            }
        }

        public static void WriteBool(char[] buf, ref int pos, bool v)
        {
            WriteString(buf, ref pos, v ? "True" : "False");
        }

        public static void WriteInt(char[] buf, ref int pos, int v)
        {
            if (v < 0)
            {
                buf[pos++] = '-';
                v = -v;
            }
            if (v == 0)
            {
                buf[pos++] = '0';
                return;
            }
            int start = pos;
            while (v > 0)
            {
                buf[pos++] = (char)('0' + v % 10);
                v /= 10;
            }
            // Reverse the digits in place.
            for (int i = start, j = pos - 1; i < j; i++, j--)
            {
                char tmp = buf[i];
                buf[i] = buf[j];
                buf[j] = tmp;
            }
        }

        public static void WriteLong(char[] buf, ref int pos, long v)
        {
            if (v < 0)
            {
                buf[pos++] = '-';
                v = -v;
            }
            if (v == 0)
            {
                buf[pos++] = '0';
                return;
            }
            int start = pos;
            while (v > 0)
            {
                buf[pos++] = (char)('0' + v % 10);
                v /= 10;
            }
            // Reverse the digits in place.
            for (int i = start, j = pos - 1; i < j; i++, j--)
            {
                char tmp = buf[i];
                buf[i] = buf[j];
                buf[j] = tmp;
            }
        }

        // Writes a float with exactly 6 decimal places, equivalent to ToString("F6").
        public static void WriteFixed6(char[] buf, ref int pos, float v)
        {
            if (v < 0)
            {
                buf[pos++] = '-';
                v = -v;
            }
            // Multiply by 1e6 and round to get all digits as a single integer.
            long scaled = (long)(v * 1000000f + 0.5f);
            long intPart = scaled / 1000000L;
            long fracPart = scaled % 1000000L;
            if (intPart == 0)
            {
                buf[pos++] = '0';
            }
            else
            {
                int start = pos;
                while (intPart > 0)
                {
                    buf[pos++] = (char)('0' + intPart % 10);
                    intPart /= 10;
                }
                // Reverse the digits in place.
                for (int i = start, j = pos - 1; i < j; i++, j--)
                {
                    char tmp = buf[i];
                    buf[i] = buf[j];
                    buf[j] = tmp;
                }
            }
            buf[pos++] = '.';
            // Write fractional part with leading zeros (always 6 digits).
            for (int d = 5; d >= 0; d--)
            {
                long divisor = 1;
                for (int k = 0; k < d; k++)
                {
                    divisor *= 10;
                }
                buf[pos++] = (char)('0' + (fracPart / divisor) % 10);
            }
        }

        static private string PathName(GameObject o)
        {
            string path = o.name;
            while (o.transform.parent != null)
            {
                o = o.transform.parent.gameObject;
                path = o.name + "/" + path;
            }
            return path;
        }

        [Serializable]
        private class MeshLog : Logger.Entry
        {
            public string meshGameObjectPath;
            public string colliderType;
            public Vector3 worldPosition;
            public Vector3 worldRotationDegs;
            public Vector3 worldScale;
        };
        static private MeshLog _meshLog = new MeshLog();

        private const string DELTA_TIME_FIELD = "    \"deltaTime\": ";
        private static char[] _deltaTimeBuf = new char[64];

        private const string COMMA_NEWLINE = ",\n";
        private const string REFRESH_RATE_FIELD = "    \"refreshRateHz\": ";
        private const string WIDTH_PIXELS_FIELD = "    \"widthPixels\": ";
        private const string HEIGHT_PIXELS_FIELD = "    \"heightPixels\": ";
        private static char[] _currentResolutionBuf = new char[128];
    }
}
