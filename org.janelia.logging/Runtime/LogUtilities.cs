// Utilities to simplify certain common types of logging.

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Janelia
{
    public static class LogUtilities
    {
        public static void LogAllMeshes()
        {
            MeshFilter[] meshes = (MeshFilter[])Resources.FindObjectsOfTypeAll(typeof(MeshFilter));
            foreach (MeshFilter mesh in meshes)
            {
                GameObject obj = mesh.gameObject;
                if (obj.hideFlags == HideFlags.None)
                {
                    _meshLog.meshGameObjectPath = PathName(obj);
                    Collider collider = obj.GetComponent<Collider>();
                    _meshLog.colliderType = collider.GetType().Name;
                    _meshLog.worldPosition = obj.transform.position;
                    _meshLog.worldRotationDegs = obj.transform.eulerAngles;
                    _meshLog.worldScale = obj.transform.lossyScale;
                    Logger.Log(_meshLog);
                }
            }
        }

        public static void LogDeltaTime()
        {
            _deltaTimeLog.deltaTime = Time.deltaTime;
            Logger.Log(_deltaTimeLog);
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
        private struct MeshLog
        {
            public string meshGameObjectPath;
            public string colliderType;
            public Vector3 worldPosition;
            public Vector3 worldRotationDegs;
            public Vector3 worldScale;
        };
        static private MeshLog _meshLog = new MeshLog();

        [Serializable]
        private struct DeltaTimeLog
        {
            public float deltaTime;
        };
        static private DeltaTimeLog _deltaTimeLog = new DeltaTimeLog();
    }
}
