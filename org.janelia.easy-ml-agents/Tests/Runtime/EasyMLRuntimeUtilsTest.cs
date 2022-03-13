using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{

    public static class EasyMLRuntimeUtilsTest
    {
        [Test]
        public static void TestNoTaggedChild()
        {
            GameObject parent0 = new GameObject();
            parent0.name = "Parent0";
            GameObject child0a = new GameObject();
            child0a.name = "Child0a";
            child0a.transform.parent = parent0.transform;
            GameObject child0b = new GameObject();
            child0b.name = "Child0b";
            child0b.transform.parent = parent0.transform;

            GameObject child = EasyMLRuntimeUtils.FindChildWithTag(parent0, "Player");

            Assert.That(child, Is.Null);

            List<GameObject> children = EasyMLRuntimeUtils.FindChildrenWithTag(parent0, "Player");

            Assert.That(children, Has.Count.EqualTo(0));
        }

        [Test]
        public static void TestOneTaggedChild()
        {
            GameObject parent1 = new GameObject();
            parent1.name = "Parent1";
            GameObject child1a = new GameObject();
            child1a.name = "Child1a";
            child1a.transform.parent = parent1.transform;
            GameObject child1b = new GameObject();
            child1b.name = "Child1b";
            child1b.transform.parent = parent1.transform;

            child1a.tag = "Player";
            GameObject child = EasyMLRuntimeUtils.FindChildWithTag(parent1, "Player");

            Assert.That(child, Has.Property("name").EqualTo("Child1a"));

            List<GameObject> children = EasyMLRuntimeUtils.FindChildrenWithTag(parent1, "Player");

            Assert.That(children, Has.Count.EqualTo(1));
            Assert.That(children, Has.Exactly(1).Property("name").EqualTo("Child1a"));
        }

        [Test]
        public static void TestTwoTaggedChildren()
        {
            GameObject parent2 = new GameObject();
            parent2.name = "Parent2";
            GameObject child2a = new GameObject();
            child2a.name = "Child2a";
            child2a.transform.parent = parent2.transform;
            GameObject child2b = new GameObject();
            child2b.name = "Child2b";
            child2b.transform.parent = parent2.transform;
            GameObject child2c = new GameObject();
            child2c.name = "Child2c";
            child2c.transform.parent = parent2.transform;

            child2a.tag = "Player";
            child2b.tag = "Player";
            GameObject child = EasyMLRuntimeUtils.FindChildWithTag(parent2, "Player");

            Assert.That(child, Has.Property("name").EqualTo("Child2a").Or.EqualTo("Child2b"));

            List<GameObject> children = EasyMLRuntimeUtils.FindChildrenWithTag(parent2, "Player");

            Assert.That(children, Has.Count.EqualTo(2));
            Assert.That(children, Has.Exactly(1).Property("name").EqualTo("Child2a"));
            Assert.That(children, Has.Exactly(1).Property("name").EqualTo("Child2b"));
        }

        [Test]
        public static void TestTaggedChildAndGrandchild()
        {
            GameObject parent3 = new GameObject();
            parent3.name = "Parent3";
            GameObject child3a = new GameObject();
            child3a.name = "Child3a";
            child3a.transform.parent = parent3.transform;
            GameObject child3b = new GameObject();
            child3b.name = "Child3b";
            child3b.transform.parent = parent3.transform;
            GameObject child3c = new GameObject();
            child3c.name = "Child3c";
            child3c.transform.parent = child3a.transform;

            child3b.tag = "Player";
            child3c.tag = "Player";
            GameObject child = EasyMLRuntimeUtils.FindChildWithTag(parent3, "Player");

            Assert.That(child, Has.Property("name").EqualTo("Child3b").Or.EqualTo("Child3c"));

            List<GameObject> children = EasyMLRuntimeUtils.FindChildrenWithTag(parent3, "Player");

            Assert.That(children, Has.Count.EqualTo(2));
            Assert.That(children, Has.Exactly(1).Property("name").EqualTo("Child3b"));
            Assert.That(children, Has.Exactly(1).Property("name").EqualTo("Child3c"));
        }
    }
}
