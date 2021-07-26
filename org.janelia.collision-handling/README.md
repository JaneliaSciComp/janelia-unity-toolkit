# Janelia Collision Handling

## Summary

This package (org.janelia.collision-handling) adds some functionality for collision detection and response.

An example is the `Janelia.KinematicCollisionHandler` class, which adds simple collision handling for a Transform moving kinematically, like the `Transform` from a `GameObject` representing a fly walking on a treadmill.  

This package also supports logging of the kinematic motion, and playback of the motion in a log file.  User interface for choosing a log file to play back appears in the dialog created by the [org.janelia.logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging) launcher script (located in the main project directory, and having the suffix `Launcher.hta`).

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Janelia.KinematicSubject

This base class updates the rotation and translation of a `GameObject`'s `Transform` using `Janelia.KinematicCollisionHandler` to prevent collisions, and logs the motion with `Janelia.Logger` from [org.janelia.logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging).  A subclass of this base class must have an object conforming to the `Janelia.KinematicSubject.IKinematicUpdater` interface to provide the rotation and translation at each frame.  The motion data in the log can be read and played back by the base class, using command-line options for the application.

Optionally, this class can support a simple limit to the motion, a spherical boundary past which the subject cannot move.  When the subject hits this limiting boundary it slides along the spherical surface.  This limit take effect if the `limitDistance` field is greater than zero (the default).  The `limitCenter` field specifies the center of the sphere.  These fields are passed to the `Janelia.KinematicCollisionHandler` when execution starts.

This class also supports localized motion limits by looking for special `GameObjects` using a naming convention.  For example, a `GameObject` with a `BoxCollider` and a name starting with `LimitTranslationToForward_` applies a limit when the subject is within the box, with the limit preventing any translation backwards relative to the box's forward axis.  This functionality is used by the [org.janelia.radial-arm-maze](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.radial-arm-maze) package.

This class also adds user interface for the [org.janelia.logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging) launcher script, using the `Janelia.Logger.AddLauncherRadioButtonPlugin` function.

### Janelia.ExampleKinematicSubject

This subclass of `Janelia.KinematicSubject` gives an example of how to use the base class, with the `Janelia.KinematicSubject.IKinematicUpdater` object getting translation and rotation from the user via keyboard commands.

### Janelia.KinematicCollisionHandler

The following example code shows how to use the  `Janelia.KinematicCollisionHandler` class:

```csharp
using UnityEngine;

public class ButtonMoveable : MonoBehaviour
{
    private Janelia.KinematicCollisionHandler _collisionHandler;

    void Start()
    {
        // Set up the collision handler to act on this GameObject's transform.
        _collisionHandler = new Janelia.KinematicCollisionHandler(transform);
    }

    void Update()
    {
        // Let the user translate the transform with arrow keys.
        float delta = 0.225f;
        Vector3 translation = default;
        if (Input.GetKeyDown("left"))
        {
            translation = new Vector3(-delta, 0, 0);
        }
        if (Input.GetKeyDown("right"))
        {
            translation = new Vector3(delta, 0, 0);
        }
        if (Input.GetKeyDown("up"))
        {
            translation = new Vector3(0, 0, delta);
        }
        if (Input.GetKeyDown("down"))
        {
            translation = new Vector3(0, 0, -delta);
        }

        if (translation != default)
        {
            Vector3 actualTranslation = _collisionHandler.CorrectTranslation(translation);
            transform.Translate(actualTranslation);

            Debug.Log("frame " + Time.frameCount + ": translation " + translation + " becomes " + actualTranslation);
        }
    }
}
```

## Testing

To run this package's unit tests, use the following steps:
1. Create a new Unity project and add this package.
2. In the directory for the new project, in its `Packages` subdirectory, edit the `manifest.json` file to add a `"testables"` section as follows:
    ```
    {
      "dependencies": {
       ...
      },
      "testables": ["org.janelia.collision-handling"]
    }
    ```
    Note the comma separating the `"dependencies"` and `"testables"` sections.
3. In the Unity editor's "Window" menu, under "General", choose "Test Runner".
4. In the new "Test Runner" window, choose the "PlayMode" tab.
5. There should be an item for the new project, with items underneath it for "Janelia.Collision-handling.RuntimeTests.dll", etc.
6. Press the "Run All" button.
7. All the items under the new project will have green check marks if the tests succeed.
