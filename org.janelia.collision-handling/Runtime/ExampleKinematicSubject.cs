// A simple example of how to use `Janelia.KinematicSubject`.
// In this case, the `IKinematicUpdater` instance checks the keyboard arrow keys
// to let the user specify the translation and rotation.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    public class ExampleKinematicSubject : Janelia.KinematicSubject
    {
        class KeyboardUpdater : Janelia.KinematicSubject.IKinematicUpdater
        {
            public float deltaRotation = 1.0f;
            public float deltaTranslation = 5.0f;

            // Must be defined in this sublcass, but can be empty if
            // this subclass does nothing special as the application starts.
            public void Start()
            {
            }

            // Must be defined in this sublcass, but can be empty if
            // this subclass does nothing special on each frame.
            public void Update()
            {
            }

            // Must be defined in this subclass.
            public Vector3? Translation()
            {
                if (Input.GetKey("q") || Input.GetKey(KeyCode.Escape))
                {
                    Application.Quit();
                }

                if (Input.GetKey("up"))
                {
                    return new Vector3(0, 0, deltaTranslation);
                }
                if (Input.GetKey("down"))
                {
                    return new Vector3(0, 0, -deltaTranslation);
                }

                return null;
            }

            // Must be defined in this subclass.
            public Vector3? RotationDegrees()
            {
                if (Input.GetKey("left"))
                {
                    return new Vector3(0, -deltaRotation, 0);
                }
                if (Input.GetKey("right"))
                {
                    return new Vector3(0, deltaRotation, 0);
                }

                return null;
            }
        }

        new void Start()
        {
            // The `updater` field (inherited from the `KinematicSubject` base class)
            // must be set in this `Start()` method.
            updater = new KeyboardUpdater();

            // The `collisionRadius` and `collisionPlaneNormal` fields are optional,
            // and if set, they are passed to the `Janelia.KinematicCollisionHandler`
            // created in the base class.
            collisionRadius = 1.0f;
            foreach (Transform child in transform)
            {
                if (child.gameObject.name.EndsWith("Marker"))
                {
                    collisionRadius = Mathf.Max(child.localScale.x, child.localScale.z);
                }
            }
            collisionPlaneNormal = new Vector3(0, 1, 0);

            // Let the base class finish the initial set-up.
            base.Start();
        }
    }
}
