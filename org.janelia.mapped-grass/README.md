# Janelia Mapped Grass Generator

## Summary

This package (`org.janelia.mapped-grass`) sets up a complete virtual world for a fly walking on a trackball among clumps of grass. The grass clumps are arranged stochastically based on various map files. The map files and other details are define in a "spec" file in the [JSON format](https://en.wikipedia.org/wiki/JSON).  

## Installation

1. Install `org.janelia.package-installer` by following the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#without-dependencies).
2. From the Unity editor's "Window" menu, choose "Install Package and Dependencies".
3. In the file chooser that appears, navigate to `org.janelia.mapped-grass` and choose the `package.json` file.
4. In the "PackageInstaller" window that appears, note that there should be about eight packages to be installed. Press the "Install" button.

## Setting Up a Virtual World

1. A "Mapped Grass" item should have appeared in the Unity editor's menu bar. Choose the "Set Up Grass" button.
2. In the "Set Up Mapped Grass" window that appears, press the "Choose spec file..." button.
3. In the file chooser that appears, navigate to the location of a JSON spec file.
4. The spec file must at least include the following values, set to the paths (relative to the location of the spec file) to CSV files:
    * `"objectDensityGridFile"`
    * `"intensityGridFile"`
    * `"heightGridFile"`
    * `"orientationProbDensityFile"`
    * `"elevationProbDensityFile"`
5. Back in the "Set Up Mapped Grass" window, press the "Set Up" button.
6. When set-up is complete, a "SetupCamerasNGon" window will remain visible. Close that window.
7. To change details of the scene, edit the JSON file, choose "Mapped Grass/Set Up Grass" again, and press the "Set Up" button again.
8. Build the executable: save the scene, choose "File/Build Settings...", under "Player Settings" set the "Company Name", create the "Build" folder, and build.

## Running an Experiment

1. Open a shell.
2. Make sure Python is available. The following examples use `py`, the default Windows installation.
3. Run the script that runs the three phases of the experiment (assuming a common location for this toolkit). The following uses a duration of 2 minutes for each phase:
    ```
    py .\Documents\vr\janelia-unity-toolkit\org.janelia.mapped-grass\Experiments\run_experiment.py --exe .\Documents\vr\app\Build\app.exe --duration 120
    ```
    See `run_experiment.py` for more options.
4. [FicTrac](https://github.com/rjdmoore/fictrac) track the motion of the fly on the trackball and sends it to the application. 

## Details

### Projector Configuration

The following fields define the projector configuration as in the [ethoVR project](https://www.haberkernlab.de/ethoVR/getting-started/#3-create-game-objects-and-link-up-package-code-with-objects):
```
{
    ...
    "projectorCount": 4,
    "emptySideCount": 1,
    "projectorOffsetForward": 0,
    "projectorOffsetLeft": 0,
    "projectorWidth": 800,
    "projectorHeight": 600,
    "projectorFovHorizDeg": 40,
    "fractionalHeight": 0.737f, 
    ...
}
```

### Subject Motion

By default, the subject fly will be teleported back to the origin when it reaches the edge of the grass field. This behavior is implemented by the `Distance Teleporter` component on the `Fly` object in the Unity scene; some fields on that component control details of the teleportion. To disable teleportation altogether use the following:
```
{
    ...
    "teleportAtEdge": false,
    ...
}
```

Updates from FicTrac are treated as incremental updates, unless the `"useFicTracIntegratedHeading"` field indicates to use FicTrac's integrated values:
```
{
    ...
    "useFicTracIntegratedHeading": true,
    ...
}
```

### Random Seed

The JSON spec may contain a specific seed for the random number generator, to give repeatable results from that generator at each run:
```
{
    ...
    "randomSeed": 123,
    ...
}
```
As `"randomSeed"` of 0 (the default) indicates that no random seed is explicilty applied, and the random numbers will be different on each run.

### Colors

```
{
    ...
    "objectColor": "#2FAE2F",
    "groundColor": "#3f3a0b",
    "skyColor": "#000000",
    ...
}
```

### Sky Box

To override the constant `"skyColor"` and use a custom sky box, add a construct like the following to the JSON spec file:
```
{
    ...
    "skyBoxImageFiles": [
        "front.png",
        "back.png",
        "left.png",
        "right.png",
        "top.png"
    ],
    ...
}
```
For example:
1. Download https://opengameart.org/sites/default/files/mountain-skyboxes.zip
2. From `mountain-skyboxes.zip` extract `mountain-skyboxes` and place it in the directory cotaining the JSON spec file.
3. Update the JSON file to include:
```
    "skyBoxImageFiles": [
        "mountain-skyboxes/Maskonaive/posz.jpg",
        "mountain-skyboxes/Maskonaive/negz.jpg",
        "mountain-skyboxes/Maskonaive/posx.jpg",
        "mountain-skyboxes/Maskonaive/negx.jpg",
        "mountain-skyboxes/Maskonaive/posy.jpg"
    ],
```
4. Choose the "Mapped Grass/Set Up Grass" menu item and press the "Set Up" button again.

### Lighting

Several fields control shadows:
```
{
    "shadows": 4,
    "shadowDistance": 2,
    "shadowBias": 0.001,
    "shadowNormalBias": 0.001,
    ...
}
```

The `"shadows"` field choose between different levels of shadowing:
* 0 = no shadows
* 1 = hard-edged shadows, low resolution
* 2 = hard-edged shadows, medium resolution
* 3 = hard-edged shadows, high resolution
* 4 = soft-edged shadows, high resolution

Generally a higher shadow value means a lower frame rate.

It is unlikely that the other shadow-related parameters will need to be specified, but they are available for completeness. 
* The `"shadowDistance"` is the distance from the camera (in meters, not the decimeters used for other units) where shadows stop being added.
* The `"shadowBias"` and `"shadowNormaBias"` affect how close the edge of a shadow is to the base of the object that casts the shadows. The default values should work well but if shadows look detached, it might help to lower these values.

To disable shadows and all other lighting effects, so the color of an object is purely its intrinsic color, use the following:
```
{
    ...
    "lit": false,
}
```

