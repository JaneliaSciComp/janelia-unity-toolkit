// Simple collision detection and response for a Transform moving kinematically
// (e.g., the Transform from a GameObject representing a fly walking on a treadmill
// to move through a virtual world).  Collisions with all Colliders in the scene
// will be handled (and note that most standard 3D objects have a Collider by
// default).  The algorithms treat the Transform's object as a sphere and
// support an approximate form of sliding along the surface of any contacted Collider.

using System;
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
            _touchDistance = radius / 1000.0f;
        }

        // Correct the specified translation to account for collisions, and apply it to
        // this handler's Transform.  Returns the corrected translation.

        public Vector3 CorrectTranslation(Vector3 translation, bool approximateSliding = true)
        {
            Vector3 validTranslation = translation;
            Vector3 translationWorld = _transform.rotation * translation;

            // Turn the proposed translation vector into a ray and check for the closest interesection
            // with a collider in the scene.

            RaycastHit closestHit = default;
            float contactDistance = 0;
            if (ClosestHit(translationWorld, ref closestHit, ref contactDistance, true))
            {
                validTranslation = contactDistance * translation.normalized;

                if (approximateSliding && (Mathf.Abs(contactDistance) < _touchDistance))
                {
                    // If this Transform is essentially touching the interesected collider, then 
                    // turn the proposed translation vector into an approximate "sliding" translation
                    // along the collider's surface.  Approximate that surface as a plane at the
                    // intersection point.

                    Vector3 validTranslationWorld = Vector3.ProjectOnPlane(translationWorld, closestHit.normal);

                    // This sliding translation can itself intersect another collider, so check for another
                    // ray interesection.

                    float contactDistance2 = 0;
                    if (ClosestHit(validTranslationWorld, ref closestHit, ref contactDistance2, false))
                    {
                        // If there is such a "secondary" intersection, just clip the translation and don't
                        // try to do another level of sliding.

                        validTranslationWorld = contactDistance2 * validTranslationWorld.normalized;
                    }

                    validTranslation = Quaternion.Inverse(_transform.rotation) * validTranslationWorld;
                }
            }

            if (planarMotionNormal != null)
            {
                // Optionally, don't allow the collision response to move off the original plane of motion.

                Vector3 normal = (Vector3)planarMotionNormal;
                validTranslation = Vector3.ProjectOnPlane(validTranslation, normal);
            }

            return validTranslation;
        }

        private bool ClosestHit(Vector3 translationWorld, ref RaycastHit closestHit, ref float contactDistance, bool checkLast)
        {
            Ray ray = new Ray(_transform.position, translationWorld);
            float maxDistance = translationWorld.magnitude + radius;

            bool limited = false;
            RaycastHit limitHit = new RaycastHit();
            if (limitDistance > 0)
            {
                limited = Intersect.SphereWithRay(ref limitHit, limitCenter, limitDistance, ray, maxDistance);
                closestHit = limitHit;
            }

            bool collided = Physics.Raycast(ray, out RaycastHit colliderHit, maxDistance);

            if (collided)
            {
                if (!limited || (colliderHit.distance < limitHit.distance))
                {
                    closestHit = colliderHit;
                }
            }

            if (limited || collided)
            {
                contactDistance = (closestHit.point - _transform.position).magnitude;
                contactDistance -= radius;

                // Move back an extra little bit, because Unity's `Physics.Raycast` function will ignore
                // a collider that contains the ray's origin.

                float extra = Vector3.Project(closestHit.normal, ray.direction).magnitude * _touchDistance;
                contactDistance -= extra;

                contactDistance = Math.Max(contactDistance, 0);
                return true;
            }

            return false;
        }

        private Transform _transform;
        private float _touchDistance;
    }
}
