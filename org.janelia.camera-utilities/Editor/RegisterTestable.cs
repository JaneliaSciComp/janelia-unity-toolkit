using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Janelia
{
    // Makes this package's tests visible in the Test Runner of any project that installs it.
    //
    // Unity lists tests from the project's `Assets` folder automatically, but tests that live inside
    // a package (this package's `Tests/Runtime`) appear only if the package name is present in the
    // `"testables"` array of the project's `Packages/manifest.json`. Packages installed with
    // "Add package from disk" (or the Janelia package installer) never get that entry, so the
    // Test Runner shows "No tests to show" for them.
    //
    // This class runs on every domain reload. If the manifest lacks the entry, it adds the entry and
    // asks the Package Manager to resolve again. If the entry is already there, it returns without
    // touching the file.
    [InitializeOnLoad]
    public static class CameraUtilitiesRegisterTestable
    {
        private const string PACKAGE = "org.janelia.camera-utilities";

        static CameraUtilitiesRegisterTestable()
        {
            string projectDir = Directory.GetParent(Application.dataPath).FullName;
            string manifestPath = Path.Combine(projectDir, "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return;
            }

            string json = File.ReadAllText(manifestPath);
            string quoted = "\"" + PACKAGE + "\"";

            Match match = Regex.Match(json, "\"testables\"\\s*:\\s*\\[(?<items>[^\\]]*)\\]");
            if (match.Success)
            {
                Group items = match.Groups["items"];
                if (items.Value.Contains(quoted))
                {
                    return;
                }
                string existing = items.Value.Trim();
                string updated = (existing.Length == 0) ? quoted : existing + ", " + quoted;
                json = json.Substring(0, items.Index) + updated + json.Substring(items.Index + items.Length);
            }
            else
            {
                string trimmed = json.TrimEnd();
                int close = trimmed.LastIndexOf('}');
                if (close < 0)
                {
                    Debug.LogWarning($"RegisterTestable: could not parse {manifestPath}; not adding {PACKAGE} to testables");
                    return;
                }
                json = trimmed.Substring(0, close).TrimEnd() + ",\n  \"testables\": [" + quoted + "]\n}\n";
            }

            File.WriteAllText(manifestPath, json);
            Debug.Log($"RegisterTestable: added {PACKAGE} to \"testables\" in {manifestPath}");
            Client.Resolve();
        }
    }
}
