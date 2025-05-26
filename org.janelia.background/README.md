# Janelia Background

## Summary

This package (org.janelia.background) adds functionality for background displays, like a "horizon" images on a cylinder around the experimental subject (e.g., a fly).

_Note: performance improves after rebooting the system in some cases._  See the [section on performance (below)](#performance) for more details.

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

An element in the `"textures"` array can be a path to directory, in which case all the image files in that directory will be used in sorted order (and non-image files will be skipped). The image files in the directory will be sorted, but note that to achieve a "human" sort it is necessary to pad any numbers with leading zeros (a common practice in graphics applications like [Blender](https://docs.blender.org/manual/en/latest/render/output/properties/output.html)).  For example, the naive file names might be:
```
f1, f2, f10, f11, f100, f101
```
Those names would be sorted as:
```
f1, f10, f100, f101, f11, f2
```
To get the more expected order use:
```
f001, f002, f010, f011, f100, f101
```

If `"separatorTexture"` is ommitted, a plain black texture will be used as the separator.  The textures may be specified as full paths (e.g., the separator texture in the example, above), or as paths relative to the location of the JSON file itself (e.g., the other textures in the example, above).

Each time the background texture is changed, an entry is written to the [Janelia Unity Toolkit log file](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging), with the form of these examples:

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

## Performance

This code should be able to update background textures at 120 Hz (i.e., `"durationSecs": 0.0083`) on a modern Windows 11 system with a modern GPU connected to a 120 Hz display.

If it won't, first _try rebooting the system_.  It seems that sometimes performance improves after resources related to rendering are reset.

To check performance, look for a line like the following in [standard Unity log, `Player.log`](https://docs.unity3d.com/2022.3/Documentation/Manual/LogFiles.html) for the `Test/spec-03-120hz.json`:
```
Showing backgrounds took 1.986 sec (1986 ms)
```

Detailed performance information also apperars in the [Janelia Unity Toolkit log file](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging), in an entry like the following near the end of the log:
```json
{
    "timeSecs": 1.9941792488098145,
    "frame": 231.0,
    "timeSecsAfterSplash": 1.9941792488098145,
    "frameAfterSplash": 231.0,
    "splashScreenDurationSec": 0.08500000089406967,
    "backgroundsTotalDurationSec": 1.9819999933242798,
    "expectedBackgroundsTotalDurationSecs": 1.9919942617416382,
    "backgroundsTotalCount": 240,
    "complete": false,
    "skippedBackgrounds": [
        1,
        6,
        23,
        48,
        76,
        87,
        105,
        106,
        107,
        156
    ]
},
```

The `"skippedBackgrounds"` section lists the background textures that were not shown in order to keep the overall performance on schedule for meeting the expected duration (as in `"expectedBackgroundsTotalDurationSecs"`).

If the size of `"skippedBackgrounds"` is larger than desired, try making a few changes to the JSON input file that lists the background textures:
* Set `"durationSecs"` to match the refresh rate of the display device (e.g., 1 / 120 = 0.0083 for a 120 Hz display).
* Set `"complete": true`.

The `"complete"` setting instructs the code to display exactly one background texture per frame, regardless of whether a frame has fallen behind the schedule for meeting the expected duration.  In the fragment of the log above, using `"complete": true` would have most effect around frame 105, where some Windows operating system issues caused the momentary slowness that was mitigated by skipping some background textures.

Note that setting `"complete": true` will have no effect if `"durationSecs"` does not match thd refresh rate of the display device, or if there are separator textures with `separatorDurationSecs"` that does not equal `"durationSecs"`.

## Testing

This project's `Test` directory (folder) contains JSON files with sequences of background-cylinder textures, and these files can be used for testing correctness and performance.

To run the tests:
1. Create a Unity project using this package, e.g., `C:\Users\scientist\Documents\UnityAppWithBackground`.
2. [Disable the splash screen](https://docs.unity3d.com/2022.3/Documentation/Manual/playersettings-windows.html#Splash), which shows the Unity logo at startup, in the project's "player settings". Doing so is not strictly necessary, but gives more useful timings without the extra couple of seconds spent displaying the splash screen.
3. Build the project.
4. Open a Windows shell (e.g., PowerShell) and navigate to the directory containing the executable (`.exe` file).
5. In the shell, run a command like the following:
    ```PowerShell
    Measure-Command { Start-Process UnityAppWithBackground.exe -ArgumentList '-backgroundCylinderTexture ../../janelia-unity-toolkit/org.janelia.background/test/spec-01-1hz.json' -Wait }
    ```
    The argument `-backgroundCylinderTexture` should be follwed by the _relative path_ to the JSON file for the test being run, with that path being _relative to the executable's directory_ (so in this example, the JSON file is in `C:\Users\scientist\Documents\janelia-unity-toolkit\org.janelia.background\test`).
6. The executable should run, displaying the sequence of background images specified by the JSON file.
7. When the executable finishes, the shell should display the timing (duration) of the executable.  Note that even with the splash screen disabled, all Unity executables have a little extra overhead, which is probably about 1 second for a modern Windows 11 laptop.
8. As mentioned in the [section on performance](#performance), the
[standard Unity log, `Player.log`](https://docs.unity3d.com/2022.3/Documentation/Manual/LogFiles.html) and the [Janelia Unity Toolkit log file](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging) have more detailed information about exactly how much time was spent on the display of the background images (without the extra overhead).
