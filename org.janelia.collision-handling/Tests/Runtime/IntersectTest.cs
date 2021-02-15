using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Janelia
{
    public static class IntersectTest
    {
        [Test]
        public static void TestSphereWithRayMiss1()
        {
            Vector3 sphereCenter = new Vector3(0, 0, 0);
            float sphereRadius = 1;
            Ray ray = new Ray(new Vector3(2, 0, 0), new Vector3(0, 1, 0));
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayMiss2()
        {
            Vector3 sphereCenter = new Vector3(0, 0, 0);
            float sphereRadius = 1;
            Ray ray = new Ray(new Vector3(2, 0.5f, 0), (new Vector3(1, -1, 0)).normalized);
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit1a()
        {
            Vector3 sphereCenter = new Vector3(0, 0, 0);
            float sphereRadius = 1;
            Ray ray = new Ray(new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(new Vector3(0, 1, 0)).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo(new Vector3(0, -1, 0)).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit1b()
        {
            Vector3 sphereCenter = new Vector3(0, 0, 0);
            float sphereRadius = 1;
            Ray ray = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(new Vector3(1, 0, 0)).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo(new Vector3(-1, 0, 0)).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit1c()
        {
            Vector3 sphereCenter = new Vector3(0, 0, 0);
            float sphereRadius = 1;
            Ray ray = new Ray(new Vector3(0, 0, 0), new Vector3(0, -1, 0));
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(new Vector3(0, -1, 0)).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo(new Vector3(0, 1, 0)).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit1d()
        {
            Vector3 sphereCenter = new Vector3(0, 0, 0);
            float sphereRadius = 1;
            Ray ray = new Ray(new Vector3(0, 0, 0), new Vector3(-1, 0, 0));
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(new Vector3(-1, 0, 0)).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo(new Vector3(1, 0, 0)).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit2a()
        {
            Vector3 sphereCenter = new Vector3(0.5f, 0, 1);
            float sphereRadius = 2;
            Vector3 rayDir = new Vector3(0, 0, 1);
            Ray ray = new Ray(sphereCenter + 0.5f * rayDir, rayDir);
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(sphereCenter + sphereRadius * rayDir).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo(-rayDir).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit2b()
        {
            Vector3 sphereCenter = new Vector3(1, 0, 0.5f);
            float sphereRadius = 2;
            Vector3 rayDir = new Vector3(1, 0, 0);
            Ray ray = new Ray(sphereCenter + 0.5f * rayDir, rayDir);
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(sphereCenter + sphereRadius * rayDir).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo(-rayDir).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit2c()
        {
            Vector3 sphereCenter = new Vector3(0.5f, 0, -1);
            float sphereRadius = 2;
            Vector3 rayDir = new Vector3(0, 0, -1);
            Ray ray = new Ray(sphereCenter + 0.5f * rayDir, rayDir);
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(sphereCenter + sphereRadius * rayDir).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo(-rayDir).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit2d()
        {
            Vector3 sphereCenter = new Vector3(-1, 0, 0.5f);
            float sphereRadius = 2;
            Vector3 rayDir = new Vector3(-1, 0, 0);
            Ray ray = new Ray(sphereCenter + 0.5f * rayDir, rayDir);
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(sphereCenter + sphereRadius * rayDir).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo(-rayDir).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }

        [Test]
        public static void TestSphereWithRayHit3()
        {
            Vector3 sphereCenter = new Vector3(0, 0, 0);
            float sphereRadius = 10;
            // x*x + x*x = r*r, 2*x*x = r*r, x*x = r*r/2, x = sqrt(r*r/2)
            float x = Mathf.Sqrt(sphereRadius * sphereRadius / 2);
            Ray ray = new Ray(new Vector3(x, 0, 0), new Vector3(0, 0, 1));
            RaycastHit hit = new RaycastHit();
            bool intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray);
            Assert.IsTrue(intersected);
            Assert.That(hit.point, Is.EqualTo(new Vector3(x, 0, x)).Using(Vector3EqualityComparer.Instance));
            Assert.That(hit.normal, Is.EqualTo((new Vector3(-1, 0, -1)).normalized).Using(Vector3EqualityComparer.Instance));
            float maxDistance = sphereRadius / 2;
            intersected = Intersect.SphereWithRay(ref hit, sphereCenter, sphereRadius, ray, maxDistance);
            Assert.IsFalse(intersected);
        }
    }
}
