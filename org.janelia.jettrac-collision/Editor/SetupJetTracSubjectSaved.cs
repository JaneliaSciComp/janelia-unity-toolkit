using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{

    [Serializable]
    public class SetupJetTracSubjectSaved : ScriptableObject
    {
        public string subjectName;
        public string headName;
        public float headRotation;
        public float headHeight;
    }
}
