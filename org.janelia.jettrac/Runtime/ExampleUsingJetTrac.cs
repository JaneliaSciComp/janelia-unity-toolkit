using UnityEngine;

namespace Janelia
{
    // The direction for forward motion is the positive local X axis.
    // The up direction is the positive local Y axis.
    // Assumes the associated `GameObject` has a child named `Head`
    // whose local `Y` rotation is 90 degrees, so that if the main camera is a child
    // of `Head` then it will be looking in the direction of forward motion 
    // (i.e. the camera's positive Z axis will be rotated to match the main `GameObject`'s
    // positive X axis) for the jETTrac's head rotation at start up.

    // Also binds the 'q' and Escape keys to quit the application.

    public class ExampleUsingJetTrac : MonoBehaviour
    {
        public bool readHead = true;
        public bool allowBodyRotation = true;
        public bool smooth = true;
        public int smoothingWindow = 3;

        public bool debug = false;

        public void Start()
        {
            _reader = new JetTracReader();
            _reader.debug = debug;

            float headRotationYDegs0 = 0;
            _headTransform = transform.Find("Head");
            if (_headTransform != null)
            {
                headRotationYDegs0 = _headTransform.eulerAngles.y;
            }

            _transformer = new JetTracTransformer(headRotationYDegs0, smoothingWindow);
            _reader.Start();
        }

        public void Update()
        {
            if (Input.GetKey("q") || Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            Vector3 bodyPosition = transform.position;
            Vector3 bodyRotation = transform.eulerAngles;
            Vector3 headRotation = Vector3.zero;
            if (_headTransform)
            {
                headRotation = _headTransform.eulerAngles;
            }

            JetTracParser.BallMessage ballMessage = new JetTracParser.BallMessage();
            while (_reader.GetNextBallMessage(ref ballMessage))
            {
                if (debug)
                {
                    Debug.Log(Now() + "ball: " + ballMessage.deviceTimestampUs + " us, " + ballMessage.x0 + ", " + ballMessage.y0 + ", "
                        + ballMessage.x1 + ", " + ballMessage.y1 + ", smooth " + smooth);
                }

                _transformer.AddInput(ballMessage);
            }
            if (allowBodyRotation)
            {
                _transformer.Update(ref bodyPosition, ref bodyRotation.y, smooth);
            }
            else{
                _transformer.Update(ref bodyPosition, smooth);
            }

            if (debug)
            {
                Debug.Log(Now() + "body pos " + bodyPosition + ", body rot " + bodyRotation.y + ", head rot " + headRotation.y);
            }

            transform.position = bodyPosition;
            transform.eulerAngles = bodyRotation;

            if (readHead && (_headTransform != null))
            {

                JetTracParser.HeadMessage headMessage = new JetTracParser.HeadMessage();
                while (_reader.GetNextHeadMessage(ref headMessage))
                {
                    if (debug)
                    {
                        Debug.Log(Now() + "head: " + headMessage.deviceTimestampUs + " us, " + headMessage.angleDegs);
                    }

                    _transformer.AddInput(headMessage);
                }
                _transformer.Update(ref headRotation.y, smooth);

                if (debug)
                {
                    Debug.Log(Now() + "body pos " + bodyPosition + ", body rot " + bodyRotation.y + ", head rot " + headRotation.y);
                }

                _headTransform.eulerAngles = headRotation;
            }
        }

        public void OnDisable()
        {
            if (debug)
            {
                Debug.Log("OnDisable(): frame " + Time.frameCount);
            }
            _reader.OnDisable();
        }

        private string Now()
        {
            if (Application.isEditor)
            {
                return "";
            }
            return "[" + Time.frameCount + ", " + Time.time + "] ";
        }

        private Transform _headTransform;
        private JetTracReader _reader;
        private JetTracTransformer _transformer;
    }
}
