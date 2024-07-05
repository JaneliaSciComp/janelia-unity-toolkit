# Janelia Package Installer

## Summary

This package (org.janelia.package-installer) overcomes a weakness of the standard [Unity Package Manager user interface](https://docs.unity3d.com/Manual/upm-ui.html): for a local package installed from local files (e.g., from a cloned Github repository), the packages it depends on must be installed manually in the correct order.  This package adds a "Install Package and Dependencies" item in the Unity editor's "Window" menu, which launches a user interface to install the dependencies of a package along with the package, or a list of packages from a "manifest" file.

Another feature of this package is a class that scripts can use to programmatically load local packages.  This class, `Janelia.AssetPackageImporter`, hides the complexities loading packages with the standard  [`AssetDatabase`](https://docs.unity3d.com/ScriptReference/AssetDatabase-importPackageStarted.html) class.  This feature is most useful for [local asset packages](https://docs.unity3d.com/Manual/AssetPackages.html) stored in local `.unitypackage` files.

## Installation

The [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation) give details of installing [org.janelia.package-installer](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer) itself, and then using it to install another package with its dependencies.  Note that `org.janelia.package-installer` requires Unity version 2019.3.0 or later, to avoid an error about invalid package names with the `file:` prefix.

## Details

To install one package and its dependencies, choose "Install Package and Dependencies" from the Unity editor's "Window" menu.  Then use the file chooser that appears to navigate to and choose the `package.json` file for that one package.  Another dialog will appear listing the package and dependent packages to be installed.  If the list is correct, press the "Install" button.

To install multiple packages and their dependencies from a "manifest" file, choose the "Window/Install Package and Dependencies" menu item, and this time use the file chooser to select the manifest file listing multiple packages in a [JSON](https://en.wikipedia.org/wiki/JSON) array.  An example `manifest.json` file might be:
```json
[
    "org.janelia.general",
    "org.janelia.fictrac-collision"
]
```
The packages may be listed in any order.  Choosing this manifest file would load a number of packages, including `org.janelia.collision-handling` (needed by `org.janelia.fictrac-collision`) and `org.janelia.logging` (needed by both  `org.janelia.general` and `org.janelia.fictrac-collision`), as well as `org.janelia.general` and `org.janelia.fictrac-collision` themselves.

When a manifest file contains only the names of packages, as above, then a heuristic search looks for the named packages on the local file system.  The search starts with the parent directory (folder) of the manifest file and progresses back towards the root directory.  The search checks for packages in each directory visited this way, and also in each of that directory's subdirectories whose names contain the string "unity" (e.g., "janelia-unity-toolkit", "more-unity-packages").  A package is loaded from the first such directory where it is found.

Consider the following example file system:
```
VR/
   janelia-unity-toolkit/
                         org.janelia.general/
                         org.janelia.logging/
   more-unity-packages/
                       edu.university.main/
                       edu.university.used-by-main/
   manifest1.json
   manifests/
             manifest2.json
   testing/
           janelia-unity-toolkit/
                                 org.janelia.general/
                                 org.janelia.logging/
           manifest3.json
```
Each of the three manifest files is as follows:
```json
[
    "org.janelia.general",
    "edu.university.main"
]
```

The specifc packages selected for `manifest1.json` will be:
```
VR/janelia-unity-toolkit/org.janelia.general
VR/janelia-unity-toolkit/org.janelia.logging
VR/more-unity-packages/edu.unversity.main
VR/more-unity-packages/edu.unversity.used-by-main
```
The same specific packages will be selected for `manifest2.json`.

But for `manifest3.json` the packages will be:
```
VR/testing/janelia-unity-toolkit/org.janelia.general
VR/testing/janelia-unity-toolkit/org.janelia.logging
VR/more-unity-packages/edu.unversity.main
VR/more-unity-packages/edu.unversity.used-by-main
```

This heuristic for resolving packages is bypassed if the manifest file lists packages with full path names like:
`"C:\\Users\\hubbardp\\VR\\janelia-unity-toolkit\\org.janelia.general"`.

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
