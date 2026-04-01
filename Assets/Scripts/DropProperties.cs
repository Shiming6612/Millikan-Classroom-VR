using UnityEngine;

[DisallowMultipleComponent]
public class DropProperties : MonoBehaviour
{
    public float minMassKg = 4.5e-16f;
    public float maxMassKg = 1.3e-15f;

    public int minChargeMultiple = 1;
    public int maxChargeMultiple = 5;

    public bool randomizeOnSpawn = false;
    public bool applyMassToRigidbody = true;

    public float MassKg { get; private set; }
    public float ChargeC { get; private set; }
    public int ChargeMultiple { get; private set; }

    Rigidbody rb;

    const double ElementaryCharge = 1.602176634e-19;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (randomizeOnSpawn)
            RandomizeAndApply();
    }

    public void RandomizeAndApply()
    {
        float minM = Mathf.Min(minMassKg, maxMassKg);
        float maxM = Mathf.Max(minMassKg, maxMassKg);
        MassKg = Random.Range(minM, maxM);

        int minQ = Mathf.Min(minChargeMultiple, maxChargeMultiple);
        int maxQ = Mathf.Max(minChargeMultiple, maxChargeMultiple);
        ChargeMultiple = Random.Range(minQ, maxQ + 1);

        ChargeC = (float)(ChargeMultiple * ElementaryCharge);

        if (applyMassToRigidbody && rb != null)
            rb.mass = MassKg;
    }
}