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

```json
{
  "timeoutSecs": 0,
  "backgroundCylinderTexture": "C:\Users\labadmin\Documents\experiments\experiment03\cylinder.png"
}
```

Note that with this approach it would be possible to use one standalone executable for a number of experiments that differ only in the particular background texture being presented to the subject.
For now, at least, the texture file should be specified with a full path, but that path can have `\` or `\\` or `/` as separator characters.

For additional flexibility, the `"backgroundCylinderTexture"` value can be the (full) path to a [JSON](https://www.json.org) file that specifies a _sequence_ of background-cylinder textures and their timing.  An example of the file is as follows:

```json
{
    "durationSecs": 30,
    "textures" : [
        "textures/A.png",
        "textures/B.png",
        "textures/C.png"
    ],
    "separatorDurationSecs": 5,
    "separatorTexture": "C:\Users\labadmin\Documents\experiments\S.png"
}
```

With this specification, the session will proceed as follows:

1. the Unity splash screen will be visible for a few seconds;
2. the separator texture, `S.png`, will be visible for 5 seconds;
3. the first texture, `A.png`, will be visible for 30 seconds;
4. the separator texture, `S.png`, will be visible for 5 seconds;
5. the second texture, `B.png`, will be visible for 30 seconds;
6. the separator texture, `S.png`, will be visible for 5 seconds;
7. the final texture, `C.png`, will be visible for 30 seconds;
8. the separator texture, `S.png`, will be visible for 5 seconds;
9. the session will end.

An element in the `"textures"` array can be a path to directory, in which case all the image files in that directory will be used in sorted order (and non-image files will be skipped).

If `"separatorTexture"` is ommitted, a plain black texture will be used as the separator.  The textures may be specified as full paths (e.g., the separator texture in the example, above), or as paths relative to the location of the JSON file itself (e.g., the other textures in the example, above).

Each time the background texture is changed, an entry is written to the log file, with the form of these examples:

```json
{
    "timeSecs": 5.036252498626709,
    "frame": 301.0,
    "timeSecsAfterSplash": 5.016252517700195,
    "frameAfterSplash": 299.0,
    "backgroundTextureNowInUse": "C:\\Users\\labadmin\\Documents\\experiments\\experiment04/textures/A.png",
    "durationSecs": 30.0
}
```
```json
{
    "timeSecs": 35.051822662353519,
    "frame": 2102.0,
    "timeSecsAfterSplash": 35.031822204589847,
    "frameAfterSplash": 2100.0,
    "separatorTextureDurationSecs": 5.0
}
```

### Janelia.BackgroundUtilities

#### Janelia.BackgroundUtilities.SetCylinderTextureOffset

Applies `SetTextureOffset` to the texture on the background cylinder, moving the texture horizontally and/or vertically.  Can be used for animating the texture offset (e.g., when called in the `Update` function of a `MonoBehaviour`).
