using System;
using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace Janelia
{
    // TODO: Much of the code in this class is duplicated in `MazeLayout.cs` from
    // `org.janelia.radial-arm-maze`, so it should be factored out and shared.

    public static class MaterialUtils
    {
        public static void AddStandard(GameObject obj, string colorStr, float glossiness = 0.1f)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            Material mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, "Assets/Materials/" + obj.name + ".mat");

            Color color;
            ColorUtility.TryParseHtmlString(colorStr, out color);
            mat.SetColor("_Color", color);

            mat.SetFloat("_Glossiness", glossiness);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            mr.material = mat;
        }

        public static void AddStandardTextured(GameObject obj, string texturePath, string colorStr = "#ffffff", float glossiness = 0.1f)
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

            mat.SetFloat("_Glossiness", glossiness);

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
                if (! File.Exists(texturePath))
                {
                    Debug.Log("Cannot find texture file '" + texturePath + "'");
                    return;
                }

                // Then, delete any existing texture asset of the same name.
                string textureFile = Path.GetFileName(texturePath);
                path = "Assets/Textures/" + textureFile;
                AssetDatabase.DeleteAsset(path);

                // Next, copy the texture file into the "Textures" subdirectory.
                string texturePathDst = Path.Combine(texturesDir, textureFile);
                try
                {
                    File.Copy(texturePath, texturePathDst);
                }
                catch (System.Exception)
                {
                    Debug.Log("Cannot copy texture '" + texturePath + "' to Assets/Textures");
                    return;
                }

                // Finally, update the asset database, which seems to be necessary after the `File.Copy`.
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

        private static void EnsureDirectory(string path)
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

    }
}

#endif
