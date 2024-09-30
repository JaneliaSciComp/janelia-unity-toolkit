using UnityEngine;

namespace Janelia
{
    [ExecuteAlways]
    public class OffAxisPerspectiveCamera : MonoBehaviour
    {
        public GameObject screen;
        private Camera _camera;

        public void Start()
        {
            _camera = GetComponent<Camera>();
        }

        public void LateUpdate()
        {
            // This function sets _camera's projectionMatrix and worldToCameraMatrix properties.
            // If the worldToCameraMatrix property is ever set, it must be set at every frame, as
            // Unity no longer updates it when the parent changes.  The projectonMatrix need not be
            // set at every frame, but it is simpler to do so since many of the intermediate calculations
            // are shared.

            if (screen != null)
            {
                CameraUtilities.SetOffAxisPerspectiveMatrices(_camera, screen);
            }
        }
    }
}
