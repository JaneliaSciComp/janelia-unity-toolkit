using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    public class ExampleUsingFicTrac : MonoBehaviour
    {
        public string ficTracServerAddress = "127.0.0.1";
        public int ficTracServerPort = 2000;
        public float ficTracBallRadius = 0.5f;

        public void Start()
        {
            _ficTracReader = new FicTracReader(ficTracServerAddress, ficTracServerPort);
            _ficTracReader.Start();
        }

        public void Update()
        {
            long readTimestampMs = 0;
            while (_ficTracReader.GetNextMessage(ref _ficTracMessage, ref readTimestampMs))
            {
                // https://github.com/rjdmoore/fictrac/blob/master/doc/data_header.txt
                // COL     PARAMETER                       DESCRIPTION
                // 1       frame counter                   Corresponding video frame(starts at #1).
                // 6-8     delta rotation vector (lab)     Change in orientation since last frame,
                //                                         represented as rotation angle / axis(radians)
                //                                         in laboratory coordinates(see
                //                                         * configImg.jpg).
                
                // https://www.researchgate.net/figure/Visual-output-from-the-FicTrac-software-see-supplementary-video-a-A-segment-of-the_fig2_260044337
                // Rotation about `a_x` is sideways translation
                // Rotation about `a_y` is forward/backward translation
                // Rotation about `a_z` is heading change

                float forward = _ficTracMessage.deltaRotLab.y * ficTracBallRadius;
                float sideways = _ficTracMessage.deltaRotLab.x * ficTracBallRadius;
                _translation.Set(forward, 0, sideways);

                float headingChangeDegrees = _ficTracMessage.deltaRotLab.z * Mathf.Rad2Deg;
                _rotation.Set(0, -headingChangeDegrees, 0);

                transform.Translate(_translation);
                transform.Rotate(_rotation);
            }
        }

        public void OnDisable()
        {
            _ficTracReader.OnDisable();
        }

        private FicTracReader _ficTracReader;
        private FicTracReader.Message _ficTracMessage = new FicTracReader.Message();
        private Vector3 _translation = new Vector3();
        private Vector3 _rotation = new Vector3();
    }
}
