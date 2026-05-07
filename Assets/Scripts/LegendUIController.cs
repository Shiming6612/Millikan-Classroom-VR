using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LegendUIController : MonoBehaviour
{
    [Header("Refs")]
    public DropSelectionManager selectionManager;
    public VoltageKnobInput voltageSource;
    public ElectricFieldVolume fieldVolume;

    [Header("UI")]
    public CanvasGroup panelGroup;
    public TMP_Text titleText;
    public TMP_Text massText;
    public TMP_Text chargeText;
    public TMP_Text radiusText;
    public TMP_Text voltageText;
    public TMP_Text hintText;

    [Header("Correct State")]
    public float toleranceV = 1f;
    public Color correctColor = Color.green;
    public float correctFontSizeMultiplier = 1.25f;
    public AudioSource correctSfxSource;
    public AudioClip correctSfx;

    [Header("Tutorial Completion")]
    public float tutorialHoldCorrectSeconds = 1f;

    [Header("Debug")]
    public bool logDebug;

    private bool wasCorrect;
    private bool tutorialSolvedSent;
    private float correctHoldTimer;

    private Color baseColor;
    private float baseSize;
    private FontStyles baseStyle;
    private bool cached;

    private SelectableDrop lastSelected;
    private readonly Dictionary<SelectableDrop, int> runtimeIds = new Dictionary<SelectableDrop, int>();
    private int nextRuntimeId = 1;

    private BottomTutorialController tutorialController;

    private void Awake()
    {
        tutorialController = FindFirstObjectByType<BottomTutorialController>();
    }

    private void OnEnable()
    {
        if (selectionManager != null)
            selectionManager.OnSelectionChanged += HandleSelectionChanged;

        CacheBaseStyle();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (selectionManager != null)
            selectionManager.OnSelectionChanged -= HandleSelectionChanged;
    }

    private void Update()
    {
        SelectableDrop selectedDrop = selectionManager != null ? selectionManager.CurrentSelected : null;

        if (selectedDrop != lastSelected)
        {
            lastSelected = selectedDrop;
            RefreshAll();
        }

        RefreshVoltage(selectedDrop);
    }

    private void HandleSelectionChanged(SelectableDrop selectedDrop)
    {
        lastSelected = selectedDrop;
        RefreshAll();
    }

    private void RefreshAll()
    {
        SelectableDrop selectedDrop = selectionManager != null ? selectionManager.CurrentSelected : null;

        SetPanel(true);

        tutorialSolvedSent = false;
        correctHoldTimer = 0f;

        if (selectedDrop == null)
        {
            if (titleText != null) titleText.text = "Drop--";
            if (massText != null) massText.text = "Mass: --";
            if (chargeText != null) chargeText.text = "Charge: --";
            if (radiusText != null) radiusText.text = "Radius: --";
            if (hintText != null) hintText.text = "";

            wasCorrect = false;
            RestoreStyle();
            RefreshVoltage(null);
            return;
        }

        if (titleText != null)
        {
            int displayId = GetDisplayId(selectedDrop);
            titleText.text = displayId > 0 ? "Drop" + displayId.ToString("00") : "Drop--";
        }

        DropProperties dropProperties = FindDropProperties(selectedDrop);

        if (dropProperties != null)
        {
            if (massText != null)
                massText.text = "Mass: " + (dropProperties.MassKg * 1e15f).ToString("0.000") + " pg";

            if (chargeText != null)
                chargeText.text = "Charge: " + dropProperties.ChargeMultiple + " e";

            if (radiusText != null)
                radiusText.text = "Radius: " + dropProperties.RadiusMicrometer.ToString("0.00") + " µm";
        }
        else
        {
            if (massText != null) massText.text = "Mass: --";
            if (chargeText != null) chargeText.text = "Charge: --";
            if (radiusText != null) radiusText.text = "Radius: --";
        }

        wasCorrect = false;
        RestoreStyle();
        RefreshVoltage(selectedDrop);
    }

    private void RefreshVoltage(SelectableDrop selectedDrop)
    {
        CacheBaseStyle();

        float currentVoltage = voltageSource != null ? voltageSource.CurrentVoltage : 0f;

        if (fieldVolume != null && fieldVolume.invertVoltage)
            currentVoltage = -currentVoltage;

        float roundedCurrentVoltage = RoundToOneDecimal(Mathf.Abs(currentVoltage));

        float hoverVoltage = 0f;
        bool canCalculateHoverVoltage = selectedDrop != null && TryHoverVoltage(selectedDrop, out hoverVoltage);

        float roundedHoverVoltage = RoundToOneDecimal(hoverVoltage);

        if (voltageText != null)
        {
            voltageText.text = voltageSource != null
                ? "Voltage: " + roundedCurrentVoltage.ToString("0.0") + " V"
                : "Voltage: --";
        }

        bool correct =
            canCalculateHoverVoltage &&
            voltageSource != null &&
            Mathf.Abs(roundedCurrentVoltage - roundedHoverVoltage) <= toleranceV;

        if (correct)
            ApplyCorrectStyle();
        else
            RestoreStyle();

        if (correct && !wasCorrect)
        {
            if (correctSfxSource != null && correctSfx != null)
                correctSfxSource.PlayOneShot(correctSfx);

            if (logDebug)
            {
                Debug.Log(
                    "[LegendUI] Correct voltage. Current=" +
                    roundedCurrentVoltage.ToString("0.0") +
                    " V, Hover=" +
                    roundedHoverVoltage.ToString("0.0") +
                    " V"
                );
            }
        }

        if (correct)
        {
            correctHoldTimer += Time.deltaTime;

            if (!tutorialSolvedSent && correctHoldTimer >= tutorialHoldCorrectSeconds)
            {
                if (tutorialController == null)
                    tutorialController = FindFirstObjectByType<BottomTutorialController>();

                if (tutorialController != null)
                    tutorialController.NotifyVoltageSolved();

                tutorialSolvedSent = true;
            }
        }
        else
        {
            correctHoldTimer = 0f;
            tutorialSolvedSent = false;
        }

        wasCorrect = correct;

        if (hintText != null)
        {
            if (!canCalculateHoverVoltage || voltageSource == null)
            {
                hintText.text = "";
            }
            else if (roundedCurrentVoltage > roundedHoverVoltage + toleranceV)
            {
                hintText.text = "Status: Steigt";
            }
            else if (roundedCurrentVoltage < roundedHoverVoltage - toleranceV)
            {
                hintText.text = "Status: Fällt";
            }
            else
            {
                hintText.text = "Status: Schwebt";
            }
        }
    }

    private bool TryHoverVoltage(SelectableDrop selectedDrop, out float hoverVoltage)
    {
        hoverVoltage = 0f;

        if (fieldVolume == null)
            return false;

        DropProperties dropProperties = FindDropProperties(selectedDrop);

        if (dropProperties == null)
            return false;

        float mass = Mathf.Max(1e-18f, dropProperties.MassKg);
        float charge = Mathf.Abs(dropProperties.ChargeC);

        if (charge < 1e-20f)
            return false;

        float plateSpacing = fieldVolume.GetPlateSpacingMeters();

        if (plateSpacing <= 1e-6f)
            return false;

        Vector3 fieldDirection =
            fieldVolume.fieldDirection.sqrMagnitude > 1e-6f
                ? fieldVolume.fieldDirection.normalized
                : Vector3.up;

        Vector3 gravity = GetGravityVector(selectedDrop);

        float gravityAlongField = Mathf.Abs(Vector3.Dot(gravity, fieldDirection));

        if (gravityAlongField <= 1e-6f)
            return false;

        float fieldScale = Mathf.Max(1e-6f, fieldVolume.fieldScale);

        // PDF formula:
        // F_el = F_G
        // q * U / d = m * g
        // U = m * g * d / q
        hoverVoltage = (mass * gravityAlongField * plateSpacing) / (charge * fieldScale);

        return hoverVoltage > 0f;
    }

    private Vector3 GetGravityVector(SelectableDrop selectedDrop)
    {
        Vector3 gravity = Physics.gravity;

        if (selectedDrop == null)
            return gravity;

        Rigidbody rb = selectedDrop.GetComponent<Rigidbody>();

        if (rb == null)
            rb = selectedDrop.GetComponentInParent<Rigidbody>();

        if (rb == null)
            rb = selectedDrop.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            OilDrop oilDrop = rb.GetComponent<OilDrop>();

            if (oilDrop != null)
                gravity = oilDrop.customGravity;
        }

        return gravity;
    }

    private DropProperties FindDropProperties(SelectableDrop selectedDrop)
    {
        if (selectedDrop == null)
            return null;

        DropProperties dropProperties = selectedDrop.GetComponent<DropProperties>();

        if (dropProperties == null)
            dropProperties = selectedDrop.GetComponentInParent<DropProperties>();

        if (dropProperties == null)
            dropProperties = selectedDrop.GetComponentInChildren<DropProperties>();

        return dropProperties;
    }

    private int GetDisplayId(SelectableDrop selectedDrop)
    {
        if (selectedDrop == null)
            return -1;

        if (selectedDrop.dropId >= 0)
            return selectedDrop.dropId + 1;

        if (runtimeIds.TryGetValue(selectedDrop, out int id))
            return id;

        id = nextRuntimeId++;
        runtimeIds[selectedDrop] = id;

        return id;
    }

    private float RoundToOneDecimal(float value)
    {
        return Mathf.Round(value * 10f) / 10f;
    }

    private void SetPanel(bool on)
    {
        if (panelGroup == null)
            return;

        panelGroup.alpha = on ? 1f : 0f;
        panelGroup.interactable = on;
        panelGroup.blocksRaycasts = on;
    }

    private void CacheBaseStyle()
    {
        if (cached || voltageText == null)
            return;

        baseColor = voltageText.color;
        baseSize = voltageText.fontSize;
        baseStyle = voltageText.fontStyle;
        cached = true;
    }

    private void ApplyCorrectStyle()
    {
        if (voltageText == null)
            return;

        voltageText.color = correctColor;
        voltageText.fontStyle = baseStyle | FontStyles.Bold;
        voltageText.fontSize = baseSize * Mathf.Max(1f, correctFontSizeMultiplier);
    }

    private void RestoreStyle()
    {
        if (voltageText == null || !cached)
            return;

        voltageText.color = baseColor;
        voltageText.fontStyle = baseStyle;
        voltageText.fontSize = baseSize;
    }
}