using System;
using System.Collections.Generic;
using UnityEngine;

namespace Janelia
{
    public class ExampleUsingFicTrac : MonoBehaviour
    {
        public string ficTracServerAddress = "127.0.0.1";
        public int ficTracServerPort = 2000;

        public void Start()
        {
            _ficTracReader = new FicTracReader(ficTracServerAddress, ficTracServerPort);
            _ficTracReader.Start();
        }

        public void Update()
        {
            // Each message from FicTrac is a string of data values ("columns") separted by commas.
            // The standard C# code for reading messages from a socket and extracting some of the
            // columns generates a lot of temporary strings, and thus triggers garbarge collection.
            // As an alternative, `FicTracReader` provides a `GetNextMessage` routine that reuses
            // an internal `Byte[]` and sets the `ref i0` argument to the index where the next
            // message begins.

            Byte[] dataFromSocket = null;
            long dataTimestampMs = 0;
            int i0 = -1;
            while (_ficTracReader.GetNextMessage(ref dataFromSocket, ref dataTimestampMs, ref i0))
            {
                bool valid = true;

                // Then the indices, relative to `i0`, of individual data columns can be found
                // with `FicTracUtilities.NthSplit`, and the numerical values can be parsed
                // the indices using `FicTracUtilities.ParseDouble` and `FicTracUtilities.ParseLong`.
                // The FicTrac documentation indicates the columns of interest.

                // https://github.com/rjdmoore/fictrac/blob/master/doc/data_header.txt
                // COL     PARAMETER                       DESCRIPTION
                // 1       frame counter                   Corresponding video frame(starts at #1).
                // 6-8     delta rotation vector (lab)     Change in orientation since last frame,
                //                                         represented as rotation angle / axis(radians)
                //                                         in laboratory coordinates(see
                //                                         * configImg.jpg).

                int i6 = 0, len6 = 0;
                FicTracUtilities.NthSplit(dataFromSocket, i0, 6, ref i6, ref len6);
                float a = (float)FicTracUtilities.ParseDouble(dataFromSocket, i6, len6, ref valid);
                if (!valid)
                    break;

                int i7 = 0, len7 = 0;
                FicTracUtilities.NthSplit(dataFromSocket, i0, 7, ref i7, ref len7);
                float b = (float)FicTracUtilities.ParseDouble(dataFromSocket, i7, len7, ref valid);
                if (!valid)
                    break;

                int i8 = 0, len8 = 0;
                FicTracUtilities.NthSplit(dataFromSocket, i0, 8, ref i8, ref len8);
                float c = (float)FicTracUtilities.ParseDouble(dataFromSocket, i8, len8, ref valid);
                if (!valid)
                    break;

                // https://www.researchgate.net/figure/Visual-output-from-the-FicTrac-software-see-supplementary-video-a-A-segment-of-the_fig2_260044337
                // Rotation about `a_x` is sideways translation
                // Rotation about `a_y` is forward/backward translation
                // Rotation about `a_z` is heading change

                float forward = b * Mathf.Rad2Deg;
                float sideways = a * Mathf.Rad2Deg;
                _translation.Set(forward, 0, sideways);

                float heading = c * Mathf.Rad2Deg;
                _rotation.Set(0, -heading, 0);

                transform.Translate(_translation);
                transform.Rotate(_rotation);
            }
        }

        public void OnDisable()
        {
            _ficTracReader.OnDisable();
        }

        private FicTracReader _ficTracReader;
        private Vector3 _translation = new Vector3();
        private Vector3 _rotation = new Vector3();
    }
}
