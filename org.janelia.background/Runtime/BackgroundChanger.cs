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

            // When true, and when 1 / `durationSecs` matches the display refresh rate,
            // then all textures will be shown even if variations in Unity's frame rate
            // causes the overall elapsed time to excede the expected value.
            // Otherwise, occasional textures can be omitted (i.e., frames dropped) to
            // maintain the expected overall elapsed time.
            public bool complete = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void OnBeforeSplashScreen()
        {
            _timeSplashStartMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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

            public void Update()
            {
                if (Input.GetKey(KeyCode.Escape))
                {
                    Application.Quit();
                }

                if (!_splashIsFinished && SplashScreen.isFinished)
                {
                    _splashIsFinished = true;
                    _timeBackgroundsStartMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    _currentTextureIndex = 0;
                    UseTexture(_currentTextureIndex);
                    _currentTime = _textureDurationSecs[_currentTextureIndex];

                    _usingDisplayRate = UsingDisplayRate();

                    return;
                }

                int nextTextureIndex = _currentTextureIndex;
                double nextTime = _currentTime;

                if (_usingDisplayRate)
                {
                    // Show every background texture, no matter what the delay.
                    nextTextureIndex += 1;
                    nextTime = Time.realtimeSinceStartup;

                    if (nextTextureIndex >= _texturePaths.Count)
                    {
                        ReportTextureUsage();
                        Application.Quit();
                    }
                }
                else
                {
                    // Allow skipping over background textures to stay on the overall time schedule.
                    while (nextTime <= Time.timeAsDouble)
                    {
                        nextTime += _textureDurationSecs[nextTextureIndex];
                        nextTextureIndex += 1;

                        if (nextTextureIndex >= _texturePaths.Count)
                        {
                            ReportTextureUsage();
                            Application.Quit();
                        }
                    }
                }

                if (nextTextureIndex != _currentTextureIndex)
                {
                    _currentTextureIndex = nextTextureIndex;
                    _currentTime = nextTime;
                    UseTexture(_currentTextureIndex);
                }
            }

            private void LoadTexturePaths()
            {
                bool useSeparator = ((_spec.separatorTexture != null) && (_spec.separatorTexture.Length > 0) && (_spec.separatorDurationSecs > 0));
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
                                if (useSeparator)
                                {
                                    _texturePaths.Add("separator");
                                    _textureDurationSecs.Add(_spec.separatorDurationSecs);
                                }
                                _texturePaths.Add(file);
                                _textureDurationSecs.Add(_spec.durationSecs);
                            }
                        }
                        if (useSeparator)
                        {
                            _texturePaths.Add("separator");
                            _textureDurationSecs.Add(_spec.separatorDurationSecs);
                        }
                    }
                    else
                    {
                        if (useSeparator)
                        {
                            _texturePaths.Add("separator");
                            _textureDurationSecs.Add(_spec.separatorDurationSecs);
                        }
                        _texturePaths.Add(pathFull);
                        _textureDurationSecs.Add(_spec.durationSecs);
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
                    LoadTexture(separatorPathFull, ref _separatorTexture);
                }
                if (_separatorTexture == null)
                {
                    Debug.LogError("BackgroundChanger: cannot create separator texture from file '" + _spec.separatorTexture +"'");
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
            private void LoadTexture(string texturePath, ref Texture2D texture)
            {
                if (!File.Exists(texturePath))
                {
                    Debug.Log("BackgroundChanger: cannot find texture file " + texturePath);
                    texture = SolidTexture(Color.red);
                }
                else
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

                    if (texture == null)
                    {
                        const int ToBeReplacedByLoadImage = 2;
                        const bool MipMaps = false;
                        texture = new Texture2D(ToBeReplacedByLoadImage, ToBeReplacedByLoadImage, TextureFormat.RGBA32, MipMaps);
                    }
                    texture.LoadImage(_textureBytes);
                }
            }

            private bool UsingDisplayRate()
            {
                bool noSeparators = ((_spec.separatorTexture == null) || (_spec.separatorTexture.Length == 0) || (_spec.separatorDurationSecs <= 0));
                bool compatibleSeparators = (_spec.separatorDurationSecs == _spec.durationSecs);
                if ((noSeparators || compatibleSeparators) && _spec.complete)
                {
                    int displayRateHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
                    int backgroundRateHz = Mathf.RoundToInt(1.0f / _spec.durationSecs);
                    if (Mathf.Abs(displayRateHz - backgroundRateHz) < 1)
                    {
                        return true;
                    }
                }
                return false;
            }

            private void UseTexture(int index)
            {
                if ((_material == null) || (index >= _texturePaths.Count))
                {
                    return;
                }
                string path = _texturePaths[index];
                if (path == "separator")
                {
                    _material.SetTexture("_MainTex", _separatorTexture);

                    _currentChangingToSeparatorTextureLog.separatorTextureDurationSecs = _spec.separatorDurationSecs;
                    Logger.Log(_currentChangingToSeparatorTextureLog);
                }
                else
                {
                    if (index < _texturePaths.Count)
                    {
                        LoadTexture(_texturePaths[index], ref _texture);
                        _material.SetTexture("_MainTex", _texture);

                        _currentChangingTextureLog.backgroundTextureNowInUse = _texturePaths[index];
                        _currentChangingTextureLog.durationSecs = _spec.durationSecs;
                        Logger.Log(_currentChangingTextureLog);
                    }
                }
                if (index < _texturePaths.Count)
                {
                    if (_textureUsedAtFrame == null)
                    {
                        _textureUsedAtFrame = new int[_texturePaths.Count];
                        for (int i = 0; i < _textureUsedAtFrame.Length; i++)
                        {
                            _textureUsedAtFrame[i] = -1;
                        }
                    }
                    _textureUsedAtFrame[index] = Time.frameCount;
                }
            }

            private void ReportTextureUsage()
            {
                BackgroundsSummaryLog log = new BackgroundsSummaryLog();

                float expectedDurationBackgroundsSec = 0;
                foreach (float duration in _textureDurationSecs)
                {
                    expectedDurationBackgroundsSec += duration;
                }

                long durationSplashMs = _timeBackgroundsStartMs - _timeSplashStartMs;
                long timeBackgroundsEndMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long durationBackgroundsMs = timeBackgroundsEndMs - _timeBackgroundsStartMs;
                log.splashScreenDurationSec = durationSplashMs / 1000.0f;
                log.backgroundsTotalDurationSec = durationBackgroundsMs / 1000.0f;
                log.backgroundsTotalCount = _texturePaths.Count;
                log.complete = _usingDisplayRate;

                log.expectedBackgroundsTotalDurationSecs = 0;
                for (int i = 0; i < _textureDurationSecs.Count; ++i)
                {
                    log.expectedBackgroundsTotalDurationSecs += _textureDurationSecs[i];
                }

                log.skippedBackgrounds = new List<int>();
                for (int i = 0; i < _textureUsedAtFrame.Length; ++i)
                {
                    if (_textureUsedAtFrame[i] == -1)
                    {
                        log.skippedBackgrounds.Add(i);
                    }
                }
                Logger.Log(log);

                Debug.Log($"Splash screen took {durationSplashMs / 1000.0f} sec ({durationSplashMs} ms)");
                Debug.Log($"Showing backgrounds took {durationBackgroundsMs / 1000.0f} sec ({durationBackgroundsMs} ms)");
                Debug.Log($"Showing backgrounds expected to take {expectedDurationBackgroundsSec} sec");
                Debug.Log($"Skipped {log.skippedBackgrounds.Count} of {_textureUsedAtFrame.Length} backgrounds");
            }
        }

        private static Spec _spec;
        private static string _specFilePath;

        private static Material _material;
        private static List<string> _texturePaths = new List<string>();
        private static List<float> _textureDurationSecs = new List<float>();
        private static byte[] _textureBytes;
        private static Texture2D _texture;
        private static Texture2D _separatorTexture;
        private static GameObject _object;
        private static bool _splashIsFinished = false;

        private static bool _usingDisplayRate;

        private static int _currentTextureIndex;
        private static double _currentTime;

        private static long _timeSplashStartMs;
        private static long _timeBackgroundsStartMs;
        private static int[] _textureUsedAtFrame;

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

        [Serializable]
        private class BackgroundsSummaryLog : Logger.Entry
        {
            public float splashScreenDurationSec;
            public float backgroundsTotalDurationSec;
            public float expectedBackgroundsTotalDurationSecs;
            public int backgroundsTotalCount;
            public bool complete;
            public List<int> skippedBackgrounds;
        };

    }
}
