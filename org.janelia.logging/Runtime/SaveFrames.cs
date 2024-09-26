using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
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

                if (args.Contains("-format"))
                {
                    i = Array.IndexOf(args, "-format");
                    if (i + 1 < args.Length)
                    {
                        _format = args[i + 1];
                    }
                    Debug.Log("Format: " + _format);
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
        private static string _format = "";
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

                StartCoroutine(CaptureFrames());
            }

            public void OnDisable()
            {
                if (_capturing)
                { 
                    float elapsedMsAvg = (float)_elapsedMsSum / _elapsedMsCount;
                    Debug.Log("SaveFrames: average time to save a frame: " + elapsedMsAvg + " ms");
                }
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
                // For the shader `org.janelia.logging/Assets/Resources/Flip.shader`
                // the name to use when loading is just `Flip`.
                Shader flipShader = Resources.Load("Flip", typeof(Shader)) as Shader;
                Material flipMaterial = new Material(flipShader);
                
                int width = Screen.width;
                int height = Screen.height;
                if (_downsampleHeight > 0)
                {
                    float ratio = _downsampleHeight / (float)Screen.height;
                    width = Mathf.RoundToInt(ratio * Screen.width);
                    height = _downsampleHeight;
                }

                int i = 0;
                while (_capturing)
                {
                    yield return new WaitForEndOfFrame();
                    if ((_frame.Length > 0) && (i % _savingPeriod == 0))
                    {
                        long t1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                        // https://docs.unity3d.com/ScriptReference/RenderTexture.GetTemporary.html
                        // "This function is optimized for when you need a quick RenderTexture to do some temporary calculations.
                        // Internally Unity keeps a pool of temporary render textures, so a call to GetTemporary most often 
                        // just returns an already created one."
                        RenderTexture renderTextureNeedsFlipping = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.BGRA32);
                        _renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.BGRA32);

                        // Using `ScreenCapture` and `AsyncGPUReadback` is considerably faster than `Texture2D.ReadPixels()`.
                        ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTextureNeedsFlipping);
                        Graphics.Blit(renderTextureNeedsFlipping, _renderTexture, flipMaterial, 0);
                        RenderTexture.ReleaseTemporary(renderTextureNeedsFlipping);

                        // This call seems to make `player.log` contain this messsage:
                        // `'B8G8R8A8_SRGB' doesn't support ReadPixels usage on this platform. Async GPU readback failed.`
                        // Yet `ReadbackCompleted()` detects no error and the data does seem to be accessible as expected.
                        AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.BGRA32, ReadbackCompleted);

                        long t2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        long elapsedMs = t2 - t1;
                        if (elapsedMs >= 0)
                        {
                            _elapsedMsSum += elapsedMs;
                            _elapsedMsCount += 1;
                        }
                    }
                    i++;
                }
            }

            private void ReadbackCompleted(AsyncGPUReadbackRequest request)
            {
                if (!request.done)
                {
                    Debug.Log("SaveFrames.ReadbackCompleted AsyncGPUReadbackRequest done false, _frame " + _frame);
                }
                else if (request.hasError)
                {
                    Debug.Log("SaveFrames.ReadbackCompleted AsyncGPUReadbackRequest hasError true, _frame " + _frame);
                }
                else
                {
                    uint widthUint = (uint)_renderTexture.width;
                    uint heightUint = (uint)_renderTexture.height;
                    UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat = _renderTexture.graphicsFormat;
                    RenderTexture.ReleaseTemporary(_renderTexture);

                    using (Unity.Collections.NativeArray<byte> requestBytes = request.GetData<byte>())
                    {
                        byte[] imageBytes = requestBytes.ToArray();
                        SaveAsFormat(imageBytes, graphicsFormat, widthUint, heightUint);
                    }
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

            private void SaveAsFormat(byte[] imageBytes, UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat, uint width, uint height)
            {
                if ((_format.ToLower() == "graytxt") || (_format.ToLower() == "greytxt"))
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < imageBytes.Length; i += 4)
                    {
                        sb.Append($"{imageBytes[i]}");
                        string s = ((i + 4) % (width * 4) != 0) ? " " : "\n";
                        sb.Append(s);
                    }
                    string filename = _frame + ".txt";
                    string pathname = _outputPath + "/" + filename;
                    File.WriteAllText(pathname, sb.ToString());
                }
                else if ((_format.ToLower() == "gray") || (_format.ToLower() == "grey"))
                {
                    byte[] everyFourthByte = Enumerable.Range(0, imageBytes.Length / 4).Select(i => imageBytes[i * 4]).ToArray();
                    string filename = _frame + ".bin";
                    string pathname = _outputPath + "/" + filename;
                    File.WriteAllBytes(pathname, everyFourthByte);
                }
                else
                {
                    byte[] pngBytes = ImageConversion.EncodeArrayToPNG(imageBytes, graphicsFormat, width, height);
                    string filename = _frame + ".png";
                    string pathname = _outputPath + "/" + filename;
                    File.WriteAllBytes(pathname, pngBytes);
                }
            }

            private bool _capturing = false;
            private Text _textWidget = null;
            private RenderTexture _renderTexture;

            private long _elapsedMsSum = 0;
            private long _elapsedMsCount = 0;
        }
    }
}
