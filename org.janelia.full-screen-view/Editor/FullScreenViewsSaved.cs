using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    // Needed only when PERSIST_AS_RESOURCE is defined in FullScreenViews.cs

    [Serializable]
    public class FullScreenViewsSaved : ScriptableObject
    {
        public List<string> cameraNames;
        public int progressBoxLocation;
        public int progressBoxScreen;
        public int progressBoxSize;

        public void OnEnable()
        {
            if (cameraNames == null)
            {
                cameraNames = new List<string>();
            }
        }
    }
}
