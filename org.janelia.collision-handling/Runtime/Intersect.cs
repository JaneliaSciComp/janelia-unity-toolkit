using UnityEngine;

namespace Janelia
{
    public static class Intersect
    {
        // Returns `true` if `ray` intersects the sphere with the specified center and radius, with the intersection being within
        // the distance `maxDistance` from `ray.origin`. The interesection closest to `ray.origin` is returned in the reference
        // argument `result`.

        public static bool SphereWithRay(ref RaycastHit result, Vector3 sphereCenter, float sphereRadius, Ray ray, float maxDistance = Mathf.Infinity)
        {
            // p = ray.origin
            // d = ray.direction
            // c = sphereCenter
            // r = sphereRadius
            // [(p + t * d) - c] * [(p + t * d) - c] = r * r
            // [t * d + (p - c)] * [t * d + (p - c)] = r * r
            // t * t * (d * d) + t * 2 * [d * (p - c)] + (p - c) * (p - c) = r * r
            // t * t * (d * d) + t * 2 * [d * (p - c)] + (p - c) * (p - c) - r * r = 0
            // A = d * d; B = 2 * d * (p - c); C = (p - c) * (p - c) - r * r
            // t = [-B +/- sqrt(B * B - 4 * A * C)] / 2 * A
            Vector3 originMinusCenter = ray.origin - sphereCenter;
            float A = Vector3.Dot(ray.direction, ray.direction);
            float B = 2 * Vector3.Dot(ray.direction, originMinusCenter);
            float radiusSquared = sphereRadius * sphereRadius;
            float C = Vector3.Dot(originMinusCenter, originMinusCenter) - radiusSquared;
            float denom = 2 * A;
            const float EPSILON = 1e-7f;
            if (Mathf.Abs(denom) < EPSILON)
            {
                return false;
            }
            float radicand = B * B - 4 * A * C;
            if (radicand < 0)
            {
                return false;
            }
            float root = Mathf.Sqrt(radicand);
            float t0 = (-B + root) / denom;
            float t1 = (-B - root) / denom;
            float t = (t0 > t1) ? t0 : t1;
            if (t > maxDistance)
            {
                return false;
            }
            result.point = ray.origin + t * ray.direction;
            result.distance = t;
            result.normal = (sphereCenter - result.point).normalized;
            if (originMinusCenter.sqrMagnitude > radiusSquared)
            {
                result.normal = -result.normal;
            }
            return true;
        }
    }
}