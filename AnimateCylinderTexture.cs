using System;
using UnityEngine;

public class AnimateCylinderTexture : MonoBehaviour
{
    [Header("Sweep settings (degrees)")]
    public float sweepStartDeg = 90f;
    public float sweepEndDeg   = 270f;   // texture sweeps only within [start, end]
    public bool allowWrapRange = false;  // if true, a range like 300→60 is allowed via wrap

    [Header("Velocity / repeats / stepping")]
    public float[] vRotDeg_per_sec;
    public int[] sweepRepeatVec;         // how many back-and-forths per elevation step, per vel
    public float numElevationSteps = 10.0f;

    [Header("Delays / offsets")]
    public float delaySeconds = 10f;
    public float offsetTexDeg = 0f;      // optional extra phase offset in degrees (was -90)
    public float offsetEl = 0.1f;        // starting elevation in [0,1]

    // internal state
    private int repeats = 1; // keep original intent (repeating velocities to ensure repeats)
    private Material cylinderMaterial;
    private float elevation;
    private int vel = 0;                   // index into vRotDeg_per_sec
    private float waitTime = 0f;

    private float azimuthDeg;              // current azimuth (deg) constrained to [start,end] (or wrapped range)
    public float AzimuthDeg => azimuthDeg; // read-only access for other scripts
    private float rotDir = +1f;            // +1 forward, -1 backward within the sweep window
    private int halfSweepCount = 0;        // increments every time we hit an end (start or end)
    private int elevationStepIndex = 0;    // how many elevation steps completed

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
        if (cylinderMaterial == null)
        {
            Debug.LogError("Could not load material '" + Janelia.CylinderBackgroundResources.MaterialName + "'");
        }

        // Normalize/validate sweep limits
        sweepStartDeg = NormalizeDeg(sweepStartDeg);
        sweepEndDeg   = NormalizeDeg(sweepEndDeg);

        if (!allowWrapRange && sweepEndDeg < sweepStartDeg)
        {
            // if wrap not allowed and start > end, swap so we have a proper interval
            float tmp = sweepStartDeg; sweepStartDeg = sweepEndDeg; sweepEndDeg = tmp;
        }

        elevation = Mathf.Clamp01(offsetEl);
        azimuthDeg = sweepStartDeg; // start at the beginning of the window

        ApplyTextureOffset(); // apply initial offset & log
        waitTime = Time.time; // start delay window
    }

    void Update()
    {
        // Done with all velocities?
        if (vel >= vRotDeg_per_sec.Length || vel >= sweepRepeatVec.Length)
            return;

        // honor delay between velocity blocks
        if (Time.time < (waitTime + delaySeconds))
            return;

        float speedDegPerSec = vRotDeg_per_sec[vel];
        float dAz = speedDegPerSec * rotDir * Time.deltaTime;

        // Integrate azimuth within the sweep window
        if (IsWrappedRange())
        {
            // sweep across a wrapped interval (e.g., 300 → 60)
            azimuthDeg = Wrap(azimuthDeg + dAz);

            // Check bounds along the wrapped arc
            if (!OnWrappedArc(azimuthDeg))
            {
                // We crossed out of the arc; clamp to boundary and flip
                // Determine which end was crossed by checking direction
                float target = (rotDir > 0f) ? sweepEndDeg : sweepStartDeg;
                azimuthDeg = target;
                rotDir *= -1f;
                OnHitEnd();
            }
        }
        else
        {
            // simple clamped interval [start, end]
            azimuthDeg += dAz;

            if (azimuthDeg >= sweepEndDeg)
            {
                azimuthDeg = sweepEndDeg;
                rotDir = -1f;
                OnHitEnd();
            }
            else if (azimuthDeg <= sweepStartDeg)
            {
                azimuthDeg = sweepStartDeg;
                rotDir = +1f;
                OnHitEnd();
            }
        }

        // Apply texture coords
        ApplyTextureOffset();

        // Stop condition (kept similar spirit to your original)
        // After finishing all elevation steps for all velocities, we stop updating.
        if (elevationStepIndex >= Mathf.RoundToInt(numElevationSteps))
        {
            // advance to the next velocity once we finish all steps at this velocity
            vel++;
            if (vel < vRotDeg_per_sec.Length && vel < sweepRepeatVec.Length)
            {
                // reset for next velocity
                waitTime = Time.time;
                elevationStepIndex = 0;
                halfSweepCount = 0;
                rotDir = +1f;
                azimuthDeg = sweepStartDeg;
            }
        }
    }

    private void OnHitEnd()
    {
        halfSweepCount++;

        // Each back-and-forth is two hits; after N back-and-forths, step elevation
        int neededHalfSw = 2 * sweepRepeatVec[vel] * repeats;
        if (halfSweepCount % neededHalfSw == 0)
        {
            // step elevation (evenly across (1 - offsetEl) range)
            elevationStepIndex++;
            if (numElevationSteps > 0)
            {
                elevation = Mathf.Clamp01(offsetEl + (1f - offsetEl) * (elevationStepIndex / numElevationSteps));
            }
            else
            {
                elevation = Mathf.Clamp01(offsetEl);
            }

            // restart from the start side moving forward
            rotDir = +1f;
            azimuthDeg = IsWrappedRange() ? sweepStartDeg : sweepStartDeg;
        }
    }

    private void ApplyTextureOffset()
    {
        // Convert azimuth (deg) to texture x in [0,1], with optional extra phase offset
        float xDeg = Wrap(azimuthDeg + offsetTexDeg);
        float x = xDeg / 360f;
        float y = elevation;

        Vector2 offset = new Vector2(x, y);
        if (cylinderMaterial != null)
        {
            cylinderMaterial.SetTextureOffset("_MainTex", offset);

            // log values
            _currentLogEntry.xpos = x;
            _currentLogEntry.ypos = y;
            Janelia.Logger.Log(_currentLogEntry);
        }
    }

    // --- helpers ---
    private static float NormalizeDeg(float deg)
    {
        // map to [0,360)
        deg = deg % 360f;
        if (deg < 0f) deg += 360f;
        return deg;
    }

    private static float Wrap(float deg)
    {
        if (deg >= 360f || deg < 0f) deg = NormalizeDeg(deg);
        return deg;
    }

    private bool IsWrappedRange()
    {
        // true if we intentionally allow a range that crosses 0/360
        return allowWrapRange && sweepEndDeg < sweepStartDeg;
    }

    private bool OnWrappedArc(float d)
    {
        // For a wrapped arc (e.g., 300→60), points are on arc if d >= start OR d <= end
        // (because the arc crosses 0)
        return (d >= sweepStartDeg) || (d <= sweepEndDeg);
    }
}