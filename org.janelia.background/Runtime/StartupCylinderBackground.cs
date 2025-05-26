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

            // Especially for tests run from the command line, it is useful to be able to use a commandline argument
            // to override the parameter that indictes the JSON spec file.
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-backgroundCylinderTexture")
                {
                    if (i + 1 < args.Length)
                    {
                        path = args[i + 1];
                    }
                }
            }

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
                Debug.Log("Did not load empty backgroundCylinderTexture");
            }

            string path2 = Janelia.SessionParameters.GetStringParameter("backgroundCylinderTexture2");
            if (path2.Length != 0)
            {
                UseTextureFile(path2, 1);
            }
            else
            {
                Debug.Log("Did not load empty backgroundCylinderTexture2");
            }
        }

        private static void UseTextureFile(string texturePath, int which = 0)
        {
            Debug.Log("Using background cylinder texture " + which + " file '" + texturePath + "'");

            byte[] bytes = File.ReadAllBytes(texturePath);
            const int ToBeReplacedByLoadImage = 2;
            const bool MipMaps = false;
            Texture2D texture = new Texture2D(ToBeReplacedByLoadImage, ToBeReplacedByLoadImage, TextureFormat.RGBA32, MipMaps);
            texture.filterMode = FilterMode.Bilinear;
            if (texture.LoadImage(bytes))
            {
                Material material = Resources.Load(CylinderBackgroundResources.MaterialName, typeof(Material)) as Material;
                if (material)
                {
                    switch (which)
                    {
                        case 0:
                            material.SetTexture("_MainTex", texture);
                            break;
                        case 1:
                            material.SetTexture("_SecondTex", texture);
                            break;
                        default:
                            Debug.LogError("UseTextureFile, which = " + which + " not supported");
                            break;
                    }
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
            catch (System.Exception ex)
            {
                Debug.Log($"Cannot process JSON spec \"{jsonPath}\": {ex.Message}");
                return null;
            }
        }
    }
}
