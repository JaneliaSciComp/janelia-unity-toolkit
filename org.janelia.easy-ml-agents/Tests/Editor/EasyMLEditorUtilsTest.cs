using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Janelia
{
    public class Base1 {}
    public class Final1a : Base1 {}
    public class Mid1b : Base1 {}
    public class Final1b : Mid1b {}
    public class Base2 {}

    public static class EasyMLEditorUtilsTest
    {
        [Test]
        public static void TestFindSubclasses1()
        {
            Type[] subs = EasyMLEditorUtils.GetSubclasses(typeof(Base1));

            Assert.That(subs, Has.Length.EqualTo(3));
            Assert.That(subs, Has.Exactly(1).EqualTo(typeof(Final1a)));
            Assert.That(subs, Has.Exactly(1).EqualTo(typeof(Mid1b)));
            Assert.That(subs, Has.Exactly(1).EqualTo(typeof(Final1b)));
        }

        [Test]
        public static void TestFindSubclasses2()
        {
            Type[] subs = EasyMLEditorUtils.GetSubclasses(typeof(Base2));

            Assert.That(subs, Has.Length.EqualTo(0));
        }

        [Test]
        public static void TestFindFinalSubclasses1()
        {
            Type[] subs = EasyMLEditorUtils.GetFinalSubclasses(typeof(Base1));

            Assert.That(subs, Has.Length.EqualTo(2));
            Assert.That(subs, Has.Exactly(1).EqualTo(typeof(Final1a)));
            Assert.That(subs, Has.Exactly(1).EqualTo(typeof(Final1b)));
        }

        [Test]
        public static void TestFindFinalSubclasses2()
        {
            Type[] subs = EasyMLEditorUtils.GetFinalSubclasses(typeof(Base2));

            Assert.That(subs, Has.Length.EqualTo(0));
        }
    }
}
