using NUnit.Framework;
using System;
using System.Text;

namespace Janelia
{
    public static class FicTracUtilitiesTest
    {
        [Test]
        public static void TestParseLong()
        {
            bool valid = true;

            Byte[] b00 = { (Byte)'0', 0 };
            long l00 = FicTracUtilities.ParseLong(b00, 0, b00.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(0, l00);

            Byte[] b01 = { (Byte)'1', 0 };
            long l01 = FicTracUtilities.ParseLong(b01, 0, b01.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(1, l01);

            Byte[] b02 = { (Byte)'-', (Byte)'2', 0 };
            long l02 = FicTracUtilities.ParseLong(b02, 0, b02.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(-2, l02);

            Byte[] b03 = { (Byte)' ', (Byte)'3', 0 };
            long l03 = FicTracUtilities.ParseLong(b03, 0, b03.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(3, l03);

            Byte[] b04 = { (Byte)' ', (Byte)'4', (Byte)' ', 0 };
            long l04 = FicTracUtilities.ParseLong(b04, 0, b04.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(4, l04);

            Byte[] b05 = Encoding.ASCII.GetBytes("56");
            long l05 = FicTracUtilities.ParseLong(b05, 0, b05.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(56, l05);

            Byte[] b06 = Encoding.ASCII.GetBytes("-78901");
            long l06 = FicTracUtilities.ParseLong(b06, 0, b06.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(-78901, l06);

            Byte[] b07 = Encoding.ASCII.GetBytes("2030405");
            long l07 = FicTracUtilities.ParseLong(b07, 0, b07.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(2030405, l07);

            string prefix = "skip at the beginning ";
            string suffix = " skip at the end";

            Byte[] b20 = Encoding.ASCII.GetBytes(prefix + "607");
            long l20 = FicTracUtilities.ParseLong(b20, prefix.Length, b20.Length - prefix.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(607, l20);

            Byte[] b21 = Encoding.ASCII.GetBytes("80901" + suffix);
            long l21 = FicTracUtilities.ParseLong(b21, 0, b21.Length - suffix.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(80901, l21);

            Byte[] b22 = Encoding.ASCII.GetBytes(prefix + "2030405" + suffix);
            int space = 1;
            long l22 = FicTracUtilities.ParseLong(b22, prefix.Length - space, b22.Length - prefix.Length - suffix.Length + space, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(2030405, l22);

            Byte[] b30 = Encoding.ASCII.GetBytes("607.0");
            long l30 = FicTracUtilities.ParseLong(b30, 0, b30.Length, ref valid);
            Assert.IsFalse(valid);
            Assert.AreEqual(0, l30);

            Byte[] b31 = Encoding.ASCII.GetBytes("bad809");
            long l31 = FicTracUtilities.ParseLong(b31, 0, b31.Length, ref valid);
            Assert.IsFalse(valid);
            Assert.AreEqual(0, l31);

            Byte[] b32 = Encoding.ASCII.GetBytes("10bad2");
            long l32 = FicTracUtilities.ParseLong(b32, 0, b32.Length, ref valid);
            Assert.IsFalse(valid);
            Assert.AreEqual(0, l32);

            Byte[] b33 = Encoding.ASCII.GetBytes("304bad");
            long l33 = FicTracUtilities.ParseLong(b33, 0, b33.Length, ref valid);
            Assert.IsFalse(valid);
            Assert.AreEqual(0, l33);
        }

        [Test]
        public static void TestParseDouble()
        {
            bool valid = true;

            Byte[] b00 = { (Byte)'0', 0 };
            double f00 = FicTracUtilities.ParseDouble(b00, 0, b00.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(0, f00);

            Byte[] b01 = { (Byte)'1', 0 };
            double f01 = FicTracUtilities.ParseDouble(b01, 0, b01.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(1, f01);

            Byte[] b02 = { (Byte)'-', (Byte)'2', 0 };
            double f02 = FicTracUtilities.ParseDouble(b02, 0, b02.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(-2, f02);

            Byte[] b03 = { (Byte)' ', (Byte)'3', 0 };
            double f03 = FicTracUtilities.ParseDouble(b03, 0, b03.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(3, f03);

            Byte[] b04 = { (Byte)' ', (Byte)'4', (Byte)' ', 0 };
            double f04 = FicTracUtilities.ParseDouble(b04, 0, b04.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(4, f04);

            Byte[] b05 = Encoding.ASCII.GetBytes("56");
            double f05 = FicTracUtilities.ParseDouble(b05, 0, b05.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(56, f05);

            Byte[] b06 = Encoding.ASCII.GetBytes("-78.901");
            double f06 = FicTracUtilities.ParseDouble(b06, 0, b06.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(-78.901, f06);

            Byte[] b07 = Encoding.ASCII.GetBytes("20.30405");
            double f07 = FicTracUtilities.ParseDouble(b07, 0, b07.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(20.30405, f07);

            Byte[] b08 = Encoding.ASCII.GetBytes("-607.0809");
            double f08 = FicTracUtilities.ParseDouble(b08, 0, b08.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(-607.0809, f08);

            Byte[] b09 = Encoding.ASCII.GetBytes("1020.304");
            double f09 = FicTracUtilities.ParseDouble(b09, 0, b09.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(1020.304, f09);

            Byte[] b10 = Encoding.ASCII.GetBytes("  -50607.08 ");
            double f10 = FicTracUtilities.ParseDouble(b10, 0, b10.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(-50607.08, f10);

            Byte[] b11 = Encoding.ASCII.GetBytes("  901020.3   ");
            double f11 = FicTracUtilities.ParseDouble(b11, 0, b11.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(901020.3, f11);

            string prefix = "skip at the beginning ";
            string suffix = " skip at the end";

            Byte[] b20 = Encoding.ASCII.GetBytes(prefix + "60.7");
            double f20 = FicTracUtilities.ParseDouble(b20, prefix.Length, b20.Length - prefix.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(60.7, f20);

            Byte[] b21 = Encoding.ASCII.GetBytes("80.901" + suffix);
            double f21 = FicTracUtilities.ParseDouble(b21, 0, b21.Length - suffix.Length, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(80.901, f21);

            Byte[] b22 = Encoding.ASCII.GetBytes(prefix + "20304.05" + suffix);
            int space = 1;
            double f22 = FicTracUtilities.ParseDouble(b22, prefix.Length - space, b22.Length - prefix.Length - suffix.Length + space, ref valid);
            Assert.IsTrue(valid);
            Assert.AreEqual(20304.05, f22);

            Byte[] b30 = Encoding.ASCII.GetBytes("6.0e7");
            double f30 = FicTracUtilities.ParseDouble(b30, 0, b30.Length, ref valid);
            Assert.IsFalse(valid);
            Assert.AreEqual(0, f30);

            Byte[] b31 = Encoding.ASCII.GetBytes("bad8.09");
            double f31 = FicTracUtilities.ParseDouble(b31, 0, b31.Length, ref valid);
            Assert.IsFalse(valid);
            Assert.AreEqual(0, f31);

            Byte[] b32 = Encoding.ASCII.GetBytes("1.0bad2");
            double f32 = FicTracUtilities.ParseDouble(b32, 0, b32.Length, ref valid);
            Assert.IsFalse(valid);
            Assert.AreEqual(0, f32);

            Byte[] b33 = Encoding.ASCII.GetBytes("3.04bad");
            double f33 = FicTracUtilities.ParseDouble(b33, 0, b33.Length, ref valid);
            Assert.IsFalse(valid);
            Assert.AreEqual(0, f33);
        }

        [Test]
        public static void TestNthSplit()
        {
            Byte[] b = Encoding.ASCII.GetBytes("0.0, 0.10, 0.200, 1.0, 1.10, 1.200, 2.0, 2.10, 2.200");

            int i00 = -1, len00 = -1;
            FicTracUtilities.NthSplit(b, 0, 0, ref i00, ref len00);
            // "0.0"
            Assert.AreEqual(0, i00);
            Assert.AreEqual(3, len00);

            int i01 = -1, len01 = -1;
            FicTracUtilities.NthSplit(b, 0, 1, ref i01, ref len01);
            // " 0.10"
            Assert.AreEqual(4, i01);
            Assert.AreEqual(5, len01);

            int i02 = -1, len02 = -1;
            FicTracUtilities.NthSplit(b, 0, 2, ref i02, ref len02);
            // " 0.200"
            Assert.AreEqual(10, i02);
            Assert.AreEqual(6, len02);

            int i10 = -1, len10 = -1;
            FicTracUtilities.NthSplit(b, 17, 0, ref i10, ref len10);
            // " 1.0"
            Assert.AreEqual(17, i10);
            Assert.AreEqual(4, len10);

            int i11 = -1, len11 = -1;
            FicTracUtilities.NthSplit(b, 17, 1, ref i11, ref len11);
            // " 1.10"
            Assert.AreEqual(22, i11);
            Assert.AreEqual(5, len11);

            int i12 = -1, len12 = -1;
            FicTracUtilities.NthSplit(b, 17, 2, ref i12, ref len12);
            // " 1.200"
            Assert.AreEqual(28, i12);
            Assert.AreEqual(6, len12);

            int i20 = -1, len20 = -1;
            FicTracUtilities.NthSplit(b, 36, 0, ref i20, ref len20);
            // "2.0"
            Assert.AreEqual(36, i20);
            Assert.AreEqual(3, len20);

            int i21 = -1, len21 = -1;
            FicTracUtilities.NthSplit(b, 36, 1, ref i21, ref len21);
            // " 2.10"
            Assert.AreEqual(40, i21);
            Assert.AreEqual(5, len21);

            int i22 = -1, len22 = -1;
            FicTracUtilities.NthSplit(b, 36, 2, ref i22, ref len22);
            // " 2.200"
            Assert.AreEqual(46, i22);
            Assert.AreEqual(6, len22);
        }
    }
}
