# Janelia Setup Etho-VR

## Summary

Sets up a Unity scene for etho VR following the pattern used by [Hannah Haberkern at Janelia](https://haberkern-lab.github.io/ethoVR).

## Installation

Install the software as follows:
1. Install Unity.
2. Clone the [Janelia-unity-toolkit repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit).
3. Use the Unity Hub to create a new project.
4. In the Unity Editor's "Window" menu choose "Package Manager".
5. In the Package Manager window, press the "+" button in the upper left corner.
6. Choose "Add package from disk..."
7. Navigate to the Janelia-unity-toolkit's [org.janelia.package-installer directory (folder)](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.package-installer) and choose the `package.json` file.
8. When the org.janelia.package-installer packege is done loading, return to the Unity Editor's "Window" menu and choose "Install Package and Dependencies".
9. Navigate to the Janelia-unity-toolkit's [org.janelia.setup-etho-vr directory (folder)](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.setup-etho-vr) and choose the `manifest.json` file (_not_ `package.json`).
10. In the Package Installer window, press the "Install" button to install the remaining necessary packages.

Also install the necessary hardware: Display projectors, FicTrac trackball, National Instruments DAQ (NiDaqMx) unit.

## Usage

Once installation is complete, follow these steps to set up the Unity scene:
1. In the Unity Editor's "Window" menu, choose the "Setup Etho-VR" item.
2. In the Setup Etho VR window, use the checkboxes to enable setup options if desired:
    - a two-screen behavior-only rig instead of a four-screen imaging rig;
    - FicTrac input using the integrated heading instead of the incremental heading
3. Press the "Setup" button 

The scene is now ready to use.