using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sets up three projectors outside a cylindrical display surface, back projecting the panorama that is visible
// to a freely-moving subject inside the cylinder. 
// * This script should be added to the `GameObject` representing the subject. 
// * That `GameObject` should have the Main Camera as a child, with its `transform.position.y` giving the height 
//   of the subject's eyes above the ground. 
// The code to track the motion of the subject (e.g, using computer vision) must be implemented separately.

public class ExampleUsingPanoramicDisplayCamera : MonoBehaviour
{
    public float cylinderRadius = 4;
    public int projectorCount = 3;
    public float projectorFovHoriz = 40.0f;
    public int projectorWidth = 640;
    public int projectorHeight = 480;
    public Color edgeArtifactColor;

    public void Start()
    {
        Transform mainCameraTransform = transform.Find("Main Camera");
        if (mainCameraTransform != null)
        {
            // Set up the box of source cameras, pointing north, south, east, west, down and up around the subject.
            // (The final `true` argument chooses to include the optional camera pointing up).  The panorama will
            // be constructed by remapping the pixels from these source cameras.

            Vector3 offset = mainCameraTransform.position;
            float near = 0.01f;
            float far = 500.0f;
            List<GameObject> cameras = Janelia.CameraUtilities.SetupPanoramaSourceCameras(transform, offset, near, far, true);

            // Give the Main Camera the script, `Janelia.PanoramicDisplayCamera`, that constructs the panorama from
            // the source camera images.

            GameObject mainCamera = mainCameraTransform.gameObject;
            _displayCamera = mainCamera.AddComponent<Janelia.PanoramicDisplayCamera>() as Janelia.PanoramicDisplayCamera;

            // Give that script the source cameras themselves.

            for (int i = 0; i < 6; ++i)
            {
                Camera camera = cameras[i].GetComponent<Camera>() as Camera;
                _displayCamera.sourceCameras[i] = camera;
            }

            // It's often the case that the result looks better if the source cameras have more resolution than
            // the projectors that will display the panorama, so set the `sourceWidth` field (width only
            // because the source images are square).

            _displayCamera.sourceWidth = projectorWidth * 2;

            // Use the standard solution to compensate for the slight loss of brightness near the borders between
            // projectors, due to the way the cylinder curves away from the projectors at these border. The solution
            // involves creating a compensating loss away from the borders, with the maximum compensating loss
            // occuring where the cylinder is closest to the projector (at the center of the projector image). The
            // `SetupCylinderProjectorEdgeBrightener` function computes a key part of this compensation using
            // Lambert's cosine law, which states that the brightness varies as the dot product of light ray from
            // the projector and the surface normal vector where the ray hits the cylinder, which varies as the
            // cosine of the angle between those vectors. The material properties of the display surface affect
            // the magnitude of the brightness loss, and the details can be difficult to analyze, so there is a
            // user-controllable scaling factor `PanoramicDisplayCamera.surfaceMaskScale`: setting it to 0 disables
            // compensation, and setting to 1 gives a large compensation.

            Janelia.CameraUtilities.SetupCylinderProjectorMaskDelegate brightener = 
                Janelia.CameraUtilities.SetupCylinderProjectorEdgeBrightener;

            // Use the standard solution to correct for color artifacts near the borders between projectors, due
            // again to the way the cylinder curves at these borders. The `SetupCylinderProjectorEdgeColorCorrector`
            // function also uses Lambert's cosine law to determine where to color correction is needed.  The color
            // that is corrected for is specified as `SetupCylinderProjectorDelegateParams.edgeArtifactColor`. There
            // is also a user-controlled scaling factor, `PanoramicDisplayCamera.surfaceColorCorrectionScale`: setting
            // it to 0 disables color correction, and setting it to 1 gives a large compensation.

            Janelia.CameraUtilities.SetupCylinderProjectorDelegateParams.edgeArtifactColor = edgeArtifactColor;

            Janelia.CameraUtilities.SetupCylinderProjectorColorCorrectionDelegate colorCorrector =
                Janelia.CameraUtilities.SetupCylinderProjectorEdgeColorCorrector;

            // An example of recovering the scaling factors that were set interactively in a previous run
            // of the executable (in the `Update` function, below).

            if (PlayerPrefs.HasKey(PLAYER_PREF_KEY_SURFACE_MASK_SCALE))
            {
                _displayCamera.surfaceMaskScale = PlayerPrefs.GetFloat(PLAYER_PREF_KEY_SURFACE_MASK_SCALE);
            }
            if (PlayerPrefs.HasKey(PLAYER_PREF_KEY_SURFACE_COLOR_CORRECTION_SCALE))
            {
                _displayCamera.surfaceColorCorrectionScale = 
                    PlayerPrefs.GetFloat(PLAYER_PREF_KEY_SURFACE_COLOR_CORRECTION_SCALE);
            }

            // Precompute the data that will be used at runtime to map the source images to the cylinder on the GPU.
            // The `out` values prefixed `surfaceData` are textures with size `dataWidth` by `dataHeight`. The
            // `surfaceDataX`, `surfaceDataY` and `surfaceDataZ` texture describe the 3D point on the cylinder where
            // each projector pixel appears. The `surfaceDataMask` texture applies the brightness compensation described
            // above, using `brightener` which here is the standard compensation solution, described above. The 
            // `surfaceDataColorCorrection` texture applies color correction as described above, using `colorCorrector`
            // which here is the standard correction solution, described above. Also computed is the horizontal distance
            // to each projector from the center of the cylinder (`projectorDistanceXZ`) and the height of the cylinder
            // (`cylinderHeight`) because these values are constrained by the cylinder radius (`cylinderRadius`) and the
            // projectors' fields of view (`projectorFovHoriz`) assuming that there should be no overlap or gaps in the
            // images projected onto the cylinder.

            float[] surfaceDataX, surfaceDataY, surfaceDataZ;
            byte[] surfaceDataMask;
            Color[] surfaceDataColorCorrection;
            float angle0Deg = transform.localEulerAngles.y;
            int dataWidth = projectorCount * projectorWidth;
            int dataHeight = projectorHeight;
            Vector3 cylinderPosition = Vector3.zero;
            float projectorDistanceXZ;
            float cylinderHeight;
            Janelia.CameraUtilities.SetupCylinderProjectorSurface(cylinderPosition, cylinderRadius, angle0Deg,
                projectorCount, projectorFovHoriz, projectorWidth, projectorHeight, out projectorDistanceXZ, out cylinderHeight,
                out surfaceDataX, out surfaceDataY, out surfaceDataZ, out surfaceDataMask, out surfaceDataColorCorrection,
                brightener, colorCorrector);

            Debug.Log("Please make the cylinder " + cylinderHeight + " units tall.");
            Debug.Log("Please place each of the " + projectorCount + " projector(s) " + projectorDistanceXZ + 
                " units from the cylinder center (horizontally).");

            // Give that precomputed data to the script that will generate the panorama at runtime. This call would be necessary
            // for display surfaces other than a cylinder, but the values for the `surfaceData` textures would be different.
            
            _displayCamera.SetDisplaySurfaceData(dataWidth, dataHeight, surfaceDataX, surfaceDataY, surfaceDataZ, surfaceDataMask,
                surfaceDataColorCorrection);
        }
    }

    public void Update()
    {
        // An example of interactive controls for the brightness compensation and color correction, which could be
        // used to get the proper correction before the experiment begins.

        bool changed = false;
        float value = 0.0f;
        bool shifted = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKey("0"))
        {
            value = 0.0f;
            changed = true;
        }
        if (Input.GetKey("5"))
        {
            value = 0.5f;
            changed = true;
        }
        if (Input.GetKey("9"))
        {
            value = 0.9f;
            changed = true;
        }
        if (changed)
        {
                if (shifted)
                {
                    _displayCamera.surfaceColorCorrectionScale = value;
                    PlayerPrefs.SetFloat(PLAYER_PREF_KEY_SURFACE_COLOR_CORRECTION_SCALE, _displayCamera.surfaceColorCorrectionScale);
                }
                else
                {
                    _displayCamera.surfaceMaskScale = value;
                    PlayerPrefs.SetFloat(PLAYER_PREF_KEY_SURFACE_MASK_SCALE, _displayCamera.surfaceMaskScale);
                }
        }
    }

    private Janelia.PanoramicDisplayCamera _displayCamera;
    private const string PLAYER_PREF_KEY_SURFACE_MASK_SCALE = "PanoramicDisplayCamera.SurfaceMaskScale";
    private const string PLAYER_PREF_KEY_SURFACE_COLOR_CORRECTION_SCALE = "PanoramicDisplayCamera.surfaceColorCorrectionScale";
}
