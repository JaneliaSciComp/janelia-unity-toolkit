using System;
using UnityEngine;

public class AnimateCylinderTexture : MonoBehaviour
{
    public float[] vRotDeg_per_sec;

    public int[] sweepRepeatVec;

    public float numElevationSteps = 10.0f;

    public float delaySeconds = 10;
    public float offsetTex = -90.0f;

    public float offsetEl= 0.1f;

    private int repeats = 1; // use repeating velocities to ensure repeats
    private Material cylinderMaterial;
    private float elevation;
    private int currentStep = 1;
    private float rotDir = 1.0f;
    private int vel = 0;
    private float waitTime = 0;

    // Current azimuth in degrees (0-360), readable by other scripts (e.g., LED trigger)
    private float _azimuthDeg;
    public float AzimuthDeg => _azimuthDeg;


    //set up logging
    [Serializable]
    private class textureLogEntry : Janelia.Logger.Entry
    {
        public float xpos;
        public float ypos;

    };

    private textureLogEntry _currentLogEntry = new textureLogEntry();


    void Start()
    {
        cylinderMaterial = Resources.Load(Janelia.CylinderBackgroundResources.MaterialName, typeof(Material)) as Material;
        //Resources.Load(Janelia.CylinderBackgroundResources.MaterialName, typeof(Material)) as Material

        if (cylinderMaterial == null)
        {
            Debug.LogError("Could not load material'" + Janelia.CylinderBackgroundResources.MaterialName + "'");
        }

        //reset texture based on offset
        elevation = offsetEl;
        float x = offsetTex / 360.0f;
        float y = elevation;

        Vector2 offset = new Vector2(x, y);

        cylinderMaterial.SetTextureOffset("_MainTex", offset);

        // Store initial azimuth (normalized to 0-360)
        _azimuthDeg = ((x % 1f) + 1f) % 1f * 360f;

        //log values
        _currentLogEntry.xpos = x;
        _currentLogEntry.ypos = y;
        Janelia.Logger.Log(_currentLogEntry);
    }

    void Update()
    {
        // Done with all velocities
        if (vel >= vRotDeg_per_sec.Length || vel >= sweepRepeatVec.Length)
            return;

        //check if a velocity has been completed
        if (currentStep > repeats * 2 * sweepRepeatVec[vel] * numElevationSteps)
        {
            vel+=1;
            currentStep = 1;
            elevation = offsetEl;
            if (vel <= vRotDeg_per_sec.Length)
            {
                waitTime = Time.time;
            }
        }

        if (cylinderMaterial & Time.time >= (waitTime+delaySeconds))
        {

            float dTime = Time.time - (waitTime+delaySeconds);
            float x = offsetTex/360.0f + dTime * rotDir * (vRotDeg_per_sec[vel] / 360.0f) % 1;

            //check if a round has been completed
            if (dTime * (vRotDeg_per_sec[vel] / 360.0f ) > currentStep)
            {
                if (currentStep % (2*sweepRepeatVec[vel]) == 0) // we finished the nth repeat, change elevation and reset direction
                {
                    elevation += (1-offsetEl) / numElevationSteps;
                    rotDir = 1.0f;
                }
                else if (currentStep % 2 == 0) // finished an even round --> second of two repeats
                {
                    rotDir = 1.0f; // now change direction to +ve
                }
                else // finished an uneven round --> first of two repeats
                {
                    rotDir = -1.0f; // now change direction to -ve
                }
                currentStep += 1;

            }

            float y = elevation;
            Vector2 offset = new Vector2(x, y);

            // Store current azimuth (normalized to 0-360)
            _azimuthDeg = ((x % 1f) + 1f) % 1f * 360f;

            //stop if done
            if (currentStep*vel <= (repeats * 2 * numElevationSteps * sweepRepeatVec[sweepRepeatVec.Length-1] * vRotDeg_per_sec.Length))
            {

                cylinderMaterial.SetTextureOffset("_MainTex", offset);

                //log values
                _currentLogEntry.xpos = x;
                _currentLogEntry.ypos = y;
                Janelia.Logger.Log(_currentLogEntry);
            }

        }

    }

}
