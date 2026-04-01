using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ElectricFieldVolume : MonoBehaviour
{
    public VoltageKnobInput voltageSource;
    public bool invertVoltage;
    public Transform upperPlate;
    public Transform lowerPlate;
    public float plateSpacingMetersOverride = 0.01f;
    public float fieldScale = 1f;
    public Vector3 fieldDirection = new Vector3(0, 1, 0);
    public float voltageSmoothing = 20f;
    public float maxAccel = 30f;
    public int nullCleanupIntervalFrames = 30;
    public bool logEnterExit;

    readonly HashSet<Rigidbody> bodies = new HashSet<Rigidbody>();
    float voltageSmooth;
    int cleanupCounter;

    void Awake()
    {
        var trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;

        float v = voltageSource != null ? voltageSource.CurrentVoltage : 0f;
        voltageSmooth = invertVoltage ? -v : v;
    }

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        bodies.Add(rb);

        if (logEnterExit)
            Debug.Log($"[ElectricFieldVolume] Enter: {rb.name}", this);
    }

    void OnTriggerStay(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        bodies.Add(rb);
    }

    void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        bodies.Remove(rb);

        if (logEnterExit)
            Debug.Log($"[ElectricFieldVolume] Exit: {rb.name}", this);
    }

    void FixedUpdate()
    {
        if (nullCleanupIntervalFrames > 0)
        {
            cleanupCounter++;
            if (cleanupCounter >= nullCleanupIntervalFrames)
            {
                cleanupCounter = 0;
                bodies.RemoveWhere(rb => rb == null);
            }
        }

        if (bodies.Count == 0) return;

        float vRaw = voltageSource != null ? voltageSource.CurrentVoltage : 0f;
        if (invertVoltage) vRaw = -vRaw;

        float alpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, voltageSmoothing) * Time.fixedDeltaTime);
        voltageSmooth = Mathf.Lerp(voltageSmooth, vRaw, alpha);

        Vector3 dir = fieldDirection.sqrMagnitude > 1e-6f ? fieldDirection.normalized : Vector3.up;
        float d = GetPlateSpacingMeters();
        if (d <= 1e-6f) return;

        foreach (var rb in bodies)
        {
            if (rb == null) continue;

            var dp = rb.GetComponent<DropProperties>();
            if (dp == null) continue;

            float q = dp.ChargeC;
            float m = Mathf.Max(1e-18f, dp.MassKg);
            if (Mathf.Abs(q) < 1e-20f) continue;

            Vector3 g = Physics.gravity;
            var oil = rb.GetComponent<OilDrop>();
            if (oil != null)
                g = oil.customGravity;

            float gAlong = Vector3.Dot(g, dir);
            float gAbs = Mathf.Abs(gAlong);
            if (gAbs < 1e-6f) continue;

            float hoverVoltage = (m * gAbs * d) / (Mathf.Abs(q) * Mathf.Max(1e-6f, fieldScale));
            if (hoverVoltage <= 1e-6f) continue;

            float ratio = Mathf.Abs(voltageSmooth) / hoverVoltage;
            float electricAccel = gAbs * ratio;

            bool electricAlongField = q >= 0f;
            Vector3 electricDir = electricAlongField ? dir : -dir;

            Vector3 accel = electricDir * electricAccel;
            accel = Vector3.ClampMagnitude(accel, maxAccel);

            rb.AddForce(accel, ForceMode.Acceleration);
        }
    }

    public float GetPlateSpacingMeters()
    {
        if (plateSpacingMetersOverride > 0f)
            return plateSpacingMetersOverride;

        Vector3 dir = fieldDirection.sqrMagnitude > 1e-6f ? fieldDirection.normalized : Vector3.up;

        if (upperPlate != null && lowerPlate != null)
            return Mathf.Abs(Vector3.Dot(upperPlate.position - lowerPlate.position, dir));

        var box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Vector3 s = box.bounds.size;
            return Mathf.Abs(dir.x) * s.x + Mathf.Abs(dir.y) * s.y + Mathf.Abs(dir.z) * s.z;
        }

        return 0f;
    }
}