using System;
using UnityEngine;
using UnityEngine.XR;

public class DropSelectionManager : MonoBehaviour
{
    [Header("Ray Setup")]
    public Transform rayOrigin;
    public float rayLength = 10f;
    public LayerMask oilDropLayerMask = ~0;
    public float sphereCastRadius = 0.03f;
    public bool enableHoverHighlight = true;

    [Header("Ray Visual")]
    public bool showLine = true;
    public LineRenderer line;
    public Color rayNormalColor = Color.white;
    public Color rayHitColor = Color.yellow;

    [Header("Input")]
    public bool useXRTriggerInput = true;
    public bool allowPrimaryButtonFallback = false;
    public XRNode triggerHand = XRNode.RightHand;

    [Header("Ray Transform")]
    public Vector3 rayLocalOffset = Vector3.zero;
    public Vector3 rayLocalDirection = Vector3.forward;

    [Header("Runtime State")]
    public bool selectionEnabled = false;

    [Header("Debug")]
    public bool logHits = false;
    public bool logSelection = true;
    public bool logInput = false;

    public SelectableDrop CurrentSelected => _selected;
    public event Action<SelectableDrop> OnSelectionChanged;

    private SelectableDrop _selected;
    private SelectableDrop _hovered;

    private bool _prevTriggerPressed;
    private bool _prevPrimaryPressed;

    private void Start()
    {
        RefreshLineVisibility(false);
    }

    private void Update()
    {
        if (!selectionEnabled)
        {
            if (_hovered != null)
            {
                _hovered.SetHovered(false);
                _hovered = null;
            }

            RefreshLineVisibility(false);
            return;
        }

        if (rayOrigin == null)
        {
            RefreshLineVisibility(false);
            return;
        }

        Vector3 origin = rayOrigin.TransformPoint(rayLocalOffset);
        Vector3 dir = rayOrigin.TransformDirection(rayLocalDirection.normalized);
        Ray ray = new Ray(origin, dir);

        bool hitSomething = Physics.SphereCast(
            ray,
            sphereCastRadius,
            out RaycastHit hit,
            rayLength,
            oilDropLayerMask,
            QueryTriggerInteraction.Ignore
        );

        SelectableDrop hitDrop = null;
        if (hitSomething && hit.collider != null)
            hitDrop = hit.collider.GetComponentInParent<SelectableDrop>();

        if (logHits)
            Debug.Log(hitDrop != null ? $"[DropSelection] Hit {hitDrop.name}" : "[DropSelection] No hit");

        UpdateHover(hitDrop);
        UpdateLine(ray, hitSomething, hit);

        if (GetSelectDown() && hitDrop != null)
        {
            if (logSelection)
                Debug.Log($"[DropSelection] Confirm select: {hitDrop.name}");

            SetSelected(hitDrop);
        }
    }

    public void SetSelectionEnabled(bool enabled)
    {
        if (selectionEnabled == enabled)
            return;

        selectionEnabled = enabled;

        if (!selectionEnabled)
        {
            ClearSelectionAndHover();
            RefreshLineVisibility(false);

            if (logSelection)
                Debug.Log("[DropSelection] Selection ray disabled.");
        }
        else
        {
            RefreshLineVisibility(showLine);

            if (logSelection)
                Debug.Log("[DropSelection] Selection ray enabled.");
        }
    }

    public void ClearSelectionAndHover()
    {
        if (_hovered != null)
        {
            _hovered.SetHovered(false);
            _hovered = null;
        }

        SetSelected(null);
    }

    private void UpdateHover(SelectableDrop hitDrop)
    {
        if (!enableHoverHighlight)
        {
            if (_hovered != null) _hovered.SetHovered(false);
            _hovered = null;
            return;
        }

        if (_hovered == hitDrop) return;

        if (_hovered != null) _hovered.SetHovered(false);
        _hovered = hitDrop;
        if (_hovered != null) _hovered.SetHovered(true);
    }

    private void UpdateLine(Ray ray, bool hitSomething, RaycastHit hit)
    {
        if (line == null) return;

        if (!showLine || !selectionEnabled)
        {
            line.enabled = false;
            return;
        }

        line.enabled = true;
        line.useWorldSpace = true;
        line.positionCount = 2;

        Vector3 end = ray.origin + ray.direction * rayLength;
        if (hitSomething) end = ray.origin + ray.direction * hit.distance;

        line.SetPosition(0, ray.origin);
        line.SetPosition(1, end);

        Color c = hitSomething ? rayHitColor : rayNormalColor;
        line.startColor = c;
        line.endColor = c;
    }

    private void RefreshLineVisibility(bool visible)
    {
        if (line != null)
            line.enabled = visible && selectionEnabled && showLine;
    }

    public void SetSelected(SelectableDrop newSelected)
    {
        if (_selected == newSelected) return;

        if (_selected != null) _selected.SetSelected(false);

        _selected = newSelected;

        if (_selected != null) _selected.SetSelected(true);

        if (logSelection)
            Debug.Log(_selected != null ? $"[DropSelection] Selected: {_selected.name}" : "[DropSelection] Selected: None");

        OnSelectionChanged?.Invoke(_selected);

        if (_selected != null)
        {
            BottomTutorialController tutorial = FindFirstObjectByType<BottomTutorialController>();
            if (tutorial != null)
            {
                tutorial.NotifyDropSelected();
            }
        }
    }

    private bool GetSelectDown()
    {
        if (!useXRTriggerInput)
            return false;

        InputDevice device = InputDevices.GetDeviceAtXRNode(triggerHand);
        if (!device.isValid)
        {
            if (logInput)
                Debug.LogWarning("[DropSelection] XR device invalid: " + triggerHand);
            return false;
        }

        bool triggerPressed = false;
        bool primaryPressed = false;

        device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryPressed);

        bool triggerDown = triggerPressed && !_prevTriggerPressed;
        bool primaryDown = allowPrimaryButtonFallback && primaryPressed && !_prevPrimaryPressed;

        if (logInput && (triggerPressed || primaryPressed || triggerDown || primaryDown))
        {
            Debug.Log($"[DropSelection] hand={triggerHand}, triggerPressed={triggerPressed}, primaryPressed={primaryPressed}, triggerDown={triggerDown}, primaryDown={primaryDown}");
        }

        _prevTriggerPressed = triggerPressed;
        _prevPrimaryPressed = primaryPressed;

        return triggerDown || primaryDown;
    }

    private void OnDrawGizmosSelected()
    {
        if (rayOrigin == null) return;

        Vector3 origin = rayOrigin.TransformPoint(rayLocalOffset);
        Vector3 dir = rayOrigin.TransformDirection(rayLocalDirection.normalized);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(origin, 0.01f);
        Gizmos.DrawLine(origin, origin + dir * 0.2f);
    }
}