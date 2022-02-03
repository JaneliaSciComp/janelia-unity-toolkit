# Janelia Experimental Technology's Trackball (jETTrac) Support

## Summary

This package (org.janelia.jettrac) supports the use in Unity of jETTrac, a system for tracking the motion of an animal walking on a ball, and also the rotation of the animal's head.  The jETTrac system is built by [Janelia Experimental Technologies (jET)](https://www.janelia.org/support-team/janelia-experimental-technology).

The jETTrac device communicates to the host computer through USB ports as a [USB HID](https://en.wikipedia.org/wiki/USB_human_interface_device_class) device. It uses the
[raw HID](http://www.pjrc.com/teensy/rawhid.html) protocol to support high update rates and low latency.  The code to read the raw HID data is a [native plugin](https://docs.unity3d.com/Manual/NativePlugins.html) written in C.


## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation) to install this package and its dependency, [org.janelia.io](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).  The native plugin to actually read data from the device is a dynamically loaded library (i.e., the `SimpleRawHid.dll` file on Windows) that must be somewhere under the `Assets` folder of the project.  No extra steps should be necessary to get a `dll` built for Windows 10: it should get installed autotically from the [`Assets` folder of org.janelia.jettrac](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.jettrac/Assets).  Should it be necessary to build the library from source code, that source code is in the [`SimpleRawHid` folder of org.janelia.jettrac](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.jettrac/SimpleRawHid).

Once the packages are installed, the host computer should recognize the jETTrac device when its cables (one for the ball tracker and one for the head encoder) are plugged into the host's USB ports.  If any odd errors occur, try unplugging the cables and replugging them again.

## Calibration

To use the output of the jETTrac ball tracker in a Unity application, there must be a calibration scale to map the ball tracker's native measurements (the raw pixel displacements in the `JetTracParser.BallMessage` fields `x1`, `y1`, `x2`, `y2`) to Unity distance units.  By default, the code has a calibration scale that should be about right for a ball with 20 cm diameter and Unity units where 1 unit is 10 cm (0.1 m).  These Unity units seem to work better with jETTrac than the Unity default of 1 unit being 1 m.

To recalibrate the ball tracker, choose the "jETTrac Manual Calibration" menu item from the Unity editor's "Window/jETTrac" menu.  Enter the ball diameter (in Unity units) and press the "Start calibration" button.  Then manually rotate the ball once around as smoothly and precisely as possible.  Once set, the calibration scale is stored (i.e., in the Windows registry) and reused for all future sessions.

The jETTrac head encoder measures rotation in degrees, and needs no calibration.

## Details

### Casing

The official abbreviation of ["Janelia Experimental Technology"](https://www.janelia.org/support-team/janelia-experimental-technology) group is "jET", with a lowercase "j" followed by uppercase "ET".  That casing is perserved in the name of the device, "jETTrac", but that casing is awkward in code.  So code identifiers like class names follow a more standard ["camel case"](https://en.wikipedia.org/wiki/Camel_case) pattern with "Jet" instead of "jET", as in `JetTracReader`. Some file names follow Unity conventions of all lower case, as in `org.janelia.jettrac`.

### `Janelia.ExampleUsingJetTrac`

Shows how to use `JetTracReader` to get messages from the device, and then use `JetTracTransformer` to convert these messages into changes to the kinematics (position and orientation) of a `GameObject` in Unity.

### `Janelia.JetTracReader`

Reads raw `Byte[]` data from the device, and uses `JetTracParser` to convert the bytes into the numeric fields of the `JetTracParser.BallMessage` and `JetTracParser.HeadMessage` structs.

### `Janelia.JetTracParser`

Parses the raw `Byte[]` data from the device.  Goes to some effort to avoid creating temporary values that would lead to garbage collection.

### `Janelia.JetTracTransformer`

Accumulates a series of messages read from the device (i.e., `JetTracParser.BallMessage` and `JetTracParser.HeadMessage` instances) and then computes the aggregate changes to the position and orientation of a `GameObject`.  Supports optional smoothing, implemented as a running average.

### `Janelia.JetTracIdentifier`

Determines which of multiple raw HID devices plugged in to the USB ports is the ball tracker, and which is the head encoder.

## Testing

To run this package's unit tests, use the following steps:
1. Create a new Unity project and add this package.
2. In the directory for the new project, in its `Packages` subdirectory, edit the `manifest.json` file to add a `"testables"` section as follows:
    ```
    {
      "dependencies": {
       ...
      },
      "testables": ["org.janelia.jettrac"]
    }
    ```
    Note the comma separating the `"dependencies"` and `"testables"` sections.
3. In the Unity editor's "Window" menu, under "General", choose "Test Runner".
4. In the new "Test Runner" window, choose the "PlayMode" tab.
5. There should be an item for the new project, with items underneath it for "Janelia.Jettrac.RuntimeTests.dll", etc.
6. Press the "Run All" button.
7. All the items under the new project will have green check marks if the tests succeed.
