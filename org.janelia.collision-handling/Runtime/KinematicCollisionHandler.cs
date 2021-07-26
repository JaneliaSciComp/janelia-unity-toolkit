// Simple collision detection and response for a Transform moving kinematically
// (e.g., the Transform from a GameObject representing a fly walking on a treadmill
// to move through a virtual world).  Collisions with all Colliders in the scene
// will be handled (and note that most standard 3D objects have a Collider by
// default).  The algorithms treat the Transform's object as a sphere and
// support an approximate form of sliding along the surface of any contacted Collider.

using UnityEngine;

namespace Janelia
{
    public class KinematicCollisionHandler
    {
        // Collisions are calculated for a sphere of this radius.

        public float radius;

        // If not null, the corrected translation is projected onto the plane with this normal.

        public Vector3? planarMotionNormal;

        // If `limitDistance` is greater than zero, then motion will be limited to being within
        // a boundary sphere of radius `limitDistance` centered at `limitCenter`.  If this sphere
        // is hit then translation will be corrected to slide along the sphere surface.

        public float limitDistance = 0;
        public Vector3 limitCenter = Vector3.zero;

        // Initialize collision handling for the specified Transform, represented
        // as a sphere with the specified radius.

        public KinematicCollisionHandler(Transform transf, Vector3? planarMotionNorm = null, float limitDist = 0, Vector3? limitCtr = null,
            float rad = 0.1f)
        {
            _transform = transf;
            radius = rad;
            planarMotionNormal = planarMotionNorm;
            limitDistance = limitDist;
            limitCenter = (limitCtr != null) ? (Vector3)limitCtr : limitCenter;
        }

        // Correct the specified translation to account for collisions, and apply it to
        // this handler's Transform.  Returns the corrected translation.

        public Vector3 CorrectTranslation(Vector3 translation, bool approximateSliding = true)
        {
            Vector3 validTranslation = translation;

            Vector3 translationWorld = _transform.rotation * translation;
            Ray ray = new Ray(_transform.position, translationWorld);
            float maxDistance = translationWorld.magnitude + radius;

            RaycastHit limitHit = new RaycastHit();
            bool limited = false;
            if (limitDistance > 0)
            {
                limited = Intersect.SphereWithRay(ref limitHit, limitCenter, limitDistance, ray, maxDistance);
            }

            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            if ((hits.Length > 0) || limited)
            {
                float closestDistance = 2.0f * maxDistance;
                RaycastHit closestHit = default;
                foreach (RaycastHit hit in hits)
                {
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        closestHit = hit;
                    }
                }

                if (limited && (limitHit.distance < closestDistance))
                {
                    closestDistance = limitHit.distance;
                    closestHit = limitHit;
                }

                float contactDistance = (closestHit.point - _transform.position).magnitude;
                contactDistance -= radius;
                validTranslation = contactDistance * translation.normalized;

                if (approximateSliding && (Mathf.Abs(contactDistance) < radius / 1000.0f))
                {
                    Vector3 validTranslationWorld = Vector3.ProjectOnPlane(translationWorld, closestHit.normal);
                    validTranslation = Quaternion.Inverse(_transform.rotation) * validTranslationWorld;
                }
            }

            if (planarMotionNormal != null)
            {
                Vector3 normal = (Vector3)planarMotionNormal;
                validTranslation = Vector3.ProjectOnPlane(validTranslation, normal);
            }

            return validTranslation;
        }

        private Transform _transform;
    }
}
