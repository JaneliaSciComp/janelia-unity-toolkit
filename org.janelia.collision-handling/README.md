# Janelia Collision Handling

## Summary

This package (org.janelia.collision-handling) adds some functionality for collision detection and response.

An example is the `Janelia.KinematicCollisionHandler` class, which adds simple collision handling for a Transform moving kinematically, like the `Transform` from a `GameObject` representing a fly walking on a treadmill.  

This package also supports logging of the kinematic motion, and playback of the motion in a log file.  User interface for choosing a log file to play back appears in the dialog created by the [org.janelia.logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging) launcher script (located in the main project directory, and having the suffix "Launcher.hta").

## Installation

Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

### Janelia.KinematicSubject

This base class updates the rotation and translation of a `GameObject`'s `Transform` using `Janelia.KinematicCollisionHandler` to prevent collisions, and logs the motion with `Janelia.Logger` from [org.janelia.logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging).  A subclass of this base class must have an object conforming to the `Janelia.KinematicSubject.IKinematicUpdater` interface to provide the rotation and translation at each frame.  The motion data in the log can be read and played back by the base class, using command-line options for the application.

This class also adds user interface for the [org.janelia.logging](https://github.com/JaneliaSciComp/janelia-unity-toolkit/tree/master/org.janelia.logging) launcher script, using the `Janelia.Logger.AddLauncherPlugin` function.

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
            // Let the collision handler correct the translation, with approximated sliding contact,
            // and apply it to this GameObject's transform.  The corrected translation is returned.
            Vector3 actualTranslation = _collisionHandler.Translate(translation);

            Debug.Log("frame " + Time.frameCount + ": translation " + translation + " becomes " + actualTranslation);
        }
    }
}
```
