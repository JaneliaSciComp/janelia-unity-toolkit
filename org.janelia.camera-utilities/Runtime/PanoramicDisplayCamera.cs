#define PROGRESS_BOX
#define PROFILE

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Renders panoramas in real time from a freely moving viewpoint. The viewpoint comes from the `Transform`
// for the `GameObject` this script is attached to. The panoramic rendering involves rendering a box of
// views around the viewpoint---north, south, east, west, down, and optionally up---and then using a custom
// shader (`Assets/Resources/PanoramicDisplay.shader`) to remap those renderings into the panorama. The
// remapping is specific to the geometry of the display surface (e.g., a cylinder around the viewpoint back-
// projected onto by three external projectors, or a hemisphere projected onto by one overhead projector, etc.).
// The details of this geometry is passed to this script as the application is starting. Additional textures
// to correct the brightness and color of the images on the display surfaces can also be specified.

namespace Janelia
{
    public class PanoramicDisplayCamera : MonoBehaviour
    {
        // The cameras that render the box around the viewpoint.
        public Camera[] sourceCameras = new Camera[6];

        // The width of each of the source cameras. The height is the same, as each side of the box is a square.
        public int sourceWidth = 512;

        // Scaling factors for the masking (e.g., to give brightness compensation) and the color correction.
        // These scaling factors can be changed as the application is running.  See the comments for
        // `SetDisplaySurfaceData`, below, and also in `ExampleUsingPanoramicDisplayCamera.cs`.
        public float surfaceMaskScale = 1;
        public float surfaceColorCorrectionScale = 0;
        public bool invertColorAtMask0 = false;

        // If the panorama is to be displayed with more than one external display or projector, the images for
        // all the displays are adjoined horizontally into a single wide image, and this image "bleeds" from
        // the left projector onto the others, to the right.
        public int leftProjectorIndex = 2;
        public static int leftProjectorIndexStatic = 2;

        // `false` means the surface is stationary and the cameras move relative to it.
        // `true` means the surface moves (translates and rotates) along with the cameras.
        public bool movingSurface = false;

        // An optional height displacement, necessarily only in unusual circumstances.
        public float offsetY = 0;

#if PROGRESS_BOX
        // The "progress box" is four squares that change from black to white as the frame count progresses.
        // Each square acts like one binary digit of `Time.frameCount % 16`, and a set of photo diodes positioned
        // over the squares record the true frame rate for the displayed frames.
        public bool showProgressBox = false;
        // The squares are arranged in a 2x2 grid, with this position as its center.
        public Vector2Int progressBoxPosition = new Vector2Int(150, 300);
        // The empty space between the boxes in the 2x2 grid.
        public Vector2Int progressBoxSeparation = new Vector2Int(10, 10);
        // The size (width and height) of each individual box.
        public int progressBoxSize = 50;

        // By default, interactive changes to the positions of the squares are saved as Unity player preferences,
        // but this delegate is a hook for code that saves the changes elsewhere. The code implementing this
        // alternative is responsible for restoring the changes by setting the `progressBoxPosition` and other
        // properties directly at startup.
        public delegate void ProgressBoxSaveDelegate(Vector2Int position, Vector2Int separation);
        public ProgressBoxSaveDelegate progressBoxSaveDelegate = null;
#endif

        public void SetAntialiasing(int level)
        {
            int clamped = Mathf.Clamp(level, 0, 2);
            _material.SetInt("_Antialiasing", clamped);
        }

        // A larger value (e.g., 1) reduces cracks between cameras.
        public void SetCrackReduction(float factor)
        {
            _material.SetFloat("_CrackReduction", factor);
        }

        // A client should call this function before any `Update` functions are called (e.g. call it in `Start`).
        // The arrays are a `dataWidth` by `dataHeight` grid of elements in row-major order, sized to match the
        // pixels on the final display projector, or projectors; the data for multiple projectors must be concatenated
        // into each array in the left-right order of the projectors (e.g., the order on the Windows Extended Desktop).
        // The `surfaceXData`, `surfaceYData` and `surfaceZData` arrays define a 3D point for each projector pixel,
        // with that 3D point being where the pixel is projected on the physical display surface.
        // The `surfaceMaskData` array has a 0-255 value for each projector pixel, interpreted as a 8-bit float, which 
        // works with the `surfaceMaskScale` scalar value to adjust the final color for each projector pixel.
        // Specifically, `c1 = c0 * (1 - surfaceMaskScale * s1)`, where `c1` is the adjusted color, `c0` is the
        // unadjusted color, and `s1` is the value of the `surfaceMaskData` at the pixel.  So to get a simple "on/off" 
        // mask, use `surfaceMaskScale` of 1, and set `s1` to 0 for "on" (color unchanged) and 255 for "off" (black).
        // The `surfaceColorCorrectionData` array has a `Color` for each projector pixel, which works with the 
        // `surfaceColorCorrectionScale` scalar value to further adjust the final color for each projector pixels.
        // Specifically, `c2 = c1 * (1 - surfaceColorCorrectionScale * s2)`, where `c2` is the compensated color, and
        // `s2` is the color value in `surfaceColorCorrectionData` at the pixel (so that color is subtracted out).
        // See `ExampleUsingPanoramicDisplayCamera` for an example of how the mask can be used to add brightness
        // compensation for a cylindrical display screen, and also an example of applying color correction for this
        // display screen.

        public void SetDisplaySurfaceData(int dataWidth, int dataHeight, float[] surfaceXData, float[] surfaceYData, float[] surfaceZData, 
            byte[] surfaceMaskData, Color[] surfaceColorCorrectionData, bool fullScreen = false)
        {
            if (!SystemInfo.SupportsTextureFormat(TextureFormat.RFloat))
            {
                Debug.Log("TextureFormat.RFloat is not supported");
                return;
            }
            if (!SystemInfo.SupportsTextureFormat(TextureFormat.R8))
            {
                Debug.Log("TextureFormat.R8 is not supported");
                return;
            }

            if (fullScreen)
            {
                RefreshRate rate = Screen.currentResolution.refreshRateRatio;
                // Request `ExclusiveFullScreen` with the appropriate refresh rate to get the most efficient display path
                // (which avoids compositing with the Desktop Window Manager or DWM).
                Screen.SetResolution(dataWidth, dataHeight, FullScreenMode.ExclusiveFullScreen, rate);
                Debug.Log($"Screen.SetResolution({dataWidth}, {dataHeight}, FullScreenMode.ExclusiveFullScreen, {rate})");
            }
            else
            {
                // The final "false" is important, to turn off full-screen display, so an extra wide image will
                // spill over onto other displays that are adjacent in the Windows extended desktop.
                Screen.SetResolution(dataWidth, dataHeight, false);
            }

            SetupMaterial();

            _projectorSurfaceXTexture = new Texture2D(dataWidth, dataHeight, TextureFormat.RFloat, mipChain: false, linear: true);
            _projectorSurfaceXTexture.SetPixelData(surfaceXData, mipLevel: 0);
            _projectorSurfaceXTexture.filterMode = FilterMode.Bilinear;
            _projectorSurfaceXTexture.Apply();

            _material.SetTexture("_TexProjectorSurfaceX", _projectorSurfaceXTexture);

            _projectorSurfaceYTexture = new Texture2D(dataWidth, dataHeight, TextureFormat.RFloat, mipChain: false, linear: true);
            _projectorSurfaceYTexture.SetPixelData(surfaceYData, mipLevel: 0);
            _projectorSurfaceYTexture.filterMode = FilterMode.Bilinear;
            _projectorSurfaceYTexture.Apply();

            _material.SetTexture("_TexProjectorSurfaceY", _projectorSurfaceYTexture);

            _projectorSurfaceZTexture = new Texture2D(dataWidth, dataHeight, TextureFormat.RFloat, mipChain: false, linear: true);
            _projectorSurfaceZTexture.SetPixelData(surfaceZData, mipLevel: 0);
            _projectorSurfaceZTexture.filterMode = FilterMode.Bilinear;
            _projectorSurfaceZTexture.Apply();

            _material.SetTexture("_TexProjectorSurfaceZ", _projectorSurfaceZTexture);

            _projectorSurfaceMaskTexture = new Texture2D(dataWidth, dataHeight, TextureFormat.R8, mipChain: false, linear: true);
            _projectorSurfaceMaskTexture.SetPixelData(surfaceMaskData, mipLevel: 0);
            _projectorSurfaceMaskTexture.filterMode = FilterMode.Bilinear;
            _projectorSurfaceMaskTexture.Apply();

            _material.SetTexture("_TexMask", _projectorSurfaceMaskTexture);

            _projectorSurfaceColorCorrectionTexture = new Texture2D(dataWidth, dataHeight, TextureFormat.ARGB32, mipChain: false, linear: true);
            _projectorSurfaceColorCorrectionTexture.SetPixels(surfaceColorCorrectionData, miplevel: 0);
            _projectorSurfaceColorCorrectionTexture.filterMode = FilterMode.Bilinear;
            _projectorSurfaceColorCorrectionTexture.Apply();

            _material.SetTexture("_TexColorCorrection", _projectorSurfaceColorCorrectionTexture);
        }

        public void Start()
        {
            Camera camera = GetComponent<Camera>();
            if (camera.targetDisplay != 0)
            {
                Debug.LogWarning("PanoramicDisplayCamera: attached camera should have 'Target Display' set to 'Display 1'");
            }

            // Don't render anything with this camera, as OnRenderImage() will completely replace
            // its image with the concatenation of the displayCamera images.
            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.Nothing;

            SetupSourceCameras(sourceWidth, sourceWidth);
            SetupBlackTexture(sourceWidth, sourceWidth);

            Debug.Log($"QualitySettings.GetQualityLevel() {QualitySettings.GetQualityLevel()} (of {QualitySettings.count} possible)");
            Debug.Log($"QualitySettings.antiAliasing {QualitySettings.antiAliasing}");
            Debug.Log($"QualitySettings.shadows {QualitySettings.shadows}");
            Debug.Log($"QualitySettings.shadowResolution {QualitySettings.shadowResolution}");
#if PROGRESS_BOX
            if (progressBoxSaveDelegate == null)
            {
                if (PlayerPrefs.HasKey(PLAYER_PREF_KEY_SHOW_PROGRESS_BOX))
                {
                    showProgressBox = PlayerPrefs.GetInt(PLAYER_PREF_KEY_SHOW_PROGRESS_BOX) != 0;
                }
                if (PlayerPrefs.HasKey(PLAYER_PREF_KEY_PROGRESS_BOX_POSITION_X))
                {
                    progressBoxPosition.x = PlayerPrefs.GetInt(PLAYER_PREF_KEY_PROGRESS_BOX_POSITION_X);
                }
                if (PlayerPrefs.HasKey(PLAYER_PREF_KEY_PROGRESS_BOX_POSITION_Y))
                {
                    progressBoxPosition.y = PlayerPrefs.GetInt(PLAYER_PREF_KEY_PROGRESS_BOX_POSITION_Y);
                }
                if (PlayerPrefs.HasKey(PLAYER_PREF_KEY_PROGRESS_BOX_SEPARATION_X))
                {
                    progressBoxSeparation.x = PlayerPrefs.GetInt(PLAYER_PREF_KEY_PROGRESS_BOX_SEPARATION_X);
                }
                if (PlayerPrefs.HasKey(PLAYER_PREF_KEY_PROGRESS_BOX_SEPARATION_Y))
                {
                    progressBoxSeparation.y = PlayerPrefs.GetInt(PLAYER_PREF_KEY_PROGRESS_BOX_SEPARATION_Y);
                }
            }
#endif
        }

        public void Update()
        {
            _material.SetFloat("_MaskScale", surfaceMaskScale);
            _material.SetFloat("_ColorCorrectionScale", surfaceColorCorrectionScale);
            _material.SetInt("_InvertColorAtMask0", invertColorAtMask0 ? 1 : 0);

#if PROGRESS_BOX
            bool updatePlayerPrefs = false;

            Vector2Int newProgressBoxPosition = progressBoxPosition;
            Vector2Int newProgressBoxSeparation = progressBoxSeparation;
            if (Input.GetKeyDown(KeyCode.P))
            {
                showProgressBox = !showProgressBox;
                updatePlayerPrefs = true;
            }
            else if (Input.GetKey(KeyCode.W))
            {
                newProgressBoxPosition.y -= 1;
                updatePlayerPrefs = true;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                newProgressBoxPosition.y += 1;
                updatePlayerPrefs = true;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                newProgressBoxPosition.x -= 1;
                updatePlayerPrefs = true;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                newProgressBoxPosition.x += 1;
                updatePlayerPrefs = true;
            }
            else if (Input.GetKey(KeyCode.Alpha1))
            {
                if (newProgressBoxSeparation.x > 1)
                {
                    newProgressBoxSeparation.x -= 1;
                    updatePlayerPrefs = true;
                }
            }
            else if (Input.GetKey(KeyCode.Alpha2))
            {
                newProgressBoxSeparation.x += 1;
                updatePlayerPrefs = true;
            }
            else if (Input.GetKey(KeyCode.Alpha3))
            {
                if (newProgressBoxSeparation.y > 1)
                {
                    newProgressBoxSeparation.y -= 1;
                    updatePlayerPrefs = true;
                }
            }
            else if (Input.GetKey(KeyCode.Alpha4))
            {
                newProgressBoxSeparation.y += 1;
                updatePlayerPrefs = true;
            }
            if (updatePlayerPrefs)
            {
                if (ProgressBoxIsValid(newProgressBoxPosition, newProgressBoxSeparation))
                {
                    progressBoxPosition = newProgressBoxPosition;
                    progressBoxSeparation = newProgressBoxSeparation;
                    if (progressBoxSaveDelegate != null)
                    {
                        progressBoxSaveDelegate(progressBoxPosition, progressBoxSeparation);
                    }
                    else
                    {
                        PlayerPrefs.SetInt(PLAYER_PREF_KEY_SHOW_PROGRESS_BOX, showProgressBox ? 1 : 0);
                        PlayerPrefs.SetInt(PLAYER_PREF_KEY_PROGRESS_BOX_POSITION_X, progressBoxPosition.x);
                        PlayerPrefs.SetInt(PLAYER_PREF_KEY_PROGRESS_BOX_POSITION_Y, progressBoxPosition.y);
                        PlayerPrefs.SetInt(PLAYER_PREF_KEY_PROGRESS_BOX_SEPARATION_X, progressBoxSeparation.x);
                        PlayerPrefs.SetInt(PLAYER_PREF_KEY_PROGRESS_BOX_SEPARATION_Y, progressBoxSeparation.y);
                        PlayerPrefs.Save();
                    }
                }
            }
#endif
#if PROFILE
            _profileDeltaTimeSum += Time.deltaTime;
            _profileDeltaTimeCount += 1;
            if (Time.frameCount % _profilePeriod == 0)
            {
                float mean = _profileDeltaTimeSum / _profileDeltaTimeCount;
                float meanMs = Mathf.Round(mean * 1000);
                float rate = Mathf.Round(1 / mean);
                Debug.Log($"PanoramicDisplayCamera mean time between frames for the last {_profilePeriod} frames: {meanMs} ms ({rate} Hz)");
                _profileDeltaTimeSum = 0;
                _profileDeltaTimeCount = 0;
            }
#endif
        }

        public void SetToBlack(bool black)
        {
            _black = black;
        }

        public void OnRenderImage(RenderTexture input, RenderTexture output)
        {
            if (!SourceCamerasAreValid() ||!TexturesAreValid())
            {
                return;
            }

            RenderSourceCameras();

            // Do this every frame to suppress the warning:
            // "OnRenderImage() possibly didn't write anything to the destination texture!"
            Graphics.SetRenderTarget(output);

            int finalWidth = input.width;
            int finalHeight = input.height;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, finalWidth, finalHeight, 0);

            DrawFromSourceCameraTextures(finalWidth, finalHeight);

#if PROGRESS_BOX
            DrawProgressBox();
#endif

            GL.PopMatrix();
        }

        private void SetupSourceCameras(int width, int height)
        {
            _materialCameraPositionName = new string[6];
            _materialCameraForwardName = new string[6];
            _materialCameraUpName = new string[6];
            _materialCameraRightName = new string[6];
            _materialCameraNearName = new string[6];
            _materialCameraFovHorizName = new string[6];
            _materialCameraFovVertName = new string[6];
            _materialCameraTexName = new string[6];

            int n = 0;
            for (int i = 0; i < 6; i++)
            {
                if (sourceCameras[i] != null)
                {
                    sourceCameras[i].targetTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    sourceCameras[i].enabled = false;

                    _initialForward[i] = sourceCameras[i].transform.forward;
                    _initialUp[i] = sourceCameras[i].transform.up;
                    _initialRight[i] = sourceCameras[i].transform.right;

                    string baseName = "_Camera" + i.ToString();
                    _materialCameraPositionName[i] = baseName + "Position";
                    _materialCameraForwardName[i] = baseName + "Forward";
                    _materialCameraUpName[i] = baseName + "Up";
                    _materialCameraRightName[i] = baseName + "Right";
                    _materialCameraNearName[i] = baseName + "Near";
                    _materialCameraFovHorizName[i] = baseName + "FovHoriz";
                    _materialCameraFovVertName[i] = baseName + "FovVert";
                    _materialCameraTexName[i] = "_TexCamera" + i.ToString();

                    ++n;
                }
            }
            _enableSixCameras = (n == 6);
            if (_enableSixCameras)
            {
                _material.EnableKeyword("SIX_CAMERAS");
            }
        }

        private void SetupMaterial()
        {
            // For the shader `org.janelia.camera-utilities/Assets/Resources/PanoramicDisplay.shader`
            // the name to use when loading is just `PanoramicDisplay`.
            Shader shader = Resources.Load("PanoramicDisplay", typeof(Shader)) as Shader;
            if (shader != null)
            {
                _material = new Material(shader);
            }
            else
            {
                Debug.Log("Could not load PanoramicDisplay.shader");
            }
        }

        private void SetupBlackTexture(int width, int height)
        {
            _blackTexture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
            _blackTexture.SetPixels(pixels);
            _blackTexture.Apply();
        }

        private bool SourceCamerasAreValid()
        {
            int specifiedCount = 0;
            for (int i = 0; i < sourceCameras.Count(); i++)
            {
                specifiedCount += (sourceCameras[i] != null) ? 1 : 0;
            }
            if (specifiedCount < 5)
            {
                Debug.LogWarning("PanoramicDisplayCamera: at least 5 source cameras must be specified");
                return false;
            }
            return true;
        }

        private bool TexturesAreValid()
        {
            if (_projectorSurfaceXTexture == null)
            {
                Debug.Log("_projectorSurfaceXTexture is null");
                return false;
            }
            if (_projectorSurfaceYTexture == null)
            {
                Debug.Log("_projectorSurfaceYTexture is null");
                return false;
            }
            if (_projectorSurfaceZTexture == null)
            {
                Debug.Log("_projectorSurfaceZTexture is null");
                return false;
            }
            if (_projectorSurfaceMaskTexture == null)
            {
                Debug.Log("_projectorSurfaceMaskTexture is null");
                return false;
            }
            if (_projectorSurfaceColorCorrectionTexture == null)
            {
                Debug.Log("_projectorSurfaceColorCorrectionTexture is null");
                return false;
            }
            return true;
        }

        private void RenderSourceCameras()
        {
            foreach (Camera sourceCamera in sourceCameras)
            {
                if (sourceCamera != null)
                {
                    sourceCamera.Render();
                }
            }
        }

        private void DrawFromSourceCameraTextures(int finalWidth, int finalHeight)
        {
            if (sourceCameras.Count() < 5)
            {
                Debug.LogWarning($"PanoramicDisplayCamera: expecting at least 5 source cameras instead of {sourceCameras.Count()}");
                return;
            }

            int n = _enableSixCameras ? 6 : 5;
            for (int i = 0; i < n; i++)
            {
                SetMaterialCamera(_material, sourceCameras[i], i);
            }

            Graphics.DrawTexture(new Rect(0, 0, finalWidth, finalHeight), sourceCameras[0].targetTexture, _material);
        }

        private void SetMaterialCamera(Material material, Camera camera, int i)
        {
            Vector3 pos = camera.transform.position + new Vector3(0, offsetY, 0);
            material.SetVector(_materialCameraPositionName[i], movingSurface ? Vector3.zero : pos);
            material.SetVector(_materialCameraForwardName[i], movingSurface ? _initialForward[i] : camera.transform.forward);
            material.SetVector(_materialCameraUpName[i], movingSurface ? _initialUp[i] : camera.transform.up);
            material.SetVector(_materialCameraRightName[i], movingSurface ? _initialRight[i] : camera.transform.right);
            material.SetFloat(_materialCameraNearName[i], camera.nearClipPlane);
            float fovHoriz = camera.fieldOfView;
            float fovVert = Camera.VerticalToHorizontalFieldOfView(fovHoriz, camera.aspect);
            material.SetFloat(_materialCameraFovHorizName[i], fovHoriz);
            material.SetFloat(_materialCameraFovVertName[i], fovVert);

            RenderTexture cameraTexture = camera.targetTexture;
            if (cameraTexture != null)
            {
                cameraTexture.filterMode = FilterMode.Bilinear;
                material.SetTexture(_materialCameraTexName[i], _black ? _blackTexture : cameraTexture);
            }
        }

#if UNITY_EDITOR
        // The following three functions are part of the complicated pattern necessary for
        // a `leftProjectorIndex` value set in the Inspector to be acessible by `AdjoiningDisplaysCameraBuilder`
        // at build time, when this value is used as an argument in the Windows shortcut file.
        // TODO: Rename `AdjoiningDisplaysCameraBuilder` since it is now used here, too.

        private void OnValidate()
        {
            leftProjectorIndexStatic = leftProjectorIndex;
        }

        private static int GetMonitorIndex()
        {
            return leftProjectorIndexStatic;
        }

        [InitializeOnLoadMethod]
        public static void SetupDelegate()
        {
            // TODO: Rename `AdjoiningDisplaysCameraBuilder` since it is now used here, too.
            AdjoiningDisplaysCameraBuilder.getMonitorIndexDelegate = GetMonitorIndex;
        }
#endif

#if PROGRESS_BOX
        private void DrawProgressBox()
        {
            if (showProgressBox && ProgressBoxIsValid())
            {
                InitializeProgressTexturesIfNeeded(progressBoxSize);
                int count = Time.frameCount % 16;
                int mask = 1;
                for (int i = 0; i < 4; ++i)
                {
                    Texture2D progressTex = ((count & mask) != 0) ? _progressTextureOn : _progressTextureOff;
                    Vector2Int p = ProgressBoxTexturePosition(i);
                    Rect r = new Rect(p.x, p.y, progressBoxSize, progressBoxSize);
                    Graphics.DrawTexture(r, progressTex);
                    mask *= 2;
                }
            }
        }

        private void InitializeProgressTexturesIfNeeded(int size)
        {
            if ((_progressTextureOn == null) || (_progressTextureOn.width != size))
            {
                _progressTextureOn = MakeProgressTexture(size, size, true);
            }
            if ((_progressTextureOff == null) || (_progressTextureOff.width != size))
            {
                _progressTextureOff = MakeProgressTexture(size, size, false);
            }
        }

        private Texture2D MakeProgressTexture(int width, int height, bool on)
        {
            Texture2D result = new Texture2D(width, height);
            Color color = on ? new Color(1, 1, 1, 1) : new Color(0, 0, 0, 1);
            Color[] pixels = Enumerable.Repeat(color, width * height).ToArray();
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        private Vector2Int ProgressBoxTexturePosition(int i)
        {
            return ProgressBoxTexturePosition(i, progressBoxPosition, progressBoxSeparation);
        }

        private Vector2Int ProgressBoxTexturePosition(int i, Vector2Int pos, Vector2Int sep)
        {
            // The squares in the grid should have the following arrangement, where the exponent is `i`:
            // 2^0 2^1
            // 2^2 2^3
            Vector2Int p = pos;
            float shx = ((float) sep.x) / 2;
            float shy = ((float) sep.y) / 2;
            p.x += ((i == 0) || (i == 2)) ? -(Mathf.CeilToInt(shx) + progressBoxSize) : Mathf.FloorToInt(shx);
            p.y += ((i == 0) || (i == 1)) ? -(Mathf.CeilToInt(shy) + progressBoxSize) : Mathf.FloorToInt(shy);
            return p ;
        }

        private bool ProgressBoxIsValid(bool warn = true)
        {
            return ProgressBoxIsValid(progressBoxPosition, progressBoxSeparation, warn);
        }

        private bool ProgressBoxIsValid(Vector2Int pos, Vector2Int sep, bool warn = false)
        {
            if ((sep.x < 1) || (sep.y < 1))
            {
                if (warn && !_progressBoxWarned)
                {
                    Debug.Log($"Invalid progress box separation {sep}");
                    _progressBoxWarned = true;
                }
                return false;
            }
            int w = (_projectorSurfaceXTexture != null) ? _projectorSurfaceXTexture.width : Screen.width;
            int h = (_projectorSurfaceXTexture != null) ? _projectorSurfaceXTexture.height : Screen.height;
            Vector2Int p0 = ProgressBoxTexturePosition(0, pos, sep);
            Vector2Int p3 = ProgressBoxTexturePosition(3, pos, sep);
            if ((p0.x < 0) || (p0.y < 0) || (p3.x + progressBoxSize >= w) || (p3.y + progressBoxSize >= h))
            {
                if (warn && !_progressBoxWarned)
                {
                    Debug.Log($"Invalid progress box position {pos} and separation {sep} for screen size ({Screen.width}, {Screen.height})");
                    _progressBoxWarned = true;
                }
                return false;
            }
            return true;
        }
#endif

        private Material _material;

        string[] _materialCameraPositionName;
        string[] _materialCameraForwardName;
        string[] _materialCameraUpName;
        string[] _materialCameraRightName;
        string[] _materialCameraNearName;
        string[] _materialCameraFovHorizName;
        string[] _materialCameraFovVertName;
        string[] _materialCameraTexName;

        private bool _black = false;
        private Texture2D _blackTexture;

        private Texture2D _projectorSurfaceXTexture;
        private Texture2D _projectorSurfaceYTexture;
        private Texture2D _projectorSurfaceZTexture;
        private Texture2D _projectorSurfaceMaskTexture;
        private Texture2D _projectorSurfaceColorCorrectionTexture;

        private bool _enableSixCameras = false;

        private Vector3[] _initialForward = new Vector3[6];
        private Vector3[] _initialUp = new Vector3[6];
        private Vector3[] _initialRight = new Vector3[6];

#if PROGRESS_BOX
        private Texture2D _progressTextureOn;
        private Texture2D _progressTextureOff;
        private const string PLAYER_PREF_KEY_SHOW_PROGRESS_BOX = "PanoramicDisplayCamera.ShowProgressBox";
        private const string PLAYER_PREF_KEY_PROGRESS_BOX_POSITION_X = "PanoramicDisplayCamera.ProgressBoxPositionX";
        private const string PLAYER_PREF_KEY_PROGRESS_BOX_POSITION_Y = "PanoramicDisplayCamera.ProgressBoxPositionY";
        private const string PLAYER_PREF_KEY_PROGRESS_BOX_SEPARATION_X = "PanoramicDisplayCamera.ProgressBoxSeparationX";
        private const string PLAYER_PREF_KEY_PROGRESS_BOX_SEPARATION_Y = "PanoramicDisplayCamera.ProgressBoxSeparationY";
        private bool _progressBoxWarned = false;
#endif
#if PROFILE
        private float _profileDeltaTimeSum = 0;
        private int _profileDeltaTimeCount = 0;
        private int _profilePeriod = 500;
#endif
    }
}