using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SimpleHistogramPanel : MonoBehaviour
{
    public GameObject panelRoot;
    public TMP_Text histogramText;

    private const float ElementaryCharge = 1.602176634e-19f;

    private readonly List<float> qOverEValues = new List<float>();

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        Hide();
    }

    public void Clear()
    {
        qOverEValues.Clear();
        UpdateText();
    }

    public void AddMeasurement(float chargeCoulomb)
    {
        float qOverE = chargeCoulomb / ElementaryCharge;
        qOverEValues.Add(qOverE);
        UpdateText();
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        UpdateText();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void UpdateText()
    {
        if (histogramText == null)
            return;

        int[] bins = new int[6];

        for (int i = 0; i < qOverEValues.Count; i++)
        {
            int n = Mathf.RoundToInt(qOverEValues[i]);
            n = Mathf.Clamp(n, 1, 5);
            bins[n]++;
        }

        string text = "Ladungsverteilung\n\n";

        for (int i = 1; i <= 5; i++)
        {
            text += i + "e: " + MakeBars(bins[i]) + "\n";
        }

        text += "\nMesswerte:\n";

        for (int i = 0; i < qOverEValues.Count; i++)
        {
            text += "Drop " + (i + 1).ToString("00") +
                    ": q ¡Ö " + Mathf.RoundToInt(qOverEValues[i]) + "e\n";
        }

        histogramText.text = text;
    }

    private string MakeBars(int count)
    {
        if (count <= 0)
            return "-";

        string bars = "";

        for (int i = 0; i < count; i++)
            bars += "|";

        return bars;
    }
}