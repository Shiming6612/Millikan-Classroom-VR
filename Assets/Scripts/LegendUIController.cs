using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LegendUIController : MonoBehaviour
{
    public DropSelectionManager selectionManager;
    public VoltageKnobInput voltageSource;
    public ElectricFieldVolume fieldVolume;

    public CanvasGroup panelGroup;
    public TMP_Text titleText;
    public TMP_Text massText;
    public TMP_Text chargeText;
    public TMP_Text voltageText;
    public TMP_Text hintText;

    public float toleranceV = 5f;
    public Color correctColor = Color.green;
    public float correctFontSizeMultiplier = 1.25f;
    public AudioSource correctSfxSource;
    public AudioClip correctSfx;

    public bool logDebug;

    private bool wasCorrect;
    private Color baseColor;
    private float baseSize;
    private FontStyles baseStyle;
    private bool cached;

    private SelectableDrop lastSelected;
    private readonly Dictionary<SelectableDrop, int> runtimeIds = new Dictionary<SelectableDrop, int>();
    private int nextRuntimeId = 1;

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
        SelectableDrop sel = selectionManager ? selectionManager.CurrentSelected : null;

        if (sel != lastSelected)
        {
            lastSelected = sel;
            RefreshAll();
        }

        RefreshVoltage(sel);
    }

    private void HandleSelectionChanged(SelectableDrop sel)
    {
        if (logDebug)
            Debug.Log(sel != null
                ? $"[LegendUI] OnSelectionChanged -> {sel.name}"
                : "[LegendUI] OnSelectionChanged -> None");

        lastSelected = sel;
        RefreshAll();
    }

    private void RefreshAll()
    {
        SelectableDrop sel = selectionManager ? selectionManager.CurrentSelected : null;
        SetPanel(true);

        if (sel == null)
        {
            if (titleText) titleText.text = "Drop--";
            if (massText) massText.text = "Mass: --";
            if (chargeText) chargeText.text = "Charge: --";
            if (hintText) hintText.text = "";
            wasCorrect = false;
            RestoreStyle();
            RefreshVoltage(null);
            return;
        }

        if (titleText)
        {
            int displayId = GetDisplayId(sel);
            titleText.text = displayId > 0 ? $"Drop{displayId:00}" : "Drop--";
        }

        DropProperties dp = FindDropProperties(sel);

        if (dp != null)
        {
            if (massText) massText.text = $"Mass: {dp.MassKg * 1e15f:0.000} pg";
            if (chargeText) chargeText.text = $"Charge: {dp.ChargeMultiple} e";
        }
        else
        {
            if (massText) massText.text = "Mass: --";
            if (chargeText) chargeText.text = "Charge: --";
        }

        wasCorrect = false;
        RestoreStyle();
        RefreshVoltage(sel);
    }

    private void RefreshVoltage(SelectableDrop sel)
    {
        CacheBaseStyle();

        float v = voltageSource ? voltageSource.CurrentVoltage : 0f;
        if (fieldVolume != null && fieldVolume.invertVoltage) v = -v;

        if (voltageText)
            voltageText.text = voltageSource ? $"Voltage: {v:0.0} V" : "Voltage: --";

        float hoverV = 0f;
        bool can = sel != null && TryHoverVoltage(sel, out hoverV);
        bool correct = can && voltageSource != null && Mathf.Abs(Mathf.Abs(v) - hoverV) <= toleranceV;

        if (correct) ApplyCorrectStyle();
        else RestoreStyle();

        if (correct && !wasCorrect && correctSfxSource != null && correctSfx != null)
            correctSfxSource.PlayOneShot(correctSfx);

        wasCorrect = correct;

        if (hintText)
        {
            if (!can || voltageSource == null) hintText.text = "";
            else if (Mathf.Abs(v) > hoverV + toleranceV) hintText.text = "State: Rise";
            else if (Mathf.Abs(v) < hoverV - toleranceV) hintText.text = "State: Fall";
            else hintText.text = "State: Hover";
        }
    }

    private bool TryHoverVoltage(SelectableDrop sel, out float hoverV)
    {
        hoverV = 0f;
        if (fieldVolume == null) return false;

        DropProperties dp = FindDropProperties(sel);
        if (dp == null) return false;

        float m = Mathf.Max(1e-18f, dp.MassKg);
        float q = Mathf.Abs(dp.ChargeC);
        if (q < 1e-20f) return false;

        float d = fieldVolume.GetPlateSpacingMeters();
        if (d <= 1e-6f) return false;

        Vector3 dir = fieldVolume.fieldDirection.sqrMagnitude > 1e-6f ? fieldVolume.fieldDirection.normalized : Vector3.up;
        Vector3 g = Physics.gravity;

        Rigidbody rb = sel.GetComponent<Rigidbody>();
        if (rb == null) rb = sel.GetComponentInParent<Rigidbody>();
        if (rb == null) rb = sel.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            var od = rb.GetComponent<OilDrop>();
            if (od != null) g = od.customGravity;
        }

        float gAlong = Mathf.Abs(Vector3.Dot(g, dir));
        float scale = Mathf.Max(1e-6f, fieldVolume.fieldScale);

        hoverV = (m * gAlong * d) / (q * scale);
        return true;
    }

    private DropProperties FindDropProperties(SelectableDrop sel)
    {
        if (sel == null) return null;

        DropProperties dp = sel.GetComponent<DropProperties>();
        if (dp == null) dp = sel.GetComponentInParent<DropProperties>();
        if (dp == null) dp = sel.GetComponentInChildren<DropProperties>();

        return dp;
    }

    private int GetDisplayId(SelectableDrop sel)
    {
        if (sel == null) return -1;

        if (sel.dropId >= 0)
            return sel.dropId + 1;

        if (runtimeIds.TryGetValue(sel, out int id))
            return id;

        id = nextRuntimeId++;
        runtimeIds[sel] = id;
        return id;
    }

    private void SetPanel(bool on)
    {
        if (panelGroup == null) return;
        panelGroup.alpha = on ? 1f : 0f;
        panelGroup.interactable = on;
        panelGroup.blocksRaycasts = on;
    }

    private void CacheBaseStyle()
    {
        if (cached || voltageText == null) return;
        baseColor = voltageText.color;
        baseSize = voltageText.fontSize;
        baseStyle = voltageText.fontStyle;
        cached = true;
    }

    private void ApplyCorrectStyle()
    {
        if (voltageText == null) return;
        voltageText.color = correctColor;
        voltageText.fontStyle = baseStyle | FontStyles.Bold;
        voltageText.fontSize = baseSize * Mathf.Max(1f, correctFontSizeMultiplier);
    }

    private void RestoreStyle()
    {
        if (voltageText == null || !cached) return;
        voltageText.color = baseColor;
        voltageText.fontStyle = baseStyle;
        voltageText.fontSize = baseSize;
    }
}