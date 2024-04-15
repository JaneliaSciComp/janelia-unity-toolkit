# Janelia Background

## Summary

This package (org.janelia.background) adds functionality for background displays, like a "horizon" images on a cylinder around the experimental subject (e.g., a fly).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### `Janelia.SetupCylinderBackground`

Adds the `Window/Set Up Background Cylinder` menu item in the Unity editor.  This menu item launches a dialog for placing a background cylinder around a subject `GameObject`.  The cylinder is constrained to translate along with the subject, but note that this behavior occurs only in a stand-alone executable or when the "play" button is pressed in the Unity editor.  The cylinder's rotation does not change as the subject moves, but the dialog does give an option for setting its initial rotation around its spine (_y_ axis), which controls the orientation of the texture on the cylinder.

### `Janelia.StartupCylinderBackground`

Code that runs when a standalone executable starts, to load a texture to be placed on the background cylinder built by `Janelia.SetupCylinderBackground`.  The path to the texture file is a *session parameter* as implemented by the
[org.janelia.general package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.general).  The session parameter is named `backgroundCylinderTexture`, and an example of how it appears in the launcher script's user interface (along with the standard `timeoutSecs` parameter) is the following:

```json
{
  "timeoutSecs": 0,
  "backgroundCylinderTexture": "C:\Users\labadmin\Documents\experiments\experiment03\cylinder.png"
}
```

An optional second texture can be added with the `backgroundCylinderTexture2` session parameter, and the final image on the cylinder will be the second texture composited over the first.  This mixing of textures is most useful when the second texture has a transparent background (as supported by PNG files).
```json
{
  "timeoutSecs": 0,
  "backgroundCylinderTexture": "C:\Users\labadmin\Documents\experiments\experiment03\cylinder.png",
  "backgroundCylinderTexture2": "C:\Users\labadmin\Documents\experiments\experiment03\cylinder2.png"
}
```

Note that with this approach it would be possible to use one standalone executable for a number of experiments that differ only in the particular background texture being presented to the subject. For now, at least, the texture file should be specified with a full path, but that path can have `\` or `\\` or `/` as separator characters.

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

For now, at least, the `backgroundCylinderTexture2` parameter can be only the path to a single image, which will be reused for every texture in the sequence for `backgroundCylinderTexture`.

### `Janelia.BackgroundUtilities`

#### `Janelia.BackgroundUtilities.SetCylinderTextureOffset`

Applies `SetTextureOffset` to the texture on the background cylinder, moving the texture horizontally and/or vertically. Using the optional argument `which = 2` applies the offset to the second texture (i.e., from the `backgroundCylinderTexture2` session parameter). Can be used for animating the texture offset (e.g., when called in the `Update` function of a `MonoBehaviour`).


### `Janelia.KeyboardMoveBackground`

Gives keyboard controls for the offset (horizontal and/or vertical displacement) of the second background texture (i.e., from the `backgroundCylinderTexture2` session parameter). Add this script to the subject `GameObject` that has the background cylinder around it. The arrow keys control the offset by default; to use the W, A, S, D keys instead, set `keykeyWASD` to `true`. By default, *compensation* for the heading rotation of this subject is applied to the second texture's offset before the offset specified by the keyboard, so this texture does not move in a camera parented to the subject; to disable this compensation, set `headingCompensation` to `false`.