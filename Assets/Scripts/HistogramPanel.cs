using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HistogramPanel : MonoBehaviour
{
    public TMP_Text optionalText;

    private const float ElementaryCharge = 1.602176634e-19f;
    private readonly List<float> qOverEValues = new List<float>();

    public void Clear()
    {
        qOverEValues.Clear();
        RefreshOptionalText();
    }

    public void AddMeasurement(float chargeCoulomb)
    {
        float qOverE = Mathf.Abs(chargeCoulomb) / ElementaryCharge;
        qOverEValues.Add(qOverE);
        RefreshOptionalText();
    }

    public string GetHistogramText()
    {
        int[] bins = new int[6];

        for (int i = 0; i < qOverEValues.Count; i++)
        {
            int n = Mathf.RoundToInt(qOverEValues[i]);
            n = Mathf.Clamp(n, 1, 5);
            bins[n]++;
        }

        string text = "Ladungsverteilung\n\n";

        for (int i = 1; i <= 5; i++)
            text += i + "e: " + MakeBars(bins[i]) + "\n";

        text += "\nMesswerte:\n";

        for (int i = 0; i < qOverEValues.Count; i++)
        {
            text += "Drop " + (i + 1).ToString("00") +
                    ": q ¡Ö " + Mathf.RoundToInt(qOverEValues[i]) + "e\n";
        }

        text += "\nJede Ladung liegt nahe bei einem ganzzahligen Vielfachen von e.";

        return text;
    }

    private void RefreshOptionalText()
    {
        if (optionalText != null)
            optionalText.text = GetHistogramText();
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