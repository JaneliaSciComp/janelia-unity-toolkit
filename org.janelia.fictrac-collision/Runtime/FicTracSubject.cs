using UnityEngine;

namespace Janelia
{
    // Makes the associated `GameObject` have collision detection and response
    // (per the org.janelia.collision-handling package) for kinematic motion from
    // the FicTrac system (per org.janelia.fictrac).  Uses `Janelia.FicTracUpdater`
    // implying the following orientation:
    // The direction for forward motion is the positive local X axis.
    // The up direction is the positive local Y axis.

    // Also binds the 'q' and Escape keys to quit the application.

    public class FicTracSubject : Janelia.KinematicSubject
    {
        // Remember that default values here are overridden by the values saved in the Unity scene.
        // So the values here are used only when an object using this script is first created.
        // After that, changes must be made in the Unity editor's Inspector.
        public string ficTracServerAddress = "127.0.0.1";
        public int ficTracServerPort = 2000;
        public float ficTracBallRadius = 0.5f;
        public int smoothingCount = 3;
        public bool logFicTracMessages = false;

        public new void Start()
        {
            _typedUpdater = new FicTracUpdater
            {
                ficTracServerAddress = ficTracServerAddress,
                ficTracServerPort = ficTracServerPort,
                ficTracBallRadius = ficTracBallRadius,
                smoothingCount = smoothingCount,
                logFicTracMessages = logFicTracMessages
            };
            updater = _typedUpdater;

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

        public new void Update()
        {
            if (Input.GetKey("q") || Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            base.Update();
        }

        public void OnDisable()
        {
            _typedUpdater.OnDisable();
        }

        private FicTracUpdater _typedUpdater;
    }
}
