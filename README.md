# janelia-unity-toolkit

## Summary
This repository contains [packages](https://docs.unity3d.com/Manual/Packages.html) for the Unity game engine, with an emphasis on supporting animal studies in virtual reality (VR).  The packages are meant to be loosely coupled to promote flexible reuse.

## Installation
Follow the Unity documentation for [installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html) saved outside a Unity project. In more detail:
1. Clone this repository into a directory (folder) on your computer. It is simplest to use a directory outside the directories of any Unity projects.
2. Create a new Unity project or open an existing one.
3. In the Unity editor's "Window" menu, choose the "Package Manager" item.
4. Press the small "+" button in the upper-left corner of the Package Manager window, and click on the "Add package from disk..." item that appears.
![Package Manager "+" button](installation.png)
5. In the file chooser that appears, navigate to the directory with the downloaded repository. Then go down one more level into the subdirectory for the particular package to be installed (e.g., "org.janelia.full-screen.view").
6. Select the "package.json" file and press "Open" (or double click).
7. Unity should load that package and make its functionality available, and it should be visible in the Package Manager's list of packages, (e.g., as "Janelia Full Screen View").
