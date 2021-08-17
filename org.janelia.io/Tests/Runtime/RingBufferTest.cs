using NUnit.Framework;
using System;
using System.Text;

namespace Janelia
{
    public static class RingBufferTest
    {
        [Test]
        public static void TestBasic()
        {
            int ItemCount = 4;
            int ItemSizeBytes = 32;
            RingBuffer buffer = new RingBuffer(ItemCount, ItemSizeBytes);

            buffer.Give(Encoding.ASCII.GetBytes("one"));
            buffer.Give(Encoding.ASCII.GetBytes("two"));

            Byte[] taken = new Byte[ItemSizeBytes];
            long timestamp = 0;
            bool didTake = false;

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsTrue(didTake);
            Assert.AreEqual("one", Encoding.ASCII.GetString(taken, 0, "one".Length));
            Assert.AreNotEqual(0, timestamp);

            buffer.Give(Encoding.ASCII.GetBytes("three"));

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsTrue(didTake);
            Assert.AreEqual("two", Encoding.ASCII.GetString(taken, 0, "two".Length));
            Assert.AreNotEqual(0, timestamp);

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsTrue(didTake);
            Assert.AreEqual("three", Encoding.ASCII.GetString(taken, 0, "three".Length));
            Assert.AreNotEqual(0, timestamp);

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsFalse(didTake);

            buffer.Give(Encoding.ASCII.GetBytes("four"));
            buffer.Give(Encoding.ASCII.GetBytes("five"));
            buffer.Give(Encoding.ASCII.GetBytes("six"));
            buffer.Give(Encoding.ASCII.GetBytes("seven"));

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsTrue(didTake);
            Assert.AreEqual("four", Encoding.ASCII.GetString(taken, 0, "four".Length));
            Assert.AreNotEqual(0, timestamp);

            buffer.Give(Encoding.ASCII.GetBytes("eight"));
            buffer.Give(Encoding.ASCII.GetBytes("nine"));

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsTrue(didTake);
            Assert.AreEqual("six", Encoding.ASCII.GetString(taken, 0, "six".Length));
            Assert.AreNotEqual(0, timestamp);

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsTrue(didTake);
            Assert.AreEqual("seven", Encoding.ASCII.GetString(taken, 0, "seven".Length));
            Assert.AreNotEqual(0, timestamp);

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsTrue(didTake);
            Assert.AreEqual("eight", Encoding.ASCII.GetString(taken, 0, "eight".Length));
            Assert.AreNotEqual(0, timestamp);

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsTrue(didTake);
            Assert.AreEqual("nine", Encoding.ASCII.GetString(taken, 0, "nine".Length));
            Assert.AreNotEqual(0, timestamp);

            didTake = buffer.Take(ref taken, ref timestamp);
            Assert.IsFalse(didTake);
        }
    }
}