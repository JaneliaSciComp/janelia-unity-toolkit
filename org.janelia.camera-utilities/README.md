# Janelia Camera Utilities

## Summary
This package (org.janelia.camera-utilities) implements various utilities related to cameras.  For example, there is code to implement [off-axis perspective projection using the approach of Robert Kooima](http://csc.lsu.edu/~kooima/articles/genperspective/).

## Installation
Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).


## Details

### Janelia.OffAxisPerspectiveCamera

Makes the attached camera use off-axis perspective projection.

### Janelia.AdjoiningDisplaysCamera and Janelia.AdjoiningDisplaysCameraBuilder

Unity supports ["multi-display"](https://docs.unity3d.com/Manual/MultiDisplay.html), with a game having multiple cameras each shown on its own external display monitor.  Framerates above 60 Hz are not possible, though (as of 2019).

The `Janelia.AdjoiningDisplaysCamera` script implements an alternative with higher performance.  It combines the multiple camera images into one wide image that is associated with another, main camera.  The associated `Janelia.AdjoiningDisplaysCameraBuilder` builds a standalone executable with the special options to make the main camera's wide image extend across all the external displays, putting the appropriate part on each display.

To use `Janelia.AdjoiningDisplaysCamera` on Windows:

1. Connect the external displays so they are numbered `2` through `N` in the Windows "Display Settings".  Make sure that `2` is the leftmost, and then the display numbers increase in order.

<p align="center">
<img src="./adjoiningDisplaysCamera.PNG" height="400">
</p>

2. In the "Display Settings", give each display the appropriate resolution (e.g., 1920 horizontal and 1080 vertical).  The resolution must be the same for all displays, and they must have the same "Orientation".

3. In the "Display Settings", make sure the "Multiple displays" fields says "Extend desktop to this display" (not "Duplicate desktop").

4. In the Unity editor, add the `AdjoiningDisplaysCamera` script to one camera, such as the standard `Main Camera`.  The "Target Display" for this camera should be "Display 1".

5. Open `AdjoiningDisplaysCamera`'s "Display Cameras" section, set the "Size" field to the number of external displays, and set "Element `i`" to the camera for external display `i+1` (because the first "Element" is `0` but the first external display is `1`).

6. Set `AdjoiningDisplaysCamera`'s "Display Width" and "Display Height" fields to match the external display resolution from step 2.

7. To build the standalone executable, use the menu item "File/Build and Make Adjoining-Displays Shortcut", which triggers code in `AdjoiningDisplaysCameraBuilder`.  When the build is complete, it adds a shortcut file, `standalone`, to the Unity project's root folder.  This shortcut runs the executable with the necessary [command line arguments](https://docs.unity3d.com/Manual/CommandLineArguments.html) to make the wide image extend onto all the external displays (i.e., `-popupwindow -screen-fullscreen 0 -monitor 2`)

When the game is running, it can display a _progress box_, a small square that alternates between black and white with each frame (so a photodiode attached to an oscilloscope can give an accurate indication of the frame rate).  
- Pressing the `c` key changes which display shows the progress box.
- Pressing the `p` key changes which corner of that display contains the progress box, with the fifth press hiding the progress box altogether.

Also, pressing the `m` key toggles mirroring of the displays on and off (useful for back-projected displays).

### Frame Packing

For animal participants who can see very fast changes (e.g., the _Drosophila_ fruit fly), the `AdjoiningDisplaysCamera` supports an optional way of improving the visual smoothness through "frame packing" for DLP (digital light processing) projectors.   When ready to render frame _i_ at time _t\_i_, `AdjoiningDisplaysCamera` interpolates the camera pose (position, orientation) at three fractions of the interval from _t\_i-1_ to _t\_i_, and renders the scene at each fraction.  It then packs the resulting image from each fraction into one color channel of the image that is finally displayed.  [A DLP displays each color channel successively](https://www.benq.com/en-us/business/resource/trends/dlp-and-3lcd-projectors.html) for a fraction of the overall frame time, so the net effect is that the images for the interpolated camera poses are visible as frames at a higher frame rate to animals capable of seeing at the higher rate.  The higher-rate frames appear in grayscale instead of color, but that is acceptable in some applications.

The Unity Editor's Inspector gives control over what the three fractions are, and what color channel corresponds to each fraction.  The fractions are controlled with three 0-to-1 sliders.  What color channel corresponds to each fraction is controled by a "packing order" value; a packing order of "GRB", for example, indicates that the first fraction corresponds to the display of green, the second fraction to red and the third to blue.

Note that while frame packing increases effective frame rate and smoothness, it does _not_ reduce latency.  In fact, it actually increases the latency by roughly the time to draw one frame.
