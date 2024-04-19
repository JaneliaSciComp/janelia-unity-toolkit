using System;
using UnityEngine;

namespace Janelia
{
    public class DistanceTeleporter : MonoBehaviour
    {
        public GameObject distanceFrom;
        public float thresholdDistance;
        public GameObject teleportTo;

        public void Start()
        {
            float distance = SessionParameters.GetFloatParameter("teleportAtDistance", thresholdDistance);
            thresholdDistance = distance;
        }

        public void LateUpdate()
        {
            if (distanceFrom != null)
            {
                float distance = Vector3.Distance(transform.position, distanceFrom.transform.position);
                if (distance >= thresholdDistance)
                {
                    if (teleportTo)
                    {
                        Vector3 position = teleportTo.transform.position;
                        position.y = transform.position.y;
                        transform.position = position;

                        Vector3 eulerAngles = transform.eulerAngles;
                        eulerAngles.y = teleportTo.transform.eulerAngles.y;
                        transform.eulerAngles = eulerAngles;

                        _currentTeleportedLog.teleportedToPosition = position;
                        _currentTeleportedLog.teleportedToHeadingDegs = eulerAngles.y;
                        _currentTeleportedLog.thresholdDistance = thresholdDistance;
                        _currentTeleportedLog.distanceFrom = distanceFrom.transform.position;
                        Logger.Log(_currentTeleportedLog);
                    }
                }
            }
        }

        [Serializable]
        private class TeleportedLog : Logger.Entry
        {
            public Vector3 teleportedToPosition;
            public float teleportedToHeadingDegs;
            public float thresholdDistance;
            public Vector3 distanceFrom;

        };
        private TeleportedLog _currentTeleportedLog = new TeleportedLog();
    }
}