using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HistogramPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject histogramRoot;

    [Header("Bars")]
    public RectTransform[] barFills;
    public TMP_Text[] barLabels;
    public TMP_Text[] countLabels;

    [Header("Visual")]
    public float maxBarHeight = 120f;
    public float minVisibleBarHeight = 6f;

    private const float ElementaryCharge = 1.602176634e-19f;
    private readonly List<float> qOverEValues = new List<float>();

    public int MeasurementCount => qOverEValues.Count;

    private void Awake()
    {
        Hide();
        UpdateBars();
    }

    public void Clear()
    {
        qOverEValues.Clear();
        UpdateBars();
    }

    public void AddMeasurement(float chargeCoulomb)
    {
        float qOverE = Mathf.Abs(chargeCoulomb) / ElementaryCharge;
        qOverEValues.Add(qOverE);
        UpdateBars();
    }

    public void Show()
    {
        if (histogramRoot != null)
            histogramRoot.SetActive(true);

        UpdateBars();
    }

    public void Hide()
    {
        if (histogramRoot != null)
            histogramRoot.SetActive(false);
    }

    public string GetGuideText()
    {
        return
            "Ladungsverteilung\n\n" +
            "Die Balken zeigen, bei welchen Vielfachen der Elementarladung e deine Messwerte liegen.\n\n" +
            "Jede Ladung liegt nahe bei einem ganzzahligen Vielfachen von e.";
    }

    public string GetShortResultText()
    {
        if (qOverEValues.Count == 0)
            return "Noch keine Messwerte vorhanden.";

        string text = "Messwerte:\n";

        for (int i = 0; i < qOverEValues.Count; i++)
        {
            text += "Drop " + (i + 1).ToString("00") +
                    ": q ¡Ö " + Mathf.RoundToInt(qOverEValues[i]) + "e\n";
        }

        return text;
    }

    private void UpdateBars()
    {
        int[] bins = new int[6];

        for (int i = 0; i < qOverEValues.Count; i++)
        {
            int n = Mathf.RoundToInt(qOverEValues[i]);
            n = Mathf.Clamp(n, 1, 5);
            bins[n]++;
        }

        int maxCount = 1;

        for (int i = 1; i <= 5; i++)
            maxCount = Mathf.Max(maxCount, bins[i]);

        for (int i = 0; i < 5; i++)
        {
            int binIndex = i + 1;
            int count = bins[binIndex];

            if (barLabels != null && i < barLabels.Length && barLabels[i] != null)
                barLabels[i].text = binIndex + "e";

            if (countLabels != null && i < countLabels.Length && countLabels[i] != null)
                countLabels[i].text = count.ToString();

            if (barFills != null && i < barFills.Length && barFills[i] != null)
            {
                float height = count <= 0
                    ? 0f
                    : Mathf.Lerp(minVisibleBarHeight, maxBarHeight, count / (float)maxCount);

                Vector2 size = barFills[i].sizeDelta;
                size.y = height;
                barFills[i].sizeDelta = size;
            }
        }
    }
}