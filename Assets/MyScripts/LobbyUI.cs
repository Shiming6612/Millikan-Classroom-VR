using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInput;

    [Tooltip("Drag the prefab object that has ColorWheelControl on it.")]
    public ColorWheelControl colorWheel;

    [Tooltip("Optional: small Image to preview the selected color.")]
    public Image colorPreview;

    private Color lastColor;

    private void Start()
    {
        // Initialize wheel from saved ClientData (if available)
        if (ClientData.Instance != null)
        {
            lastColor = ClientData.Instance.PlayerColor;

            if (colorWheel != null)
            {
                colorWheel.PickColor(lastColor); // sets wheel UI + internal state
                // Important: PickColor updates the wheel state, Selection will be updated during Update/drag,
                // so we also refresh preview and data now:
            }

            ApplyColorToClientData(lastColor);
        }
        else
        {
            lastColor = Color.red;
            ApplyColorToClientData(lastColor);
        }
    }

    private void Update()
    {
        if (colorWheel == null || ClientData.Instance == null)
            return;

        // Read current selected color from the wheel
        Color current = colorWheel.Selection;

        if (current != lastColor)
        {
            lastColor = current;
            ApplyColorToClientData(current);
        }
    }

    private void ApplyColorToClientData(Color c)
    {
        if (ClientData.Instance != null)
            ClientData.Instance.PlayerColor = c;

        if (colorPreview != null)
            colorPreview.color = c;
    }

    public void SaveName()
    {
        if (nameInput != null && ClientData.Instance != null)
            ClientData.Instance.PlayerName = nameInput.text;
    }

    public void StartGame()
    {
        SaveName();
        SceneManager.LoadScene("TAI"); // keep your scene name
    }
}
