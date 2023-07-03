using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Janelia
{
    // A class to manage the saving of rendered frames.  The class is static, with a
    // `RuntimeInitializeOnLoadMethod` function that triggers the setup processing
    // automatically, without the need for adding a new `MonoBehavior` in the scene.
    // The frame capturing relies on a coroutine, which does require a `MonoBehavior`,
    // so the static class creates one itself.

    public static class SaveFrames
    {
        public static void SetFrame(int frame)
        {
            _frame = frame.ToString("D5");
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Contains("-saveFrames"))
            {
                int i = Array.IndexOf(args, "-saveFrames");
                _savingPeriod = 1;
                if (i + 1 < args.Length)
                {
                    int period;
                    if (int.TryParse(args[i + 1], out period))
                    {
                        _savingPeriod = period;
                    }
                }
                Debug.Log("Saving period: " + _savingPeriod.ToString());

                if (args.Contains("-numbers"))
                {
                    _showFrameNumbers = true;
                }
                Debug.Log("Show frame numbers: " + _showFrameNumbers);

                if (args.Contains("-height"))
                {
                    i = Array.IndexOf(args, "-height");
                    if (i + 1 < args.Length)
                    {
                        int height;
                        if (int.TryParse(args[i + 1], out height))
                        {
                            _downsampleHeight = height;
                        }
                    }
                    if (_downsampleHeight > 0)
                    {
                        Debug.Log("Downsampled height: " + _downsampleHeight);
                    }
                }

                if (args.Contains("-output"))
                {
                    i = Array.IndexOf(args, "-output");
                    if (i + 1 < args.Length)
                    {
                        _outputPath = args[i + 1];
                    }
                    Debug.Log("Output: " + _outputPath);
                }

                _object = new GameObject("SaveFrames");
                _object.hideFlags = HideFlags.HideAndDontSave;
                _object.AddComponent<SaveFramesInternal>();
            }
        }

        private static int _savingPeriod = 1;
        private static bool _showFrameNumbers = false;
        private static int _downsampleHeight = 0;
        private static string _outputPath;
        private static GameObject _object;
        internal static string _frame = "";

        // The class with the coroutine that will wait until the end of each frame, grab the pixels,
        // and save them.

        private class SaveFramesInternal : MonoBehaviour
        {
            public void Start()
            {
                _capturing = true;

                if (string.IsNullOrEmpty(_outputPath))
                {
                    _outputPath = Logger.logDirectory + "/Frames";
                    DateTime now = DateTime.Now;
                    _outputPath += "_" + now.ToString("yyyy") + "-" + now.ToString("MM") + "-" +
                        now.ToString("dd") + "_" + now.ToString("HH") + "-" + now.ToString("mm") + "-" +
                        now.ToString("ss");
                }
                EnsureDirectory(_outputPath);

                Debug.Log("Saving frames to folder: " + _outputPath);

                if (_showFrameNumbers)
                {
                    SetupTextWidget();
                }

                _texture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false); // HEY!! RGB24, false);
                if (_downsampleHeight > 0)
                {
                    float ratio = _downsampleHeight / (float)Screen.height;
                    int downsampleWidth = Mathf.RoundToInt(ratio * Screen.width);
                    _textureDownsampled = new Texture2D(downsampleWidth, _downsampleHeight, TextureFormat.ARGB32, false); 
                }

                StartCoroutine(CaptureFrames());
            }

            public void OnDisable()
            {
                _capturing = false;
            }

            public void LateUpdate()
            {
                if (_textWidget != null)
                {
                    _textWidget.text = _frame;
                }
            }

            private IEnumerator CaptureFrames()
            {
                int i = 0;
                while (_capturing)
                {
                    yield return new WaitForEndOfFrame();
                    if ((_frame.Length > 0) && (i % _savingPeriod == 0))
                    {
                        // A more sophisticated approach would use `ScreenCapture.CaptureScreenshotIntoRenderTexture`
                        // and `AsyncGPUReadback.Request`.  But improving performance on the main thread is not so
                        // important here, because the most common use case involves saving frames being played back
                        // from a log file.  The current approach has no asynchronous behavior so it is simple and
                        // reliable.
                        _texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                        _texture.Apply();

                        byte[] pngBytes;
                        if (_downsampleHeight > 0)
                        {
                            int width = _textureDownsampled.width;
                            int height = _textureDownsampled.height;
                            RenderTexture wasActive = RenderTexture.active;
                            RenderTexture renderTextureDownsampled = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                            RenderTexture.active = renderTextureDownsampled;
                            Graphics.Blit(_texture, renderTextureDownsampled);
                            _textureDownsampled.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                            _textureDownsampled.Apply();
                            RenderTexture.active = wasActive;
                            RenderTexture.ReleaseTemporary(renderTextureDownsampled);
                            uint widthUint = (uint)_textureDownsampled.width;
                            uint heightUint = (uint)_textureDownsampled.height;
                            pngBytes = ImageConversion.EncodeArrayToPNG(_textureDownsampled.GetRawTextureData(), _textureDownsampled.graphicsFormat, widthUint, heightUint);
                        }
                        else
                        {
                            uint width = (uint)Screen.width;
                            uint height = (uint)Screen.height;
                            pngBytes = ImageConversion.EncodeArrayToPNG(_texture.GetRawTextureData(), _texture.graphicsFormat, width, height);
                        }
                        string filename = _frame + ".png";
                        string pathname = _outputPath + "/" + filename;
                        File.WriteAllBytes(pathname, pngBytes);
                    }
                    i++;
                }
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

            private void SetupTextWidget()
            {
                GameObject obj;
                GameObject textObj;
                Canvas canvas;
                RectTransform rectTransform;

                obj = new GameObject();
                obj.name = "FrameCanvas";
                obj.AddComponent<Canvas>();

                canvas = obj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                obj.AddComponent<CanvasScaler>();

                textObj = new GameObject();
                textObj.transform.parent = obj.transform;
                textObj.name = "FrameText";

#if UNITY_EDITOR
                int fontSize = 18;
#else
                int fontSize = Mathf.RoundToInt(Mathf.Max(Screen.currentResolution.height / 50, 18));
#endif
                _textWidget = textObj.AddComponent<Text>();
                _textWidget.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _textWidget.fontSize = fontSize;
                _textWidget.color = Color.red;
                _textWidget.text = "Frame";

                rectTransform = _textWidget.GetComponent<RectTransform>();

                float insetForWidth = fontSize;
                float width = fontSize * 100;
                float insetForHeight = 0;
                float height = fontSize * 2;
                rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, insetForWidth, width);
                rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, insetForHeight, height);
            }

            private bool _capturing = false;
            private Text _textWidget = null;
            private Texture2D _texture;
            private Texture2D _textureDownsampled;
        }
    }
}
