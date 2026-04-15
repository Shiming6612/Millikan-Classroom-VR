using TMPro;
using UnityEngine;

public class BottomTutorialController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dialogueText;
    public TMP_Text buttonHintText;
    public GameObject dialogueRoot;

    [Header("Tutorial Arrows")]
    public GameObject arrowSetup;
    public GameObject arrowSprayer;
    public GameObject arrowSelectDrop;
    public GameObject arrowLight;
    public GameObject arrowCapacitor;
    public GameObject arrowVoltageKnob;

    [Header("Tutorial Progress")]
    public bool dropletTriggered = false;
    public bool dropSelected = false;
    public bool voltageSolved = false;

    [Header("Disable These During Tutorial")]
    public Behaviour[] componentsToDisableDuringTutorial;

    public static bool TutorialInputLocked { get; private set; }

    private int currentStep = 0;
    private const int LastStepIndex = 14;

    private void Start()
    {
        if (dialogueRoot == null)
            dialogueRoot = gameObject;

        TutorialInputLocked = true;
        SetTutorialInputComponentsEnabled(false);
        HideAllArrows();
        ShowStep();
    }

    private void OnEnable()
    {
        TutorialInputLocked = true;
        SetTutorialInputComponentsEnabled(false);
    }

    private void OnDisable()
    {
        TutorialInputLocked = false;
        SetTutorialInputComponentsEnabled(true);
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            TryNextStep();
        }

        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            PreviousStep();
        }
    }

    private void TryNextStep()
    {
        // Spray step
        if (currentStep == 4 && !dropletTriggered)
        {
            if (buttonHintText != null)
                buttonHintText.text = "Erzeuge zuerst ein Tröpfchen.    B = Zurück";
            return;
        }

        // Selection step
        if (currentStep == 6 && !dropSelected)
        {
            if (buttonHintText != null)
                buttonHintText.text = "Wähle zuerst ein Tröpfchen aus.    B = Zurück";
            return;
        }

        // Voltage success step
        if (currentStep == 13 && !voltageSolved)
        {
            if (buttonHintText != null)
                buttonHintText.text = "Stelle zuerst die richtige Spannung ein.    B = Zurück";
            return;
        }

        NextStep();
    }

    public void NextStep()
    {
        if (currentStep < LastStepIndex)
        {
            currentStep++;
            ShowStep();
        }
        else
        {
            EndTutorial();
        }
    }

    public void PreviousStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
            ShowStep();
        }
    }

    private void ShowStep()
    {
        if (dialogueText != null)
            dialogueText.text = GetDialogueForCurrentStep();

        if (buttonHintText != null)
            buttonHintText.text = "A = Weiter    B = Zurück";

        UpdateArrowForStep(currentStep);
    }

    private string GetDialogueForCurrentStep()
    {
        switch (currentStep)
        {
            case 0:
                return "Willkommen im VR-Labor.\nGehe bitte zum Millikan-Aufbau.";

            case 1:
                return "Die Geschichte beginnt in Chicago im Jahr 1909.\nMillikan wollte herausfinden, ob elektrische Ladung aus kleinsten Einheiten besteht.";

            case 2:
                return "Du siehst hier keine echte Apparatur in Originalgröße.\nDies ist eine vergrößerte VR-Version, damit du alle Teile besser erkennen und selbst benutzen kannst.";

            case 3:
                return "In diesem Versuch untersuchen wir winzige geladene Öltröpfchen.\nMit der richtigen Spannung kann ein Tröpfchen zwischen den Platten schweben.";

            case 4:
                return "Das ist der Zerstäuber.\nMit ihm werden neue Öltröpfchen in den Aufbau gesprüht.\nRichte deinen Controller darauf und löse ihn aus.";

            case 5:
                return "Wenn die Tröpfchen nicht richtig im Aufbau landen, kannst du neu starten.\nHalte den Zerstäuber dafür etwa 3 Sekunden lang gedrückt.";

            case 6:
                return "Benutze den roten Strahl, um ein Tröpfchen anzuvisieren.\nBestätige die Auswahl mit dem Trigger.";

            case 7:
                return "Sehr gut.\nIm Panel siehst du jetzt die Masse, die Ladung und die Spannung des ausgewählten Tröpfchens.";

            case 8:
                return "Die Lichtquelle beleuchtet die Kammer.\nSo werden die Tröpfchen gut sichtbar.";

            case 9:
                return "Das ist der Plattenkondensator.\nZwischen den Platten entsteht das elektrische Feld.\nDer Plattenabstand beträgt 6 mm.";

            case 10:
                return "Jetzt kannst du die passende Spannung berechnen.\nBenutze dafür die Formel U = mgd / q.";

            case 11:
                return "Halte den Spannungsregler fest.\nBewege den Controller nach links oder rechts.\nSo änderst du die Spannung.";

            case 12:
                return "Für kleine Änderungen halte X am linken Controller gedrückt.\nDann kannst du feiner nachregeln.";

            case 13:
                return "Ist der Wert richtig, wird die Spannung grün.\nAußerdem hörst du ein Signal.\nBleibt der Wert kurz stabil, ist die Aufgabe geschafft.";

            case 14:
                return "Super.\nDu hast das Tutorial abgeschlossen.\nJetzt kannst du den Versuch selbstständig weiter ausprobieren. Viel Spaß!";

            default:
                return "";
        }
    }

    private void UpdateArrowForStep(int step)
    {
        HideAllArrows();

        switch (step)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                if (arrowSetup != null) arrowSetup.SetActive(true);
                break;

            case 4:
            case 5:
                if (arrowSprayer != null) arrowSprayer.SetActive(true);
                break;

            case 6:
            case 7:
                if (arrowSelectDrop != null) arrowSelectDrop.SetActive(true);
                break;

            case 8:
                if (arrowLight != null) arrowLight.SetActive(true);
                break;

            case 9:
            case 10:
                if (arrowCapacitor != null) arrowCapacitor.SetActive(true);
                break;

            case 11:
            case 12:
            case 13:
                if (arrowVoltageKnob != null) arrowVoltageKnob.SetActive(true);
                break;

            case 14:
                break;
        }
    }

    private void HideAllArrows()
    {
        if (arrowSetup != null) arrowSetup.SetActive(false);
        if (arrowSprayer != null) arrowSprayer.SetActive(false);
        if (arrowSelectDrop != null) arrowSelectDrop.SetActive(false);
        if (arrowLight != null) arrowLight.SetActive(false);
        if (arrowCapacitor != null) arrowCapacitor.SetActive(false);
        if (arrowVoltageKnob != null) arrowVoltageKnob.SetActive(false);
    }

    public void NotifyDropletTriggered()
    {
        dropletTriggered = true;

        if (currentStep == 4)
        {
            NextStep();
        }
    }

    public void NotifyDropSelected()
    {
        dropSelected = true;

        if (currentStep == 6)
        {
            NextStep();
        }
    }

    public void NotifyVoltageSolved()
    {
        if (voltageSolved)
            return;

        voltageSolved = true;

        if (currentStep == 13)
        {
            NextStep();
        }
    }

    private void EndTutorial()
    {
        TutorialInputLocked = false;
        SetTutorialInputComponentsEnabled(true);
        HideAllArrows();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void SetTutorialInputComponentsEnabled(bool enabled)
    {
        if (componentsToDisableDuringTutorial == null)
            return;

        for (int i = 0; i < componentsToDisableDuringTutorial.Length; i++)
        {
            if (componentsToDisableDuringTutorial[i] != null)
            {
                componentsToDisableDuringTutorial[i].enabled = enabled;
            }
        }
    }
}