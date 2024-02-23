# janelia-unity-toolkit

## Summary
This repository contains [packages](https://docs.unity3d.com/Manual/Packages.html) for the Unity game engine, with an emphasis on supporting animal studies in virtual reality (VR).  The packages are meant to be loosely coupled to promote flexible reuse.

To get started with a setup that has been used in the [Jayaraman lab at Janelia](https://www.janelia.org/lab/jayaraman-lab), follow the directions for the [org.janelia.setup-etho-vr](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.setup-etho-vr) package.

Examples of the functionality supported in this toolkit include:
* frame rate management ([org.janelia.force-render-rate](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.force-render-rate))
* extensible logging of activity during a session, with optional saving of rendered frames ([org.janelia.logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging))
* set up of virtual cameras in various patterns for multiple external displays, including back projection on non-planar (e.g., cylindrical) screens, with optional capabilities like ["frame packing"](org.janelia.camera-utilities/README.md#frame-packing) to increase the effective frame rate ([org.janelia.camera-utilities](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.camera-utilities), [org.janelia.setup-cameras-box](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.setup-cameras-box), [org.janelia.setup-cameras-n-gon](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.setup-cameras-n-gon))
* multiple external displays when running in the editor ([org.janelia.full-screen-view](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.full-screen-view))
* input of an animal's kinematic motion from trackball systems like [FicTrac](http://rjdmoore.net/fictrac/) ([org.janelia.fictrac](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.fictrac), [org.janelia.fictrac-collision](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.fictrac-collision)) and [jETTrac](https://www.janelia.org/support-team/janelia-experimental-technology) ([org.janelia.jettrac](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.jettrac), [org.janelia.jettrac-collision](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.jettrac-collision))
* collision detection and simple sliding response for kinematic motion ([org.janelia.collision-handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling))
* communication with a National Instruments data acquisition (NI DAQ) device using the NI-DAQmx driver ([org.janelia.ni-daq-mx](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.ni-daq-mx))
* communication with [Univeral Robots](https://www.universal-robots.com) devices, like the [UR10e](https://www.universal-robots.com/products/ur10-robot/) arm, using the [Real Time Data Exchange (RTDE)](https://www.universal-robots.com/articles/ur/interface-communication/real-time-data-exchange-rtde-guide/) interface ([org.janelia.ur](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.ur), [org.janelia.ur_rtde](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.ur_rtde))
* communication over sockets and serial ports without triggering excess garbage collection ([org.janelia.io](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.io))
* background images ([org.janelia.background](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.background))
* generation of simple maze environments ([org.janelia.radial-arm-maze](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.radial-arm-maze)) and meadow environments ([org.janelia.meadow](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.meadow))
* simplified setup of reinforcement learning with [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents) ([org.janelia.easy-ml-agents](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.easy-ml-agents))
* other general capabilities, like making a standalone executable end automatically after running for a specified period ([org.janelia.general](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.general))

## Installation

Use Unity's approach for [installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html) saved outside a Unity project.  Start by cloning this repository into a directory (folder) on your computer; it is simplest to use a directory outside the directories of any Unity projects.  Note that once a Unity project starts using a package from this directory, the project automatically gets any updates due to a Git pull in the directory.

### With Dependencies

Some of the packages in this repository depend on other packages (e.g., the [package for collision handling](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.collision-handling) uses the [package for logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging)), but the standard [Unity Package Manager window](https://docs.unity3d.com/Manual/upm-ui.html) does not automatically install such dependencies.  To overcome this weakness, use the [org.janelia.package-installer](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer) package.  Once it is installed (as described in the next subsection), use it to install another package as follows:

1. Choose "Install Package and Dependencies" from the Unity editor's "Window" menu.
2. In the file chooser that appears, navigate to the directory with the [janelia-unity-toolkit](https://github.com/JaneliaSciComp/janelia-unity-toolkit) repository. Then go down one more level into the subdirectory for the particular package to be installed (e.g., `org.janelia.collision-handling`).
3. Select the `package.json` file and press the "Open" button (or double click).
4. A window appears, listing the package and other packages on which it depends.  Any dependent package already installed appears in parentheses.
5. Press the "Install" button to install all the listed packages in order.
6. All the listed packages now should be active.  They should appear in the Unity editor's "Project" tab, in the "Packages" section, and also should appear in the editor's Package Manager window, launched from the "Package Manger" item in the editor's "Window" menu.

This system also can install multiple packages (and their dependencies) at once.  In step 3, above, choose a file like `manifest.json` that contains a list of package names:
```
[
    "org.janelia.setup-camera-n-gon",
    "org.janelia.fictrac-collision"
]
```
Each of the listed packages (and their dependencies) will be loaded in turn, as if they were chosen individually in step 3.  See the documentation on [org.janelia.package-installer](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer) for more details.

Note that `org.janelia.package-installer` requires Unity version 2019.3.0 or later.

### Without Dependencies

To install the [org.janelia.package-installer](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer) package (or to install a package without its dependencies), use the following steps:
1. Choose "Package Manager" from the Unity editor's "Window" menu.
2. Press the small "+" button in the upper-left corner of the Package Manager window, and click on the "Add package from disk..." item that appears.
![Package Manager "+" button](installation.png)
3. Use the file chooser to navigate to a package's directory (e.g., `org.janelia.package-installer`) in the cloned [janelia-unity-toolkit](https://github.com/JaneliaSciComp/janelia-unity-toolkit) repository.
4. Select the `package.json` file and press "Open" (or double click).
5. Unity should load that package, and it should appear in the Package Manager window and in the "Project" tab.
