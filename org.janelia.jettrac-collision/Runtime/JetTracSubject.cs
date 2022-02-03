using UnityEngine;

namespace Janelia
{
    // Makes the associated `GameObject` have collision detection and response
    // (per the org.janelia.collision-handling package) for kinematic motion from
    // the jETTrac system (per org.janelia.jettrac).  Uses `Janelia.JetTracUpdater`
    // implying the following orientation:
    // The direction for forward motion is the positive local X axis.
    // The up direction is the positive local Y axis.
    // Assumes the associated `GameObject` has a child named `Head`
    // whose local `Y` rotation is 90 degrees, so that if the main camera is a child
    // of `Head` then it will be looking in the direction of forward motion 
    // (i.e. the camera's positive Z axis will be rotated to match the main `GameObject`'s
    // positive X axis) for the jETTrac's head rotation at start up.

    // Also binds the 'q' and Escape keys to quit the application.

    public class JetTracSubject : KinematicSubject
    {
        // Remember that default values here are overridden by the values saved in the Unity scene.
        // So the values here are used only when an object using this script is first created.
        // After that, changes must be made in the Unity editor's Inspector.
        public bool readHead = true;
        public string headName = "Head";
        public bool allowBodyRotation = false;
        public bool smooth = false;
        public int smoothingWindow = 3;
        public bool logJetTracMessages = false;

        public new void Start()
        {
            float headRotationYDegs0 = 0;
            _headTransform = transform.Find(headName);
            if (_headTransform != null)
            {
                headRotationYDegs0 = _headTransform.eulerAngles.y;
            }

            _typedUpdater = new JetTracUpdater();
            _typedUpdater.headRotationYDegs0 = headRotationYDegs0;
            _typedUpdater.readHead = readHead;
            _typedUpdater.headTransform = _headTransform;

            _typedUpdater.allowBodyRotation = allowBodyRotation;
            _typedUpdater.smooth = smooth;
            _typedUpdater.smoothingWindow = smoothingWindow;
            _typedUpdater.logJetTracMessages = logJetTracMessages;

            // The `debug` field is inherited from the base `Janelia.KinematicSubject`.
            _typedUpdater.debug = debug;

            updater = _typedUpdater;

            // The `collisionRadius` and `collisionPlaneNormal` fields are optional,
            // and if set, they are passed to the `Janelia.KinematicCollisionHandler`
            // created in the base class.  Do not set the `collisionRadius` field here,
            // so the value from the Unity Editor's Insepctor will be used.  But do set
            // `collisionPlaneNormal` to a value that is consisten with the assumptions
            // of `org.janelia.jettrac`.
            collisionPlaneNormal = new Vector3(0, 1, 0);

            if (readHead)
            {
                _playbackAugmenter = new JetTracPlabackAugmenter();
            }

            // Let the base class finish the initial set-up.
            base.Start();
        }

        public new void Update()
        {
            if (Input.GetKey("q") || Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            // Let the base updater update this subject's position, with sliding collision handling.
            base.Update();

            if (readHead && !PlaybackActive)
            {
                // The base updater does not know about this subject's "head", so it must be
                // rotated explicitly.
                _headTransform.eulerAngles = _typedUpdater.HeadRotationDegrees();

                // And this change must be logged, for augmenting the log playback.
                _playbackAugmenter.Log(headName, _typedUpdater.HeadRotationDegrees());
            }
        }

        public void OnDisable()
        {
            _typedUpdater.OnDisable();
        }

        Transform _headTransform;
        private JetTracUpdater _typedUpdater;
        private JetTracPlabackAugmenter _playbackAugmenter;
    }
}
