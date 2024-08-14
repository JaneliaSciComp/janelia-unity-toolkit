using System;
using System.IO;
using UnityEngine;

namespace Janelia
{
    public class MuJoCoKinematic : MonoBehaviour
    {
        public int overallQposOffset;

        // Returns an `animationFrame` that can be modified.
        public delegate void AnimationFrameDelegate(int frame, out float[] animationFrame, out int length);
        public AnimationFrameDelegate animationFrameDelegate;

        // Modifies the `animationFrame` in place.
        public delegate void TweakAnimationFrameDelegate(int frame, float[] animationFrame, int length);
        public TweakAnimationFrameDelegate tweakAnimationFrameDelegate;

        public int slowdown = 1;

        public bool debug = false;


        public void Start()
        {
            Mujoco.MjScene.Instance.Mode = Mujoco.MjScene.SimulationMode.KINEMATIC;

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            
            if (animationFrameDelegate == null)
            {
                animationFrameDelegate = DefaultAnimationFrameDelegate;
            }
        }

        public void Update()
        {
            int frame = Time.frameCount / slowdown;

            float[] animationFrame;
            int frameLength;
            animationFrameDelegate(frame, out animationFrame, out frameLength);
            MuJoCoRuntimeUtilities.AddFreeJointPose(_initialPosition, _initialRotation, animationFrame);

            if (tweakAnimationFrameDelegate != null)
            {
                tweakAnimationFrameDelegate(frame, animationFrame, frameLength);
            }

            if (debug)
            {
                Debug.Log("MuJoCoKinematic.Update SetKinematics([" + animationFrame[0] + ", " + animationFrame[1] + ", " + animationFrame[2] + ", ...])");
            }

            Mujoco.MjScene.Instance.SetKinematics(animationFrame, overallQposOffset, frameLength);
        }

        private void DefaultAnimationFrameDelegate(int frame, out float[] animationFrame, out int length)
        {
            if (_defaultAnimationFrame == null)
            {
                _defaultAnimationFrame = new float[] { 0, 0, 0, 1, 0, 0, 0 };
                _animationFrame = new float[_defaultAnimationFrame.Length];
            }
            for (int i = 0; i < _defaultAnimationFrame.Length; ++i)
            {
                _animationFrame[i] = _defaultAnimationFrame[i];
            }
            animationFrame = _animationFrame;
            length = _animationFrame.Length;
        }

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        float[] _defaultAnimationFrame;
        float[] _animationFrame;
    }
}
