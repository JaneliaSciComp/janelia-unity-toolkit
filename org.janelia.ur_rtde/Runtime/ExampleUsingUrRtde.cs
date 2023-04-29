using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class ExampleUsingUrRtde : MonoBehaviour
{
    void Start()
    {
        _controlInterface = new Janelia.UrRtdeControlInterface("172.17.0.2");
        _receiveInterface = new Janelia.UrRtdeReceiveInterface("172.17.0.2");
    }

    void Update()
    {
        const float D = 0.025f;
        float dx = 0;
        float dz = 0;
        if (Input.GetKeyDown("up"))
        {
            dz = D;
        }
        if (Input.GetKeyDown("down"))
        {
            dz =-D;
        }
        if (Input.GetKeyDown("left"))
        {
            dx = D;
        }
        if (Input.GetKeyDown("right"))
        {
            dx =-D;
        }
        if ((dx != 0) || (dz != 0))
        {
            float[] pose;
            bool ok = _receiveInterface.GetActualTcpPose(out pose);
            Debug.Log("Got tool pose; success: " + ok);
            if (ok) {
                pose[0] += dx;
                pose[2] += dz;
                Debug.Log("New tool pose: " + pose[0] + ", " + pose[1] + ", " + pose[2] + ", " + pose[3] + ", " + pose[4] + ", " + pose[5]);
                ok = _controlInterface.MoveL(pose);
                Debug.Log("Moved tool to new position; success: " + ok);
            }
        }
        if (Input.GetKeyDown("m"))
        {            
            float velocity = 2.0f;
            float acceleration = 1.3f;
            Janelia.UrRtdeControlInterface.Path path = new Janelia.UrRtdeControlInterface.Path();
            path.AddEntry(new Janelia.UrRtdeControlInterface.PathEntry {
                moveType = Janelia.UrRtdeControlInterface.PathEntry.MoveType.MoveJ,
                positionType = Janelia.UrRtdeControlInterface.PathEntry.PositionType.PositionJoints,
                parameters = new List<float> {rad(-80), rad(-97), rad(-114), rad(-58), rad(91), rad(44), velocity, acceleration, 0}
            });
            path.AddEntry(new Janelia.UrRtdeControlInterface.PathEntry {
                moveType = Janelia.UrRtdeControlInterface.PathEntry.MoveType.MoveJ,
                positionType = Janelia.UrRtdeControlInterface.PathEntry.PositionType.PositionJoints,
                parameters = new List<float> {rad(-50), rad(-97), rad(-114), rad(-58), rad(91), rad(44), velocity, acceleration, 0}
            });
            bool ok = _controlInterface.MovePath(path);
            Debug.Log("Moved joints on path; success: " + ok);
        }
    }

    private float rad(float deg)
    {
        return deg / 180 * Mathf.PI;
    }

    private Janelia.UrRtdeControlInterface _controlInterface;
    private Janelia.UrRtdeReceiveInterface _receiveInterface;
}
