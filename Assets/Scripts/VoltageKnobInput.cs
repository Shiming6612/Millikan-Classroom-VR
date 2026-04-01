using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Oculus.Interaction;
using UnityEngine.XR;

public class VoltageKnobInput : MonoBehaviour
{
    [Header("UI")]
    public Slider voltageSlider;
    public TMP_Text voltageText;

    [Header("Knob Setup")]
    public Transform knobTransform;
    public Transform pivot;
    public GrabInteractable grabInteractable;

    [Header("Hands / Controllers (optional manual refs)")]
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("Auto Find Interactor Transforms")]
    public bool autoFindInteractorTransforms = true;
    public string[] interactorNameHints = new string[]
    {
        "GrabbingCollider",
        "ControllerGrabLocation",
        "LeftHandAnchor",
        "RightHandAnchor"
    };

    [Header("Voltage Range (V)")]
    public float minVoltage = 0f;
    public float maxVoltage = 800f;
    public float startVoltage = 0f;

    [Header("Input Mapping")]
    public Vector3 knobLocalAxis = new Vector3(0, 0, 1);
    public float degreesForFullRange = 360f;
    public bool invertDirection = false;

    [Header("Fine Tune (hold X on left controller)")]
    public bool enableFineTune = true;
    public float fineTuneMultiplier = 3f;

    [Header("Stability")]
    public float deadzoneDegrees = 3f;
    public float smoothing = 12f;

    [Header("Grab Gating")]
    public float maxGrabDistance = 2f;

    [Header("Snapping")]
    public bool snapToStep = true;
    public float voltageStep = 1f;

    [Header("Visual Highlight (grab only)")]
    public Renderer[] targetRenderers;
    public bool useEmissionHighlight = true;
    public Color grabbedColor = Color.yellow;
    public float grabbedEmissionIntensity = 2.5f;

    [Header("Visual Rotation")]
    public Transform visualRoot;
    public Vector3 visualLocalAxis = new Vector3(0, 0, 1);
    public float visualMinAngle = 0f;
    public float visualMaxAngle = 180f;

    [Header("Debug")]
    public bool debugLogs = false;

    public float CurrentVoltage { get; private set; }

    private bool grabbed;
    private Transform activeController;
    private float grabStartVoltage;

    private Vector3 axisWorld;
    private Vector3 prevVectorOnPlane;
    private bool hasPrevVector;
    private float accumDeg;

    private readonly List<Transform> cachedInteractors = new List<Transform>();

    private Material[] runtimeMats;
    private Color[] baseColors;
    private Color[] baseSpriteColors;
    private bool[] hasEmissionProps;
    private bool[] isSpriteRenderer;

    private Quaternion visualInitialLocalRotation;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Start()
    {
        if (knobTransform == null)
            knobTransform = transform;

        if (pivot == null)
            pivot = knobTransform;

        if (visualRoot == null)
            visualRoot = knobTransform;

        visualInitialLocalRotation = visualRoot.localRotation;

        SetVoltage(startVoltage, true);

        grabbed = false;
        activeController = null;
        hasPrevVector = false;
        accumDeg = 0f;

        RefreshInteractorCache();
        InitMaterials();
        RefreshVisualState();
        ApplyVisualRotationFromVoltage();
    }

    private void Update()
    {
        if (!grabbed || activeController == null)
            return;

        axisWorld = knobTransform.TransformDirection(knobLocalAxis.normalized);

        Vector3 currentVec = Vector3.ProjectOnPlane(activeController.position - pivot.position, axisWorld);

        if (currentVec.sqrMagnitude < 1e-6f)
            currentVec = Vector3.ProjectOnPlane(activeController.forward, axisWorld);

        if (currentVec.sqrMagnitude < 1e-6f)
            return;

        currentVec.Normalize();

        if (!hasPrevVector)
        {
            prevVectorOnPlane = currentVec;
            hasPrevVector = true;
            return;
        }

        float delta = Vector3.SignedAngle(prevVectorOnPlane, currentVec, axisWorld);

        if (Mathf.Abs(delta) < deadzoneDegrees)
            delta = 0f;

        accumDeg += delta;
        prevVectorOnPlane = currentVec;

        float usedDeg = invertDirection ? -accumDeg : accumDeg;

        float effectiveDegreesForFullRange = degreesForFullRange;
        if (enableFineTune && IsLeftXHeld())
            effectiveDegreesForFullRange *= Mathf.Max(1f, fineTuneMultiplier);

        float range = maxVoltage - minVoltage;
        float denom = Mathf.Max(1e-3f, effectiveDegreesForFullRange);
        float targetVoltage = grabStartVoltage + (usedDeg / denom) * range;

        float clampedVoltage = Mathf.Clamp(targetVoltage, minVoltage, maxVoltage);
        if (!Mathf.Approximately(clampedVoltage, targetVoltage))
        {
            targetVoltage = clampedVoltage;
            float t = (targetVoltage - grabStartVoltage) / Mathf.Max(1e-6f, range);
            usedDeg = t * denom;
            accumDeg = invertDirection ? -usedDeg : usedDeg;
        }

        float alpha = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        float smoothVoltage = Mathf.Lerp(CurrentVoltage, targetVoltage, alpha);

        SetVoltage(smoothVoltage);

        if (debugLogs)
            Debug.Log($"[VoltageKnobInput] grabbed={grabbed}, fineTune={IsLeftXHeld()}, accumDeg={accumDeg:F2}, V={CurrentVoltage:F0}");
    }

    public void BeginGrab()
    {
        RefreshInteractorCache();

        Transform chosen = FindSelectingInteractorTransformFromGrab();
        if (chosen == null)
            chosen = ChooseNearestController();

        if (chosen == null)
        {
            if (debugLogs)
                Debug.LogWarning("[VoltageKnobInput] BeginGrab failed: no valid interactor found.");
            return;
        }

        activeController = chosen;
        axisWorld = knobTransform.TransformDirection(knobLocalAxis.normalized);

        Vector3 startVec = Vector3.ProjectOnPlane(activeController.position - pivot.position, axisWorld);
        if (startVec.sqrMagnitude < 1e-6f)
            startVec = Vector3.ProjectOnPlane(activeController.forward, axisWorld);

        if (startVec.sqrMagnitude < 1e-6f)
        {
            if (debugLogs)
                Debug.LogWarning("[VoltageKnobInput] BeginGrab failed: interactor vector too small.");
            activeController = null;
            return;
        }

        prevVectorOnPlane = startVec.normalized;
        hasPrevVector = true;
        accumDeg = 0f;

        grabStartVoltage = CurrentVoltage;
        grabbed = true;

        RefreshVisualState();

        if (debugLogs)
            Debug.Log($"[VoltageKnobInput] BeginGrab with {activeController.name}");
    }

    public void EndGrab()
    {
        if (debugLogs && activeController != null)
            Debug.Log($"[VoltageKnobInput] EndGrab from {activeController.name}");

        grabbed = false;
        activeController = null;
        hasPrevVector = false;

        SetVoltage(CurrentVoltage, true);
        RefreshVisualState();
    }

    private Transform FindSelectingInteractorTransformFromGrab()
    {
        if (grabInteractable == null)
            return null;

        var views = grabInteractable.SelectingInteractorViews;
        if (views == null)
            return null;

        foreach (var v in views)
            if (v is Component c) return c.transform;

        return null;
    }

    private void RefreshInteractorCache()
    {
        cachedInteractors.Clear();

        TryAddInteractor(leftControllerTransform);
        TryAddInteractor(rightControllerTransform);

        if (autoFindInteractorTransforms)
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);

            foreach (Transform t in all)
            {
                if (t == null) continue;

                for (int i = 0; i < interactorNameHints.Length; i++)
                {
                    string hint = interactorNameHints[i];
                    if (string.IsNullOrWhiteSpace(hint)) continue;

                    if (t.name.Contains(hint))
                    {
                        TryAddInteractor(t);
                        break;
                    }
                }
            }
        }
    }

    private void TryAddInteractor(Transform t)
    {
        if (t == null) return;
        if (cachedInteractors.Contains(t)) return;
        cachedInteractors.Add(t);
    }

    private Transform ChooseNearestController()
    {
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var t in cachedInteractors)
        {
            if (t == null) continue;

            float d = Vector3.Distance(t.position, transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return best;
    }

    private bool IsLeftXHeld()
    {
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!left.isValid) return false;

        bool value;
        return left.TryGetFeatureValue(CommonUsages.primaryButton, out value) && value;
    }

    private void SetVoltage(float v, bool forceUI = false)
    {
        float clamped = Mathf.Clamp(v, minVoltage, maxVoltage);

        if (snapToStep && voltageStep > 0f)
        {
            clamped = Mathf.Round(clamped / voltageStep) * voltageStep;
            clamped = Mathf.Clamp(clamped, minVoltage, maxVoltage);
        }

        float edgeSnapThreshold = Mathf.Max(voltageStep * 5f, 5f);

        if (clamped <= minVoltage + edgeSnapThreshold)
            clamped = minVoltage;
        else if (clamped >= maxVoltage - edgeSnapThreshold)
            clamped = maxVoltage;

        if (!forceUI && Mathf.Approximately(clamped, CurrentVoltage))
            return;

        CurrentVoltage = clamped;

        if (voltageSlider != null)
        {
            voltageSlider.minValue = minVoltage;
            voltageSlider.maxValue = maxVoltage;
            voltageSlider.value = CurrentVoltage;
        }

        if (voltageText != null)
            voltageText.text = $"{CurrentVoltage:0.0} V";

        ApplyVisualRotationFromVoltage();
    }

    private void ApplyVisualRotationFromVoltage()
    {
        if (visualRoot == null) return;

        float t = Mathf.InverseLerp(minVoltage, maxVoltage, CurrentVoltage);
        float angle = Mathf.Lerp(visualMinAngle, visualMaxAngle, t);

        visualRoot.localRotation =
            visualInitialLocalRotation *
            Quaternion.AngleAxis(angle, visualLocalAxis.normalized);
    }

    private void InitMaterials()
    {
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        runtimeMats = new Material[targetRenderers.Length];
        baseColors = new Color[targetRenderers.Length];
        baseSpriteColors = new Color[targetRenderers.Length];
        hasEmissionProps = new bool[targetRenderers.Length];
        isSpriteRenderer = new bool[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;

            runtimeMats[i] = targetRenderers[i].material;
            isSpriteRenderer[i] = targetRenderers[i] is SpriteRenderer;

            if (runtimeMats[i].HasProperty(BaseColorID))
                baseColors[i] = runtimeMats[i].GetColor(BaseColorID);
            else if (runtimeMats[i].HasProperty(ColorID))
                baseColors[i] = runtimeMats[i].GetColor(ColorID);
            else
                baseColors[i] = Color.white;

            if (isSpriteRenderer[i])
            {
                SpriteRenderer sr = targetRenderers[i] as SpriteRenderer;
                baseSpriteColors[i] = sr != null ? sr.color : Color.white;
            }
            else
            {
                baseSpriteColors[i] = Color.white;
            }

            hasEmissionProps[i] = runtimeMats[i].HasProperty(EmissionColorID);
        }
    }

    private void RefreshVisualState()
    {
        if (grabbed) ApplyHighlight(grabbedColor, grabbedEmissionIntensity);
        else RestoreBaseLook();
    }

    private void ApplyHighlight(Color color, float emissionIntensity)
    {
        if (runtimeMats == null) return;

        for (int i = 0; i < runtimeMats.Length; i++)
        {
            if (runtimeMats[i] == null || targetRenderers[i] == null) continue;

            if (isSpriteRenderer[i])
            {
                SpriteRenderer sr = targetRenderers[i] as SpriteRenderer;
                if (sr != null) sr.color = color;
            }
            else
            {
                if (runtimeMats[i].HasProperty(BaseColorID))
                    runtimeMats[i].SetColor(BaseColorID, color);
                else if (runtimeMats[i].HasProperty(ColorID))
                    runtimeMats[i].SetColor(ColorID, color);
            }

            if (useEmissionHighlight && hasEmissionProps[i])
            {
                runtimeMats[i].EnableKeyword("_EMISSION");
                runtimeMats[i].SetColor(EmissionColorID, color * emissionIntensity);
            }
        }
    }

    private void RestoreBaseLook()
    {
        if (runtimeMats == null) return;

        for (int i = 0; i < runtimeMats.Length; i++)
        {
            if (runtimeMats[i] == null || targetRenderers[i] == null) continue;

            if (isSpriteRenderer[i])
            {
                SpriteRenderer sr = targetRenderers[i] as SpriteRenderer;
                if (sr != null) sr.color = baseSpriteColors[i];
            }
            else
            {
                if (runtimeMats[i].HasProperty(BaseColorID))
                    runtimeMats[i].SetColor(BaseColorID, baseColors[i]);
                else if (runtimeMats[i].HasProperty(ColorID))
                    runtimeMats[i].SetColor(ColorID, baseColors[i]);
            }

            if (hasEmissionProps[i])
                runtimeMats[i].SetColor(EmissionColorID, Color.black);
        }
    }
}