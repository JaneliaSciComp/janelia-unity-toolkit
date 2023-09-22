// Makes the attached camera display the images of N other cameras, adjoined with each
// to form a kind of panorama.  Useful as a work-around for the limited performance of 
// Unity's "multi-display" capability, for building a stand-alone game application
// (not running in the Unity editor) that has multiple cameras each filling an external
// display.  
// To use:
// 1. Connect the external displays so they are numbered 2 through N in the Windows 
// "Display Settings".  Make sure that 2 is the leftmost, and then the display numbers 
// increase in order.
// 2. In the "Display Settings", give each display the appropriate resolution.  
// The resolution must be the same for all displays, and they must have the same 
// "Orientation"
// 3. In the "Display Settings", make sure the "Multiple displays" fields says "Extend 
// desktop to this display" (not "Duplicate desktop").
// 4. Add this script to one camera, such as the standard "Main Camera".  The "Target 
// Display" for this camera should be "Display 1".
// 5. In this script's "Display Cameras" section, set the "Size" field to the number 
// of external displays, and set "Element i" to the camera for external display i+1.
// 6. Set this script's "Display Width" and "Display Height" fields to match the external 
// display resolution from step 2.
// 7. Build the game.  The `AdjoiningDisplaysCameraBuilder.PerformBuild` function is a
// convenient way to do so.
// 8. The `AdjoiningDisplaysCameraBuilder.OnPostprocessBuild` function will run at the
// end of the build process.  It creates a Windows shortcut called "standalone" in the
// Unity project root folder.  This shortcut runs the game executable with the command
// line arguments that make the adjoined images extend from display 2 onto all the 
// other displays, making each camera's image appear on the appropriate display.  These
// arguments are: -popupwindow -screen-fullscreen 0 -monitor 2

#define SUPPORT_KEYBOARD_SHORTCUTS

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Janelia
{
    public class AdjoiningDisplaysCamera : MonoBehaviour
    {
        public Camera[] displayCameras = new Camera[4];

        public int displayWidth = 480;
        public int displayHeight = 720;


        public int leftDisplayIndex = 2;
        public static int leftDisplayIndexStatic = 2;

        public bool mirror = false;

        public enum ProgressBoxLocation
        {
            NONE = 0,
            UPPER_LEFT = 1,
            UPPER_RIGHT = 2,
            LOWER_LEFT = 3,
            LOWER_RIGHT = 4
        }
        public ProgressBoxLocation progressBoxLocation = ProgressBoxLocation.LOWER_LEFT;
        public int progressBoxCamera = 1;
        public int progressBoxSize = 70;

        public bool packFrames = false;

        public enum FramePackingOrder
        {
            RGB, RBG, GRB, GBR, BGR, BRG
        }
        public FramePackingOrder packingOrder = FramePackingOrder.RGB;

        [Range(0.0f, 1.0f)]
        public float packingFraction1 = 0.0f;
        [Range(0.0f, 1.0f)]
        public float packingFraction2 = 0.3f;
        [Range(0.0f, 1.0f)]
        public float packingFraction3 = 0.5f;
        private const int NUM_FRAMES_TO_PACK = 3;

#if UNITY_EDITOR
        [MenuItem("File/Build and Make Adjoining-Displays Shortcut")]
        public static void PerformBuild()
        {
            Janelia.AdjoiningDisplaysCameraBuilder.PerformBuild();
        }
#endif

#if UNITY_EDITOR
        // The following three functions are part of the complicated pattern necessary for
        // a leftDisplayIndex value set in the Inspector to be acessible by AdjoiningDisplaysCameraBuilder
        // at build time, when this value isu sed as an argument in the Windows shortcut file.

        private void OnValidate()
        {
            leftDisplayIndexStatic = leftDisplayIndex;
        }

        private static int GetMonitorIndex()
        {
            return leftDisplayIndexStatic;
        }

        [InitializeOnLoadMethod]
        public static void SetupDelegate()
        {
            Janelia.AdjoiningDisplaysCameraBuilder.getMonitorIndexDelegate = GetMonitorIndex;
        }
#endif

        private void Start()
        {
            Camera camera = GetComponent<Camera>();
            if (camera.targetDisplay != 0)
            {
                Debug.LogWarning("AdjoiningDisplaysCamera: attached camera should have 'Target Display' set to 'Display 1'");
            }

            // Don't render anything with this camera, as OnRenderImage() will completely replace
            // its image with the concatenation of the displayCamera images.
            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.Nothing;

            // The final "false" is important, to turn off full-screen display, so an extra wide image will
            // spill over onto other displays that are adjacent in the Windows extended desktop.
            Screen.SetResolution(displayWidth * displayCameras.Count(), displayHeight, false);

            if (SetupPackingSubject())
            {
                SetupPackingSubjectCopies();
                SetupPackingMaterial();

                foreach (Camera displayCamera in displayCameras)
                {
                    SetupRenderTextures(displayCamera);
                }

                SaveUpdateForPacking();
                SaveUpdateForPacking();
            }
        }

        private void Update()
        {
#if SUPPORT_KEYBOARD_SHORTCUTS
            if (Input.GetKeyDown("m"))
            {
                mirror = !mirror;
            }
            if (Input.GetKeyDown("p"))
            {
                // Cycle through the enum values.
                int n = Enum.GetNames(typeof(ProgressBoxLocation)).Length;
                progressBoxLocation = (ProgressBoxLocation)(((int)progressBoxLocation + 1) % n);
            }
            if (Input.GetKeyDown("c"))
            {
                // Cycle through the cameras.
                progressBoxCamera = (progressBoxCamera + 1) % displayCameras.Count();
            }
#endif
        }

        private void LateUpdate()
        {
            SaveUpdateForPacking();
            UpdatePackingSubjectCopies();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Needed only with 2021.3.5f1?  No longer a problem with 2021.3.19f1?
            if (Time.frameCount <= _latestFrameProcessed)
            {
                return;
            }
            _latestFrameProcessed = Time.frameCount;

            if (!DisplayCamerasAreValid())
            {
                return;
            }

            RenderDisplayCameras();

            // Do this every frame to suppress the warning:
            // "OnRenderImage() possibly didn't write anything to the destination texture!"
            Graphics.SetRenderTarget(destination);

            // For some reason, this extra, explicit clearing is needed when the background is not the sky box.
            Camera camera = displayCameras.FirstOrDefault(cam => cam.clearFlags == CameraClearFlags.SolidColor);
            if (camera != null)
            {
                GL.Clear(true, true, camera.backgroundColor);
            }

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, displayWidth * displayCameras.Count(), displayHeight, 0);

            DrawFromDisplayCameraTextures();
            DrawProgressBox();

            GL.PopMatrix();
        }

        private bool SetupPackingSubject()
        {
            if (packFrames)
            {
                // Find a common ancestor of all the cameras, and use it as the subject
                // whose pose (position, rotation) will be interpolated back to the 
                // previous frame's values for packing intermediate frames.

                _packingSubject = null;
                List<Transform> parents = new List<Transform>();
                foreach (Camera displayCamera in displayCameras)
                {
                    if (displayCamera.transform.parent == null)
                    {
                        Debug.Log("Frame packing requires that display camera '" + displayCamera.name + "' have a parent.");
                        return false;
                    }
                    parents.Add(displayCamera.transform.parent);
                }
                List<Transform> currents;
                do
                {
                    currents = parents;
                    Transform match = null;
                    foreach (Transform transform in currents)
                    {
                        if (match == null)
                        {
                            match = transform;
                        }
                        else if (transform != match)
                        {
                            match = null;
                            break;
                        }
                    }
                    if (match != null)
                    {
                        _packingSubject = match.gameObject;
                        Debug.Log("Found packing subject '" + _packingSubject.name + "'");
                        break;
                    }
                    else
                    {
                        parents = new List<Transform>();
                        foreach (Transform current in currents)
                        {
                            if (current.parent != null)
                            {
                                parents.Add(current.parent);
                            }
                        }
                    }
                }
                while (currents.Count == parents.Count);
            }
            return true;
        }

        private bool DoPackFrames()
        {
            return (packFrames && (_packingSubject != null));
        }

        private void SetupPackingSubjectCopies()
        {
            if (DoPackFrames())
            {
                for (int i = 1; i < NUM_FRAMES_TO_PACK; ++i)
                {
                    GameObject copy = Instantiate(_packingSubject);
                    copy.name = _packingSubject.name + "-packFramesCopy" + i;
                    _packingSubjectCopies.Add(copy);
                }
            }
        }

        private void SetupPackingMaterial()
        {
            if (DoPackFrames())
            {
                // For the shader `org.janelia.camera-utilities/Assets/Resources/PackRGB.shader`
                // the name to use when loading is just `PackRGB`.
                Shader shader = Resources.Load("PackRGB", typeof(Shader)) as Shader;
                if (shader != null)
                {
                    _packingMaterial = new Material(shader);
                }
                else
                {
                    Debug.Log("Could not load PackRGB.shader");
                }
            }
        }

        private void SetupRenderTextures(Camera camera)
        {
            if (camera != null)
            {
                camera.enabled = false;
                if (DoPackFrames())
                {
                    if (!_packingRenderTextures.ContainsKey(camera.name))
                    {
                        _packingRenderTextures[camera.name] = new RenderTexture[NUM_FRAMES_TO_PACK];
                        for (int i = 0; i < NUM_FRAMES_TO_PACK; ++i)
                        {
                            _packingRenderTextures[camera.name][i] = new RenderTexture(displayWidth, displayHeight, 24, RenderTextureFormat.ARGB32);
                        }
                    }
                }
                else
                {
                    if (camera.targetTexture == null)
                    {
                        camera.targetTexture = new RenderTexture(displayWidth, displayHeight, 24, RenderTextureFormat.ARGB32);
                    }
                }
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

#if UNITY_EDITOR
            // The texture must be saved as an asset or the reference to the texture will be lost.
            // https://forum.unity.com/threads/unity-randomly-loses-references.356082/

            string name = even ? "TmpProgressTextureEven.asset" : "TmpProgressTextureOdd.asset";
            AssetDatabase.CreateAsset(result, "Assets/" + name);
#endif

            return result;
        }

        private void SaveUpdateForPacking()
        {
            if (DoPackFrames())
            {
                _packingSubjectPosition[0] = _packingSubjectPosition[1];
                _packingSubjectEulerAngles[0] = _packingSubjectEulerAngles[1];
                _packingSubjectPosition[1] = _packingSubject.transform.position;
                _packingSubjectEulerAngles[1] = _packingSubject.transform.eulerAngles;
            }
        }

        // The argument is a fraction, 0 <= t <= 1, not an actual time.
        private Vector3 InterpolatePackingSubjectPosition(float t)
        {
            t = Mathf.Clamp(t, 0, 1);
            Vector3 v0 = _packingSubjectPosition[0];
            Vector3 v1 = _packingSubjectPosition[1];
            return v0 + t * (v1 - v0);
        }

        // The argument is a fraction, 0 <= t <= 1, not an actual time.
        private Vector3 InterpolatePackingSubjectEulerAngles(float t)
        {
            t = Mathf.Clamp(t, 0, 1);
            Vector3 v0 = _packingSubjectEulerAngles[0];
            Vector3 v1 = _packingSubjectEulerAngles[1];
            float x = Mathf.LerpAngle(v0[0], v1[0], t);
            float y = Mathf.LerpAngle(v0[1], v1[1], t);
            float z = Mathf.LerpAngle(v0[2], v1[2], t);
            return new Vector3(x, y, z);
        }

        private void UpdatePackingSubjectCopies()
        {
            if (DoPackFrames())
            {
                float[] ts = new float[]{ packingFraction1, packingFraction2, packingFraction3 };

                // Do NUM_FRAMES_TO_PACK - 1, as _packingSubject will be the last one,
                // with the pose of the current frame.
                for (int i = 0; i < NUM_FRAMES_TO_PACK - 1; ++i)
                {
                    GameObject copy = _packingSubjectCopies[i];
                    float t = ts[i];
                    copy.transform.position = InterpolatePackingSubjectPosition(t);
                    copy.transform.eulerAngles = InterpolatePackingSubjectEulerAngles(t);
                }
            }
        }

        private bool DisplayCamerasAreValid()
        {
            bool result = true;
            for (int i = 0; i < displayCameras.Count(); i++)
            {
                if (displayCameras[i] == null)
                {
                    Debug.LogWarning("AdjoiningDisplaysCamera: displayCameras[" + i + "] must be specified");
                    result = false;
                }
            }
            return result;
        }

        private void RenderDisplayCameras()
        {
            if (DoPackFrames())
            {
                for (int i = 0; i < NUM_FRAMES_TO_PACK; ++i)
                {
                    GameObject subject = (i < NUM_FRAMES_TO_PACK - 1) ? _packingSubjectCopies[i] : _packingSubject;
                    Camera[] cameras = subject.GetComponentsInChildren<Camera>();
                    foreach (Camera camera in cameras)
                    {
                        camera.targetTexture = _packingRenderTextures[camera.name][i];
                        camera.Render();
                    }
                }
            }
            else
            {
                foreach (Camera displayCamera in displayCameras)
                {
                    displayCamera.Render();
                }
            }
        }

        private Texture PackingRenderTextureRed(int cameraIndex)
        {
            int i;
            switch (packingOrder)
            {
                case FramePackingOrder.RGB:
                case FramePackingOrder.RBG:
                    i = 0;
                    break;
                case FramePackingOrder.GRB:
                case FramePackingOrder.BRG:
                    i = 1;
                    break;
                default:
                    i = 2;
                    break;
            }
            return _packingRenderTextures[displayCameras[cameraIndex].name][i];
        }

        private Texture PackingRenderTextureGreen(int cameraIndex)
        {
            int i;
            switch (packingOrder)
            {
                case FramePackingOrder.GRB:
                case FramePackingOrder.GBR:
                    i = 0;
                    break;
                case FramePackingOrder.RGB:
                case FramePackingOrder.BGR:
                    i = 1;
                    break;
                default:
                    i = 2;
                    break;
            }
            return _packingRenderTextures[displayCameras[cameraIndex].name][i];
        }

        private Texture PackingRenderTextureBlue(int cameraIndex)
        {
            int i;
            switch (packingOrder)
            {
                case FramePackingOrder.BRG:
                case FramePackingOrder.BGR:
                    i = 0;
                    break;
                case FramePackingOrder.RBG:
                case FramePackingOrder.GBR:
                    i = 1;
                    break;
                default:
                    i = 2;
                    break;
            }
            return _packingRenderTextures[displayCameras[cameraIndex].name][i];
        }

        private void DrawFromDisplayCameraTextures()
        {
            for (int i = 0; i < displayCameras.Count(); i++)
            {
                float x = mirror ? displayWidth * (i + 1) : displayWidth * i;
                float w = mirror ? -displayWidth : displayWidth;
                if (DoPackFrames())
                {
                    Texture texR = PackingRenderTextureRed(i);
                    Texture texG = PackingRenderTextureGreen(i);
                    Texture texB = PackingRenderTextureBlue(i);
                    _packingMaterial.SetTexture("_TexR", texR);
                    _packingMaterial.SetTexture("_TexG", texG);
                    _packingMaterial.SetTexture("_TexB", texB);
                    Graphics.DrawTexture(new Rect(x, 0, w, displayHeight), texR, _packingMaterial);
                }
                else
                {
                    Graphics.DrawTexture(new Rect(x, 0, w, displayHeight), displayCameras[i].targetTexture);
                }
            }
        }

        private void DrawProgressBox()
        {
            if (progressBoxLocation != ProgressBoxLocation.NONE)
            {
                InitializeProgressTexturesIfNeeded(progressBoxSize);

                progressBoxCamera = Math.Max(0, Math.Min(progressBoxCamera, displayCameras.Count() - 1));
                float x, y;
                switch (progressBoxLocation)
                {
                    case ProgressBoxLocation.UPPER_LEFT:
                        x = progressBoxCamera * displayWidth;
                        y = 0;
                        break;
                    case ProgressBoxLocation.UPPER_RIGHT:
                        x = (progressBoxCamera + 1) * displayWidth - progressBoxSize;
                        y = 0;
                        break;
                    case ProgressBoxLocation.LOWER_LEFT:
                        x = progressBoxCamera * displayWidth;
                        y = displayHeight - progressBoxSize;
                        break;
                    default:
                        x = (progressBoxCamera + 1) * displayWidth - progressBoxSize;
                        y = displayHeight - progressBoxSize;
                        break;
                }

                if (DoPackFrames())
                {
                    bool even = (Time.frameCount % 2 == 0);
                    Texture2D progressTexR = even ? _progressTextureEven : _progressTextureOdd;
                    Texture2D progressTexG = even ? _progressTextureOdd  : _progressTextureEven;
                    Texture2D progressTexB = even ? _progressTextureEven : _progressTextureOdd;
                    _packingMaterial.SetTexture("_TexR", progressTexR);
                    _packingMaterial.SetTexture("_TexG", progressTexG);
                    _packingMaterial.SetTexture("_TexB", progressTexB);
                    Graphics.DrawTexture(new Rect(x, y, progressBoxSize, progressBoxSize), progressTexR, _packingMaterial);
                }
                else
                {
                    Texture2D progressTex = (Time.frameCount % 2 == 0) ? _progressTextureEven : _progressTextureOdd;
                    Graphics.DrawTexture(new Rect(x, y, progressBoxSize, progressBoxSize), progressTex);
                }
            }
        }

        private Texture2D _progressTextureEven;
        private Texture2D _progressTextureOdd;

        private int _latestFrameProcessed = -1;

        private Dictionary<string, RenderTexture[]> _packingRenderTextures = new Dictionary<string, RenderTexture[]>();

        private Material _packingMaterial;

        private GameObject _packingSubject = null;
        private List<GameObject> _packingSubjectCopies = new List<GameObject>();

        private Vector3[] _packingSubjectPosition = new Vector3[2] { Vector3.zero, Vector3.zero };
        private Vector3[] _packingSubjectEulerAngles = new Vector3[2] { Vector3.zero, Vector3.zero };
    }
}
