# Janelia Package Installer

## Summary

This package (org.janelia.package-installer) overcomes a weakness of the standard [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html): for a local package installed from local files (e.g., from a cloned Github repository), the packages it depends on must be installed manually in the correct order.  This package adds a "Install Package and Dependencies" item in the Unity editor's "Window" menu, which launches a user interface to install the dependencies of a package along with the package, or a list of packages from a "manifest" file.

## Installation

The [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation) give details of installing [org.janelia.package-installer](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer) itself, and then using it to install another package with its dependencies.  Note that `org.janelia.package-installer` requires Unity version 2019.3.0 or later, to avoid an error about invalid package names with the `file:` prefix.

## Details

This package also can install multiple packages (and their dependencies) at once.  After selecting "Install Package and Dependencies" from the Unity editor's "Window" menu, use the file chooser that appears to choose a "manifest" file listing multiple packages in a [JSON](https://en.wikipedia.org/wiki/JSON) array.  An example `manifest.json` file might be:
```
[
    "org.janelia.general",
    "org.janelia.fictrac-collision"
]
```
The packages may be listed in any order.  Choosing this manifest file would load a number of packages, including `org.janelia.collision-handling` (needed by `org.janelia.fictrac-collision`) and `org.janelia.logging` (needed by both  `org.janelia.general` and `org.janelia.fictrac-collision`), as well as `org.janelia.general` and `org.janelia.fictrac-collision` themselves.

When a manifest file contains only the names of packages, as above, then a few rules are used to look for the code for the named packages.  The first rule is to check for a `janelia-unity-toolkit` directory that is sibling of the manifest file; this rule would resolve the name for the `manifest1.json` file in the following excerpt of a directory structure:
```
VR/
   janelia-unity-toolkit/
                         org.janelia.camera-utilities/
                         org.janelia.collision-handling/
                         org.janelia.fictrac-collision/
                         org.janelia.general/
                         org.janelia.logging/
   manifest1.json
   manifests/
             manifest2.json
```
The second rule is to look in the manifest file's parent directory; this rule would resolve `manifest2.json`.  A manifest file also can list full path names like `"C:\\Users\\hubbardp\\VR\\janelia-unity-toolkit\\org.janelia.general"`, for maximum flexibility.

## Testing

To run this package's unit tests, use the following steps:
1. Create a new Unity project and add this package.
2. In the directory for the new project, in its "Packages" subdirectory, edit the "manifest.json" file to add a `"testables"` section as follows:
```
{
  "dependencies": {
   ...
  },
  "testables": ["org.janelia.package-installer"]
}
```
Note the comma separating the `"dependencies"` and `"testables"` sections.
3. In the Unity editor's "Window" menu, under "General", choose "Test Runner".
4. In the new "Test Runner" window, choose the "EditMode" tab.
5. There should be an item for the new project, with items underneath it for "Janelia.Package-installer.EditorTests.dll", etc.
6. Press the "Run All" button.
7. All the items under the new project will have green check marks if the tests succeed.
