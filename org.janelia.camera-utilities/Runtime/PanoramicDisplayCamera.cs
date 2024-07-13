#define PROGRESS_BOX

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
        // The cameras that renderig the box around the viewpoint.
        public Camera[] sourceCameras = new Camera[6];

        // The width of each of the source cameras. The height is the same, as each side of the box is a square.
        public int sourceWidth = 512;

        // Scaling factors for the masking (e.g., to give brightness compensation) and the color correction.
        // These scaling factors can be changed as the application is running.  See the comments for
        // `SetDisplaySurfaceData`, below, and also in `ExampleUsingPanoramiceDisplayCamera.cs`.
        public float surfaceMaskScale = 1;
        public float surfaceColorCorrectionScale = 0;

        // If the panorama is to be displayed with more than one external display or projector, the images for
        // all the displays are adjoined horizontally into a single wide image, and this image "bleeds" from
        // the left projector onto the others, to the right.
        public int leftProjectorIndex = 2;
        public static int leftProjectorIndexStatic = 2;

#if PROGRESS_BOX
        public bool showProgressBox = false;
        public Vector2Int progressBoxPosition = new Vector2Int(100, 100);
        public int progressBoxSize = 50;
#endif

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
        // `surfaceColorCorrectionScale` scalar value to further adjuest the final color for each projector pixels.
        // Specifically, `c2 = c1 * (1 - surfaceColorCorrectionScale * s2)`, where `c2` is the compensated color, and
        // `s2` is the color value in `surfaceColorCorrectionData` at the pixel (so that color is subtracted out).
        // See `ExampleUsingPanoramicDisplayCamera` for an example of how the mask can be used to add brightness
        // compensation for a cylindrical display screen, and also an example of applying color correction for this
        // display screen.

        public void SetDisplaySurfaceData(int dataWidth, int dataHeight, float[] surfaceXData, float[] surfaceYData, float[] surfaceZData, 
            byte[] surfaceMaskData, Color[] surfaceColorCorrectionData)
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

            // The final "false" is important, to turn off full-screen display, so an extra wide image will
            // spill over onto other displays that are adjacent in the Windows extended desktop.
            Screen.SetResolution(dataWidth, dataHeight, false);

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
        }

        public void Update()
        {
            _material.SetFloat("_MaskScale", surfaceMaskScale);
            _material.SetFloat("_ColorCorrectionScale", surfaceColorCorrectionScale);

#if PROGRESS_BOX
            if (Input.GetKeyDown("p"))
            {
                showProgressBox = !showProgressBox;
            }
            if (Input.GetKey("w"))
            {
                progressBoxPosition.y -= 1;
            }
            else if (Input.GetKey("a"))
            {
                progressBoxPosition.x -= 1;
            }
            else if (Input.GetKey("s"))
            {
                progressBoxPosition.y += 1;
            }
            else if (Input.GetKey("d"))
            {
                progressBoxPosition.x += 1;
            }
#endif
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
            int n = 0;
            for (int i = 0; i < 6; i++)
            {
                if (sourceCameras[i] != null)
                {
                    sourceCameras[i].targetTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    sourceCameras[i].enabled = false;
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
                Debug.LogWarning("PanoramicDisplayCamera: expecting at least 5 source cameras instead of " + sourceCameras.Count());
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
            string baseName = "_Camera" + i.ToString(); 
            material.SetVector(baseName + "Position", camera.transform.position);
            material.SetVector(baseName + "Forward", camera.transform.forward);
            material.SetVector(baseName + "Up", camera.transform.up);
            material.SetVector(baseName + "Right", camera.transform.right);
            material.SetFloat(baseName + "Near", camera.nearClipPlane);
            float fovHoriz = camera.fieldOfView;
            float fovVert = Camera.VerticalToHorizontalFieldOfView(fovHoriz, camera.aspect);
            material.SetFloat(baseName + "FovHoriz", fovHoriz);
            material.SetFloat(baseName + "FovVert", fovVert);

            RenderTexture cameraTexture = camera.targetTexture;
            cameraTexture.filterMode = FilterMode.Bilinear;

            string name = "_TexCamera" + i.ToString();
            material.SetTexture(name, cameraTexture);
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
            if (showProgressBox)
            {
                InitializeProgressTexturesIfNeeded(progressBoxSize);
                Texture2D progressTex = (Time.frameCount % 2 == 0) ? _progressTextureEven : _progressTextureOdd;
                Rect r = new Rect(progressBoxPosition.x, progressBoxPosition.y, progressBoxSize, progressBoxSize);
                Graphics.DrawTexture(r, progressTex);
            }
        }

        private void InitializeProgressTexturesIfNeeded(int size)
        {
            if ((_progressTextureEven == null) || (_progressTextureEven.width != size))
            {
                _progressTextureEven = MakeProgressTexture(size, size, true);
            }
            if ((_progressTextureOdd == null) || (_progressTextureOdd.width != size))
            {
                _progressTextureOdd = MakeProgressTexture(size, size, false);
            }
        }

        private Texture2D MakeProgressTexture(int width, int height, bool even)
        {
            Texture2D result = new Texture2D(width, height);
            Color color = even ? new Color(0, 0, 0, 1) : new Color(1, 1, 1, 1);
            Color[] pixels = Enumerable.Repeat(color, width * height).ToArray();
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }
#endif

        private Material _material;

        private Texture2D _projectorSurfaceXTexture;
        private Texture2D _projectorSurfaceYTexture;
        private Texture2D _projectorSurfaceZTexture;
        private Texture2D _projectorSurfaceMaskTexture;
        private Texture2D _projectorSurfaceColorCorrectionTexture;

        private bool _enableSixCameras = false;

#if PROGRESS_BOX
        private Texture2D _progressTextureEven;
        private Texture2D _progressTextureOdd;
#endif
    }
}