using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace Janelia
{
    public static class CameraUtilities
    {
        // Based on Robert Kooima's "Generalized Perspective Projection":
        // http://160592857366.free.fr/joe/ebooks/ShareData/Generalized%20Perspective%20Projection.pdf

        // The projection screen is assumed to be a quad, which started with unit size (i.e., from -0.5 to 0.5)
        // before being scaled to the proper size, with its XY plane being normal to the camera view vector.

        public static void SetOffAxisPerspectiveMatrices(Camera camera, GameObject screen)
        {
            if ((camera != null) && (screen != null))
            {
                // The screen's lower-left corner.
                Vector3 pa = screen.transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0.0f));
                // The screen's lower-right corner.
                Vector3 pb = screen.transform.TransformPoint(new Vector3(0.5f, -0.5f, 0.0f));
                // The screen's upper-left corner.
                Vector3 pc = screen.transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0.0f));
                // The eye (camera) position.
                Vector3 pe = camera.transform.position;

                // The right axis of the screen.
                Vector3 vr = Vector3.Normalize(pb - pa);
                // The up axis of the screen.
                Vector3 vu = Vector3.Normalize(pc - pa);
                // From pe (eye) to va (lower-left corner).
                Vector3 va = pa - pe;
                // Screen normal.  Note that due to Unity's left-handed coordinate system,
                // we must use vu cross vr instead of vr cross vu as presented by Kooima.
                Vector3 vn = Vector3.Normalize(Vector3.Cross(vu, vr));

                // The screen normal points out, away from the front of the screen.
                // So its direction should be opposite to the vector from pe to va.
                bool facingScreenBack = (Vector3.Dot(va, vn) > 0);
                if (facingScreenBack)
                {
                    // Flip left and right (i.e., negate X).
                    pa = screen.transform.TransformPoint(new Vector3(0.5f, -0.5f, 0.0f));
                    pb = screen.transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0.0f));
                    pc = screen.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0.0f));

                    // Update the screen axes.
                    vr = Vector3.Normalize(pb - pa);
                    vu = Vector3.Normalize(pc - pa);
                    va = pa - pe;
                    vn = -vn;
                }

                // From pe (eye) to pb (lower-right) and pc (upper-left).
                Vector3 vb = pb - pe;
                Vector3 vc = pc - pe;

                float d = -Vector3.Dot(vn, va);
                float n = camera.nearClipPlane;
                float f = camera.farClipPlane;
                float l = Vector3.Dot(vr, va) * n / d;
                float r = Vector3.Dot(vr, vb) * n / d;
                float b = Vector3.Dot(vu, va) * n / d;
                float t = Vector3.Dot(vu, vc) * n / d;

                Matrix4x4 pm = Matrix4x4.Frustum(l, r, b, t, n, f);

                Matrix4x4 rm = Matrix4x4.identity;
                rm.SetRow(0, vr);
                rm.SetRow(1, vu);
                rm.SetRow(2, vn);
                Matrix4x4 tm = Matrix4x4.Translate(-pe);

                camera.projectionMatrix = pm;
                camera.worldToCameraMatrix = rm * tm;
            }
        }

        // Sets up a cube of cameras in the north, south, east, west, down, and optionally up, directions around
        // the point that is the `parent` position plus the `offset`. These cameras are useful as the sources for
        // a the remapping performed by `PanoramicDisplayCamera`.

        public static List<GameObject> SetupPanoramaSourceCameras(Transform parent, Vector3 offset, float near, float far, bool includeTop)
        {
            const int SIDE_COUNT = 4;
            float fovDeg = 360.0f / SIDE_COUNT;
            float aspect = 1.0f;

            string name;
            Vector3 cameraEuler;
            Vector3 cameraPosition = offset;
            GameObject camera;

            List<GameObject> result = new List<GameObject>();

            float cameraAngleY = 0;
            int targetDisplay = 2;
            for (int i = 0; i < SIDE_COUNT; ++i)
            {
                name = "SourceCamera" + i.ToString() + "Side";
                cameraEuler = new Vector3(0, cameraAngleY, 0);
                camera = CreateCamera(name, fovDeg, aspect, near, far, targetDisplay++, cameraPosition, cameraEuler, parent);
                result.Add(camera);
                cameraAngleY += fovDeg;
            }

            name = "SourceCamera" + SIDE_COUNT.ToString() + "Bottom";
            cameraEuler = new Vector3(90, 0, 0);
            camera = CreateCamera(name, fovDeg, aspect, near, far, targetDisplay++, cameraPosition, cameraEuler, parent);
            result.Add(camera);

            if (includeTop)
            {
                name = "SourceCamera" + (SIDE_COUNT + 1).ToString() + "Top";
                cameraEuler = new Vector3(-90, 0, 0);
                camera = CreateCamera(name, fovDeg, aspect, near, far, targetDisplay++, cameraPosition, cameraEuler, parent);
                result.Add(camera);
            }

            return result;
        }

        // Computes the 3D positions on the cylindrical screen where the projector's pixels will appear.
        // These positions are needed as input to the `PanoramicDisplayCamera`, which uses them to remap a rendering
        // of the scene on a box around the current position. The array of pixels has size `projWidth` by `projHeight`.
        // The cylinder is centered at `surfPos` and has radius `surfRadius`. Each of the `projCount` projectors has a
        // field of view angle (in degrees) `projFovHorizDeg`. Given this projector count and FOV, full coverage of
        // the cyindrical screen constrains some other parameters: the cylinder height (returned as `surfHeight`) and
        // the distance of the projectors from the screen center in the XZ plane (`projDistXZ`).
        // The value of `angle0Deg` should match any Y rotation applied to the parent of the camera with the
        // `PanoramicDisplayCamera` script (i.e., the parent's `localEulerAngles.y`).
        // The optional `SetupCylinderProjectorMaskDelegate` can define a function to set the `dataMask` value
        // at each pixel (for example, so the mask will correct for the projected brightness being a bit lower
        // at the projectors' edges, where the cylinder curves away from the projectors); see the declarations
        // of `SetupCylinderProjectorMaskDelegate`, etc., later in this file. If this delegate is null then
        // the mask is 255 everywhere, meaning it has no effect.
        // The optional `SetupCylinderProjectorColorCorrectionDelegate` can define a function to set the
        // `dataColorCorrection` value at each pixel (for example, to correct for color anomalies at the projectors'
        // edges); see the declarations of `SetupCylinderProjectorColorCorrectionDelegate`, etc., later in this file.
        // If this delegate is null then the color correction is `Color.white` everywhere, meaning that there is
        // no color correction.

        public static void SetupCylinderProjectorSurface(Vector3 surfPos, float surfRadius, float angle0Deg,
            int projCount, float projFovHorizDeg, int projWidth, int projHeight,
            out float projDistXZ, out float surfHeight, out float[] dataX, out float[] dataY, out float[] dataZ, 
            out byte[] dataMask, out Color[] dataColorCorrection,
            SetupCylinderProjectorMaskDelegate maskDelegate = null,
            SetupCylinderProjectorColorCorrectionDelegate colorCorrectionDelegate = null)
        {
            long time0 = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (!CylinderComputeParameters(surfRadius, projCount, projFovHorizDeg, projWidth, projHeight, out projDistXZ, out surfHeight))
            {
                projDistXZ = surfHeight = 0;
                dataX = dataY = dataZ = null;
                dataMask = null;
                dataColorCorrection = null;
                return;
            }
            Debug.Log("SetupCylinderProjectorSurface computed projector distance (XZ) " + projDistXZ + ", cylinder height " + surfHeight);

            int dataWidth = projCount * projWidth;
            int dataHeight = projHeight;
            dataX = new float[dataWidth * dataHeight];
            dataY = new float[dataWidth * dataHeight];
            dataZ = new float[dataWidth * dataHeight];
            dataMask = new byte[dataWidth * dataHeight];
            dataColorCorrection = new Color[dataWidth * dataHeight];

            bool[] reportedProjPos = new bool[3]{ false, false, false };

            int iData = 0;
            for (int iHeight = 0; iHeight < dataHeight; ++iHeight)
            {
                for (int iWidth = 0; iWidth < dataWidth; ++iWidth)
                {
                    Ray projRay = CylinderProjectorRay(iWidth, iHeight, surfPos, angle0Deg, surfRadius, projCount, projDistXZ, projFovHorizDeg, 
                        projWidth, projHeight, reportedProjPos);
                    Vector3 inter = CylinderRayIntersection(surfPos, surfRadius, surfHeight, projRay, projDistXZ);
                    dataX[iData] = inter.x;
                    dataY[iData] = inter.y;
                    dataZ[iData] = inter.z;
                    dataMask[iData] = 255;
                    dataColorCorrection[iData] = Color.white;

                    SetupCylinderProjectorDelegateParams p = new SetupCylinderProjectorDelegateParams()
                    {
                        iWidth = iWidth,
                        iHeight = iHeight,
                        dataWidth = dataWidth,
                        dataHeight = dataHeight,
                        projRay = projRay,
                        surfInter = inter,
                        surfRadius = surfRadius,
                        projCount = projCount,
                        projFovHorizDeg = projFovHorizDeg,
                        projWidth = projWidth,
                        projHeight = projHeight,
                        projDistXZ = projDistXZ               
                    };
                    if (maskDelegate != null)
                    {
                        dataMask[iData] = maskDelegate(p);
                    }
                    if (colorCorrectionDelegate != null)
                    {
                        dataColorCorrection[iData] = colorCorrectionDelegate(p);
                    }

                    ++iData;
                }
            }

            long time1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Debug.Log("SetupCylinderProjectorSurface took " + (time1 - time0) + " ms");
        }

        // A function conforming to this delegate's signature can be given to `SetupCylinderProjectorSurface`
        // to compute the mask texture that `PanoramicDisplayCamera` will use to adjust the color it computes
        // for each pixel.  The function takes the details of a single pixel, stored in an instance of the
        // `SetupCylinderProjectorDelegateParams` class, and returns the value of the mask texture at that
        // pixel. An example usage is the `SetupCylinderProjectorEdgeBrightener` function, below, which
        // effectively brightens the edges of each projector's image (which may appear dimmer due to the
        // cylinder curving away from the projector) by reducing the brightness of the pixels away from the edge.

        public delegate byte SetupCylinderProjectorMaskDelegate(SetupCylinderProjectorDelegateParams p);

        // A function conforming to this delegate's signature can be given to `SetupCylinderProjectorSurface`
        // to compute the color compensation texture that `PanoramicDisplayCamera` will use to further adjust
        // the color it computes for each pixel.  The function takes the details of a single pixel, stored in
        // an instance of the `SetupCylinderProjectorDelegateParams` class, and returns the color value of the
        // correction texture at that pixel. An example usage is the `SetupCylinderProjectorEdgeColorCorrector`
        // function, below, which removes an artifact color at the edges of each projector's image.

        public delegate Color SetupCylinderProjectorColorCorrectionDelegate(SetupCylinderProjectorDelegateParams p);

        // The details of the pixel being processed by the two delegates just defined.

        public struct SetupCylinderProjectorDelegateParams
        {
            public int iWidth;
            public int iHeight;
            public int dataWidth;
            public int dataHeight;
            public Ray projRay;
            public Vector3 surfInter;
            public float surfRadius;
            // There is no field for the surface center position because it is `Vector3.zero`.
            public int projCount;
            public float projFovHorizDeg;
            public int projWidth;
            public int projHeight;
            public float projDistXZ;

            public static Color edgeArtifactColor;
        };

        // Sets up one approach to compensating for the loss of brightness at the edges of the images 
        // projected onto a cylinder (i.e., the borders between projectors) due to cylinder curving away
        // from the projector at those edges. The compensation is based on Lambert's cosine law, and also
        // includes a distance component (which probably has little effect). The compensation is also
        // scaled by `PanoramicDisplayCamera.surfaceMaskScale`, which can be changed at runtime to find
        // a compensation that matches the material properties of the display surface.

        public static byte SetupCylinderProjectorEdgeBrightener(SetupCylinderProjectorDelegateParams p)
        {
            // The shader computes `c = c0 * (1 - surfaceMaskScale * s)`, where `c` is the final color,
            // `c0` is the original color without brightening, and `s` is the per-pixel value being
            // computed here. So we want `s` to be smaller at places that need brightening, and bigger
            // at the places that are already bright: those already-bright places will lose some brightness
            // so it can be given to the other places.

            // The point on the screen closest to the projector is at the middle of the projector's image.
            float distClosest = p.projRay.origin.magnitude - p.surfRadius;
            float distAttenuationClosest = 1.0f / (distClosest * distClosest);

            float distFurthest;
            if (!SetupCylinderProjectorDistanceFurthest(p, out distFurthest))
            {
                return 255;
            }
            float distAttenuationFurthest = 1.0f / (distFurthest * distFurthest);

            Ray rayFurthest;
            Vector3 ptFurthest;
            if (!SetupCylinderProjectorIntersectionFurthest(p, out rayFurthest, out ptFurthest))
            {
                return 255;
            }
            Vector3 normalFurthest = ptFurthest.normalized;
            float lambertAttenuationFurthest = 1 - Mathf.Abs(Vector3.Dot(normalFurthest, rayFurthest.direction));

            // For the current pixel.
            float dist = (p.surfInter - p.projRay.origin).magnitude;
            float distAttenuation = 1.0f / (dist * dist);
            Vector3 normal = p.surfInter.normalized;
            float lambertAttenuation = 1 - Mathf.Abs(Vector3.Dot(normal, p.projRay.direction));

            // Normalized to be 0 at `ptFurthest` and the largest possible attenuation, 
            // `distAttenuationClosest - distAttenuationFurthest`, at the closest point.
            // Note, though, that the amount of attenuation is usually small enough that 
            // it is zero with one-byte resolution.
            float distAttenuationNormalized = distAttenuation - distAttenuationFurthest;

            // Normalized to be 0 at `ptFurthest` and the largest possible attenuation, 
            // `lambertAttenuationFurthest`, at the closest point.
            float lambertAttenuationNormalized = lambertAttenuationFurthest - lambertAttenuation;

            float attenuation = lambertAttenuationNormalized + distAttenuationNormalized;
            byte result = (byte)(255 * attenuation);
            return result;
        }

        // Sets up one approach to correcting the color at the edges of the images projected onto a cylinder
        // (i.e., the borders between projectors). This approach subtracts away a scaling of the color
        // `p.edgeArtifactColor`, where the scaling involves Lambert's cosine law. The correction is also
        // scaled by `PanoramicDisplayCamera.surfaceColorCorrectionScale`, which can be changed at runtime to
        // find color correction that matches the material properties of the display surface.

        public static Color SetupCylinderProjectorEdgeColorCorrector(SetupCylinderProjectorDelegateParams p)
        {
            Ray rayFurthest;
            Vector3 ptFurthest;
            if (!SetupCylinderProjectorIntersectionFurthest(p, out rayFurthest, out ptFurthest))
            {
                return Color.white;
            }
            Vector3 normalFurthest = ptFurthest.normalized;
            float dotFurthest = Mathf.Abs(Vector3.Dot(normalFurthest, rayFurthest.direction));

            Vector3 normal = p.surfInter.normalized;
            float dot = Mathf.Abs(Vector3.Dot(normal, p.projRay.direction));

            // The shader computes `c = c0 * ([1,1,1,1] - surfaceColorCorrectionScale * s)` where 
            // `s` is the per-pixel value computed here.

            float normalized = (1 - dot) / (1 - dotFurthest);
            return normalized * SetupCylinderProjectorDelegateParams.edgeArtifactColor;
        }

        // A utility function for computing the furthest (greatest) distance to a cylinder point illuminated by the
        // projector associated with the delegate parameters `p`. Returns false if the distance cannot be computed.

        public static bool SetupCylinderProjectorDistanceFurthest(SetupCylinderProjectorDelegateParams p, out float distFurthest)
        {
            // To compute the furthest distance, first do the horizontal part. 
            float projAngle = 2 * Mathf.PI / p.projCount;
            float chordLenHalf = Mathf.Sin(projAngle / 2) * p.surfRadius;
            float projFovHoriz = p.projFovHorizDeg * Mathf.Deg2Rad;
            float sin = Mathf.Sin(projFovHoriz / 2);
            const float EPS = 1e-7f;
            if (Mathf.Abs(sin) < EPS)
            {
                distFurthest = 0;
                return false;
            }
            float distFurthestHoriz = chordLenHalf / sin;

            // Then do the vertical part.
            float projFovVert = (float)p.projHeight / p.projWidth * projFovHoriz;
            float cos = Mathf.Cos(projFovVert / 2);
            if (Mathf.Abs(cos) < EPS)
            {
                distFurthest = 0;
                return false;
            }
            distFurthest = distFurthestHoriz / cos;
            return true;
        }

        // A utility function for computing the furthest cylinder intersection point for a ray from the projector 
        // associated with the delegate parameters `p`. Returns false if the intersection cannot be computed.

        public static bool SetupCylinderProjectorIntersectionFurthest(SetupCylinderProjectorDelegateParams p,
            out Ray ray, out Vector3 inter)
        {
            Vector3 rayOriginFurthest = new Vector3(0, 0, p.projDistXZ);
            Vector3 rayDirectionFurthest0 = (Vector3.zero - rayOriginFurthest).normalized;
            Quaternion rot0 = Quaternion.AngleAxis(p.projFovHorizDeg / 2, Vector3.up);
            Vector3 rayDirectionFurthest1 = rot0 * rayDirectionFurthest0;
            float projFovVertDeg = (float)p.projHeight / p.projWidth * p.projFovHorizDeg;
            Quaternion rot1 = Quaternion.AngleAxis(projFovVertDeg / 2, Vector3.right);
            Vector3 rayDirectionFurthest = rot1 * rayDirectionFurthest1;

            float distFurthest;
            if (!SetupCylinderProjectorDistanceFurthest(p, out distFurthest))
            {
                ray = new Ray(Vector3.zero, Vector3.zero);
                inter = Vector3.zero;
                return false;
            }

            ray = new Ray(rayOriginFurthest, rayDirectionFurthest);
            inter = rayOriginFurthest + distFurthest * rayDirectionFurthest;
            return true;
        }

        private static bool CylinderComputeParameters(float surfRadius, int projCount, float projFovHorizDeg, int projWidth, int projHeight,
            out float projDistXZ, out float surfHeight)
        {
            if (projCount < 2)
            {
                Debug.Log("CylinderComputeParameters: projector count must be 2 or greater");
                projDistXZ = surfHeight = 0;
                return false;
            }

            // Distribute the projectors evenly around the cylindrical screen.
            float projAngle = 2 * Mathf.PI / projCount;

            // Find the distance at which to place each projector so that its horizontal field of view
            // covers its share of the cylindrical boundary.
            float d1 = Mathf.Cos(projAngle / 2) * surfRadius;
            float chordLenHalf = Mathf.Sin(projAngle / 2) * surfRadius;
            float projFovHoriz = projFovHorizDeg * Mathf.Deg2Rad;
            float tan = Mathf.Tan(projFovHoriz / 2);
            const float EPS = 1e-7f;
            if (Mathf.Abs(tan) < EPS)
            {
                Debug.Log("CylinderComputeParameters: projector FOV (horizontal) is too small");
                projDistXZ = surfHeight = 0;
                return false;
            }
            float d2 = chordLenHalf / tan;
            projDistXZ = d1 + d2;

            // Find the cylindrical height that would be fully covered by the projectors' vertical fields of view,
            // which are determined by the aspect ratio and the horizontal fields of view.
            float projFovVert = (float)projHeight / projWidth * projFovHoriz;
            float surfHeightHalf = Mathf.Tan(projFovVert / 2) * d2;
            surfHeight = 2 * surfHeightHalf;

            return true;
        }

        private static Ray CylinderProjectorRay(int iWidth, int iHeight, Vector3 surfPos, float angle0Deg, float surfRadius, 
            int projCount, float projDistXZ, float projFovHorizDeg, int projWidth, int projHeight, bool[] reportedProjPos)
        {
            // To compute the ray corresponding to the pixel indexed by `iWidth` and `iHeight`, start with a projector in
            // a canonical pose, positioned on the X axis with its view plane in the YZ plane. The ray goes from the projector
            // position to the pixel, which has a 3D position on the view plane. Then the canonical ray is rotated to the
            // proper position and orientation for the particular projector's location around the cylindrical screen.

            Vector3 projPos = new Vector3(projDistXZ, surfPos.y, 0);

            // The X coordinate of the view plane just needs to be somewhere between the cylindrical screen (having radius
            // `surfRadius`) and the canonical projector position (with X being `projDistXZ`).
            float dProjToPixX = (projDistXZ - surfRadius) / 2;
            float pixX = projDistXZ - dProjToPixX;

            float projFovHoriz = projFovHorizDeg * Mathf.Deg2Rad;
            float tanHoriz = Mathf.Tan(projFovHoriz / 2);
            float pixZ0 = -tanHoriz * dProjToPixX;
            float pixZ1 =  tanHoriz * dProjToPixX;

            // Use the middle of each pixel region.
            float halfPixZ = (pixZ1 - pixZ0) / projWidth / 2;    
            pixZ0 += halfPixZ;
            pixZ1 -= halfPixZ;

            int iWidthProj = iWidth % projWidth;
            // Use `iWidthProj` relative to `pixZ1` instead of `pixZ0` to avoid left-right mirroring.
            float pixZ = pixZ1 - ((float)iWidthProj / (projWidth - 1)) * (pixZ1 - pixZ0);

            float projFovVert = (float)projHeight / projWidth * projFovHoriz;
            float tanVert = Mathf.Tan(projFovVert / 2);
            float pixY0 = -tanVert * dProjToPixX;
            float pixY1 =  tanVert * dProjToPixX;

            // Use the middle of each pixel region.
            float halfPixY = (pixY1 - pixY0) / projHeight / 2;
            pixY0 += halfPixY;
            pixY1 -= halfPixY;

            float pixY = pixY0 + ((float)iHeight / (projHeight - 1)) * (pixY1 - pixY0);

            Vector3 pixPos = new Vector3(pixX, pixY, pixZ);

            // Now rotate the canonical ray to have the proper position and orientation for the particular projector.

            int iProj = iWidth / projWidth;
            float angleProjDeg = 360.0f / projCount * (iProj - 0.25f) + 180f + angle0Deg;

            Quaternion rot = Quaternion.AngleAxis(angleProjDeg, Vector3.up);
            Vector3 rayOrig = rot * projPos;
            Vector3 rayDir = rot * (pixPos - projPos).normalized;

            if ((reportedProjPos != null) && !reportedProjPos[iProj])
            {
                reportedProjPos[iProj] = true;
                Debug.Log("Please position projector " + iProj + " at " + rayOrig);
            }

            return new Ray(rayOrig, rayDir);
        }

        private static Vector3 CylinderRayIntersection(Vector3 surfPos, float surfRadius, float surfHeight, Ray projRay, float projDistXZ)
        {
            const float EPS = 1e-7f;

            // Work in a space with the cylinder at the origin, and then move back to the real space at the end.
            Vector3 origin = projRay.origin - surfPos;

            // Compute the intersection X and Z in 2D, on the Y = 0 plane, where the cylinder is a circle.
            // Use the parameteric formula of a point on the ray, and solve for the parameter that makes the
            // point's distance to the cyclinder center equal the cylnder radius. Doing so involves using the
            // quadratic formula.

            Vector2 r = new Vector2(origin.x, origin.z);
            Vector2 v = new Vector2(projRay.direction.x, projRay.direction.z);
            float a = Vector2.Dot(v, v);
            float b = 2 * Vector2.Dot(r, v);
            float c = Vector2.Dot(r, r) - surfRadius * surfRadius;
            
            if (Mathf.Abs(2 * a) < EPS)
            {
                Debug.Log("CylinderRayIntersection failed [1], projRay " + projRay);
                return Vector3.zero;
            }

            float sq = Mathf.Sqrt(b * b - 4 * a * c);
            float t0 = (-b + sq) / (2 * a);
            float t1 = (-b - sq) / (2 * a);
            float t2 = Mathf.Min(t0, t1);
            Vector2 p = r + t2 * v;

            // Compute the intersection Y.

            float dxz = Mathf.Sqrt(projRay.direction.x * projRay.direction.x + projRay.direction.z * projRay.direction.z);
            Vector2 v1 = new Vector2(dxz, projRay.direction.y).normalized;
            Vector2 v2 = new Vector2(1, 0);
            float cos = Vector2.Dot(v1, v2);

            if (Mathf.Abs(cos) < EPS)
            {
                Debug.Log("CylinderRayIntersection failed [2], projRay " + projRay);
                return Vector3.zero;
            }

            float t3 = (projDistXZ - surfRadius) / cos;
            float y = origin.y + t3 * projRay.direction.y;

            Vector3 inter = new Vector3(p.x, y, p.y);
            return inter + surfPos;
        }

        private static GameObject CreateCamera(string name, float fovVertDeg, float aspect, float near, float far, int targetDisplay, Vector3 position, Vector3 euler, Transform parent)
        {
            GameObject cameraObject = new GameObject(name);
            SetObjectDirty(cameraObject);

            Camera camera = cameraObject.AddComponent(typeof(Camera)) as Camera;
            camera.fieldOfView = fovVertDeg;
            camera.aspect = aspect;

            camera.nearClipPlane = near;
            camera.farClipPlane = far;

            camera.targetDisplay = targetDisplay;

            cameraObject.transform.parent = parent;
            cameraObject.transform.localPosition = position;
            cameraObject.transform.localEulerAngles = euler;

            return cameraObject;
        }

        private static void SetObjectDirty(GameObject obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(obj);
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
#endif
        }


    }
}
