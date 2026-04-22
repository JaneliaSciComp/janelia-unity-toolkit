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

### Sky Box

To use a custom sky box, add a construct like the following to the JSON spec file:
```
    "skyBoxImageFiles": [
        "front.png",
        "back.png",
        "left.png",
        "right.png",
        "top.png"
    ],
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