using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR

namespace Janelia
{
    public static class SetupMappedGrass
    {
        public const string EDITOR_PREF_KEY_JSON_PATH = "MappedGrassJsonPath";

        [MenuItem("Mapped Grass/Set Up Grass", priority: 1, isValidateFunction: false)]
        public static void SetupMenuItem()
        {
            string path = "";
            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_JSON_PATH))
            {
                path = EditorPrefs.GetString(EDITOR_PREF_KEY_JSON_PATH);
            }
            string title = "Set Up Mapped Grass";
            string message = "Set up a field of grass from map files" + 
                ((path != "") ? " from spec file '" + path + "'"
                 : " from default spec") + "?";
            int button = EditorUtility.DisplayDialogComplex(title, message, "Set Up", "Cancel", "Choose spec file...");
            if (button == 0)
            {
                Setup(path);
            }
            else if (button == 2)
            {
                string dirRelativeToProject = "..";
                Debug.Log(Application.dataPath);
                path = EditorUtility.OpenFilePanel("Open Mapped Grass Spec", dirRelativeToProject, "json");
                if (path.Length != 0)
                {
                    EditorPrefs.SetString(EDITOR_PREF_KEY_JSON_PATH, path);
                    SetupMenuItem();
                }
            }
        }

        private static void Setup(string jsonPath)
        {
            MappedGrassSpec spec;
            if (LoadJson(jsonPath, out spec))
            {
                if (spec.randomSeed != 0)
                {
                    UnityEngine.Random.InitState(spec.randomSeed);
                    Debug.Log($"Using random seed {spec.randomSeed}");
                }

                GameObject subject = SetupSubject(spec, "Fly");
                SetupSubjectCameraScreens(spec, subject);
                SetupAdjoiningDisplaysCamera(spec, subject);
                SetupQuality(spec);
                SetupLighting(spec);
                SetupSky(spec, subject);
                SetupSkybox(spec);  
                float width = SetupField(spec);
                SetupTeleporting(spec, subject, width);
            }
        }

        private static bool LoadJson(string jsonPath, out MappedGrassSpec spec)
        {
            spec = new MappedGrassSpec();
            if (jsonPath != "")
            {
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    JsonUtility.FromJsonOverwrite(json, spec);

                    spec.objectDensityGridFile = ResolveFilePath(spec.objectDensityGridFile, jsonPath);
                    spec.intensityGridFile = ResolveFilePath(spec.intensityGridFile, jsonPath);
                    spec.heightGridFile = ResolveFilePath(spec.heightGridFile, jsonPath);
                    spec.orientationProbDensityFile = ResolveFilePath(spec.orientationProbDensityFile, jsonPath);
                    spec.elevationProbDensityFile = ResolveFilePath(spec.elevationProbDensityFile, jsonPath);
                    ResolveSkyboxPaths(spec, jsonPath);

                    return true;
                }
                else
                {
                    Debug.Log($"Cannot find JSON file \"{jsonPath}\"");
                }
            }
            return false;
        }

        private static string ResolveFilePath(string path, string jsonPath)
        {
            if (File.Exists(path))
            {
                return path;
            }
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }
            string directory = Path.GetDirectoryName(jsonPath);
            string resolvedPath = Path.Join(directory, path);
            return resolvedPath;
        }

        private static GameObject SetupSubject(MappedGrassSpec spec, string name)
        {
            DeleteObject(name);
            GameObject subject = new GameObject(name);
            if (spec.useFicTracIntegratedHeading)
            {
                subject.AddComponent<FicTracSubjectIntegrated>();
            }
            else
            {
                subject.AddComponent<FicTracSubject>();
            }

            return subject;
        }

        private static void SetupSubjectCameraScreens(MappedGrassSpec spec, GameObject subject)
        {
            float screenHeight = spec.projectorWidth / (float)spec.projectorWidth;
            float screenWidth = spec.projectorHeight / (float)spec.projectorWidth;
            float fractionalHeight = spec.fractionalHeight;
            SetupCamerasNGon.Setup(spec.projectorCount, spec.emptySideCount, screenWidth, screenHeight, fractionalHeight, 0, spec.projectorOffsetForward, spec.projectorOffsetLeft);

            Camera[] subjectCameras = subject.GetComponentsInChildren<Camera>();
            foreach (Camera subjectCamera in subjectCameras)
            {
                Vector3 p = subjectCamera.transform.position;
                p.y = spec.subjectEyeHeight;
                subjectCamera.transform.position = p;

                // The SceneManager.cs code for changing the sky color expects to operate on
                // cameras whose names contain "Source", not "Fly".
                string cameraName = subjectCamera.name.Replace("Fly", "Source");
                subjectCamera.name = cameraName;
            }
        }

        private static void SetupAdjoiningDisplaysCamera(MappedGrassSpec spec, GameObject subject)
        {
            Camera[] subjectCameras = subject.GetComponentsInChildren<Camera>();
            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera == null)
            {
                Debug.Log("Cannot find object 'Main Camera'");
                return;
            }
            AdjoiningDisplaysCamera adjoiner = mainCamera.GetComponent<AdjoiningDisplaysCamera>();
            if (adjoiner == null)
            {
                adjoiner = mainCamera.AddComponent<AdjoiningDisplaysCamera>();
            }
            adjoiner.displayCameras = subjectCameras;

            string progressBoxPosition = spec.ProgressBoxLocationCleaned();
            switch (progressBoxPosition)
            {
                case "upperleft":
                    adjoiner.progressBoxLocation = AdjoiningDisplaysCamera.ProgressBoxLocation.UPPER_LEFT;
                    break;
                case "upperright":
                    adjoiner.progressBoxLocation = AdjoiningDisplaysCamera.ProgressBoxLocation.UPPER_RIGHT;
                    break;
                case "lowerleft":
                    adjoiner.progressBoxLocation = AdjoiningDisplaysCamera.ProgressBoxLocation.LOWER_LEFT;
                    break;
                case "lowerright":
                    adjoiner.progressBoxLocation = AdjoiningDisplaysCamera.ProgressBoxLocation.LOWER_RIGHT;
                    break;
                default:
                    adjoiner.progressBoxLocation = AdjoiningDisplaysCamera.ProgressBoxLocation.NONE;
                    break;
            }
            adjoiner.progressBoxCamera = spec.progressBoxDisplay;

            // The camera with the maximum depth is the camera that is displayed.
            // The MuJoCo fly model comes with a number of cameras that have depths higher than the default.
            // So give the main camera a depth higher than any of those cameras or it will not be displayed.
            Camera camera = mainCamera.GetComponent<Camera>();  
            camera.depth = Math.Max(MaximumCameraDepth() + 1, 1);
        }

        private static float MaximumCameraDepth()
        {
            float result = -float.MaxValue;
            foreach (Camera camera in Camera.allCameras)
            {
                result = Mathf.Max(camera.depth, result);
            }
            return result;
        }

        private static void SetupQuality(MappedGrassSpec spec)
        {
            switch (spec.shadows)
            {
                default:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    break;
                case 1:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    break;
                case 2:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    break;
                case 3:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    break;
                case 4:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    break;
            }
            QualitySettings.shadowDistance = spec.shadowDistance;
            QualitySettings.shadowNearPlaneOffset = spec.shadowNearPlaneOffset;


            // "Valid values are 0 (no MSAA), 2, 4, and 8."
            QualitySettings.antiAliasing = spec.antiAliasing;

            Debug.Log("QualitySettings.antiAliasing " + QualitySettings.antiAliasing);
            Debug.Log("QualitySettings.shadows " + QualitySettings.shadows);
            Debug.Log("QualitySettings.shadowResolution " + QualitySettings.shadowResolution);
        }

        private static void SetupLighting(MappedGrassSpec spec)
        {
            GameObject obj = GameObject.Find("Directional Light");
            if (obj != null)
            {
                Light light = obj.GetComponent<Light>();
                if (light != null)
                {
                    light.shadowBias = spec.shadowBias;
                    light.shadowNormalBias = spec.shadowNormalBias;
                    light.shadowNearPlane = spec.shadowNearPlane;
                }
            }
        }

        private static void SetupSky(MappedGrassSpec spec, GameObject subject)
        {
            string sky = spec.skyColor;
            if ((spec.skyBoxImageFiles != null) && (spec.skyBoxImageFiles.Length == 5))
            {
                sky = "sky";
            }
            IEnumerable<Camera> cameras = subject.GetComponentsInChildren<Camera>().Where(camera => camera.name.Contains("Source"));

            Color color;
            if (ColorUtility.TryParseHtmlString(sky, out color))
            {
                foreach (Camera camera in cameras)
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = color;
                }
            }
            else if (sky == "sky")
            {
                foreach (Camera camera in cameras)
                {
                    camera.clearFlags = CameraClearFlags.Skybox;
                    camera.backgroundColor = color;
                }
            }
            else
            {
                Debug.Log($"Invalid sky color \"{sky}\"");
            }
        }

        private static void ResolveSkyboxPaths(MappedGrassSpec spec, string jsonPath)
        {
            if ((spec.skyBoxImageFiles != null) && (spec.skyBoxImageFiles.Length == 5))
            {
                for (int i = 0; i < spec.skyBoxImageFiles.Length; ++i)
                {
                    spec.skyBoxImageFiles[i] = ResolveFilePath(spec.skyBoxImageFiles[i], jsonPath);
                }
            }
        }

        private static void SetupSkybox(MappedGrassSpec spec)
        {
            if ((spec.skyBoxImageFiles != null) && (spec.skyBoxImageFiles.Length == 5))
            {
                Material sky = new Material(Shader.Find("Skybox/6 Sided"));

                string[] props = { "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex" };
                for (int i = 0; i < spec.skyBoxImageFiles.Length; ++i)
                {
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(System.IO.File.ReadAllBytes(spec.skyBoxImageFiles[i]));
                    sky.SetTexture(props[i], tex);
                }

                // Solid black for the missing floor.
                Texture2D black = new Texture2D(1, 1);
                black.SetPixel(0, 0, Color.black);
                black.Apply();
                sky.SetTexture("_DownTex", black);

                RenderSettings.skybox = sky;
            }
        }

        private static float SetupField(MappedGrassSpec spec)
        {
            List<List<float>> objectDensityGrid = ReadCSV(spec.objectDensityGridFile);
            List<List<float>> heightGrid = ReadCSV(spec.heightGridFile);
            List<List<float>> intensityGrid = ReadCSV(spec.intensityGridFile);
            WeightedSampler orientationSampler;
            WeightedSampler elevationSampler;
            try
            {
                orientationSampler = new WeightedSampler(spec.orientationProbDensityFile);
                elevationSampler = new WeightedSampler(spec.elevationProbDensityFile);
            }
            catch (System.Exception ex)
            {
                Debug.Log(ex);
                return 0;
            }
            if ((objectDensityGrid == null) ||(heightGrid == null))
            {
                return 0;
            }

            // Skip the first line: it is some sort of scale.
            int cellCountX = objectDensityGrid.Count - 1;
            // Skip the first column: it is some sort of scale.
            int cellCountZ = objectDensityGrid[0].Count - 1;

            Debug.Log($"X cell count: {cellCountX}; Z cell count {cellCountZ}; grid width {spec.gridCellWidthDM} dm");

            float groundWidthX = cellCountX * spec.gridCellWidthDM * spec.groundSizeMultiplier;
            float groundWidthZ = cellCountZ * spec.gridCellWidthDM * spec.groundSizeMultiplier;
            GameObject ground = MakeGround(groundWidthX, groundWidthZ, spec.groundColor, spec.lit);

            int namePadX = Mathf.CeilToInt(Mathf.Log10(cellCountX));
            int namePadZ = Mathf.CeilToInt(Mathf.Log10(cellCountZ));
            float limit = spec.objectHorizontalLimitDM;

            Color baseColor = new Color(47/255.0f, 174/255.0f, 47/255.0f, 1.0f);
            ColorUtility.TryParseHtmlString(spec.objectColor, out baseColor);

            // Skip the first line: it is some sort of scale.
            for (int iX = 1; iX <= cellCountX; iX++)
            {
                // Skip the first column: it is some sort of scale.
                for (int iZ = 1; iZ <= cellCountZ; iZ++)
                {
                    float x = (-cellCountX / 2 + (iX - 1)) * spec.gridCellWidthDM;
                    // Flip Z to match Shivam's heatmaps.
                    float z = (cellCountZ / 2 - (iZ - 1)) * spec.gridCellWidthDM;
                    float density = objectDensityGrid[iX][iZ];
                    float height = heightGrid[iX][iZ];
                    height *= spec.objectHeightUnitToDM;
                    float intensity = intensityGrid[iX][iZ];
                    Color color = baseColor * intensity / 255.0f;
                    string namePart = $"{iX.ToString().PadLeft(namePadX, '0')}_{iZ.ToString().PadLeft(namePadZ, '0')}";
                    SetupCell(x, z, spec.gridCellWidthDM, density, spec.objectBaseWidthDM, height, orientationSampler, elevationSampler, limit, color, spec.lit, namePart, ground);
                }
            }

            Debug.Log($"Triangle count: {_triangleCount}");

            return groundWidthX;
        }

        private static List<List<float>> ReadCSV(string path)
        {
            List<List<float>> result = null;
            if (File.Exists(path))
            {
                result  = new List<List<float>>();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] values = line.Split(",");
                            List<float> floats = new List<float>();
                            foreach (string value in values)
                            {
                                if (float.TryParse(value, out float parsed))
                                {
                                    floats.Add(parsed);
                                }
                                else
                                {
                                    Debug.Log($"Cannot parse as floats the line \"{line}\" from CSV file \"{path}\"");
                                }
                            }
                            if (floats.Count > 0)
                            {
                                result.Add(floats);
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"Cannot find CSV file \"{path}\"");
            }

            return result;
        }

        private static GameObject MakeGround(float widthX, float widthZ, string color, bool lit)
        {
            string name = "Ground";
            DeleteObject(name);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = name;
            float defaultWidth = 10;
            ground.transform.localScale = new Vector3(widthX / defaultWidth, 1, widthZ / defaultWidth);
            AddMaterial(ground, color, lit);
            EditorSceneManager.MarkSceneDirty(ground.scene);
            return ground;
        }
        
        private static void SetupCell(float cellX, float cellZ, float cellWidth, float density, float baseWidth, float height, WeightedSampler orientationSampler, WeightedSampler elevationSampler, float limit, Color color, bool lit, string namePart, GameObject parent)
        {
            // Assume that `density` is in objs per dm^2 (where dm is decimeters, m / 10).
            if (cellWidth > 1)
            {
                Debug.Log($"Cell width is {cellWidth} but only widths of 1 dm (0.1 m) or less are supported");
                return;
            }

            if (density == 0.0f)
            {
                return;
            }
            // Both `density` and `cellWidth` are in dm.
            float area = cellWidth * cellWidth;
            float densityCount = density * area;
            int densityPad = Mathf.CeilToInt(Mathf.Log10(densityCount));

            int perSide = Mathf.RoundToInt(Mathf.Sqrt(densityCount));

            int iDens = 0;
            float d = cellWidth / perSide;
            float d2 = d / 2;
            for (int iX = 0; iX < perSide; ++iX)
            {
                for (int iZ = 0; iZ < perSide; ++iZ)
                {
                    float x = cellX + iX * d + d2 + (UnityEngine.Random.value - 0.5f) * d2;
                    float z = cellZ + iZ * d + d2 + (UnityEngine.Random.value - 0.5f) * d2;
                    Vector3 position = new Vector3(x, 0, z);

                    float ht = (1 - UnityEngine.Random.value * 0.1f) * height;

                    float angleY = orientationSampler.Sample();
                    float angleX = elevationSampler.Sample();

                    string namePart2 = $"{iDens.ToString().PadLeft(densityPad, '0')}";
                    string name = $"Triangle_{namePart}_{namePart2}";
                    GameObject obj = MakeTriangle(name, position, angleY, angleX, baseWidth, ht, limit, color, lit, parent);
                    _triangleCount += 1;
                    ++iDens;
                }
            }
        }

        private static GameObject MakeTriangle(string name, Vector3 position, float angleY, float angleX, float baseWidth, float height, float limit, Color color, bool lit, GameObject parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            obj.transform.eulerAngles = new Vector3(0, angleY, 0);
            obj.transform.parent = parent.transform;

            float x0 = 0;
            float x1 = height / Mathf.Tan(angleX * Mathf.Deg2Rad);
            float y = height;
            if (limit > 0)
            {
                x1 = Mathf.Min(x1, limit);
                y = x1 * Mathf.Tan(angleX * Mathf.Deg2Rad);
            }

            float z  = baseWidth / 2;
            Vector3[] vertices = new Vector3[] {
                new Vector3(x0, 0,  z),
                new Vector3(x1, y,  0),
                new Vector3(x0, 0, -z),    
                new Vector3(x0, 0, -z),
                new Vector3(x1, y,  0),
                new Vector3(x0, 0,  z)
            };
            int[] triangleVertices = new int[] {0, 1, 2, 3, 4, 5};

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            // Setting .triangles automatically computes the bounds.
            mesh.triangles = triangleVertices;
            mesh.RecalculateNormals();

            MeshFilter filter = obj.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

            AddMaterial(obj, color, lit);

            return obj;
        }

        private static void SetupTeleporting(MappedGrassSpec spec, GameObject subject, float groundWidth)
        {
            if (spec.teleportAtEdge)
            {
                GameObject obj = new GameObject("Teleporter");
                DistanceTeleporter teleporter = subject.AddComponent<DistanceTeleporter>();
                teleporter.distanceFrom = obj;
                teleporter.thresholdDistance = groundWidth / spec.groundSizeMultiplier / 2;
                teleporter.teleportTo = obj;
            }
        }

        private static void AddMaterial(GameObject obj, string colorStr, bool lit)
        {
            Color color = Color.red;
            ColorUtility.TryParseHtmlString(colorStr, out color);
            AddMaterial(obj, color, lit);
        }

        private static void AddMaterial(GameObject obj, Color color, bool lit)
        {
            Shader shader = lit ? Shader.Find("Standard") : Shader.Find("Unlit/Color");
            Material mat = new Material(shader);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Glossiness", 0);
            mat.SetFloat("_SpecularHighlights", 0);
            mat.SetFloat("_GlossyReflections", 0);
            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            mr.material = mat;
        }

        private static void DeleteObject(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private class WeightedSampler
        {
            // The input is a CSV file where each line is `value, probability`.
            public WeightedSampler(string path)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"WeightedSampler: file not found: {path}");
                }

                string[] lines;
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        lines = sr.ReadToEnd().Split(new char[]{'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                _values = new float[lines.Length];
                _cumulative = new float[lines.Length];

                float total = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split(',');
                    _values[i] = float.Parse(parts[0].Trim());
                    total += float.Parse(parts[1].Trim());
                    _cumulative[i] = total;
                }

                // Normalize cumulative weights to [0, 1].
                for (int i = 0; i < _cumulative.Length; i++)
                {
                    _cumulative[i] /= total;
                }
            }

            public float Sample()
            {
                float r = UnityEngine.Random.value;
                int i = Array.BinarySearch(_cumulative, r);
                // If `r` is not present (i.e., it is between present values) then the return value
                // is negative, to be interpreted as the bitwise complement of the next-larger index.
                if (i < 0) 
                {
                    i = ~i;
                }
                return _values[Mathf.Min(i, _values.Length - 1)];
            }

            private float[] _values;
            private float[] _cumulative;
        }

        private static int _triangleCount = 0;
    }
}

#endif