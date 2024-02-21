# Janelia FicTrac Collision-Handling Support

## Summary

This package (org.janelia.fictrac-collision) supports collision detection and response for kinematic motion coming from the FicTrac system ([http://rjdmoore.net/fictrac/](http://rjdmoore.net/fictrac/)).  FicTrac is a "webcam-based method for tracking spherical motion and generating fictive animal paths."  The basic communication with FicTrac is handled by the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### `Janelia.FicTracSubject`

Makes the associated `GameObject` have collision detection and response (per the [org.janelia.collision-handling package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling)) for kinematic motion from the FicTrac system.  Uses `Janelia.FicTracUpdater`.

### `Janelia.FicTracUpdater`

Uses `Janelia.SocketMessageReader` from the [org.janelia.io package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io).

### `Janelia.FicTracSubjectIntegrated`

A drop-in replacement for `Janelia.FicTracSubject`, with a few behavioral differences:
* it uses the "integrated animal heading (lab)" sent by FicTrac, as mentioned in the [FicTrac data_header.txt]( https://github.com/rjdmoore/fictrac/blob/master/doc/data_header.txt);
* it does not add collision handling;
* it does not support data smoothing or the `smoothingCount` field.


### `Janelia.FicTracThresholder`

Blocks the FicTrac device from reporting the free spinning of the trackball (due to the ambient air flow) if the subject animal (fly) lifts its legs off the trackball. This class is added automatically along with `Janelia.FicTracUpdater` or `Janelia.FicTracSubjectIntegrated`, but its effects are disabled by default. 

To enable it, build a standalone application and run it with the [launcher script created by the org.janelia.logging package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling). In the "session parameters" at the bottom of the dialog box created by the script, there will be lines like the following:
```
{
  "ficTracSpinWindow": 10,
  "ficTracSpinThreshold": 999999
}
```
(Other values may also appear in that block.)

The window value is the number of prececding frames over which the heading (spin) angular speed is averaged, to give an estimate of the low-frequency component of the angular speed at the current frame. The window size should be longer than the number of frames when each foot of the fly is actively pushing on the trackball during fast walking. (That way, the average during walking will include frames with angular speed of zero, and thus will be lower than the average during continuous free spinning.) Try the default value of 10 but consider making it larger if necessary.

The default threshold value of 999999 is high enough to disable the blocking. To enable blocking, set it to a value _greater_ than the heading (spin) angular speed, averaged over the window, that occurs in normal walking, and _less_ than the averaged angular speed that occurs when the fly has lifted its legs and the trackball is spinning freely.

To determine the threshold value, perform two configuration runs as follows.
1. First, run the standalone application with a subject fly walking normally but near its fastest pace. After letting it the application run for 30 seconds or so, close it and open the `Player.log` text file, from the
[place where Unity stores its logs](https://docs.unity3d.com/Manual/LogFiles.html). Search the file for the lines including "percentile":
    ```
    FicTrac spin (heading) angular speed, percentiles 1 to 10: 21.2, 22.2, 22.9, 23.0, 23.4, 23.5, 23.6, 23.8, 24.0, 24.2
    FicTrac spin (heading) angular speed, percentiles 90 to 99: 41.1, 41.4, 42.5, 42.6, 43.5, 44.1, 44.4, 45.3, 47.3, 48.8    
    ```
    Remember the 99th percentile value (e.g., 48.8 in this example); call it _A_.
2. Next, run the standalone application with no subject fly, so the FicTrac trackball spins freely. After 30 seconds or so, close the application, and search the new `Player.log` for the lines including "percentile":
    ```
    FicTrac spin (heading) angular speed, percentiles 1 to 10: 55.7, 57.3, 58.3, 58.9, 59.2, 59.3, 59.6, 59.8, 60.0, 60.2
    FicTrac spin (heading) angular speed, percentiles 90 to 99: 72.3, 72.8, 73.4, 73.9, 74.3, 74.6, 75.1, 75.8, 76.8, 80.6
    ```
    Remember the 1st percentile value (e.g., 55.7 in this example); call it _B_.
3. Pick a value between _A_ and _B_ (e.g., 54 in this example) and use it for the value of `"ficTracSpinThreshold"` in the dialog box created by the launcher script. The value entered here will be preserved across sessions.
4. If _A_ is _not_ less than _B_, then try using a higher percentile for _A_ and/or a lower percentile for _B_.
