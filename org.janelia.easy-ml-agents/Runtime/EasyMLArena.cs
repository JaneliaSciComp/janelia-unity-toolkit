using UnityEngine;

namespace Janelia
{
    /// <summary>
    /// The specification for helper functions defined in the editor assembly and passed to
    /// code in the runtime assembly.
    /// </summary>
    public interface IEasyMLSetupHelper
    {
        public bool DisplayDialog(string title, string message, string ok = "OK", string cancel = "Cancel");
        public void CreateMeshFilter(GameObject assignedTo, string objFilename);
        public void CreateTag(string tag);
        public void CreateColorMaterial(GameObject assignedTo, string color);
        public void CreatePhysicsMaterial(Collider assignedTo, float staticFriction, float dynamicFriction);
        public bool UsingURP();
    }

    /// <summary>
    /// A base class for the parent of all objects involved in the training, with
    /// functions for the initial setup and for the per-episode random placement.
    /// </summary>
    public abstract class EasyMLArena : MonoBehaviour
    {
        /// <summary>
        /// The  <see cref="GameObject.tag"/> given to an arena derived from <see cref="EasyMLArena"/>
        /// </summary>
        public static readonly string TAG_ARENA = "Arena";

        /// <summary>
        /// Performs the initial setup of the objects involved in training (except for the
        /// agent, which derives from <see cref="EasyMLAgent"/> and has its own Setup function,
        /// called after this one is called).  A derived class should override this function 
        /// to add the setup details specific to a particular use case.
        /// </summary>
        /// <param name="helper">A class with helper functions for tasks like adding tags or 
        /// creating materials</param>
        public virtual void Setup(IEasyMLSetupHelper helper)
        {
            if (gameObject.tag != TAG_ARENA)
            {
                helper.CreateTag(TAG_ARENA);
                gameObject.tag = TAG_ARENA;
                name = "Arena";
            }
        }

        /// <summary>
        /// Randomly places the objects involved in training for the beginning of a new
        /// training episode.  A derived class must override this function to add details
        /// specific to a particular use case.
        /// </summary>
        public abstract void PlaceRandomly();
    }
}