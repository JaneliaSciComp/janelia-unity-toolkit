using Unity.MLAgents.Actuators;
using UnityEngine;

namespace Janelia
{
    /// <summary>
    /// A base class for a trainable agent that moves along the ground according to forces.
    /// A derived class must implement a few abstract properties from the 
    /// <see cref="EasyMLAgent"/> base class (e.g., BehaviorName), and override the
    /// OnActionReceived and CollectObservations functions.
    /// </summary>
    public abstract class EasyMLAgentGrounded : EasyMLAgent
    {
        // New overridable properties added by this class.

        // For properties invovled in overriding, it does not work to make them 
        // "auto-implemented properties" with a compiler-generated anonymous backing field,
        // because overriding any of these properties in a derived class seems to generate
        // another backing field with the same internal name, and Unity gives an error:
        // "The same field name is serialized multiple times in the class or its parent class."

        /// <summary>
        /// The size of the agent's body mesh.  If the body covers [-S/2, S/2] in a dimension
        /// then the size should be S in that dimension.
        /// </summary>
        public virtual Vector3 BodyScale
        {
            get { return _groundedBodyScale; }
            protected set { _groundedBodyScale = value; }
        }
        private Vector3 _groundedBodyScale = new Vector3(0.1f, 0.1f, 0.1f);

        /// <summary>
        /// The color of the agent's body mesh.
        /// </summary>
        public virtual string BodyColor
        {
            get { return _groundedBodyColor; }
            protected set { _groundedBodyColor = value; }
        }
        private string _groundedBodyColor = "#ff0000";

        /// <summary>
        /// The agent's collider's static friction.
        /// </summary>
        public virtual float StaticFriction
        {
            get { return _groundedStaticFriction; }
            protected set { _groundedStaticFriction = value; }
        }
        private float _groundedStaticFriction = 0.15f;

        /// <summary>
        /// The agent's collider's dynamic friction.
        /// </summary>
        public virtual float DynamicFriction
        {
            get { return _groundedDynamicFriction; }
            protected set { _groundedDynamicFriction = value; }
        }
        private float _groundedDynamicFriction = 0.15f;

        /// <summary>
        /// The direction of the force on the agent for forward movement.
        /// </summary>
        public virtual Vector3 MoveForwardDirection
        {
            get { return transform.forward; }
        }


        // New parameters that appear in the Unity Inspector (to allow interactive
        // modification) and so cannot be properties.

        // This value cannot be too small or the agent does not move during training.
        // A smaller value (e.g. 12.5f) might work better for the heuristic keyboard
        // control, but not during training.
        [Tooltip("Force to apply when moving forward or backward")]
        public float moveForce = 20.0f;

        [Tooltip("Speed to rotate around the Y axis")]
        public float yawSpeed = 100.0f;

        // Makes yaw changes smoother.
        private float _smoothYawChange = 0.0f;

        // Overridden properties from `EasyMLAgent`.

        public override int VectorActionSize 
        {
            get { return _groundedVectorActionSize; }
            protected set { _groundedVectorActionSize = value; }
        } 
        private int _groundedVectorActionSize = 2;

        public override Vector3 ColliderSize
        {
            get { return _groundedColliderSize; }
            protected set { _groundedColliderSize = value; }
        }
        private Vector3 _groundedColliderSize;

        public override Vector3 ChildSensorSourceOffset
        {
            get { return _groundedChildSensorSourceOffset; }
            protected set { _groundedChildSensorSourceOffset = value; }
        }
        private Vector3 _groundedChildSensorSourceOffset = new Vector3(0, 0.05f, 0);

        public virtual string BodyName
        {
            get { return _groundedBodyName; }
            protected set { _groundedBodyName = value; }
        }
        private string _groundedBodyName = "Body";

        /// <summary>
        /// Called after the Setup function for the arena (the class derived from <see cref="EasyMLArena"/>).
        /// If a derived class needs to override this function for further setup, it should call base.Setup.
        /// Creates new objects or updates existing objects.
        /// </summary>
        /// <param name="helper"></param>
        public override void Setup(Janelia.IEasyMLSetupHelper helper)
        {
            // `ColliderSize` is set here, in case the derived `Setup` changes `BodyScale`.
            ColliderSize = BodyScale;

            base.Setup(helper);
            gameObject.name = "AgentGrounded";

            
            Transform bodyTransform = transform.Find(BodyName);
            GameObject body;
            if (bodyTransform == null)
            {
                body = new GameObject();
                body.name = BodyName;
                body.transform.parent = transform;
            }
            else
            {
                body = bodyTransform.gameObject;
            }
            body.transform.localScale = new Vector3(BodyScale.x, BodyScale.y, BodyScale.z);

            MeshFilter meshFilter = body.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                helper.CreateMeshFilter(body, "Wedge.obj");
            }

            helper.CreateColorMaterial(body, BodyColor);

            // Put the overall agent position at the back and bottom of the body and its collider.
            // Doing so helps to keep the agent from "tipping" forward or backward when a force is applied.
            float y = BodyScale.y / 2;
            body.transform.localPosition = new Vector3(0, y, y);
            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.center = new Vector3(0, y, y);
                helper.CreatePhysicsMaterial(collider, StaticFriction, DynamicFriction);
            }

            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
            {
                childCamera.transform.localPosition = new Vector3(0, 2 * y, y);
            }
        }

        /// <summary>
        /// Called when an action is received from either the player input or the neural network.
        /// It is also common to assign a reward in this method.
        /// </summary>
        /// <param name="actions">Action to take</param>
        ///
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (_frozen)
            {
                return;
            }

            float moveChange = actions.ContinuousActions[0];
            Vector3 dir = (moveChange > 0) ? MoveForwardDirection : transform.forward;
            _agentRigidbody.AddForce(moveChange * moveForce * dir);

            // Changes to yaw (Y rotation) are smoothed over multi)ple frames.

            float yawChange = actions.ContinuousActions[1];
            Vector3 rotationVector = transform.rotation.eulerAngles;
            _smoothYawChange = Mathf.MoveTowards(_smoothYawChange, yawChange, 2.0f * Time.fixedDeltaTime);
            float yaw = rotationVector.y + _smoothYawChange * Time.fixedDeltaTime * yawSpeed;

            transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
        }

        /// <summary>
        /// When behavior type is set to "Heuristic only" on the agent's behavior parameters,
        /// then this function will be called.  Its return value will be fed into
        /// <see cref="OnActionReceived(ActionBuffers)"/> and the neural network is ignored.
        /// </summary>
        /// <param name="actionsOut">An output action buffer</param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            float moveChange = 0.0f;
            float yawChange = 0.0f;

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                moveChange = 1.0f;
            }
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                moveChange = -1.0f;
            }

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                yawChange = -1.0f;
            }
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                yawChange = 1.0f;
            }

            Debug.Assert(VectorActionSize == 2, "Incorrect vector action size");

            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            continuousActions[0] = moveChange;
            continuousActions[1] = yawChange;
        }
    }
}