# Janelia Background

## Summary

This package (org.janelia.background) adds functionality for background displays, like a "horizon" images on a cylinder around the experimental subject (e.g., a fly).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Janelia.SetupCylinderBackground

Adds the `Window/Set Up Background Cylinder` menu item in the Unity editor.  This menu item launches a dialog for placing a background cylinder around a subject `GameObject`.

### Janelia.StartupCylinderBackground

Code that runs when a standalone executable starts, to load a texture to be placed on the background cylinder built by `Janelia.SetupCylinderBackground`.  The path to the texture file is a *session parameter* as implemented by the
[org.janelia.general package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.general).  The session parameter is named `backgroundCylinderTexture`, and an example of how it appears in the launcher script's user interface (along with the standard `timeoutSecs` parameter) is the following:

```
{
  "timeoutSecs": 0,
  "backgroundCylinderTexture": "C:\Users\labadmin\Documents\experiments\experiment03\cylinder.png"
}
```

Note that with this approach it would be possible to use one standalone executable for a number of experiments that differ only in the particular background texture being presented to the subject.
For now, at least, the texture file should be specified with a full path, but that path can have `\` or `\\` or `/` as separator characters.
