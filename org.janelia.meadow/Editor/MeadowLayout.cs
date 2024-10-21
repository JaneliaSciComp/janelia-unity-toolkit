using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR

namespace Janelia
{
    public class MeadowLayout : EditorWindow
    {
        [MenuItem("Window/Layout Meadow")]
        public static void ShowWindow()
        {
            GetWindow(typeof(MeadowLayout));
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space();
            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_JSON_PATH))
            {
                _jsonPath = EditorPrefs.GetString(EDITOR_PREF_KEY_JSON_PATH);
            }
            _jsonPath = EditorGUILayout.TextField("JSON spec file", _jsonPath);
            if (GUILayout.Button("Choose JSON spec file"))
            {
                _jsonPath = EditorUtility.OpenFilePanel("JSON spec file", ".", "json");
            }
            EditorPrefs.SetString(EDITOR_PREF_KEY_JSON_PATH, _jsonPath);
            EditorGUILayout.Space();

            _importPkgs = EditorGUILayout.Toggle("Import packages", _importPkgs);

            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_SEED))
            {
                _seed = EditorPrefs.GetInt(EDITOR_PREF_KEY_SEED);
            }
            _seed = EditorGUILayout.IntField("Random seed (0 = don't)", _seed);
            EditorPrefs.SetInt(EDITOR_PREF_KEY_SEED, _seed);

            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_LOD_BIAS))
            {
                _lodBias = EditorPrefs.GetFloat(EDITOR_PREF_KEY_LOD_BIAS);
            }
            _lodBias = EditorGUILayout.FloatField("LOD bias (more quality)", _lodBias);
            EditorPrefs.SetFloat(EDITOR_PREF_KEY_LOD_BIAS, _lodBias);

            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_CREATE_GROUND))
            {
                _createGround = EditorPrefs.GetBool(EDITOR_PREF_KEY_CREATE_GROUND);
            }
            _createGround = EditorGUILayout.Toggle("Create ground plane", _createGround);
            EditorPrefs.SetBool(EDITOR_PREF_KEY_CREATE_GROUND, _createGround);

            if (GUILayout.Button("Create instances"))
            {
                CreateInstances();
            }

            if (GUILayout.Button("Delete instances"))
            {
                DeleteInstances();
            }

            EditorGUILayout.EndVertical();
        }

        private void DeleteInstances()
        {
            Delete(CLUTTER_NAME);
            if (_createdGround)
            {
                AssetDatabase.DeleteAsset("Assets/Materials/" + GROUND_NAME + ".mat");
                Delete(GROUND_NAME);
            }
        }

        private static void Delete(string name)
        {
            GameObject o = GameObject.Find(name);
            if (o != null)
            {
                DestroyImmediate(o);
            }
        }

        private void CreateInstances()
        {
            DeleteInstances();
            LoadJson();
            if (_importPkgs)
            {
                ImportAllPackages();
            }
            else
            {
                AfterImporting();
            }
        }

        private void LoadJson()
        {
            // Filter out comment lines starting with "//" or "#".
            string[] jsonLines = File.ReadAllLines(_jsonPath)
                .Where(l => !l.Trim().StartsWith("//") && !l.Trim().StartsWith("#"))
                .ToArray();
            string json = String.Join(" ", jsonLines);

            _spec = new Spec();
            JsonUtility.FromJsonOverwrite(json, _spec);

            _spec.clutterJitterFraction = Mathf.Clamp(_spec.clutterJitterFraction, 0.0f, 0.45f);

            foreach (SpecItem item in _spec.clutterItems)
            {
                foreach (SpecLod lod in item.lods)
                {
                    if (lod.model == null)
                    {
                        lod.model = Path.GetFileNameWithoutExtension(lod.pkgFilePath);
                    }
                }
            }
        }

        private void ImportAllPackages()
        {
            List<string> pkgPathsToImport = GetAllPackagePaths();
            _importer.Import(pkgPathsToImport, (bool success, string error) =>
            {
                if (success)
                {
                    AfterImporting();
                }
            });
        }

        private List<string> GetAllPackagePaths()
        {
            return GetPackagePaths(_spec.clutterItems).ToList();
        }

        private HashSet<string> GetPackagePaths(List<SpecItem> items)
        {
            // Paths to package files are treated as relative to the JSON spec. file path.
            string jsonDir = Path.GetDirectoryName(_jsonPath);

            HashSet<string> result = new HashSet<string>();
            foreach (SpecItem item in items)
            {
                foreach (SpecLod lod in item.lods)
                {
                    string pkgFilePath = Path.Combine(jsonDir, lod.pkgFilePath);
                    result.Add(pkgFilePath);
                }
            }
            return result;
        }

        private void AfterImporting()
        {
            if (_seed != 0)
            {
                UnityEngine.Random.InitState(_seed);
            }

            LoadMasters();

            _clutter = new GameObject(); ;
            _clutter.name = CLUTTER_NAME;
            PlaceClutter();

            CreateGround();
            SetupLighting();

            QualitySettings.lodBias = _lodBias;
        }

        private void LoadMasters()
        {
            _clutterMasters = new List<Master>();

            float probabilityMin = 0;
            foreach (SpecItem item in _spec.clutterItems)
            {
                Master master = LoadMaster(item, ref probabilityMin);
                if (master != null)
                {
                    _clutterMasters.Add(master);
                }
            }

            Debug.Log("Loaded " + _clutterMasters.Count + " clutter master" + (_clutterMasters.Count > 1 ? "s" : ""));
        }

        private Master LoadMaster(SpecItem item, ref float probabilityMin)
        {
            List<Lod> lods = new List<Lod>();
            foreach (SpecLod specLod in item.lods)
            {
                string key = specLod.model + " t:gameobject";
                string[] where = new string[] { "Assets/Models" };
                string[] guids = AssetDatabase.FindAssets(key, where);
                guids = OnlyExactMatches(guids, specLod.model);
                if (guids.Length == 1)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject model = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                    lods.Add(new Lod(model, specLod.minScreenHeight));
                    Debug.Log("Loaded model '" + specLod.model + "' from unitypackage '" + specLod.pkgFilePath + "'");
                }
                else
                {
                    Debug.LogError("Cannot find model '" + specLod.model + "' in unitypackage '" + specLod.pkgFilePath + "'");
                }
            }
            float probabilityMax = Mathf.Clamp01(probabilityMin + Mathf.Clamp01(item.probability));
            Master master = new Master(lods, probabilityMin, probabilityMax);
            Debug.Log("Loaded master, probability range [" + probabilityMin + ", " + probabilityMax + ")");
            probabilityMin = probabilityMax;
            return master;
        }

        private string[] OnlyExactMatches(string[] guids, string modelName)
        {
            return guids.Where(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == modelName).ToArray();
        }

        private void PlaceClutter()
        {
            float dX = _spec.xWidth10cm / _spec.xNumClutterCells;
            float dZ = _spec.zWidth10cm / _spec.zNumClutterCells;
            float x = -_spec.xWidth10cm / 2 + dX / 2;
            float y = 0;
            int i = 0;
            for (int iX = 0; iX < _spec.xNumClutterCells; iX++)
            {
                float z = -_spec.zWidth10cm / 2 + dZ / 2;
                for (int iZ = 0; iZ < _spec.zNumClutterCells; iZ++)
                {
                    if ((Mathf.Abs(x) > _spec.xWidthClear10cm / 2) || (Mathf.Abs(z) > _spec.zWidthClear10cm / 2))
                    {
                        float jitterX = UnityEngine.Random.Range(-dX * _spec.clutterJitterFraction, dX * _spec.clutterJitterFraction);
                        float jitterZ = UnityEngine.Random.Range(-dZ * _spec.clutterJitterFraction, dZ * _spec.clutterJitterFraction);
                        Vector3 pos = new Vector3(x + jitterX, y, z + jitterZ);
                        float rotZ = UnityEngine.Random.Range(0, 360);
                        Vector3 euler = new Vector3(0, rotZ, 0);
                        float t = UnityEngine.Random.Range(0.0f, 1.0f);
                        foreach (Master master in _clutterMasters)
                        {
                            if (master.Choose(t))
                            {
                                InstantiateMaster(master, "_" + i++, pos, euler, _clutter);
                                break;
                            }
                        }
                    }
                    z += dZ;
                }
                x += dX;
            }
        }

        private void InstantiateMaster(Master master, String suffix, Vector3 pos, Vector3 euler, GameObject parent)
        {
            GameObject obj = new GameObject();
            String name = null;

            LODGroup lodGroup = obj.AddComponent<LODGroup>();
            LOD[] lods = new LOD[master.lods.Count];
            for (int i = 0; i < master.lods.Count; ++i)
            {
                Lod lod = master.lods[i];
                // TODO: Decide if it is better to use InstantiatePrefab() instead of Instantiate().
                GameObject lodObj = Instantiate(lod.model) as GameObject;
                lodObj.transform.parent = obj.transform;
                Renderer[] renderers = lodObj.GetComponentsInChildren<Renderer>();
                lods[i] = new LOD(lod.minScreenHeight, renderers);
                name = (name == null) ? lod.model.name + suffix: name;
            }
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            obj.name = name;
            obj.transform.position = pos;
            obj.transform.eulerAngles = euler;
            obj.transform.SetParent(parent.transform);
        }

        private void CreateGround()
        {
            if (_createGround)
            {
                _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                _ground.name = GROUND_NAME;
                float scale = 0.2f;
                _ground.transform.localScale = new Vector3(_spec.xWidth10cm * scale, 1, _spec.zWidth10cm * scale);

                if (_spec.groundTextureFilePath.Length > 0)
                {
                    string jsonDir = Path.GetDirectoryName(_jsonPath);
                    string textureFilePath = Path.Combine(jsonDir, _spec.groundTextureFilePath);
                    MaterialUtils.AddStandardTextured(_ground, textureFilePath, _spec.groundColor, _spec.groundGlossiness);
                }
                else
                {
                    MaterialUtils.AddStandard(_ground, _spec.groundColor, _spec.groundGlossiness);
                }

                // Keep track of whether this script created the ground, to know whether to delete it.
                _createdGround = true;
            }
            else
            {
                _createdGround = false;
            }
        }

        private void SetupLighting()
        {
            GameObject lightObj = GameObject.Find("Directional Light");
            Light light = lightObj.GetComponent<Light>();
            // The position of a directional light does not matter, but the origin is a convenient location.
            light.transform.position = Vector3.zero;
            light.shadows = LightShadows.Soft;
            // Eliminates Mach banding.
            light.shadowResolution = LightShadowResolution.VeryHigh;

            RenderSettings.ambientMode = AmbientMode.Flat;

            float r = 0.211f, g = 0.227f, b = 0.258f;
            if (_spec.ambientColorHDR.Length == 3)
            {
                r = _spec.ambientColorHDR[0];
                g = _spec.ambientColorHDR[1];
                b = _spec.ambientColorHDR[2];
            }
#if !UNITY_COLORSPACE_GAMMA
            r = Mathf.LinearToGammaSpace(r);
            g = Mathf.LinearToGammaSpace(g);
            b = Mathf.LinearToGammaSpace(b);
#endif
            float scale = Mathf.Pow(2, _spec.ambientIntensity);
            r *= scale;
            g *= scale;
            b *= scale;
#if !UNITY_COLORSPACE_GAMMA
            r = Mathf.GammaToLinearSpace(r);
            g = Mathf.GammaToLinearSpace(g);
            b = Mathf.GammaToLinearSpace(b);
#endif
            RenderSettings.ambientLight = new Color(r, g, b);
        }

        private class Lod
        {
            public GameObject model;
            public float minScreenHeight;
            public Lod(GameObject m, float h)
            {
                model = m;
                minScreenHeight = h;
            }
        }

        private class Master
        {
            public List<Lod> lods;
            public Vector2 probabilityRange;

            public Master(List<Lod> lod, float probMin, float probMax)
            {
                lods = lod;
                probabilityRange = new Vector2(probMin, probMax);
            }

            public bool Choose(float t)
            {
                return ((probabilityRange.x <= t) && (t < probabilityRange.y));
            }
        }

        [Serializable]
        private class SpecLod
        {
            public string pkgFilePath;
            public string model;
            public float minScreenHeight;
        }

        [Serializable]
        private class SpecItem
        {
            public List<SpecLod> lods = new List<SpecLod>();
            public float probability;
        }

        [Serializable]
        private class Spec
        {
            public float xWidth10cm;
            public float zWidth10cm;
            public float xWidthClear10cm = 0;
            public float zWidthClear10cm = 0;
            public int xNumClutterCells;
            public int zNumClutterCells;
            public float clutterJitterFraction = 0.3f;

            public List<SpecItem> clutterItems = new List<SpecItem>();

            public string groundColor = "#2E2516";
            public string groundTextureFilePath = "";
            public float groundGlossiness = 0.1f;

            // An array, instead of a CSS color string, to allow components to be greater than one.
            public float[] ambientColorHDR = { 0.211f, 0.227f, 0.258f };
            public float ambientIntensity = 0.8f;
        }

        private bool _importPkgs = true;

        private int _seed = 0;
        private const string EDITOR_PREF_KEY_SEED = "MeadowLayoutSeed";

        private float _lodBias = 1;
        private const string EDITOR_PREF_KEY_LOD_BIAS = "MeadowLayoutLodBias";

        private string _jsonPath;
        private const string EDITOR_PREF_KEY_JSON_PATH = "MeadowLayoutJsonPath";

        private Spec _spec;

        private AssetPackageImporter _importer = new AssetPackageImporter();

        private List<Master> _clutterMasters;

        private const string CLUTTER_NAME = "Clutter";
        private GameObject _clutter;

        private bool _createGround = true;
        private const string EDITOR_PREF_KEY_CREATE_GROUND = "MeadowLayoutCreateGround";
        private bool _createdGround = false;

        private const string GROUND_NAME = "Ground";
        private GameObject _ground;
    }
}
#endif
