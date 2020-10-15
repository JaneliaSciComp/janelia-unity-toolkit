# Janelia Package Installer

## Summary

This package (org.janelia.package-installer) overcomes a weakness of the standard [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html): for a local package installed from local files (e.g., from a cloned Github repository), the packages it depends on must be installed manually in the correct order.  This package adds a "Install Package and Dependencies" item in the Unity editor's "Window" menu, which launches a user interface to install the dependencies of a package along with the package.

## Installation

The [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation) give details of installing [org.janelia.package-installer](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer) itself, and then using it to install another package with its dependencies.

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
