using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// VR / XR-friendly version:
/// Uses pointer events (screen position from the UI ray) instead of Input.mousePosition.
/// No EventTrigger needed.
/// </summary>
public class ColorWheelControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // Output Color
    public Color Selection;

    // Control values
    private float outer;
    private Vector2 inner;

    private bool dragOuter, dragInner;

    // The Components of the wheel
    private Material mat;
    private RectTransform rectTrans, selectorOut, selectorIn;

    private float halfSize;

    private void Start()
    {
        // Get the rect transform and make x and y the same to avoid stretching
        rectTrans = GetComponent<RectTransform>();
        rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, rectTrans.sizeDelta.x);

        // Find and scale the children
        selectorOut = transform.Find("Selector_Out").GetComponent<RectTransform>();
        selectorIn = transform.Find("Selector_In").GetComponent<RectTransform>();

        selectorOut.sizeDelta = rectTrans.sizeDelta / 20.0f;
        selectorIn.sizeDelta = rectTrans.sizeDelta / 20.0f;

        // Calculate the half size
        halfSize = rectTrans.sizeDelta.x / 2f;

        // Set the material
        mat = GetComponent<Image>().material;

        // Default selected value to red (0° rotation and upper right corner in the box)
        Selection = Color.red;

        // Ensure a consistent starting state
        outer = 0f;
        inner = Vector2.zero;

        updateMaterial();
        updateColor();
        updateSelectors();
    }

    // --- Pointer Events (VR-safe) ---

    public void OnPointerDown(PointerEventData eventData)
    {
        // Decide whether user clicked ring or inner box
        if (!TryGetLocalPoint(eventData, out Vector2 local))
            return;

        // local is centered around pivot. We assume pivot is centered (0.5,0.5) like in the prefab.
        float dist = local.magnitude;

        // Outer ring region: between radius (halfSize - halfSize/4) and halfSize
        float innerRing = halfSize - halfSize / 4f;

        if (dist <= halfSize && dist >= innerRing)
        {
            dragOuter = true;
            dragInner = false;
            UpdateOuterFromLocal(local);
            updateMaterial();
            updateColor();
            updateSelectors();
            return;
        }

        // Inner square region: halfSize/2 from center in X and Y
        if (Mathf.Abs(local.x) <= halfSize / 2f && Mathf.Abs(local.y) <= halfSize / 2f)
        {
            dragInner = true;
            dragOuter = false;
            UpdateInnerFromLocal(local);
            updateColor();
            updateSelectors();
            return;
        }

        // Clicked outside: do nothing
        dragOuter = false;
        dragInner = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragOuter && !dragInner) return;

        if (!TryGetLocalPoint(eventData, out Vector2 local))
            return;

        if (dragOuter)
        {
            UpdateOuterFromLocal(local);
            updateMaterial();
            updateColor();
        }
        else if (dragInner)
        {
            UpdateInnerFromLocal(local);
            updateColor();
        }

        updateSelectors();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragOuter = false;
        dragInner = false;
    }

    // --- Helpers ---

    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 local)
    {
        // Convert screen point to local point in this rect
        // pressEventCamera is important for World Space canvases / VR
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTrans, eventData.position, eventData.pressEventCamera, out local);
    }

    private void UpdateOuterFromLocal(Vector2 local)
    {
        // local direction from center
        Vector2 dir = -local.normalized; // matches original "RectTrans.position - mousePosition" logic direction
        outer = Mathf.Atan2(-dir.x, -dir.y);
    }

    private void UpdateInnerFromLocal(Vector2 local)
    {
        // Map local coords inside inner square to 0..1
        // Inner square spans [-halfSize/2 .. +halfSize/2]
        float x = Mathf.Clamp(local.x, -halfSize / 2f, halfSize / 2f);
        float y = Mathf.Clamp(local.y, -halfSize / 2f, halfSize / 2f);

        // Original code used: dir = center - mouse; then shift by +halfSize/2 and divide by halfSize.
        // In local coords, we reproduce the same mapping:
        // inner.x: 0..1 left->right (in their logic it's flipped in selector placement, keep same)
        // inner.y: 0..1 bottom->top
        float nx = (-(x) + halfSize / 2f) / halfSize; // flip X to match original
        float ny = (-(y) + halfSize / 2f) / halfSize; // flip Y to match original

        inner = new Vector2(Mathf.Clamp01(nx), Mathf.Clamp01(ny));
    }

    private void updateSelectors()
    {
        // Outer selector: ring radius 0.85 * halfSize
        selectorOut.localPosition = new Vector3(
            Mathf.Sin(outer) * halfSize * 0.85f,
            Mathf.Cos(outer) * halfSize * 0.85f,
            1f
        );

        // Inner selector: uses the same mapping as the original script
        selectorIn.localPosition = new Vector3(
            halfSize * 0.5f - inner.x * halfSize,
            halfSize * 0.5f - inner.y * halfSize,
            1f
        );
    }

    // Update the material of the inner box to match the hue color
    private void updateMaterial()
    {
        Color c = Color.white;

        c.r = Mathf.Clamp(2 / Mathf.PI * Mathf.Asin(Mathf.Cos(outer)) * 1.5f + 0.5f, 0, 1);
        c.g = Mathf.Clamp(2 / Mathf.PI * Mathf.Asin(Mathf.Cos(2 * Mathf.PI * (1.0f / 3.0f) - outer)) * 1.5f + 0.5f, 0, 1);
        c.b = Mathf.Clamp(2 / Mathf.PI * Mathf.Asin(Mathf.Cos(2 * Mathf.PI * (2.0f / 3.0f) - outer)) * 1.5f + 0.5f, 0, 1);

        mat.SetColor("_Color", c);
    }

    // Gets called after changes
    private void updateColor()
    {
        Color c = Color.white;

        c.r = Mathf.Clamp(2 / Mathf.PI * Mathf.Asin(Mathf.Cos(outer)) * 1.5f + 0.5f, 0, 1);
        c.g = Mathf.Clamp(2 / Mathf.PI * Mathf.Asin(Mathf.Cos(2 * Mathf.PI * (1.0f / 3.0f) - outer)) * 1.5f + 0.5f, 0, 1);
        c.b = Mathf.Clamp(2 / Mathf.PI * Mathf.Asin(Mathf.Cos(2 * Mathf.PI * (2.0f / 3.0f) - outer)) * 1.5f + 0.5f, 0, 1);

        c = Color.Lerp(c, Color.white, inner.x);
        c = Color.Lerp(c, Color.black, inner.y);

        Selection = c;
    }

    // Method for setting the picker to a given color
    public void PickColor(Color c)
    {
        float max = Mathf.Max(c.r, c.g, c.b);
        float min = Mathf.Min(c.r, c.g, c.b);

        float hue = 0;
        float sat = (1 - min);

        if (max == min)
            sat = 0;

        hue = Mathf.Atan2(Mathf.Sqrt(3) * (c.g - c.b), 2 * c.r - c.g - c.b);

        outer = hue;
        inner.x = 1 - sat;
        inner.y = 1 - max;

        updateMaterial();
        updateColor();
        updateSelectors();
    }
}
