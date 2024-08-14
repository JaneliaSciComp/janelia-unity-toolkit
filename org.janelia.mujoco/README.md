# Janelia Utilities for MuJoCo

## Summary

Utilities that augment the [Unity plugin](https://mujoco.readthedocs.io/en/latest/unity.html) for the [DeepMind](https://deepmind.google/) [MuJoCo physics simulator](https://mujoco.org/).

Note that functionality related to kinematic animation requires the [`kinematics` branch of the MuJoCo Unity plugin](https://github.com/FlyVirtualReality/mujoco/tree/kinematics).  The matching MuJoCo library is [release 3.2.0 from the main MuJoCo repo](https://github.com/google-deepmind/mujoco/releases/tag/3.2.0).

## Installation

Install the software as follows:
1. Install a prebuilt MuJoCo binary for [release 3.2.0 from the main MuJoCo repo](https://github.com/google-deepmind/mujoco/releases/tag/3.2.0) as described in the [MuJoCo installation instructions](https://mujoco.readthedocs.io/en/stable/unity.html#installation-instructions).  Note that on Windows and Linux, the location of the installation is significant (i.e., in a `mujoco` subdirectory of your user directory) to allow Unity to find the libraries.
2. Clone the MuJoCo Unity plugin from [`kinematics` branch](https://github.com/FlyVirtualReality/mujoco/tree/kinematics).
3. Install Unity.
4. Clone the [janelia-unity-toolkit repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit).
5. Use the Unity Hub to create a new project.
6. In the Unity Editor's "Window" menu choose "Package Manager".
7. In the Package Manager window, press the "+" button in the upper left corner.
8. Choose "Add package from disk..."
9. Navigate to the Unity plugin's directory and choose the `package.json` file.
10. Repeat that procedure to choose the janelia-unity-toolkit's`package.json` file.

## Usage

### Hiding Collision and Inertial Bodies

When importing a MuJoCo model (with the "Import MuJoCo Scene" item from the Unity editor's "Assets" menu), chunky blue shapes will appear in addition the expected geometric meshes.  To hide them, select the top-level object for the MuJoCo model and use the "Hide MuJoCo Collision & Inertial Bodies" item from the "Assets" menu.

Show them again with the "Asset" menu's "Show MuJoCo Collision & Inertial Bodies" menu item.

### `Janelia.MuJoCoKinematics`

To support precomputed kinematic animation of a MuJoCo model, add this component to the top-level object for model.  Another component must set the `AnimationFrameDelegate` to provide in the actual animation.  The optional `TweakAnimationFrameDelegate` can provide final adjustments (e.g., to adjust the position and orientation of the model so it sits on a curved surface).

The `overallQposOffset` field supports multiple MuJoCo models with independent animation.  MuJoCo requires that the multiple models be imported as a single aggregate XML file, and it will be animated with a single aggregate state ("qpos") vector.  Each model's `Janelia.MuJoCoKinematics` should have its `overallQposOffset` indicate start of that model's region in the single aggregate state vector.

### `Janelia.MuJoCoRuntimeUtilities`

MuJoCo represents vectors and quaternions differently from Unity.  This class contains utility functions for translating back and forth between the different representations.
