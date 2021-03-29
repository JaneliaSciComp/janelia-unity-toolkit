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
            string textureFile = Janelia.SessionParameters.GetStringParameter("backgroundCylinderTexture");
            if (textureFile.Length != 0)
            {
                Debug.Log("Using background cylinder texture file '" + textureFile + "'");

                byte[] bytes = File.ReadAllBytes(textureFile);
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
                else
                {
                    Debug.LogError("Could not load cylinder background texture from file '" + textureFile + "'");
                }
            }
        }
    }
}
