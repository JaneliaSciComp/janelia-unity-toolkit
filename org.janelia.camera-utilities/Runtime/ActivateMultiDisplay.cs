using UnityEngine;

// If multiple external displays are connected, activates the so they can be used by
// Unity cameras with the `targetDisplay` field set to a value greater than 1.

namespace Janelia
{
    public class ActivateMultiDisplay : MonoBehaviour
    {
        public void Start()
        {
            if (Display.displays.Length > 1)
            {
                Debug.Log ("Activating multi-display for " + Display.displays.Length + " detected displays.");

                // `Display.displays[0]` is the primary (console) display, and always activated by default.
                // So start at display index 1.
                foreach (Display display in Display.displays)
                {
                    display.Activate();
                }
            }
            else
            {
                Debug.Log("Only 1 display detected; multi-display not activated.");

                Camera[] cameras = FindObjectsOfType<Camera>();
                foreach (Camera camera in cameras)
                {
                    // If one of the cameras targets the one display that is available,
                    // then there is nothing else to do.
                    // And remember that the `targetDisplay` value is 0 based.
                    if (camera.targetDisplay == 0)
                    {
                        Debug.Log("Camera '" + camera.name + "' should show on display 1");
                        return;
                    }
                }

                // Otherwise, as a convenience, temporarily retarget one of the cameras so
                // something is visible on the one available display.
                foreach (Camera camera in cameras)
                {
                    // Retarget the main camera if found.
                    if (camera.gameObject.tag == "Main Camera")
                    {
                        Debug.Log("Temporarily resetting main camera '" + camera.name + "' to display 1");
                        camera.targetDisplay = 0;
                        return;
                    }
                }
                foreach (Camera camera in cameras)
                {
                    // Otherwise retarget the camera currently using display 2.
                    if (camera.targetDisplay == 1)
                    {
                        Debug.Log("Temporarily resetting camera '" + camera.name + "' to display 1");
                        camera.targetDisplay = 0;
                    }
                }
            }
        }
    }
}