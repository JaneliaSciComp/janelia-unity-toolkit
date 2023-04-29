# Janelia Ur_rtde Support
## Summary

This package (org.janelia.ur_rtde) supports the use in Unity of the [Ur_rtde library](https://sdurobotics.gitlab.io/ur_rtde/), a C++ library for controlling [Univeral Robots](https://www.universal-robots.com) devices, like the [UR10e](https://www.universal-robots.com/products/ur10-robot/) arm.  [Ur_rtde](https://gitlab.com/sdurobotics/ur_rtde) was developed at the University of Southern Denmark.

## Installation

The Ur_rtde library is written in C++ so it is accessed in Unity as a [native plugin](https://docs.unity3d.com/Manual/NativePlugins.html).  It should work on Windows but so far, only Linux has been tested.

1. Install the Ur_rtde library.  The simplest approach is to [use the prebuilt version](https://sdurobotics.gitlab.io/ur_rtde/installation/installation.html#quick-install).  On Ubuntu Linux:

        $ sudo add-apt-repository ppa:sdurobotics/ur-rtde
        $ sudo apt-get update
        $ sudo apt install librtde librtde-dev
    The header files should be installed in `/usr/include/ur_rtde` and the library should be installed in `/usr/lib/x86_64-linux-gnu/librtde.so`.  Both locations should be on the default paths for compilation and linking.

2. Build the wrapper library for the native plugin:

        $ cd org.janelia.ur_rtde/cpp
        $ make

    The result should be `org.janelia.ur_rtde/cpp/libur_rtde_c.so`.

3. Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation) to install the org.janelia.ur_rtde package.  It should find the `libur_rtde_c.so` library automatically, both when running in the Unity Editor or as part of a stand-alone executable built from the editor.

## UR Simulator

For development and testing, it is helpful to use the robot simulator.  The simplest approach is to [run the simulator in a Docker container](Janelia.UrRtdeControlInterface.PathEntry.MoveType.MoveJ):

1. Install Docker and open a terminal shell.
2. Install the container:

        $ sudo docker pull universalrobots/ursim_e-series

3. Run the container, exposing the necessary ports:

        $ sudo docker run --rm -it -p 5900:5900 -p 6080:6080 -p 30004:30004 universalrobots/ursim_e-series

4. The container prints information about how to access the simulator through a web browser.  The URL should be something like:

        http://localhost:6080/vnc.html?host=localhost&port=6080


5. When this URL is loaded, browser should display a staring page with the logo "no VNC" and a login box at the upper right.  Simply press the "Connect" button (leavin the "Password" box empty).

6. When the simulator is loaded, press the blue button, "Confirm Safety Configuration".

7. Press the red "Power off" button.

8. Press the green "ON" button and press it again when it has changed to "START".  Press "Exit" to dismiss the dialog.

9. In the upper-right corner, press the icon with three horizontal bars, and choose "Settings".

10. Open the "System" section and press "Remote Control".

11. Press "Enable".  Press "Exit" to dismiss the dialog.

12. In the upper-right corner, there should be a new icon, showing a screen and a cursor and the words "Local".  Press that icon.

13. Press the "Remote control" button that appears.  The icon should change appearance, and show the words "Remote".

14. The simulator now should be ready to work with Ur_rtde.


## Details

### `ExampleUsingUrRtde.cs`

Gives a basic example of using org.janelia.ur_rtde.  Attach it as a component to any game object in the scene.  Note the way it uses the C# equivalent of the C++ enums, e.g., `Janelia.UrRtdeControlInterface.PathEntry.MoveType.MoveJ`.

### `Janelia.UrRtdeControlInterface`

C# class wrapping the [RTDE Control Interface API](https://sdurobotics.gitlab.io/ur_rtde/api/api.html#rtde-control-interface-api).

### `Janelia.UrRtdeReceiveInterface`

C# class wrapping the [RTDE Receive Interface API](https://sdurobotics.gitlab.io/ur_rtde/api/api.html#rtde-receive-interface-api).

### `Janelia.UrRtdeRobotiqGripper`

C# class wrapping the [Robotiq Gripper API](https://sdurobotics.gitlab.io/ur_rtde/api/api.html#robotiq-gripper-api).  Note that the simulator does not include a gripper.