using System;
using UnityEngine;

namespace Janelia
{
    [Serializable]
    public class SetupCylinderBackgroundSaved : ScriptableObject
    {
        public string subjectPath;
        public float radius = 1.0f;
        public float height = 0.6f;
        public float rotationY = 0.0f;
    }
}
