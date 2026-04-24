using UnityEngine;

[DisallowMultipleComponent]
public class DropProperties : MonoBehaviour
{
    [Header("Radius / Mass")]
    public float oilDensityKgPerM3 = 875.3f;

    [Tooltip("Used only in normal random mode.")]
    public float minMassKg = 4.5e-16f;

    [Tooltip("Used only in normal random mode.")]
    public float maxMassKg = 1.3e-15f;

    [Header("Charge Range (multiples of e)")]
    public int minChargeMultiple = 1;
    public int maxChargeMultiple = 5;

    [Header("Options")]
    [Tooltip("Keep this OFF if SpraySpawner controls spawning.")]
    public bool randomizeOnSpawn = false;

    public bool applyMassToRigidbody = true;

    [Header("Visual Radius")]
    public bool applyVisualScale = true;

    [Tooltip("If empty, this object's transform will be scaled.")]
    public Transform visualRoot;

    [Tooltip("Radius that corresponds to the original prefab size. Use 1.0 so r=1.0 µm is normal size.")]
    public float visualReferenceRadiusMicrometer = 1.0f;

    [Tooltip("1 = direct scaling. With reference 1.0: 0.5 µm = 0.5x, 1.0 µm = 1x, 1.5 µm = 1.5x.")]
    [Range(0.05f, 1.5f)]
    public float visualScaleStrength = 1f;

    public float RadiusMicrometer { get; private set; }
    public float MassKg { get; private set; }
    public float ChargeC { get; private set; }
    public int ChargeMultiple { get; private set; }

    private Rigidbody rb;
    private Vector3 originalVisualScale;
    private bool originalScaleCached;

    private const double ElementaryCharge = 1.602176634e-19;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        CacheOriginalScale();
    }

    private void OnEnable()
    {
        CacheOriginalScale();

        if (randomizeOnSpawn)
            RandomizeAndApply();
    }

    private void CacheOriginalScale()
    {
        if (originalScaleCached)
            return;

        Transform target = visualRoot != null ? visualRoot : transform;
        originalVisualScale = target.localScale;
        originalScaleCached = true;
    }

    public void RandomizeAndApply()
    {
        float minM = Mathf.Min(minMassKg, maxMassKg);
        float maxM = Mathf.Max(minMassKg, maxMassKg);

        MassKg = Random.Range(minM, maxM);
        RadiusMicrometer = CalculateRadiusMicrometerFromMass(MassKg);

        RandomizeChargeOnly();

        ApplyToRigidbody();
        ApplyVisualScale();

        Debug.Log($"[DropProperties] Random drop set: radius={RadiusMicrometer:0.00} µm, mass={MassKg:E3} kg, charge={ChargeMultiple}e");
    }

    public void SetTeachingRadiusAndCharge(float radiusMicrometer, int chargeMultiple)
    {
        RadiusMicrometer = Mathf.Max(0.01f, radiusMicrometer);
        MassKg = CalculateMassKgFromRadiusMicrometer(RadiusMicrometer);

        SetChargeMultipleAndApply(chargeMultiple);

        ApplyToRigidbody();
        ApplyVisualScale();

        Debug.Log($"[DropProperties] Teaching drop set: radius={RadiusMicrometer:0.00} µm, mass={MassKg:E3} kg, charge={ChargeMultiple}e");
    }

    public void SetRadiusMicrometerAndApply(float radiusMicrometer, bool keepCurrentCharge = true)
    {
        RadiusMicrometer = Mathf.Max(0.01f, radiusMicrometer);
        MassKg = CalculateMassKgFromRadiusMicrometer(RadiusMicrometer);

        if (!keepCurrentCharge || ChargeMultiple <= 0)
            RandomizeChargeOnly();

        ApplyToRigidbody();
        ApplyVisualScale();
    }

    public void SetChargeMultipleAndApply(int chargeMultiple)
    {
        ChargeMultiple = Mathf.Max(1, chargeMultiple);
        ChargeC = (float)(ChargeMultiple * ElementaryCharge);
    }

    public void RandomizeChargeOnly()
    {
        int minQ = Mathf.Min(minChargeMultiple, maxChargeMultiple);
        int maxQ = Mathf.Max(minChargeMultiple, maxChargeMultiple);

        ChargeMultiple = Random.Range(minQ, maxQ + 1);
        ChargeC = (float)(ChargeMultiple * ElementaryCharge);
    }

    private void ApplyToRigidbody()
    {
        if (applyMassToRigidbody && rb != null)
            rb.mass = MassKg;
    }

    private void ApplyVisualScale()
    {
        if (!applyVisualScale)
            return;

        CacheOriginalScale();

        Transform target = visualRoot != null ? visualRoot : transform;

        float reference = Mathf.Max(0.01f, visualReferenceRadiusMicrometer);
        float rawRatio = RadiusMicrometer / reference;

        float visualFactor = Mathf.Lerp(1f, rawRatio, visualScaleStrength);
        visualFactor = Mathf.Clamp(visualFactor, 0.05f, 1.8f);

        target.localScale = originalVisualScale * visualFactor;
    }

    private float CalculateMassKgFromRadiusMicrometer(float radiusMicrometer)
    {
        float r = radiusMicrometer * 1e-6f;
        float volume = (4f / 3f) * Mathf.PI * r * r * r;
        return oilDensityKgPerM3 * volume;
    }

    private float CalculateRadiusMicrometerFromMass(float massKg)
    {
        float m = Mathf.Max(1e-20f, massKg);
        float volume = m / Mathf.Max(1e-6f, oilDensityKgPerM3);
        float radiusMeter = Mathf.Pow((3f * volume) / (4f * Mathf.PI), 1f / 3f);
        return radiusMeter * 1e6f;
    }
}