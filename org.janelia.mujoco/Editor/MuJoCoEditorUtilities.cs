using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Janelia
{
    public static class MuJoCoEditorUtilities
    {
        [MenuItem("Assets/Hide MuJoCo Collision && Inertial Bodies")]
        public static void HideMuJoCoBodies()
        {
            GameObject root = Selection.activeGameObject;
            if (root != null)
            {
                VisitMuJoCoBodies(root.transform, false);
            }
        }

        [MenuItem("Assets/Show MuJoCo Collision && Inertial Bodies")]
        public static void ShowMuJoCoBodies()
        {
            GameObject root = Selection.activeGameObject;
            if (root != null)
            {
                VisitMuJoCoBodies(root.transform, true);
            }
        }

        [MenuItem("Assets/Delete MuJoCo Collision && Inertial Bodies")]
        public static void DeleteMuJoCoBodies()
        {
            GameObject root = Selection.activeGameObject;
            if (root != null)
            {
                List<GameObject> toDelete = new List<GameObject>();
                FindMuJoCoBodiesToDelete(root.transform, toDelete);

                foreach (GameObject obj in toDelete)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        [MenuItem("Assets/Reduce MuJoCo Fly Shadow Casters")]
        public static void ReduceMuJoCoFlyShadowCasters()
        {
            GameObject root = Selection.activeGameObject;
            if (root != null)
            {
                string[] yesCast = new string[] {
                   // "thorax", "head", "membrane", "abdomen", "coxa", "femur", "tibia", "tarsus"
                   "membrane"
                };
                string[] noCast = new string[] {
                    "black", "ocelli", "lower"
                };
                List<GameObject> toDelete = new List<GameObject>();
                FindMuJoCoShadowCasters(root.transform, yesCast, noCast);
            }
        }

        private static void VisitMuJoCoBodies(Transform current, bool show)
        {
            string name = current.name.ToLower();
            bool collision = name.Contains("collision");
            bool inertial = name.Contains("inertial");
            bool fluid = name.Contains("fluid");
            if (collision || inertial || fluid)
            {
                MeshRenderer renderer = current.gameObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = show;
                }
            }
            for (int i = 0; i < current.childCount; ++i)
            {
                VisitMuJoCoBodies(current.GetChild(i), show);
            }
        }

        private static void FindMuJoCoBodiesToDelete(Transform current, List<GameObject> toDelete)
        {
            string name = current.name.ToLower();
            bool collision = name.Contains("collision");
            bool inertial = name.Contains("inertial");
            bool fluid = name.Contains("fluid");
            if (collision || inertial || fluid)
            {
                toDelete.Add(current.gameObject);
            }
            for (int i = 0; i < current.childCount; ++i)
            {
                FindMuJoCoBodiesToDelete(current.GetChild(i), toDelete);
            }
        }

        private static void FindMuJoCoShadowCasters(Transform current, string[] yesCast, string[] noCast)
        {
            MeshRenderer renderer = current.gameObject.GetComponent<MeshRenderer>();
            if ((renderer != null) && renderer.enabled)
            {
                string name = current.name.ToLower();
                UnityEngine.Rendering.ShadowCastingMode mode = UnityEngine.Rendering.ShadowCastingMode.Off;
                for (int i = 0; i < yesCast.Length; ++i)
                {
                    if (name.Contains(yesCast[i]))
                    {
                        mode = UnityEngine.Rendering.ShadowCastingMode.On;
                        for (int j = 0; j < noCast.Length; ++j)
                        {
                            if (name.Contains(noCast[j]))
                            {
                                mode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                break;
                            }
                        }
                        // HEY!!
                        if (mode == UnityEngine.Rendering.ShadowCastingMode.On)
                            Debug.Log(name + " casting shadows");

                        break;
                    }
                }
                renderer.shadowCastingMode = mode;
            }
            for (int i = 0; i < current.childCount; ++i)
            {
                FindMuJoCoShadowCasters(current.GetChild(i), yesCast, noCast);
            }
        }
    }
}