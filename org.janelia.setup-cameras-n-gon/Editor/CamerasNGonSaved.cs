using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{

    [Serializable]
    public class CamerasNGonSaved : ScriptableObject
    {
        public string flyName;
        public List<string> cameraNames;
        public List<string> screenNames;
        public int numEmptySides;
        public float screenWidth;
        public float screenHeight;
        public float fractionalHeight;
        public float rotationY;
        public float offsetX;
        public float offsetZ;
        public float tilt;
        public float near;
        public float far;

        public void OnEnable()
        {
            if (cameraNames == null)
            {
                cameraNames = new List<string>();
                screenNames = new List<string>();
            }
        }
    }
}
