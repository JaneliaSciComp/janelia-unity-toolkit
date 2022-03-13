using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Janelia
{
    /// <summary>
    /// A abstract base class for an agent using ML-Agents (version 2) capabilities.
    /// A derived class must override the abstract properties (e.g., BehaviorName)
    /// to set values needed by ML-Agents (and which ML-Agents itself may not warn about
    /// when not specified).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(DecisionRequester))]
    public abstract class EasyMLAgent : Agent
    {
        /// <summary>
        /// The  <see cref="GameObject.tag"/> given to an agent derived from <see cref="EasyMLAgent"/>
        /// </summary>
        public static readonly string TAG_AGENT = "Agent";

        /// <summary>
        /// This behavior name must match the subsection name under "behaviors" in trainer_config.yaml.
        /// </summary>
        public abstract string BehaviorName { get; protected set; }

        /// <summary>
        /// The number of observations added in <see cref="CollectObservations(VectorSensor)"/>
        /// (and remember that one <see cref="Vector3"/> counts as 3 observations).
        /// </summary>
        public abstract int VectorObservationSize { get; protected set; }

        /// <summary>
        /// The number of observations added in <see cref="CollectObservations(VectorSensor)"/>
        /// (and remember that one <see cref="Vector3"/> counts as 3 observations).
        /// </summary>
        public abstract int VectorActionSize { get; protected set; }

        /// <summary>
        /// The size of the BoxCollider given to the agent by default.  Note that a value S in 
        /// any of the dimensions means the box covers [-S/2, S/2] in that dimension.
        /// A value of Vector3.zero prevents the collider from being added.
        /// </summary>
        public abstract Vector3 ColliderSize { get; protected set; }

        // The following properties are not abstract, because there are sensible default values
        // if the derived class does not want to override them.  But the possibility that they
        // might be overridden means that it does not work to make these "auto-implemented properties"
        // with a compiler-generated anonymous backing field.  Then overriding any of these properties
        // in a derived class seems to generate another backing field with the same internal name, and
        // Unity gives an error:
        // "The same field name is serialized multiple times in the class or its parent class."

        /// <summary>
        /// Whether to add a child sensor for raycast-based observations, in the forward direction.
        /// </summary>
        public virtual bool UseChildSensorForward 
        { 
            get { return _baseUseChildSensorForward;  }
            protected set { _baseUseChildSensorForward = value; }
        }
        private bool _baseUseChildSensorForward = true;

        /// <summary>
        /// How many rays to use for a child sensor in the forward direction.
        /// </summary>
        public virtual int ChildSensorForwardRaysPerDirection
        { 
            get { return _baseChildSensorForwardRaysPerDirection; }
            protected set { _baseChildSensorForwardRaysPerDirection = value; }
        }
        private int _baseChildSensorForwardRaysPerDirection = 6;

        /// <summary>
        /// The maximum length of the rays for a child sensor in the forward direction.
        /// </summary>
        public virtual float ChildSensorForwardRayLength
        {
            get { return _baseChildSensorForwardRaysLength; }
            protected set { _baseChildSensorForwardRaysLength = value; }
        }
        private float _baseChildSensorForwardRaysLength = 20.0f;

        /// <summary>
        /// The tags that should be detectable by raycasting in the forward direction.
        /// </summary>
        public virtual List<string> ChildSensorForwardDetectableTags
        {
            get { return _baseChildSensorForwardDetectableTags; }
            protected set { _baseChildSensorForwardDetectableTags = value; }
        }
        private List<string> _baseChildSensorForwardDetectableTags = new List<string>() { "Untagged" };

        [Tooltip("Camera showing what the agent sees")]
        public Camera agentCamera;

        [Tooltip("Whether in training mode (or, alternativelys, game mode)")]
        public bool trainingMode = true;

        // Whether the agent is frozen (intentionally not moving)
        protected bool _frozen = false;

        protected Rigidbody _agentRigidbody;

        /// <summary>
        /// Called after the Setup function for the arena (the class derived from <see cref="EasyMLArena"/>).
        /// Creates new objects or updates existing objects.
        /// </summary>
        /// <param name="helper"></param>
        public virtual void Setup(IEasyMLSetupHelper helper)
        {
            gameObject.name = "Agent";
            helper.CreateTag(TAG_AGENT);
            gameObject.tag = TAG_AGENT;

            const string CAMERA_NAME = "AgentCamera";
            Transform cameraTransform = transform.Find(CAMERA_NAME);
            if (cameraTransform == null)
            {
                GameObject cameraObject = new GameObject();
                cameraObject.name = CAMERA_NAME;
                cameraObject.transform.parent = transform;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000.0f;
            }

            BoxCollider collider = gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                if (ColliderSize != Vector3.zero)
                {
                    collider = gameObject.AddComponent<BoxCollider>();
                    collider.size = ColliderSize;
                }
            }
            else
            {
                if (ColliderSize != Vector3.zero)
                {
                    collider.size = ColliderSize;
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            MaxStep = 5000;

            BehaviorParameters behavior = GetComponent<BehaviorParameters>() as BehaviorParameters;
            if (behavior != null)
            {
                behavior.BehaviorName = BehaviorName;
                behavior.BrainParameters.VectorObservationSize = VectorObservationSize;
                behavior.BrainParameters.ActionSpec = new ActionSpec(VectorActionSize);

                const string SENSOR_OBJECT_NAME = "RaysForward";

                behavior.UseChildSensors = UseChildSensorForward;
                if (behavior.UseChildSensors)
                {
                    Transform raySensorTransform = transform.Find(SENSOR_OBJECT_NAME);
                    GameObject raySensorObject;
                    if (raySensorTransform == null)
                    {
                        raySensorObject = new GameObject();
                        raySensorObject.name = SENSOR_OBJECT_NAME;
                        raySensorObject.transform.parent = transform;
                    }
                    else
                    {
                        raySensorObject = raySensorTransform.gameObject;
                    }

                    RayPerceptionSensorComponent3D sensor = raySensorObject.GetComponent<RayPerceptionSensorComponent3D>();
                    if (sensor == null)
                    {
                        sensor = raySensorObject.AddComponent<RayPerceptionSensorComponent3D>();
                    }
                    sensor.RaysPerDirection = ChildSensorForwardRaysPerDirection;
                    // A radius of 0 chooses ray casting rather than sphere casting.
                    sensor.SphereCastRadius = 0;
                    sensor.RayLength = ChildSensorForwardRayLength;

                    // TODO: Detect if `ChildSensorDectableTags` is overriden, and issue a warning if not?
                    sensor.DetectableTags = ChildSensorForwardDetectableTags;
                }
                else
                {
                    Transform raySensorTransform = transform.Find(SENSOR_OBJECT_NAME);
                    if (raySensorTransform != null)
                    {
                        DestroyImmediate(raySensorTransform.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Called once by ML-Agents when the agent is first enabled at the start of training.
        /// </summary>
        public override void Initialize()
        {
            Debug.Log("FetchAgent.Initialize");

            base.Initialize();

            agentCamera = GetComponentInChildren<Camera>();
            _agentRigidbody = GetComponent<Rigidbody>();
            if (!trainingMode)
            {
                // If not in training mode, play forever.
                MaxStep = 0;
            }
        }

        /// <summary>
        /// Called by ML-Agents each time an episode begins.  Used to trigger random placement
        /// of objects in the arena (the class derived from <see cref="EasyMLArena"/>).
        /// </summary>
        public override void OnEpisodeBegin()
        {
            Debug.Log("FetchAgent.OnEpisodeBegin");

            base.OnEpisodeBegin();

            if (trainingMode)
            {
                // With each new episode the items in the arena should be placed randomly.
                // It seems that this method is the place to do so, because the delegate
                // `Academy.Instance.OnEnvironmentReset` seems to be called only at the
                // very start of training and not with each new episode, despite what is
                // suggested in the documentation.

                Transform current = transform.parent;
                while (current != null)
                {
                    EasyMLArena arena = current.gameObject.GetComponent<EasyMLArena>();
                    if (arena != null)
                    {
                        arena.PlaceRandomly();
                        break;
                    }
                    current = current.parent;
                }
            }

            // Stop movement before a new episode begins.
            if (_agentRigidbody != null)
            {
                _agentRigidbody.velocity = Vector3.zero;
                _agentRigidbody.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Called when an action is received from either the player input or the neural network.
        /// A derived class must override this function to generate the agent motion indicated
        /// by the action values.  It is also common to call AddReward in this function.
        /// </summary>
        /// <param name="actions">Action to take</param>
        ///
        public override void OnActionReceived(ActionBuffers actions)
        {
            // In the derived class implementation use `actions.ContinuousActions[i]`.
            throw new MissingMethodException();
        }

        /// <summary>
        /// A derived class must override this function to call sensor.AddObservation(X) with
        /// X being each relevant observation from the environment.  Note that the number of 
        /// observations must equal the VectorObservationSize property.
        /// </summary>
        /// <param name="sensor">The vector sensor</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            // In the derived class implementation use `sensor.AddObservation(X)`
            // Consider using `Debug.Assert(VectorObservationSize == 9, "Incorrect observation count")`
            throw new MissingMethodException();
        }

        /// <summary>
        /// When behavior type is set to "Heuristic only" on the agent's behavior parameters,
        /// then this function will be called.  Its return value will be fed into
        /// <see cref="OnActionReceived(ActionBuffers)"/> instead of using the neural network
        /// </summary>
        /// <param name="actionsOut">An output action buffer</param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // In the derived class implementation use:
            // ```
            // float[] continuouActions = actionsOut.ContinuousActions;
            // continuouActions[0] = x;
            // ```
            throw new MissingMethodException();
        }

        /// <summary>
        /// Prevents the agent from moving and taking actions.
        /// </summary>
        public void FreezeAgent()
        {
            Debug.Assert(trainingMode == false, "Freeze/unfreeze not supported when training");
            _frozen = true;
            _agentRigidbody.Sleep();
        }

        /// <summary>
        /// Resumes movement and actions.
        /// </summary>
        public void UnfreezeAgent()
        {
            Debug.Assert(trainingMode == false, "Freeze/unfreeze not supported when training");
            _frozen = false;
            _agentRigidbody.WakeUp();
        }
    }
}
