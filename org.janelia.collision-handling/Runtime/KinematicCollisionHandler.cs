// Simple collision detection and response for a Transform moving kinematically
// (e.g., the Transform from a GameObject representing a fly walking on a treadmill
// to move through a virtual world).  Collisions with all Colliders in the scene 
// will be handled (and note that most standard 3D objects have a Collider by
// default).  The algorithms treat the Transform's object as a small sphere and
// support an approximate form of sliding along the surface of any contacted Collider.

using UnityEngine;

namespace Janelia
{
    public class KinematicCollisionHandler
    {
        // Collisions are calculated for a sphere of this radius (which should be small).

        public float radius;

        // Initialize collision handling for the specified Transform, represented
        // as a sphere with the specified radius.

        public KinematicCollisionHandler(Transform transf, float rad = 0.1f)
        {
            radius = rad;
            _transform = transf;
        }

        // Correct the specified translation to account for collisions, and apply it to
        // this handler's Transform.  Returns the corrected translation.

        public Vector3 Translate(Vector3 translation, bool approximateSliding = true)
        {
            Vector3 validTranslation = translation;

            Ray ray = new Ray(_transform.position, translation);
            float maxDistance = translation.magnitude + radius;
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            if (hits.Length > 0)
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
                float contactDistance = (closestHit.point - _transform.position).magnitude;
                contactDistance -= radius;
                validTranslation = contactDistance * translation.normalized;

                if (approximateSliding && (Mathf.Abs(contactDistance) < radius / 1000.0f))
                {
                    validTranslation = Vector3.ProjectOnPlane(translation, closestHit.normal);
                }
            }

            _transform.Translate(validTranslation);
            return validTranslation;
        }

        private Transform _transform;
    }
}