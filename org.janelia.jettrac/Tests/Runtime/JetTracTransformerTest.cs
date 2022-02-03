using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Janelia
{
    public static class JetTracTransformerTest
    {
        private static void VerifyWithinLimits(int y0, int y1, Vector3 limitA, Vector3 limitB, UInt64 deviceTimestepUs)
        {
            Vector3 bodyPosStart = new Vector3(0, 0, 0);
            Vector3 bodyRotStart = new Vector3(0, 0, 0);
            Vector3 bodyPos = bodyPosStart;
            Vector3 bodyRot = bodyRotStart;

            JetTracTransformer tr = new JetTracTransformer();
            JetTracParser.BallMessage msg = new JetTracParser.BallMessage();

            msg.y0 = y0;
            msg.y1 = y1;
            msg.deviceTimestampUs = deviceTimestepUs;

            tr.AddInput(msg);
            tr.Update(ref bodyPos, ref bodyRot.y);

            Vector3 bodyPosChange = bodyPos - bodyPosStart;
            float angle1 = Vector3.SignedAngle(limitA, bodyPosChange, Vector3.up);
            float angle2 = Vector3.SignedAngle(limitA, limitB, Vector3.up);

            Assert.That(angle1, Is.GreaterThan(0));
            Assert.That(angle1, Is.LessThan(angle2));

            Assert.That(bodyRot, Is.EqualTo(bodyRotStart).Using(Vector3EqualityComparer.Instance));

            // Reverse the Ys to move back to the starting position.

            msg.y0 = -y0;
            msg.y1 = -y1;
            msg.deviceTimestampUs = 2 * deviceTimestepUs;

            tr.AddInput(msg);
            tr.Update(ref bodyPos, ref bodyRot.y);

            Assert.That(bodyPos, Is.EqualTo(bodyPosStart).Using(Vector3EqualityComparer.Instance));

            Assert.That(bodyRot, Is.EqualTo(bodyRotStart).Using(Vector3EqualityComparer.Instance));
        }

        private static int ToFixed(float x)
        {
            return Mathf.RoundToInt(10 * x);
        }

        [Test]
        public static void TestYs1()
        {
            // Forward ~ y0 + y1 = -10 + -10 = -20
            // Side ~ y0 - y1 = -10 - -10 = 0
            // Ball pulled back, meaning motion forward
            VerifyWithinLimits(-10, -10, new Vector3(1, 0, 1), new Vector3(1, 0, -1), 1);
        }

        [Test]
        public static void TestYs2()
        {
            // Forward ~ y0 + y1 = -1 + -20 = -21
            // Side ~ y0 - y1 = -1 - -20 = 19
            // Ball pulled back and left, meaning motion forward and right
            VerifyWithinLimits(-1, -20, new Vector3(1, 0, 0), new Vector3(0, 0, -1), 5);
        }

        [Test]
        public static void TestYs3()
        {
            // Forward ~ y0 + y1 = 10 + -10 = 0
            // Side ~ y0 - y1 = 10 - -10 = 20
            // Ball pulled left, meaning motion right
            VerifyWithinLimits(10, -10, new Vector3(1, 0, -1), new Vector3(-1, 0, -1), 10);
        }

        [Test]
        public static void TestYs4()
        {
            // Forward ~ y0 + y1 = 20 + -1 = 19
            // Side ~ y0 - y1 = 20 - -1 = 21
            // Ball pulled forward and left, meaning motion back and right
            VerifyWithinLimits(20, -1, new Vector3(0, 0, -1), new Vector3(-1, 0, 0), 50);
        }

        [Test]
        public static void TestYs5()
        {
            // Forward ~ y0 + y1 = 10 + 10 = 20
            // Side ~ y0 - y1 = 10 - 10 = 0
            // Ball pulled forward, mean motion back
            VerifyWithinLimits(10, 10, new Vector3(-1, 0, -1), new Vector3(-1, 0, 1), 100);
        }

        [Test]
        public static void TestYs6()
        {
            // Forward ~ y0 + y1 = 1 + 20 + 1
            // Side ~ y0 - y1 = 1 - 20 = -19
            // Ball pulled forward and right, so motion back and left
            VerifyWithinLimits(1, 20, new Vector3(0, 0, -1), new Vector3(0, 0, 1), 150);
        }

        [Test]
        public static void TestYs7()
        {
            // Forward ~ y0 + y1 = -10 + 10 = 0
            // Side ~ y0 - y1 = -10 - 10 = -20
            // Ball pulled right, so motion left
            VerifyWithinLimits(-10, 10, new Vector3(-1, 0, 1), new Vector3(1, 0, 1), 200);
        }

        [Test]
        public static void TestYs8()
        {
            // Forward ~ y0 + y1 = -20 + -1 = -21
            // Side ~ y0 - y1 = -20 - -1 = -19
            // Ball pulled back and right, so motion forward and left
            VerifyWithinLimits(-20, -1, new Vector3(0, 0, 1), new Vector3(1, 0, 0), 500);
        }

        [Test]
        public static void TestYsSmoothed()
        {
            Vector3 bodyPosStart = new Vector3(-1, 2, 3);
            Vector3 bodyRotStart = new Vector3(0, 123, 0);
            Vector3 bodyPos = bodyPosStart;
            Vector3 bodyRot = bodyRotStart;

            JetTracTransformer tr = new JetTracTransformer();
            const int SMOOTH_WINDOW = 3;
            JetTracTransformer trSmoothed = new JetTracTransformer(SMOOTH_WINDOW);
            JetTracParser.BallMessage msg = new JetTracParser.BallMessage();

            int y0 = -1, y1 = -2;
            int spike = 10;
            int[] y0s = new int[]{ y0, y0, y0, y0, spike * y0, y0, y0, y0, y0, y0 };
            int[] y1s = new int[]{ y1, y1, y1, y1, spike * y1, y1, y1, y1, y1, y1 };

            bool smooth = false;
            List<Vector3> bodyPosAfterUpdate = new List<Vector3>();
            for (int i = 0; i < y0s.Length; ++i)
            {
                msg.y0 = y0s[i];
                msg.y1 = y1s[i];
                msg.deviceTimestampUs = (UInt64)(i + 1);

                tr.AddInput(msg);
                tr.Update(ref bodyPos, ref bodyRot.y, smooth);
                bodyPosAfterUpdate.Add(bodyPos);
            }

            smooth = true;
            List<Vector3> bodyPosAfterUpdateSmoothed = new List<Vector3>();
            bodyPos = bodyPosStart;
            for (int i = 0; i < y0s.Length; ++i)
            {
                msg.y0 = y0s[i];
                msg.y1 = y1s[i];
                msg.deviceTimestampUs = (UInt64)(i + 1);

                trSmoothed.AddInput(msg);
                trSmoothed.Update(ref bodyPos, ref bodyRot.y, smooth);
                bodyPosAfterUpdateSmoothed.Add(bodyPos);
            }

            List<int> changes = new List<int>();
            List<int> changesSmoothed = new List<int>();
            for (int i = 0; i < bodyPosAfterUpdate.Count; ++i)
            {
                if (i > 0)
                {
                    float change = Vector3.Distance(bodyPosAfterUpdate[i], bodyPosAfterUpdate[i - 1]);
                    changes.Add(ToFixed(change));
                    float changeSmoothed = Vector3.Distance(bodyPosAfterUpdateSmoothed[i], bodyPosAfterUpdateSmoothed[i - 1]);
                    changesSmoothed.Add(ToFixed(changeSmoothed));
                }
            }

            int usualChange = changes[0];
            int spikeChange = changes.Max();
            int usualChangeCount = changes.Count(c => c == usualChange);
            int spikeChangeCount = changes.Count(c => c == spikeChange);
            Assert.That(usualChangeCount, Is.EqualTo(changes.Count - 1));
            Assert.That(spikeChangeCount, Is.EqualTo(1));

            int unusualChangeSmoothedCount = changesSmoothed.Count(c => c != usualChange);
            Assert.That(unusualChangeSmoothedCount, Is.EqualTo(SMOOTH_WINDOW));

            foreach (int changeSmoothed in changesSmoothed)
            {
                if (changeSmoothed != usualChange)
                {
                    Assert.That(changeSmoothed, Is.LessThan(spikeChange));
                }
            }
        }
    }
}
