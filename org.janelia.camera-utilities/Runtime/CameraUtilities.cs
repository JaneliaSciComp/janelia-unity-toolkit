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
    }
}
