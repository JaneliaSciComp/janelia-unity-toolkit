# Janelia Collision Handling

## Summary
This package (org.janelia.collision-handling) adds some functionality for collision detection and response.

An example is the `Janelia.KinematicCollisionHandler` class, which adds simple collision handling for a Transform moving kinematically, like the `Transform` from a `GameObject` representing a fly walking on a treadmill.  

## Installation
Follow the [installation instructions in the main repository](https://github.com/JaneliaSciComp/janelia-unity-toolkit/blob/master/README.md#installation).

## Details

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
