using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Janelia
{
    public static class PackageNodeTest
    {
        [Test]
        public static void Test1()
        {
            char Sep = Path.DirectorySeparatorChar;
            string pathRoot = Path.GetTempPath() + "Test1";
            DirectoryInfo diRoot = Directory.CreateDirectory(pathRoot);

            // A uses nothing
            // B uses A

            setupDependency(pathRoot, "a/Runtime", new string[] { });
            setupDependency(pathRoot, "b/Runtime", new string[] { "A.Runtime" });

            PackageNode nodeB = new PackageNode(pathRoot + Sep + "org.janelia.b");
            List<string> topoSort = PackageNode.TopoSort();

            Assert.AreEqual(2, topoSort.Count);
            Assert.AreEqual("org.janelia.a", Path.GetFileName(topoSort[0]));
            Assert.AreEqual("org.janelia.b", Path.GetFileName(topoSort[1]));

            diRoot.Delete(true);
        }

        [Test]
        public static void Test2()
        {
            char Sep = Path.DirectorySeparatorChar;
            string pathRoot = Path.GetTempPath() + "Test2";
            DirectoryInfo diRoot = Directory.CreateDirectory(pathRoot);

            // A uses nothing
            // B uses A
            // C uses A, B

            setupDependency(pathRoot, "a/Runtime", new string[] { });
            setupDependency(pathRoot, "b/Runtime", new string[] { "A.Runtime" });
            setupDependency(pathRoot, "c/Runtime", new string[] { "A.Runtime", "B.Runtime" });

            PackageNode nodeC = new PackageNode(pathRoot + Sep + "org.janelia.c");
            List<string> topoSort = PackageNode.TopoSort();

            Assert.AreEqual(3, topoSort.Count);
            Assert.AreEqual("org.janelia.a", Path.GetFileName(topoSort[0]));
            Assert.AreEqual("org.janelia.b", Path.GetFileName(topoSort[1]));
            Assert.AreEqual("org.janelia.c", Path.GetFileName(topoSort[2]));

            diRoot.Delete(true);
        }

        [Test]
        public static void Test3()
        {
            char Sep = Path.DirectorySeparatorChar;
            string pathRoot = Path.GetTempPath() + "Test3";
            DirectoryInfo diRoot = Directory.CreateDirectory(pathRoot);

            // A's Editor uses nothing
            // B's Editor uses B's Runtime, A's Editor
            // C's Runtime uses B's Runtime

            setupDependency(pathRoot, "a/Editor", new string[] { });
            setupDependency(pathRoot, "b/Editor", new string[] { "B.Runtime", "A.Editor" });
            setupDependency(pathRoot, "c/Runtime", new string[] { "B.Runtime" });

            PackageNode nodeC = new PackageNode(pathRoot + Sep + "org.janelia.c");
            List<string> topoSort = PackageNode.TopoSort();

            Assert.AreEqual(3, topoSort.Count);
            Assert.AreEqual("org.janelia.a", Path.GetFileName(topoSort[0]));
            Assert.AreEqual("org.janelia.b", Path.GetFileName(topoSort[1]));
            Assert.AreEqual("org.janelia.c", Path.GetFileName(topoSort[2]));

            diRoot.Delete(true);
        }

        [Test]
        public static void Test4()
        {
            char Sep = Path.DirectorySeparatorChar;
            string pathRoot = Path.GetTempPath() + "Test4";
            DirectoryInfo diRoot = Directory.CreateDirectory(pathRoot);

            // C uses A
            // D uses A
            // E uses A, B
            // F uses C, D
            // G use E
            // H uses A, F, G

            setupDependency(pathRoot, "a/Runtime", new string[] { });
            setupDependency(pathRoot, "b/Runtime", new string[] { });
            setupDependency(pathRoot, "c/Runtime", new string[]{ "A.Runtime" });
            setupDependency(pathRoot, "d/Runtime", new string[] { "A.Runtime" });
            setupDependency(pathRoot, "e/Runtime", new string[] { "A.Runtime", "B.Runtime" });
            setupDependency(pathRoot, "f/Runtime", new string[] { "C.Runtime", "D.Runtime" });
            setupDependency(pathRoot, "g/Runtime", new string[] { "E.Runtime" });
            setupDependency(pathRoot, "h/Runtime", new string[] { "A.Runtime", "F.Runtime", "G.Runtime" });

            PackageNode nodeH = new PackageNode(pathRoot + Sep + "org.janelia.h");
            List<string> topoSort = PackageNode.TopoSort();

            string pre = pathRoot + Sep + "org.janelia.";

            Assert.AreEqual(8, topoSort.Count);
            Assert.Greater(topoSort.IndexOf(pre + "c"), topoSort.IndexOf(pre + "a"));
            Assert.Greater(topoSort.IndexOf(pre + "d"), topoSort.IndexOf(pre + "a"));
            Assert.Greater(topoSort.IndexOf(pre + "e"), topoSort.IndexOf(pre + "a"));
            Assert.Greater(topoSort.IndexOf(pre + "e"), topoSort.IndexOf(pre + "b"));
            Assert.Greater(topoSort.IndexOf(pre + "f"), topoSort.IndexOf(pre + "c"));
            Assert.Greater(topoSort.IndexOf(pre + "f"), topoSort.IndexOf(pre + "d"));
            Assert.Greater(topoSort.IndexOf(pre + "g"), topoSort.IndexOf(pre + "e"));
            Assert.Greater(topoSort.IndexOf(pre + "h"), topoSort.IndexOf(pre + "a"));
            Assert.Greater(topoSort.IndexOf(pre + "h"), topoSort.IndexOf(pre + "f"));
            Assert.Greater(topoSort.IndexOf(pre + "h"), topoSort.IndexOf(pre + "g"));

            diRoot.Delete(true);
        }

        private static void setupDependency(string pathRoot, string pkgAndFolder, string[] dependentOn)
        {
            char Sep = Path.DirectorySeparatorChar;

            string[] pkgAndFolderSplit = pkgAndFolder.Split('/');
            Assert.AreEqual(2, pkgAndFolderSplit.Length);
            string pkg = pkgAndFolderSplit[0];
            string folder = pkgAndFolderSplit[1];

            Directory.CreateDirectory(pathRoot + Sep + "org.janelia." + pkg);
            Directory.CreateDirectory(pathRoot + Sep + "org.janelia." + pkg + Sep + folder);

            string asmdefDeps = "";
            foreach (string dep in dependentOn)
            {
                if (asmdefDeps.Length > 0)
                    asmdefDeps += ", ";
                asmdefDeps += "\"Janelia." + dep + "\"";
            }

            string nameAsmdef = "Janelia." + pkg.ToUpper() + "." + folder + ".asmdef";
            string pathAsmdef = pathRoot + Sep + "org.janelia." + pkg + Sep + folder + Sep + nameAsmdef;
            File.WriteAllText(pathAsmdef, "{ \"references\": [ " + asmdefDeps + " ] }");
        }
    }
}
