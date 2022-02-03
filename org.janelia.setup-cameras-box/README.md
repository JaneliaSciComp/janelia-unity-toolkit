# Janelia Setup Cameras Box with Floor

## Summary

This package (org.janelia.setup-cameras-box) sets up a rig of cameras for displaying a panorama on four screens arranged in a box, with the option of two additional overhead cameras for displaying the floor.  The details like the widiths and heights of the displays are described in a "spec" file in the [JSON format](https://en.wikipedia.org/wiki/JSON).  Installing the package causes the Unity editor's "Window" menu to have the "Setup Camera, Box with Floor" item, and choosing this item creates a panel with buttons for loading the spec file and building the camera rig.

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation) for the case "With Dependencies".  This package depends on the [org.janelia.camera-utilites package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.camera-utilities), and the simplest approach is to install both packages at once with [org.janelia.package-installer](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer).

## Example

Setting up a complete virtual environment for an animal study involves not only setting up the camera rig, but also the input device and the scenery of the environment.  Nevertheless, it makes sense to include a complete example here.

In the [`Example` folder](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.setup-cameras-box/Example), there are three files that simplify the set up of an example:

* The `example-using-jettrac-package-manifest.json` file lists the top-level packages needed for the example, and [org.janelia.package-installer](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer) can use this manifest to load all the packages, including dependencies.  As the file name suggests, this example uses [org.janelia.jettrac](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.jettrac) and [org.janelia.jettrac-collision](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.jettrac-collision) for the input device (although org.janelia.setup-cameras-box does not depend on this choice).  Loading these packages is the first step of creating the environment.

* The `example-using-jettrac-cameras-box-spec.json` file specifies the details of the camera rig, using the format described in the "Details" section, below.  It assumes that one Unity unit is 10 cm (0.1 m); these units work well with jETTrac, despite being different from the Unity default of one unit equalling 1 m.  Use the window launched by the Unity editor's "Window/Setup Camera, Box with Floor" menu item to load this file and create the camera rig.

* The `example-using-jettrac-maze-spec.json` file specifies a simple Y-maze as scenery for the environment, using the format expected by the [org.janelia.radial-arm-maze package](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer).  As for the camera rig, one Unity unit is 10 cm.  Use the window launched by the Unity editor's "Window/Layout Radial-Arm Maze" menu item to load this file and create the maze.  (The wood texture for the ground plane was obtained from [FreeStockTextures](https://freestocktextures.com/texture/floor-wood-oak,765.html) under the [Creative Commons Zero license](https://creativecommons.org/publicdomain/zero/1.0/).)

The Y-maze here is just one simple example of scenery for an animal study.  The package manifest and camera spec files in this example would work well with other scenery, as long as it also uses one Unity unit being 10 cm.  Note also that the the content created in this example can be edited further in the Unity editor if desired.


## Details

The user interface launched by the Unity editor's "Window/Setup Camera, Box with Floor" menu item has buttons for building the camera rig.  One button launches a file dialog for choosing the JSON spec file.  Another button creates a rig from the chosen spec file; note that it first deletes any existing rig, and it also reloads the spec file to get any recent edits.  Finally, there is a button to delete the rig without creating a new one.

A JSON spec file can include the following elements:

* `"subjectName"` [optional, default value: `"Subject"`]: The name of the `GameObject` that acts as the root of the camera rig.  The rig translates with this `GameObject`.

* `"side"` [required]: A nested JSON object with the following elements:
  * `"screenWidth"` [required]: The widths of the screens that will display the camera renderings.  Should match the physical measurements of the corresponding actual display devices, converted into Unity units.
  * `"screenHeight"` [required]: The heights of the screens that will display the camera renderings.  Should match the physical measurements of the corresponding actual display devices, converted into Unity units.
  * `"screenBottom"` [optional, default value: 0]: The height (_y_ coordinate, in Unity units) of the bottom of the screen in the global coordinate system of the virtual environment.
  * `"cameraY"` [optional, default value: 0]: The _y_ coordinate of the camera positions relative to the subject (i.e., the vertical displacement from the position of the subject to the animal's head), in Unity units.  Typically, the subject will be positioned on the floor of the environment, so this value must be greater than zero to render views for the animal's head that is off the floor.
  * `"cameraX"` [optional, default value: 0]:  Only necessary if the animal input device is not mounted at the center of the box.
  * `"cameraZ"` [optional, default value: 0]: Only necessary if the animal input device is not mounted at the center of the box.
  * `"displayIDs"` [optional, default value: [2, 3, 4, 5]]: The external display numbers for the four displays making forming the sides of the box (e.g., as indicated by the Windows "Detect or Identify Displays" system setting).

* `"floor"` [required]: a nested JSON object with the following elements:
  * `"screenWidth"` [required]: The widths of the two displays (or projector display areas) for the floor.  Should match the `side.screenWidth` value in most cases.
  * `"screenHeight"` [required]: The heights of the two displays (or projector display areas) for the floor.  Should be as close as possible to half of `side.screenWidth` in most cases.
  * `"camera1X"` [optional, default value: 0]: The _x_ coordinate of the first floor camera, in Unity units, relative to the center of the box.
  * `"camera1Y"` [optional, default value: 0]: The _y_ (vertical) coordinate of the first floor camera, in Unity units, relative to the center of the box.
  * `"camera1Z"` [optional, default value: 0]: The _z_ coordinate of the first floor camera, in Unity units, relative to the center of the box.  In most cases, one of `"camera1X"` and `"camera1Z"` will be 0.
  * `"camera2X"` [optional, default value: 0]: The _x_ coordinate of the second floor camera, in Unity units, relative to the center of the box.  In most cases, `"camera2X"` will either be equal to or the negative of `"camera1X"`.
  * `"camera2Y"` [optional, default value: 0]: The _y_ (vertical) coordinate of the second floor camera, in Unity units, relative to the center of the box.  In most cases, `"camera2Y"` will equal `"camera1Y"`.
  * `"camera2Z"` [optional, default value: 0]: The _z_ coordinate of the second floor camera, in Unity units, relative to the center of the box.  In most cases, `"camera2Z"` will either be equal to or the negative of `"camera1Z"`.
  * `"displayIDs"` [optional, default value: [6, 7]]:

* `"near"` [optional, default value: 0.1]: The near (front) clipping plane distance of all cameras, in Unity units.
* `"far"` [optional, default value: 100]: The far (back) clipping plane distance of all cameras, in Unity units.

The screens appear as 3D objects in the Unity editor's scene view for reference only, and are invisible to the animal.

