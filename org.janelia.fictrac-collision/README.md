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

Prevents the FicTrac device from reporting chaotic motion if the subject animal (fly) lifts its legs off the FicTrac trackball (allowing the ball to spin chaotically in the ambient air flow). This class is added automatically along with `Janelia.FicTracUpdater` or `Janelia.FicTracSubjectIntegrated`, but its effects are disabled by default. 

To enable it, build a standalone application and run it with the [launcher script created by the org.janelia.logging package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling). In the "session parameters" at the bottom of the dialog box created by the script, there will be a line like the following:
```
{
  "ficTracSpinThreshold": 999999
}
```
(Other values may also appear in that block.) Change the `999999` to a threshold value _greater_ than the spin (heading) angular speed that occurs when the fly is standing or walking normally, and _less_ than the angular speed that occurs when the fly has lifted its legs and the trackball is spinning chaotically.

To determine the threshold value, perform two configuration runs.
1. First, run the standalone application with a subject fly walking normally. After letting it run for 30 seconds or so, close the application and open the `Player.log` text file, from the
[place where Unity stores its logs](https://docs.unity3d.com/Manual/LogFiles.html). Search the file for the lines including "percentile":
    ```
    FicTrac spin (heading) angular speed, 1st percentile: 37.8
    FicTrac spin (heading) angular speed, 99th percentile: 236.4
    ```

    Remember the 99th percentile value; call it _A_.
2. Next, run the standalone application with no subject fly, so the FicTrac trackball spins chaotically. After 30 seconds or so, close the application, and search the new `Player.log` for the lines including "percentile":
    ```
    FicTrac spin (heading) angular speed, 1st percentile: 752.2
    FicTrac spin (heading) angular speed, 99th percentile: 2887.0
    ```
    Remember the 1st percentile value; call it _B_.
3. Pick a value between _A_ and _B_ and use it for the value of `"ficTracSpinThreshold"` in the dialog box created by the launcher script. The value entered here will be preserved across sessions.
4. If _A_ is _not_ less than _B_, then further analysis is required.  One approach involves changing the value of `PERCENTILES` in the `FicTracSpinThresholder.cs` file; raising it to a value greater than 1 will display more percentiles in steps 1 and 2 above, which might indicate appropriate values for _A_ and _B_.
