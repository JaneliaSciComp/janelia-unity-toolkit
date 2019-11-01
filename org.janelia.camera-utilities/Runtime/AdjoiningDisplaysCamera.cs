// Makes the attached camera display the images of N other cameras, adjoined with each
// to form a kind of panorama.

// Useful as a work-around for the poor performance of Unity's "multidisplay" capability, 
// for building a stand-alone game application (not running in the Unity editor) that has
// multiple cameras each filling an external display.  To use this work-around:
// 1. Use the Windows "Display" settings to arrange the Windows extended desktop with
// the external displays adjacent to each other horizontally (possibly rotated by
// having "Portrait" enabled).
// 2. Set the "displayWidth" and "displayHeight" fields of this script to match the
// resoluton of the external displays.
// 3. Set the camera attached to this script to have a "Target Display" of "Display 1".
// 4. In the Unity "Build Settings", in the "Player Settings", set "Fullscreen Mode" to
// "Windowed" and "Display Resolution Dialog" to "Enabled".
// 5. Build the game.
// 6. Run the executable with the "-popupwindow" commandline option, to eliminate window
// borders.  One way to do so is to make a Windows shortcut to the .exe file, right-click
// on the shortcut, choose "Propertis", and on the "Shortcut" tab's "Target" field, add
// "-popupwindow" after the .exe name.
// 7. When the "Display Resolution Dialog" appears before the game actually starts, use
// "Select Monitor" to choose the leftmost external display from the Windows extended 
// desktop.

#define SUPPORT_KEYBOARD_SHORTCUTS

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Janelia
{
    public class AdjoiningDisplaysCamera : MonoBehaviour
    {
        public Camera[] displayCameras = new Camera[4];

        public int displayWidth = 480;
        public int displayHeight = 854;

        public bool mirror = false;

        public enum ProgressBoxLocation
        {
            NONE = 0,
            UPPER_LEFT = 1,
            UPPER_RIGHT = 2,
            LOWER_LEFT = 3,
            LOWER_RIGHT = 4
        }
        public ProgressBoxLocation progressBoxLocation = ProgressBoxLocation.NONE;
        public int progressBoxCamera = 0;
        public int progressBoxSize = 50;

        private Texture2D _progressTextureEven;
        private Texture2D _progressTextureOdd;

        private int _frameCounter = 0;

        private void SetupCamera(Camera camera)
        {
            if (camera != null)
            {
                camera.enabled = false;
                if (camera.targetTexture == null)
                {
                    camera.targetTexture = new RenderTexture(displayWidth, displayHeight, 24, RenderTextureFormat.ARGB32);
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

        private void Start()
        {
            Camera camera = GetComponent<Camera>();
            if (camera.targetDisplay != 0)
            {
                Debug.LogWarning("AdjoiningDisplaysCamera: attached camera should have 'Target Display' set to 'Display 1'");
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.blue;

            // The final "false" is important, to turn off full-screen display, so an extra wide image will
            // spill over onto other displays that are adjacent in the Windows extended desktop.
            Screen.SetResolution(displayWidth * displayCameras.Count(), displayHeight, false);

            foreach (Camera displayCamera in displayCameras)
            {
                SetupCamera(displayCamera);
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

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // It should be that source != null but destination == null.

            bool keepGoing = true;
            for (int i = 0; i < displayCameras.Count(); i++)
            {
                if (displayCameras[i] == null)
                {
                    Debug.LogWarning("AdjoiningDisplaysCamera: displayCameras[" + i + "] must be specified");
                    keepGoing = false;
                }
            }
            if (!keepGoing)
            {
                return;
            }

            foreach (Camera displayCamera in displayCameras)
            {
                displayCamera.Render();
            }

            // Do this every frame to suppress the warning:
            // "OnRenderImage() possibly didn't write anything to the destination texture!"
            Graphics.SetRenderTarget(destination);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, displayWidth * displayCameras.Count(), displayHeight, 0);

            for (int i = 0; i < displayCameras.Count(); i++)
            {
                float x = mirror ? displayWidth * (i + 1) : displayWidth * i;
                float w = mirror ? -displayWidth : displayWidth;
                Graphics.DrawTexture(new Rect(x, 0, w, displayHeight), displayCameras[i].targetTexture);
            }

            if (progressBoxLocation != ProgressBoxLocation.NONE)
            {
                InitializeProgressTexturesIfNeeded(progressBoxSize);
                Texture2D progressTexture = (_frameCounter % 2 == 0) ? _progressTextureEven : _progressTextureOdd;

                progressBoxCamera = Math.Max(0, Math.Min(progressBoxCamera, displayCameras.Count() - 1));
                float x, y;
                switch (progressBoxLocation)
                {
                    case ProgressBoxLocation.UPPER_LEFT:
                        x = progressBoxCamera * displayWidth;
                        y = 0;
                        break;
                    case ProgressBoxLocation.UPPER_RIGHT:
                        x = (progressBoxCamera + 1) * displayWidth - progressTexture.width;
                        y = 0;
                        break;
                    case ProgressBoxLocation.LOWER_LEFT:
                        x = progressBoxCamera * displayWidth;
                        y = displayHeight - progressTexture.height;
                        break;
                    default:
                        x = (progressBoxCamera + 1) * displayWidth - progressTexture.width;
                        y = displayHeight - progressTexture.height;
                        break;
                }
                Graphics.DrawTexture(new Rect(x, y, progressTexture.width, progressTexture.height), progressTexture);
            }

            GL.PopMatrix();
            _frameCounter++;
        }
    }
}
