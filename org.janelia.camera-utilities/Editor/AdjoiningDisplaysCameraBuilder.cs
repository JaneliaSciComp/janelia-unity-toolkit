using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

// A convenient way to build a stand-alone game executable ("stand-alone player") and
// then create a Windows shortcut file to run that executable with command line arguments.
// The arguments make sure that the wide image produced by `Janelia.AdjoiningDisplaysCamera`
// appears spread out across the external displays so the appropriate part appears on
// each display.

namespace Janelia {
    public class AdjoiningDisplaysCameraBuilder
    {
        public static void PerformBuild()
        {
           UnityEngine.Debug.Log("PerformBuild:");

            // When a standalone executable is running, `UnityEngine.Debug.Log` values are stored in
            // `C:\Users\<username>\AppData\LocalLow\<CompanyName>\<ProductName>\Player.log`.
            // So it is helpful to have <CompanyName> set to something more meaningful than the default
            // value, "DefaultCompany".  If <CompanyName> has not been reset, use the local computer's
            // domain name (e.g., "hhmi" for a computer named "workstation1.hhmi.org").
            if (PlayerSettings.companyName == "DefaultCompany")
            {
                string domainFull = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
                string domain = Path.GetFileNameWithoutExtension(domainFull);
                PlayerSettings.companyName = domain;
            }

            UnityEngine.Debug.Log("Company name: " + PlayerSettings.companyName);
            UnityEngine.Debug.Log("Product name: " + PlayerSettings.productName);

            string[] scenes = new string[SceneManager.sceneCount];
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                scenes[i] = SceneManager.GetSceneAt(i).path;
            }

            UnityEngine.Debug.Log("Scenes: " + string.Join(", ", scenes));

            if (!Directory.Exists("Build"))
            {
                Directory.CreateDirectory("Build");
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = "Build/standalone.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appData = appData.Substring(0, appData.LastIndexOf(Path.DirectorySeparatorChar));
            string log = Path.Combine(appData, "Local", "Unity", "Editor", "Editor.log");

            if (summary.result == BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            }

            if (summary.result == BuildResult.Failed)
            {
                UnityEngine.Debug.Log("Build failed: errors logged in " + log);
            }
        }

        [PostProcessBuildAttribute(0)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            UnityEngine.Debug.Log("OnPostprocessBuild: " + pathToBuiltProject);

            string builtNoExt = pathToBuiltProject.Substring(0, pathToBuiltProject.Length - 4);
            string[] builtNoExtSplit = builtNoExt.Split('/');
            int length = builtNoExtSplit.Length;
            string shortcutPath = builtNoExtSplit[0];
            for (int i = 1; i < length; i++)
            {
                if (i != length - 2)
                {
                    shortcutPath += "\\" + builtNoExtSplit[i];
                }
            }
            string targetPath = pathToBuiltProject.Replace('/', '\\');

            // In the .NET libraries available in Unity, there seems to be no direct way to
            // create a Windows shortcut file.  So generate a little script in the Jscript
            // language to create the shortcut, and run that script with `cscript.exe` in 
            // a new process.  Jscript and `cscript.exe` should be available in all modern
            // Windows installations by default.

            string scriptPath = MakeShortcutScript(shortcutPath, targetPath);

            UnityEngine.Debug.Log("Shortcut: " + shortcutPath);
            UnityEngine.Debug.Log("Target: " + targetPath);
            UnityEngine.Debug.Log("Script: " + scriptPath);

            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }

            Process process = new Process();
            process.StartInfo.FileName = @"C:\windows\system32\cmd.exe";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            process.StandardInput.WriteLine("cscript.exe \"" + scriptPath + "\"");
            process.StandardInput.Flush();
            process.StandardInput.Close();
        }

        public delegate int GetMonitorIndexDelegate();
        public static GetMonitorIndexDelegate getMonitorIndexDelegate;

        private static string MakeShortcutScript(string shortcutPath, string targetPath)
        {
            shortcutPath = shortcutPath.Replace("\\", "\\\\");
            targetPath = targetPath.Replace("\\", "\\\\");
            int monitor = (getMonitorIndexDelegate != null) ? getMonitorIndexDelegate() : 2;
            UnityEngine.Debug.Log("Left monitor: " + monitor);
            string[] lines = {
                "main();",
                "function main() {",
                "  var ws = new ActiveXObject('WScript.Shell');",
                "  var shortcut = ws.CreateShortcut('" + shortcutPath + ".lnk');",
                "  shortcut.WindowStyle = 4;",
                "  shortcut.TargetPath = '" + targetPath + "';",
                "  shortcut.Arguments = '-popupwindow -screen-fullscreen 0 -monitor " + monitor + "';",
                "  shortcut.Save();",
                "}"
            };

            string path = Path.GetTempPath();
            string fileName = "makeShortcut.js";
            string script = Path.Combine(path, fileName);

            if (File.Exists(script))
            {
                File.Delete(script);
            }

            using (StreamWriter outputFile = new StreamWriter(script))
            {
                foreach (string line in lines)
                    outputFile.WriteLine(line);
            }

            return script;
        }
    }
}
