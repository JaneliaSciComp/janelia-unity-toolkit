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
                LoadTextures();
                if (_material != null)
                {
                    _material.SetTexture("_MainTex", _separatorTexture);
                }
            }

            public void Start()
            {
                StartCoroutine(ChangeBackground());
            }

            private void LoadTextures()
            {
                string jsonDir = Path.GetDirectoryName(_specFilePath);
                _material = Resources.Load(CylinderBackgroundResources.MaterialName, typeof(Material)) as Material;
                if (_material != null)
                {
                    foreach (string texturePath in _spec.textures)
                    {
                        string texturePathFull = Path.Combine(jsonDir, texturePath);
                        Texture2D texture = LoadTexture(texturePathFull);
                        if (texture != null)
                        {
                            _textures.Add(texture);
                            _texturePaths.Add(texturePathFull);
                        }
                        else
                        {
                            Debug.LogError("Could not create texture from file '" + texturePath +"'");
                            _textures.Add(SolidTexture(Color.red));
                            _texturePaths.Add("error");
                        }
                    }
                    _separatorTexture = SolidTexture(Color.black);
                    if (_spec.separatorTexture != null)
                    {
                        string separatorPathFull = Path.Combine(jsonDir, _spec.separatorTexture);
                        _separatorTexture = LoadTexture(separatorPathFull);
                    }
                    if (_separatorTexture == null)
                    {
                        Debug.LogError("Could not create separator texture from file '" + _separatorTexture +"'");
                    }
                }
                else
                {
                    Debug.LogError("Could not load material '" + CylinderBackgroundResources.MaterialName + "'");
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
                    Texture2D texture = new Texture2D(ToBeReplacedByLoadImage, ToBeReplacedByLoadImage, TextureFormat.RGBA32, MipMaps);
                    if (texture.LoadImage(bytes))
                    {
                        return texture;
                    }
                }
                return null;
            }

            private IEnumerator ChangeBackground()
            {
                if (_material != null)
                {
                    _material.SetTexture("_MainTex", _separatorTexture);
                }
                while (_current < _textures.Count)
                {
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
                            yield return new WaitForSeconds(_spec.separatorDurationSecs);
                        }

                        UseCurrentTexture();
                        yield return new WaitForSeconds(_spec.durationSecs);
                    }
                }

                UseSeparatorTexture();
                yield return new WaitForSeconds(_spec.separatorDurationSecs);
                
                Application.Quit();
            }
        }

        private static void UseSeparatorTexture()
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

        private static void UseCurrentTexture()
        {
            if (_material != null)
            {
                _material.SetTexture("_MainTex", _textures[_current]);
                
                ChangingTextureLog entry = new ChangingTextureLog
                {
                    backgroundTextureNowInUse = _texturePaths[_current],
                    durationSecs = _spec.durationSecs
                };
                Logger.Log(entry);

                _current++;
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