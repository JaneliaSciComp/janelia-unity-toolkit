using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Janelia
{
    [Serializable]
    public class MappedGrassSpec
    {
        // A seed of 0 is not used, meaning each run is different.
        public int randomSeed = 0;

        public float gridCellWidthDM = 0.2f;
        public string objectDensityGridFile;
        public float objectBaseWidthDM = 0.01f;
        public float objectHeightUnitToDM = 10;
        public string heightGridFile;
        public string orientationProbDensityFile;
        public string elevationProbDensityFile;
        // If the elevation is low but the height is high, then then the grass object would need a
        // long horizontal size to reach the height. This value cuts it off earlier. A value
        // matching `gridCellWidthDM` makes sense.
        public float objectHorizontalLimitDM = 0.2f;
        public string intensityGridFile;
        public string objectColor = "#2FAE2F";
        public string groundColor = "#3f3a0b";
        public string skyColor = "#000000";

        // [front, back, left, right, top]
        public string[] skyBoxImageFiles = null;

        public bool lit = true;
        // https://docs.unity3d.com/ScriptReference/ShadowQuality.html
        // 0 means no shadows; 1 means hard shadows, low resolution; 2 means hard, medium resolution;
        // 3 means hard, high resolution; 4 means hard and soft, high resolution.
        public int shadows = 4;
        public float shadowDistance = 2;
        public float shadowNearPlane = 0.2f;
        public float shadowNearPlaneOffset = 1;
        public float shadowBias = 0.001f;
        public float shadowNormalBias = 0.001f;

        // https://docs.unity3d.com/ScriptReference/QualitySettings-antiAliasing.html
        // "Choose the level of Multi-Sample Anti-aliasing (MSAA). Valid values are 0 (no MSAA), 2, 4, and 8."
        public int antiAliasing = 2;

        public float groundSizeMultiplier = 5;
        public bool teleportAtEdge = true;

        // The male Drosophila body is about 2.5 mm head to tail
        public float subjectEyeHeight = 0.025f;

        public bool useFicTracIntegratedHeading = false;

        public int projectorCount = 4;
        public int emptySideCount = 1;
        public float projectorOffsetForward = 0;
        public float projectorOffsetLeft = 0;
        public int projectorWidth = 800;
        public int projectorHeight = 600;
        public float projectorFovHorizDeg = 40;
        public float fractionalHeight = 0.737f; 

        // Choices: 0 (left-most display screen), 1 (middle), 2 (right-most).
        public int progressBoxDisplay = 0;
        // Choices: "upperleft", "upperright", "lowerleft", "lowerright" ("_" and "-" will be removed, and uppercase made lower).
        // Anything else turns off the progress box.
        public string progressBoxLocation = "upperleft";

        public string ProgressBoxLocationCleaned()
        {
            return progressBoxLocation.Replace("-", "").Replace("_", "").ToLower();
        }
    }
}
