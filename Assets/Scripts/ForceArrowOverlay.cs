using UnityEngine;

public class SimpleForceArrowOverlay : MonoBehaviour
{
    [Header("Refs")]
    public DropSelectionManager selectionManager;
    public ElectricFieldVolume fieldVolume;
    public VoltageKnobInput voltageSource;

    [Header("Arrow Objects")]
    public Transform gravityArrow;
    public Transform buoyancyArrow;
    public Transform electricArrow;

    [Header("Follow")]
    public Vector3 overlayOffset = Vector3.zero;
    public Vector3 gravityOffset = new Vector3(-0.035f, 0f, 0f);
    public Vector3 buoyancyOffset = Vector3.zero;
    public Vector3 electricOffset = new Vector3(0.035f, 0f, 0f);

    [Header("Displayed Lengths")]
    public float gravityLength = 0.03f;
    public float buoyancyLength = 0.004f;
    public float electricMinLength = 0.002f;
    public float electricMaxLength = 0.06f;

    [Header("Visibility")]
    public bool showBuoyancyArrow = true;
    public bool hideElectricWhenVoltageZero = true;
    public float minVoltageToShowElectric = 0.01f;

    [Header("Debug")]
    public bool logDebug = false;

    private BoxCollider fieldBox;
    private SelectableDrop currentSelected;

    private void Awake()
    {
        if (fieldVolume != null)
            fieldBox = fieldVolume.GetComponent<BoxCollider>();

        HideAll();
    }

    private void Update()
    {
        currentSelected = selectionManager != null ? selectionManager.CurrentSelected : null;

        if (currentSelected == null)
        {
            HideAll();
            return;
        }

        Transform target = GetSelectedTargetTransform(currentSelected);

        if (target == null)
        {
            HideAll();
            return;
        }

        if (!IsInsideField(target.position))
        {
            HideAll();
            return;
        }

        transform.position = target.position + overlayOffset;

        UpdateGravityArrow();
        UpdateBuoyancyArrow();
        UpdateElectricArrow(currentSelected);
    }

    private Transform GetSelectedTargetTransform(SelectableDrop sel)
    {
        if (sel == null)
            return null;

        Rigidbody rb = sel.GetComponent<Rigidbody>();

        if (rb == null)
            rb = sel.GetComponentInParent<Rigidbody>();

        if (rb == null)
            rb = sel.GetComponentInChildren<Rigidbody>();

        return rb != null ? rb.transform : sel.transform;
    }

    private bool IsInsideField(Vector3 worldPos)
    {
        if (fieldBox == null)
            return false;

        return fieldBox.bounds.Contains(worldPos);
    }

    private void UpdateGravityArrow()
    {
        if (gravityArrow == null)
            return;

        gravityArrow.localPosition = gravityOffset;
        SetArrowLengthAndDirection(gravityArrow, gravityLength, Vector3.down);
        gravityArrow.gameObject.SetActive(true);
    }

    private void UpdateBuoyancyArrow()
    {
        if (buoyancyArrow == null)
            return;

        if (!showBuoyancyArrow)
        {
            buoyancyArrow.gameObject.SetActive(false);
            return;
        }

        buoyancyArrow.localPosition = buoyancyOffset;
        SetArrowLengthAndDirection(buoyancyArrow, buoyancyLength, Vector3.up);
        buoyancyArrow.gameObject.SetActive(true);
    }

    private void UpdateElectricArrow(SelectableDrop sel)
    {
        if (electricArrow == null)
            return;

        float currentVoltage = voltageSource != null ? Mathf.Abs(voltageSource.CurrentVoltage) : 0f;

        if (hideElectricWhenVoltageZero && currentVoltage <= minVoltageToShowElectric)
        {
            electricArrow.gameObject.SetActive(false);
            return;
        }

        float hoverVoltage = GetHoverVoltage(sel);

        if (hoverVoltage <= 1e-6f)
        {
            electricArrow.gameObject.SetActive(false);
            return;
        }

        float ratio = currentVoltage / hoverVoltage;

        // PDF simplified visualization:
        // at hover voltage, electric arrow reaches gravity arrow length.
        float electricLength = gravityLength * ratio;
        electricLength = Mathf.Clamp(electricLength, electricMinLength, electricMaxLength);

        electricArrow.localPosition = electricOffset;
        SetArrowLengthAndDirection(electricArrow, electricLength, Vector3.up);
        electricArrow.gameObject.SetActive(true);

        if (logDebug)
        {
            Debug.Log(
                "[ForceOverlay] U=" + currentVoltage.ToString("0.0") +
                " V, U_hover=" + hoverVoltage.ToString("0.0") +
                " V, electricLength=" + electricLength.ToString("0.000")
            );
        }
    }

    private float GetHoverVoltage(SelectableDrop sel)
    {
        if (sel == null || fieldVolume == null)
            return 0f;

        DropProperties dp = FindDropProperties(sel);

        if (dp == null)
            return 0f;

        float m = Mathf.Max(1e-18f, dp.MassKg);
        float q = Mathf.Abs(dp.ChargeC);

        if (q < 1e-20f)
            return 0f;

        float d = fieldVolume.GetPlateSpacingMeters();

        if (d <= 1e-6f)
            return 0f;

        Vector3 dir = fieldVolume.fieldDirection.sqrMagnitude > 1e-6f
            ? fieldVolume.fieldDirection.normalized
            : Vector3.up;

        Vector3 g = GetGravityVector(sel);

        float gAlong = Mathf.Abs(Vector3.Dot(g, dir));
        float scale = Mathf.Max(1e-6f, fieldVolume.fieldScale);

        // PDF formula:
        // U_hover = m * g * d / q
        return (m * gAlong * d) / (q * scale);
    }

    private Vector3 GetGravityVector(SelectableDrop sel)
    {
        Vector3 gravity = Physics.gravity;

        if (sel == null)
            return gravity;

        Rigidbody rb = sel.GetComponent<Rigidbody>();

        if (rb == null)
            rb = sel.GetComponentInParent<Rigidbody>();

        if (rb == null)
            rb = sel.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            OilDrop oilDrop = rb.GetComponent<OilDrop>();

            if (oilDrop != null)
                gravity = oilDrop.customGravity;
        }

        return gravity;
    }

    private DropProperties FindDropProperties(SelectableDrop sel)
    {
        if (sel == null)
            return null;

        DropProperties dp = sel.GetComponent<DropProperties>();

        if (dp == null)
            dp = sel.GetComponentInParent<DropProperties>();

        if (dp == null)
            dp = sel.GetComponentInChildren<DropProperties>();

        return dp;
    }

    private void SetArrowLengthAndDirection(Transform arrow, float length, Vector3 direction)
    {
        if (arrow == null)
            return;

        arrow.right = -direction.normalized;

        Vector3 scale = arrow.localScale;
        scale.x = length;
        arrow.localScale = scale;
    }

    private void HideAll()
    {
        if (gravityArrow != null)
            gravityArrow.gameObject.SetActive(false);

        if (buoyancyArrow != null)
            buoyancyArrow.gameObject.SetActive(false);

        if (electricArrow != null)
            electricArrow.gameObject.SetActive(false);
    }
}