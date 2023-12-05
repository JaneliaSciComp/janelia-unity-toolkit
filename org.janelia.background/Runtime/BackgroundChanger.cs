using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Janelia
{
    // A class to manage the progression through a sequence of background cylinder textures.
    // The class is static, with a `RuntimeInitializeOnLoadMethod` function that triggers
    // the setup processing automatically, without the need for adding a new `MonoBehaviour`
    // in the scene.  The changing of the textures relies on a coroutine, which does require
    // a `MonoBehaviour`, so the static class creates one itself.

    public static class BackgroundChanger
    {
        // The format of the JSON specification file.
        [Serializable]
        public class Spec
        {
            public List<string> textures = new List<string>();
            public float durationSecs = 1;
            public string separatorTexture;
            public float separatorDurationSecs;
        }

        public static void Initialize(Spec spec, string specFilePath)
        {
            Debug.Log("BackgroundChanger using spec file " + specFilePath);
            _spec = spec;
            _specFilePath = specFilePath;
            _object = new GameObject("BackgroundChanger");
            _object.hideFlags = HideFlags.HideAndDontSave;
            _object.AddComponent<BackgroundChangerInternal>();
        }

        private class BackgroundChangerInternal : MonoBehaviour
        {
            public void Awake()
            {
                LoadTexturePaths();
                LoadSeparatorTexture();
                _material = Resources.Load(CylinderBackgroundResources.MaterialName, typeof(Material)) as Material;
                if (_material != null)
                {
                    _material.SetTexture("_MainTex", _separatorTexture);
                }
            }

            public void Start()
            {
                StartCoroutine(ChangeBackground());
            }

            private void LoadTexturePaths()
            {
                string jsonDir = Path.GetDirectoryName(_specFilePath);
                foreach (string texturePath in _spec.textures)
                {
                    string pathFull = Path.Combine(jsonDir, texturePath);
                    if (Directory.Exists(pathFull))
                    {
                        string[] supported = new string[] {".bmp", ".gif", ".jpg", ".jpeg", ".png", ".psd", ".tga", ".tif", ".tiff"};
                        string[] files = Directory.GetFiles(pathFull);
                        Array.Sort(files);
                        foreach (string file in files)
                        {
                            string ext = Path.GetExtension(file).ToLower();
                            if (supported.Contains(ext))
                            {
                                string texturePathFull = Path.Combine(pathFull, file);
                                _texturePaths.Add(texturePathFull);
                            }
                        }
                    }
                    else
                    {
                        _texturePaths.Add(pathFull);
                    }
                }
            }

            private void LoadSeparatorTexture()
            {
                _separatorTexture = SolidTexture(Color.black);
                if (_spec.separatorTexture != null)
                {
                    string jsonDir = Path.GetDirectoryName(_specFilePath);
                    string separatorPathFull = Path.Combine(jsonDir, _spec.separatorTexture);
                    _separatorTexture = LoadTexture(separatorPathFull);
                }
                if (_separatorTexture == null)
                {
                    Debug.LogError("BackgroundChanger: cannot create separator texture from file '" + _separatorTexture +"'");
                    _separatorTexture = SolidTexture(Color.red);
                }
            }

            private Texture2D SolidTexture(Color color)
            {
                int size = 200;
                Texture2D texture = new Texture2D(size, size);
                Color[] pixels = Enumerable.Repeat(color, size * size).ToArray();
                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            }

            // TODO: Move to a utilities file so this code can be shared with StartupCylinderBackground.cs.
            private Texture2D LoadTexture(string texturePath)
            {
                if (File.Exists(texturePath))
                {
                    byte[] bytes = File.ReadAllBytes(texturePath);
                    const int ToBeReplacedByLoadImage = 2;
                    const bool MipMaps = false;
                    // Create a new Texture2D each time, in case the images have different sizes.
                    Texture2D texture = new Texture2D(ToBeReplacedByLoadImage, ToBeReplacedByLoadImage, TextureFormat.RGBA32, MipMaps);
                    if (texture.LoadImage(bytes))
                    {
                        return texture;
                    }
                }
                Debug.Log("BackgroundChanger: cannot find texture file " + texturePath);
                return SolidTexture(Color.red);
            }

            private IEnumerator ChangeBackground()
            {
                UseSeparatorTexture();
                while (_current < _texturePaths.Count)
                {
                    long t0 = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (!_splashIsFinished)
                    {
                        yield return new WaitForSeconds(Time.deltaTime);
                        _splashIsFinished = SplashScreen.isFinished;
                    }
                    else
                    {
                        if (_spec.separatorDurationSecs > 0)
                        {
                            UseSeparatorTexture();

                            long t1A = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            float elapsedSecsA = (t1A - t0) / 1000.0f;
                            float waitA = Mathf.Max(_spec.separatorDurationSecs - elapsedSecsA, 0);

                            yield return new WaitForSeconds(waitA);
                        }

                        UseCurrentTexture();

                        long t1B = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        float elapsedSecsB = (t1B - t0) / 1000.0f;
                        float waitB = Mathf.Max(_spec.durationSecs - elapsedSecsB, 0);

                        yield return new WaitForSeconds(waitB);
                    }
                }

                UseSeparatorTexture();
                yield return new WaitForSeconds(_spec.separatorDurationSecs);

                Application.Quit();
            }

            private void UseSeparatorTexture()
            {
                if (_material != null)
                {
                    _material.SetTexture("_MainTex", _separatorTexture);

                    ChangingToSeparatorTextureLog entry = new ChangingToSeparatorTextureLog
                    {
                        separatorTextureDurationSecs = _spec.separatorDurationSecs
                    };
                    Logger.Log(entry);
                }
            }

            private void UseCurrentTexture()
            {
                if (_material != null)
                {
                    Texture2D texture = LoadTexture(_texturePaths[_current]);
                    _material.SetTexture("_MainTex", texture);

                    ChangingTextureLog entry = new ChangingTextureLog
                    {
                        backgroundTextureNowInUse = _texturePaths[_current],
                        durationSecs = _spec.durationSecs
                    };
                    Logger.Log(entry);

                    _current++;
                }
            }
        }

        private static Spec _spec;
        private static string _specFilePath;
        private static Material _material;
        private static List<Texture2D> _textures = new List<Texture2D>();
        private static List<string> _texturePaths = new List<string>();
        private static Texture2D _separatorTexture;
        private static GameObject _object;
        private static bool _splashIsFinished = false;
        private static int _current = 0;

        [Serializable]
        private class ChangingTextureLog : Logger.Entry
        {
            public string backgroundTextureNowInUse;
            public float durationSecs;
        };

        [Serializable]
        private class ChangingToSeparatorTextureLog : Logger.Entry
        {
            public float separatorTextureDurationSecs;
        };

    }
}