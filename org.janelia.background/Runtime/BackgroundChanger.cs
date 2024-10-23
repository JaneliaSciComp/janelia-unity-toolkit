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

                        // `Directory.GetFiles()` returns full paths.
                        string[] files = Directory.GetFiles(pathFull);

                        // Does not do a "human" sort.  So sorting `f1`, `f2`, `f10`, `f11`, `f100`, `f101` will return
                        // `f1`, `f10`, `f100`, `f101`, `f11`, `f2`, which probably is not desired.  The solution, for now,
                        // is to use zero-padded file names, `f001`, `f002`, `f010`, `f011`, `f100`, `f101`.
                        Array.Sort(files);

                        foreach (string file in files)
                        {
                            string ext = Path.GetExtension(file).ToLower();
                            if (supported.Contains(ext))
                            {
                                _texturePaths.Add(file);
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
                if ((_spec.separatorTexture != null) && (_spec.separatorTexture.Length > 0))
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
                    using (FileStream fs = new FileStream(texturePath, FileMode.Open, FileAccess.Read))
                    {
                        int length = (int)fs.Length;
                        if ((_textureBytes == null) || (length > _textureBytes.Length))
                        {
                            _textureBytes = new byte[length];
                        }
                        fs.Read(_textureBytes, 0, length);
                    }

                    if (_texture == null)
                    {
                        const int ToBeReplacedByLoadImage = 2;
                        const bool MipMaps = false;
                        _texture = new Texture2D(ToBeReplacedByLoadImage, ToBeReplacedByLoadImage, TextureFormat.RGBA32, MipMaps);
                    }
                    if (_texture.LoadImage(_textureBytes))
                    {
                        return _texture;
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

                    _currentChangingToSeparatorTextureLog.separatorTextureDurationSecs = _spec.separatorDurationSecs;
                    Logger.Log(_currentChangingToSeparatorTextureLog);
                }
            }

            private void UseCurrentTexture()
            {
                if (_material != null)
                {
                    Texture2D texture = LoadTexture(_texturePaths[_current]);
                    _material.SetTexture("_MainTex", texture);

                    _currentChangingTextureLog.backgroundTextureNowInUse = _texturePaths[_current];
                    _currentChangingTextureLog.durationSecs = _spec.durationSecs;
                    Logger.Log(_currentChangingTextureLog);

                    _current++;
                }
            }
        }

        private static Spec _spec;
        private static string _specFilePath;
        private static Material _material;
        private static List<string> _texturePaths = new List<string>();
        private static byte[] _textureBytes;
        private static Texture2D _texture;
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
        private static ChangingTextureLog _currentChangingTextureLog = new ChangingTextureLog();

        [Serializable]
        private class ChangingToSeparatorTextureLog : Logger.Entry
        {
            public float separatorTextureDurationSecs;
        };
        private static ChangingToSeparatorTextureLog _currentChangingToSeparatorTextureLog = new ChangingToSeparatorTextureLog();
    }
}
