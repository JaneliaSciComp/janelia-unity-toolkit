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
    public class RadialArmLayout : EditorWindow
    {
        [MenuItem("Window/Layout Radial-Arm Maze")]
        public static void ShowWindow()
        {
            RadialArmLayout window = (RadialArmLayout)GetWindow(typeof(RadialArmLayout));
        }

        public RadialArmLayout()
        {
        }

        public void OnEnable()
        {
            _gameObjs = new List<GameObject>();
            foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if ((obj.name == TOP_LEVEL_NAME) || (obj.name == GROUND_NAME) ||
                    obj.name.StartsWith(WALL_NAME) || obj.name.StartsWith(LIMIT_TRANSLATION_TO_NAME))
                {
                    _gameObjs.Add(obj);
                }
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();

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

            if (GUILayout.Button("Create maze"))
            {
                CreateMaze();
            }

            if (GUILayout.Button("Delete maze"))
            {
                DeleteMaze();
            }

            EditorGUILayout.EndVertical();
        }

        private void DeleteMaze()
        {
            foreach (GameObject obj in _gameObjs)
            {
                try
                {
                    AssetDatabase.DeleteAsset("Assets/Materials/" + obj.name + ".mat");
                    DestroyImmediate(obj);
                }
                catch (Exception)
                {
                }
            }
        }

        private void CreateMaze()
        {
            DeleteMaze();
            LoadJson();
            CreateWalls();
            CreateGround();
            SetupLighting();
        }

        private void LoadJson()
        {
            string json = File.ReadAllText(_jsonPath);
            _spec = new Spec();
            JsonUtility.FromJsonOverwrite(json, _spec);

            _spec.arms.Sort((arm0, arm1) => (int)(arm0.angleDegs - arm1.angleDegs));

            // TODO: When `AdjustForUnequalWidths` is fully implemented, remove this code,
            // which forces all arm widths to the maximum width.
            float width = _spec.arms.Max(arm => arm.width);
            foreach (SpecArm arm in _spec.arms)
            {
                arm.width = width;
            }
        }

        private void CreateWalls()
        {
            _maze = new GameObject();
            _maze.name = TOP_LEVEL_NAME;
            _gameObjs.Add(_maze);

            _bbox = new Bounds();
            float maxWidth = 0;

            Vector3 Y = new Vector3(0, 1, 0);
            Vector3 Z = new Vector3(0, 0, 1);

            for (int i = 0; i < _spec.arms.Count; ++i)
            {;
                SpecArm arm0 = _spec.arms[i];
                Vector3 spineNorm0 = Matrix4x4.Rotate(Quaternion.Euler(0, arm0.angleDegs, 0)).MultiplyVector(Z);
                Vector3 spine0 = spineNorm0 * arm0.length;

                _bbox.Encapsulate(spine0);

                int j = (i + 1) % _spec.arms.Count;
                SpecArm arm1 = _spec.arms[j];
                Vector3 spineNorm1 = Matrix4x4.Rotate(Quaternion.Euler(0, arm1.angleDegs, 0)).MultiplyVector(Z);
                Vector3 spine1 = spineNorm1 * arm1.length;

                float halfAngleBetween = (arm1.angleDegs - arm0.angleDegs) / 2;
                if (arm1.angleDegs < arm0.angleDegs)
                    halfAngleBetween = (arm1.angleDegs + 360 - arm0.angleDegs) / 2;

                float width = Mathf.Max(arm0.width / 2, arm1.width / 2);
                maxWidth = Mathf.Max(width, maxWidth);

                Vector3 toInterNorm = Matrix4x4.Rotate(Quaternion.Euler(0, arm0.angleDegs + halfAngleBetween, 0)).MultiplyVector(Z);
                float toInterDist = width / Mathf.Sin(halfAngleBetween * Mathf.Deg2Rad);
                Vector3 inter = toInterNorm * toInterDist;

                inter = AdjustForUnequalWidths(inter, arm0, spineNorm0, arm1, spineNorm1);

                _bbox.Encapsulate(inter);

                Vector3 sidewaysNorm0 = Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)).MultiplyVector(spineNorm0);
                Vector3 sideways0 = sidewaysNorm0 * arm0.width / 2;
                Vector3 outer0 = spine0 + sideways0;

                Vector3 mid0 = (inter + outer0) / 2 + Y * _spec.height / 2;
                float lengthAfterInter0 = Vector3.Distance(inter, outer0);

                CreateWall(WALL_NAME + i + "_" + j, mid0, sidewaysNorm0, Y, lengthAfterInter0, arm0.color);

                Vector3 midEnd0 = spine0 + Y * _spec.height / 2;
                CreateWall(WALL_NAME + i, midEnd0, spineNorm0, Y, arm0.width, arm0.color, arm0.endTexture);

                Vector3 sidewaysNorm1 = Matrix4x4.Rotate(Quaternion.Euler(0, -90, 0)).MultiplyVector(spineNorm1);
                Vector3 sideways1 = sidewaysNorm1 * arm1.width / 2;
                Vector3 outer1 = spine1 + sideways1;

                Vector3 mid1 = (inter + outer1) / 2 + Y * _spec.height / 2;
                float lengthAfterInter1 = Vector3.Distance(inter, outer1);

                CreateWall(WALL_NAME + j + "_" + i, mid1, sidewaysNorm1, Y, lengthAfterInter1, arm1.color);

                CreateLimitTranslationTo(arm0, i, spineNorm0, mid0 - sideways0, lengthAfterInter0);
            }

            _bbox.Expand(maxWidth * 2f);
        }

        private Vector3 AdjustForUnequalWidths(Vector3 inter, SpecArm arm0, Vector3 spine0, SpecArm arm1, Vector3 spine1)
        {
            // TODO: Push `inter` in the direction of the spine of the arm with the smaller width.
            return inter;
        }

        private void CreateWall(string name, Vector3 mid, Vector3 forward, Vector3 upwards, float length, string color, string texture = "")
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _gameObjs.Add(wall);

            wall.transform.SetParent(_maze.transform);

            wall.name = name;
            wall.transform.localScale = new Vector3(length, _spec.height, _spec.thickness);
            wall.transform.position = mid;
            wall.transform.rotation = Quaternion.LookRotation(forward, upwards);

            // Each wall is what is known in Unity as a "static collider," a `GameObject` that has a `Collider`
            // but no `RigidBody`.  A static collider is not expected to move, but should still work with the
            // interesection functionality in `Physics.Raycast`.  Surprisingly, that intersection functionality
            // is not reliable if the static collider has a nonuniform `transform.localScale`, unless its
            // `BoxCollider` has its `size` value adjusted in particular ways (which are not necessary if
            // `transform.localScale` is a uniform scale).  For the case of a wall, it is most important that
            // the `size` have the proper `z` dimension, since that is where the wall is thin.  To make that
            // `z` dimension work correctly, it seems to be necessary for the `x` dimension to be 1 and for
            // the `y` dimension to be something other than 1.  Making the `y` dimension larger than 1 makes
            // the collider a bit taller than the wall, but that does not really matter.

            BoxCollider collider = wall.GetComponent<Collider>() as BoxCollider;
            collider.size = new Vector3(1, 1.1f, _spec.thickness);

            CreateMaterial(wall, color, texture);
        }

        private void CreateLimitTranslationTo(SpecArm arm, int iArm, Vector3 spineNorm, Vector3 mid, float lengthAfterInter)
        {
            switch (arm.limitTranslationTo.ToLower())
            {
                case "forward":
                    GameObject limiter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _gameObjs.Add(limiter);
                    limiter.name = LIMIT_TRANSLATION_TO_NAME + "Forward_" + iArm;
                    limiter.transform.position = mid;
                    limiter.transform.localScale = new Vector3(arm.width, _spec.height, lengthAfterInter);
                    limiter.transform.rotation = Quaternion.LookRotation(spineNorm, new Vector3(0, 1, 0));

                    // The limiter should be invisible, and should not trigger normal siding collisons.
                    // But it is useful to be able to disable it in the editor with the "active" checkbox.
                    // So hide it through the renderer.
                    MeshRenderer renderer = limiter.GetComponent<MeshRenderer>() as MeshRenderer;
                    renderer.enabled = false;

                    // And disable normal sliding collisions through the collider.
                    BoxCollider collider = limiter.GetComponent<BoxCollider>() as BoxCollider;
                    collider.enabled = false;

                    break;
                default:
                    break;
            }
        }

        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _gameObjs.Add(ground);
            ground.name = GROUND_NAME;
            ground.transform.position = _bbox.center - new Vector3(0, _spec.thickness / 2, 0);
            ground.transform.localScale = new Vector3(_bbox.size.x, _spec.thickness, _bbox.size.z);

            string color = "#ffffff";
            string texturePath = "";
            if (_spec.groundColorOrTexture.StartsWith("#"))
            {
                color = _spec.groundColorOrTexture;
            }
            else
            {
                texturePath = _spec.groundColorOrTexture;
            }
            CreateMaterial(ground, color, texturePath);
        }

        private void CreateMaterial(GameObject obj, string colorStr, string texturePath = "")
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            Material mat = new Material(Shader.Find("Standard"));
            string path = "Assets/Materials/" + obj.name + ".mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);

            Color color;
            ColorUtility.TryParseHtmlString(colorStr, out color);
            mat.SetColor("_Color", color);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            mr.material = mat;

            if (texturePath.Length > 0)
            {
                // Copy the texture into the "Textures" directory under "Assets.
                // First, make sure that directory exsists.
                string projectDir = System.IO.Directory.GetCurrentDirectory();
                string texturesDir = Path.Combine(projectDir, "Assets", "Textures");
                EnsureDirectory(texturesDir);

                // Next, make sure the texture file to be copied can be found.
                string jsonDir = Path.GetDirectoryName(_jsonPath);
                string texturePathSrc = Path.Combine(jsonDir, texturePath);
                if (! File.Exists(texturePathSrc))
                {
                    Debug.Log("Cannot find texture file '" + texturePathSrc + "'");
                    return;
                }

                // Then delete any existing texture asset of the same name.
                string textureFile = Path.GetFileName(texturePath);
                path = "Assets/Textures/" + textureFile;
                AssetDatabase.DeleteAsset(path);

                // Next copy the texture file into the "Textures" subdirectory.
                string texturePathDst = Path.Combine(texturesDir, textureFile);
                try
                {
                    File.Copy(texturePathSrc, texturePathDst);
                }
                catch (System.Exception)
                {
                    Debug.Log("Cannot copy texture '" + texturePath + "' to Assets/Textures");
                    return;
                }

                // Finally update the asset database, which seems to be necessary after the `File.Copy`.
                AssetDatabase.Refresh();

                Texture2D texture = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
                if (texture != null)
                {
                    mat.SetTexture("_MainTex", texture);

                    // Flip the texture vertically.
                    mat.SetTextureScale("_MainTex", new Vector2(1, -1));
                }
                else
                {
                    Debug.Log("Cannot load texture '" + path + "'");
                }
            }
        }

        private void SetupLighting()
        {
            GameObject lightObj = GameObject.Find("Directional Light");
            Light light = lightObj.GetComponent<Light>();

            // The position of a directional light does not matter, but the origin is a convenient location.
            light.transform.position = Vector3.zero;
            light.shadows = LightShadows.Soft;

            // Reduces Mach banding.
            light.shadowResolution = LightShadowResolution.VeryHigh;
        }

        private void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception e)
                {
                    Debug.Log("Cannot create " + path + ": " + e.ToString());
                }
            }
        }

        [Serializable]
        private class SpecArm
        {
            public float angleDegs;
            public float length;
            public float width;
            public string color = "#ffffff";
            public string endTexture = "";
            public string limitTranslationTo = "";
        }

        [Serializable]
        private class Spec
        {
            public float height;
            public float thickness = 0.3f;
            public List<SpecArm> arms = new List<SpecArm>();
            public string groundColorOrTexture = "#66583D";
        }

        private string _jsonPath;
        private const string EDITOR_PREF_KEY_JSON_PATH = "RadialArmLayoutJsonPath";

        private Spec _spec;

        private Bounds _bbox;
        private GameObject _maze;
        private List<GameObject> _gameObjs;

        private const string LIMIT_TRANSLATION_TO_NAME = "LimitTranslationTo";

        private const string TOP_LEVEL_NAME = "Maze";
        private const string GROUND_NAME = "Ground";
        private GameObject _ground;

        private const string WALL_NAME = "Wall_";
    }
}

#endif