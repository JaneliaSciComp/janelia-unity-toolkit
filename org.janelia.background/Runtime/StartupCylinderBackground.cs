using System.IO;
using UnityEngine;

namespace Janelia
{
    public class StartupCylinderBackground
    {
        // Executed after `Awake` methods and before `Start` methods.
        [RuntimeInitializeOnLoadMethod]
        private static void OnRuntimeMethodLoad()
        {
            string path = Janelia.SessionParameters.GetStringParameter("backgroundCylinderTexture");
            if (path.Length != 0)
            {
                if (Path.GetExtension(path) == ".json")
                {
                    UseJsonFile(path);
                }
                else
                {
                    UseTextureFile(path);
                }
            }
            else
            {
                Debug.LogError("Could not load cylinder background texture from file '" + path + "'");
            }
        }

        private static void UseTextureFile(string texturePath)
        {
            Debug.Log("Using background cylinder texture file '" + texturePath + "'");

            byte[] bytes = File.ReadAllBytes(texturePath);
            const int ToBeReplacedByLoadImage = 2;
            const bool MipMaps = false;
            Texture2D texture = new Texture2D(ToBeReplacedByLoadImage, ToBeReplacedByLoadImage, TextureFormat.RGBA32, MipMaps);
            if (texture.LoadImage(bytes))
            {
                Material material = Resources.Load(CylinderBackgroundResources.MaterialName, typeof(Material)) as Material;
                if (material)
                {
                    material.SetTexture("_MainTex", texture);
                }
                else
                {
                    Debug.LogError("Could not load material'" + CylinderBackgroundResources.MaterialName + "'");
                }
            }
        }

        private static void UseJsonFile(string jsonPath)
        {
            BackgroundChanger.Spec spec = GetSpec(jsonPath);
            if (spec != null)
            {
                BackgroundChanger.Initialize(spec, jsonPath);
            }
        }

        private static BackgroundChanger.Spec GetSpec(string jsonPath)
        {
            try
            {
                string json = File.ReadAllText(jsonPath);
                BackgroundChanger.Spec spec = new BackgroundChanger.Spec();
                JsonUtility.FromJsonOverwrite(json, spec);
                return spec;
            }
            catch (System.Exception)
            {
                Debug.Log("Cannot process JSON spec '" + jsonPath + "'");
                return null;
            }
        }
    }
}
