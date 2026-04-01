using System.Collections;
using UnityEngine;

public class ExperimentController : MonoBehaviour
{
    [Header("Shell")]
    public OuterShellToggle shellToggle;

    [Header("Spray")]
    public SpraySpawner spraySpawner;

    [Header("Input")]
    public float longPressSeconds = 3f;

    private bool _isHolding = false;
    private bool _longPressTriggered = false;
    private Coroutine _holdRoutine;

    // 给 PressBulbTrigger.cs 调用
    public void BulbPressed()
    {
        Bulb_Select();
    }

    // 给 PressBulbTrigger.cs 调用
    public void BulbReleased()
    {
        Bulb_Unselect();
    }

    // Bind to Bulb: When Select()
    public void Bulb_Select()
    {
        _isHolding = true;
        _longPressTriggered = false;

        if (_holdRoutine != null) StopCoroutine(_holdRoutine);
        _holdRoutine = StartCoroutine(LongPressWatcher());
    }

    // Bind to Bulb: When Unselect()
    public void Bulb_Unselect()
    {
        _isHolding = false;

        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }

        // If long press already reset, do nothing
        if (_longPressTriggered) return;

        // Short press: open shell + spray
        if (shellToggle != null) shellToggle.SetCutaway(true);
        if (spraySpawner != null) spraySpawner.SprayOnce();
    }

    public void ResetExperiment()
    {
        if (shellToggle != null) shellToggle.SetCutaway(false);
        if (spraySpawner != null) spraySpawner.ResetAllDrops();
    }

    private IEnumerator LongPressWatcher()
    {
        float t = 0f;
        while (_isHolding)
        {
            t += Time.deltaTime;
            if (t >= longPressSeconds)
            {
                _longPressTriggered = true;
                ResetExperiment();
                yield break;
            }
            yield return null;
        }
    }
}