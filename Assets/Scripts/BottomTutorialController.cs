using TMPro;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("Experiment References")]
    public SpraySpawner spraySpawner;
    public RadiusSliderController radiusSliderController;

    [Header("Optional Visuals")]
    public GameObject forceArrowsRoot;
    public GameObject histogramRoot;
    public GameObject notebookRoot;

    [Header("Radius Checklist")]
    public float radiusTargetTolerance = 0.08f;
    public TMP_Text radiusChecklistText;

    [Header("Measurement Task")]
    public int requiredFloatingDroplets = 5;
    public TMP_Text floatingCounterText;

    [Header("Sounds")]
    public AudioSource taskCompleteSfxSource;
    public AudioClip taskCompleteSfx;

    [Header("Disable These During Tutorial")]
    public Behaviour[] componentsToDisableDuringTutorial;

    [Header("Story Completed")]
    public UnityEvent onStoryCompleted;

    public static bool TutorialInputLocked { get; private set; }

    public bool IsSessionActive => tutorialSessionActive;
    public int MeasurementsCompleted => measurementsCompleted;

    private bool tutorialSessionActive = false;
    private int currentStep = 0;

    private int measurementsCompleted = 0;

    private bool radius05Done = false;
    private bool radius10Done = false;
    private bool radius15Done = false;

    private bool firstMeasurementExplanationShown = false;

    private const int LastStepIndex = 29;

    private void Start()
    {
        if (dialogueRoot == null)
            dialogueRoot = gameObject;

        ResetTutorialProgress();

        tutorialSessionActive = false;
        TutorialInputLocked = false;

        SetTutorialInputComponentsEnabled(true);
        HideAllArrows();
        HideOptionalVisuals();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void Update()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 15)
            UpdateRadiusChecklistFromSlider();

        if (OVRInput.GetDown(OVRInput.Button.One))
            TryAdvanceWithAButton();
    }

    public void BeginTutorialSession()
    {
        ResetTutorialProgress();

        tutorialSessionActive = true;
        TutorialInputLocked = true;

        SetTutorialInputComponentsEnabled(false);
        HideAllArrows();
        HideOptionalVisuals();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);

        ShowStep();
    }

    public void EndTutorialSession()
    {
        tutorialSessionActive = false;
        TutorialInputLocked = false;

        if (radiusSliderController != null)
            radiusSliderController.EndRadiusTask();

        if (spraySpawner != null)
            spraySpawner.ReturnToRandomModeAndClearDrops();

        SetTutorialInputComponentsEnabled(true);
        HideAllArrows();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        onStoryCompleted?.Invoke();

        ResetTutorialProgress();
    }

    private void ResetTutorialProgress()
    {
        currentStep = 0;

        measurementsCompleted = 0;
        firstMeasurementExplanationShown = false;

        radius05Done = false;
        radius10Done = false;
        radius15Done = false;

        UpdateFloatingCounter();
        UpdateRadiusChecklistText();
    }

    private void TryAdvanceWithAButton()
    {
        if (IsBlockingTaskStep(currentStep))
        {
            RefreshCurrentText();
            return;
        }

        if (currentStep == 15)
        {
            if (IsRadiusChecklistComplete())
                NextStep();
            else
                RefreshCurrentText();

            return;
        }

        if (currentStep == 24)
        {
            if (measurementsCompleted < requiredFloatingDroplets)
            {
                currentStep = 20;
                ShowStep();
            }
            else
            {
                NextStep();
            }

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
            EndTutorialSession();
        }
    }

    private void ShowStep()
    {
        ApplyStepSideEffects(currentStep);

        RefreshCurrentText();

        UpdateFloatingCounter();
        UpdateRadiusChecklistText();
        UpdateArrowForStep(currentStep);
    }

    private void RefreshCurrentText()
    {
        if (dialogueText != null)
            dialogueText.text = GetDialogueForCurrentStep();

        if (buttonHintText != null)
            buttonHintText.text = GetButtonHintForCurrentStep();
    }

    private void ApplyStepSideEffects(int step)
    {
        switch (step)
        {
            case 0:
                HideOptionalVisuals();
                break;

            case 15:
                if (radiusSliderController != null)
                    radiusSliderController.StartRadiusTask();

                if (spraySpawner != null)
                    spraySpawner.EnableTutorialRadiusMode();
                break;

            case 16:
                if (radiusSliderController != null)
                    radiusSliderController.EndRadiusTask();

                if (spraySpawner != null)
                    spraySpawner.ReturnToRandomModeAndClearDrops();
                break;

            case 17:
                if (forceArrowsRoot != null)
                    forceArrowsRoot.SetActive(true);
                break;

            case 20:
                if (spraySpawner != null)
                    spraySpawner.ReturnToRandomModeAndClearDrops();
                break;

            case 25:
                if (histogramRoot != null)
                    histogramRoot.SetActive(true);
                break;

            case 27:
                if (notebookRoot != null)
                    notebookRoot.SetActive(true);
                break;
        }
    }

    private bool IsBlockingTaskStep(int step)
    {
        return step == 12 || step == 20 || step == 21 || step == 22;
    }

    private string GetDialogueForCurrentStep()
    {
        switch (currentStep)
        {
            case 0:
                return "Ah. Ein Klassenzimmer. Gut. Das kenne ich.\nBei uns sahen sie etwas anders aus — aber das Prinzip ist dasselbe.\nMein Name ist Robert Andrews Millikan.\nIch war Physikprofessor an der University of Chicago — und später am California Institute of Technology.";

            case 1:
                return "Aber egal… Ich habe ein Problem.\nOder genauer gesagt: Ich habe eine Frage.\nIst elektrische Ladung unteilbar?\nGibt es ein kleinstes elektrisches Paket — eine Art Atom der Ladung?";

            case 2:
                return "Oder fließt Elektrizität einfach kontinuierlich, wie Wasser durch einen Schlauch?\nIch habe ein Experiment gebaut, das diese Frage beantworten kann.\nAber ich kann es nicht alleine durchführen.\nDazu brauche ich einen Assistenten — wie dich!";

            case 3:
                return "Du müsstest die Geräte bedienen, während ich erkläre, was gerade passiert.\nAußerdem musst du gut aufpassen.\nGenau wie mein Doktorand Harvey Fletcher damals.\nBist du bereit, diese Rolle zu übernehmen und mir zu helfen?";

            case 4:
                return "Ausgezeichnet. Dann legen wir los.\nKomm zum Experiment.\nIch zeige dir, womit wir es zu tun haben.";

            case 5:
                return "Gut. Dann fangen wir von vorne an.\nIch bin 1868 in Morrison, Illinois geboren.\nPhysik hat mich schon immer fasziniert.\nDie Frage, woraus Materie wirklich besteht.";

            case 6:
                return "1909 haben mein Doktorand Harvey Fletcher und ich begonnen, diesen Apparat hier zu entwickeln.\nFletcher hatte die entscheidende Idee: Statt Wasser — Öl.\nUnsere Tröpfchen bleiben stundenlang stabil.\nDas klingt banal. Aber es hat alles verändert.";

            case 7:
                return "Was wir herausfinden wollten: Gibt es eine kleinste Einheit elektrischer Ladung?\nOder ist Elektrizität so etwas wie eine Flüssigkeit?\nJ.J. Thomson hatte 1897 gezeigt, dass es Elektronen gibt.\nAber wie groß ist ihre Ladung? Das wusste niemand genau.";

            case 8:
                return "Hier ist er. Mein Apparat.\nFünf Dinge arbeiten zusammen.\nUnd jedes einzelne ist entscheidend.\nIch erkläre dir jede einzelne Komponente nacheinander.";

            case 9:
                return "Dieser einfache Zerstäuber — fast wie ein Parfümflakon — ist der Anfang von allem.\nEin kurzer Druck, und Millionen winziger Öltröpfchen werden in die Messkammer geblasen.\nDurch die Reibung beim Zerstäuben laden sich viele davon elektrisch auf.\nGenau das sind die entscheidenden Öltröpfchen für uns.";

            case 10:
                return "Diese Tröpfchen sind viel zu klein, um sie direkt zu sehen.\nDas Mikroskop macht sie sichtbar — als helle Lichtpunkte auf dunklem Hintergrund.\nAber Vorsicht: Das Mikroskop spiegelt das Bild.\nDeshalb sehen wir in der Simulation direkt, ob die Öltröpfchen sinken oder steigen.";

            case 11:
                return "Das Licht kommt schräg von der Seite.\nOhne es würden wir gar nichts sehen.\nDie Tröpfchen streuen das Licht wie Staubkörner in einem Sonnenstrahl.\nPlötzlich leuchten sie auf.";

            case 12:
                return "Aufgabe: Zerstäuber benutzen.\nDas Feld ist ausgeschaltet.\nRichte deine Hand auf den Zerstäuber.\nDrücke den rechten Trigger, um Öltröpfchen zu erzeugen.";

            case 13:
                return "Wie du siehst, werden die ersten Tröpfchen in den Apparat gesprüht.\nDie Tröpfchen fallen.\nLangsam — aber sie fallen.\nDie Schwerkraft zieht sie nach unten.";

            case 14:
                return "Warum ist das wichtig?\nAus der Fallgeschwindigkeit eines Tröpfchens lässt sich der Radius r berechnen.\nWir brauchen den Radius, um später die Ladung zu bestimmen.\nSchnelleres Fallen bedeutet: größeres Tröpfchen.";

            case 15:
                if (IsRadiusChecklistComplete())
                {
                    return "Radius-Aufgabe abgeschlossen.\nDu hast 0,5 µm, 1,0 µm und 1,5 µm getestet.\nJetzt kannst du mit A fortfahren.";
                }

                return "Aufgabe: Radius einstellen.\nDer Regler reicht von 0,3 µm bis 2,0 µm.\nStelle nacheinander diese Größen ein:\n\n" + GetRadiusChecklistString();

            case 16:
                return "Gut. Du verstehst jetzt: Der Radius bestimmt, wie schnell ein Tröpfchen fällt.\nUnd aus der Fallgeschwindigkeit können wir den Radius berechnen.\nJetzt kommt der eigentliche Schritt.";

            case 17:
                return "Wir sehen einen Regler, um die Spannung im Feld einzustellen.\nDreh den Regler langsam nach oben.\nDie elektrische Kraft wird stärker.\nDas Tröpfchen verlangsamt sich.";

            case 18:
                return "Wenn die Spannung zu hoch wird, steigt das Tröpfchen.\nVersuch es so einzustellen, dass das Tröpfchen schwebt.\nDann hält das elektrische Feld es gegen die Schwerkraft.\nDie beiden Kräfte heben sich auf.";

            case 19:
                return "Beim Schweben gilt also:\nF_el = F_G.\nDie elektrische Kraft ist so groß wie die Schwerkraft.\nJetzt messen wir das an einem einzelnen Öltröpfchen.";

            case 20:
                return "Aufgabe: Neue Öltröpfchen erzeugen.\nRichte auf den Zerstäuber.\nDrücke den rechten Trigger.\n\nMessung: " + measurementsCompleted + "/" + requiredFloatingDroplets;

            case 21:
                return "Aufgabe: Öltröpfchen auswählen.\nZiele mit dem roten Strahl auf ein Tröpfchen.\nDrücke den rechten Trigger.\n\nMessung: " + measurementsCompleted + "/" + requiredFloatingDroplets;

            case 22:
                return "Aufgabe: Tröpfchen zum Schweben bringen.\nGreife den Spannungsregler.\nStelle die Spannung so ein, dass das Tröpfchen möglichst ruhig bleibt.\n\nMessung: " + measurementsCompleted + "/" + requiredFloatingDroplets;

            case 23:
                return "Aus diesem Gleichgewicht folgt die gesuchte Spannung.\nF_el = F_G.\nq · U / d = m · g.\nDaraus folgt: q = m · g · d / U.";

            case 24:
                return "Das war deine erste Ladungsmessung.\nAber eine Messung ist noch keine Wissenschaft.\nWas ich brauche, ist ein Muster.\nBringe bitte weitere Tröpfchen nacheinander zum Schweben.";

            case 25:
                return "Ich habe nicht ein Tröpfchen gemessen.\nIch habe hunderte gemessen. Über Monate.\nDie Ladungen waren nie zufällig verteilt.\nSie häuften sich immer an denselben Stellen.";

            case 26:
                return "Elektrische Ladung ist nicht kontinuierlich.\nSie kommt in Paketen.\nDas kleinste Paket ist die Elementarladung e.\nJedes Tröpfchen trägt ein, zwei, drei oder mehr dieser Pakete.";

            case 27:
                return "Ich muss dir etwas zeigen.\n1978 entdeckte Gerald Holton meine Original-Notizbücher.\nDarin standen mehr Messungen, als ich veröffentlicht hatte.\nNeben manchen Datenpunkten standen Notizen wie: 'Won't work' oder 'Error — discard'.";

            case 28:
                return "War das falsch? Ich glaube: Nein.\nIch habe Messungen ausgeschlossen, bei denen ich technische Fehler erkannt habe.\nAber es gibt eine klare Anforderung: Transparenz.\nWas ich ausschließe — und warum — das muss dokumentiert sein.";

            case 29:
                return "Es war 1913.\nIch publizierte meinen Endwert: e = 1,592 mal zehn hoch minus neunzehn Coulomb.\nDer heute akzeptierte Wert ist 1,602 mal zehn hoch minus neunzehn Coulomb.\nAber der eigentliche Beitrag ist nicht die Zahl. Es ist das Prinzip: elektrische Ladung ist gequantelt.";

            default:
                return "";
        }
    }

    private string GetButtonHintForCurrentStep()
    {
        if (IsBlockingTaskStep(currentStep))
            return "";

        if (currentStep == 15 && !IsRadiusChecklistComplete())
            return "";

        return "A: Weiter";
    }

    private void UpdateRadiusChecklistFromSlider()
    {
        if (radiusSliderController == null)
            return;

        float radius = radiusSliderController.GetCurrentRadiusMicrometer();

        bool changed = false;

        if (!radius05Done && Mathf.Abs(radius - 0.5f) <= radiusTargetTolerance)
        {
            radius05Done = true;
            changed = true;
        }

        if (!radius10Done && Mathf.Abs(radius - 1.0f) <= radiusTargetTolerance)
        {
            radius10Done = true;
            changed = true;
        }

        if (!radius15Done && Mathf.Abs(radius - 1.5f) <= radiusTargetTolerance)
        {
            radius15Done = true;
            changed = true;
        }

        if (changed)
        {
            PlayTaskCompleteSound();
            UpdateRadiusChecklistText();
            RefreshCurrentText();
        }
    }

    private bool IsRadiusChecklistComplete()
    {
        return radius05Done && radius10Done && radius15Done;
    }

    private string GetRadiusChecklistString()
    {
        string a = radius05Done ? "✓ r = 0,5 µm" : "□ r = 0,5 µm";
        string b = radius10Done ? "✓ r = 1,0 µm" : "□ r = 1,0 µm";
        string c = radius15Done ? "✓ r = 1,5 µm" : "□ r = 1,5 µm";

        return a + "\n" + b + "\n" + c;
    }

    private void UpdateRadiusChecklistText()
    {
        if (radiusChecklistText != null)
            radiusChecklistText.text = GetRadiusChecklistString();
    }

    private void UpdateFloatingCounter()
    {
        if (floatingCounterText != null)
            floatingCounterText.text = measurementsCompleted + "/" + requiredFloatingDroplets;
    }

    private void UpdateArrowForStep(int step)
    {
        HideAllArrows();

        switch (step)
        {
            case 8:
                if (arrowSetup != null) arrowSetup.SetActive(true);
                break;

            case 9:
            case 12:
            case 20:
                if (arrowSprayer != null) arrowSprayer.SetActive(true);
                break;

            case 11:
                if (arrowLight != null) arrowLight.SetActive(true);
                break;

            case 17:
            case 18:
            case 19:
            case 23:
                if (arrowCapacitor != null) arrowCapacitor.SetActive(true);
                break;

            case 22:
                if (arrowVoltageKnob != null) arrowVoltageKnob.SetActive(true);
                break;

            case 21:
                if (arrowSelectDrop != null) arrowSelectDrop.SetActive(true);
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

    private void HideOptionalVisuals()
    {
        if (forceArrowsRoot != null)
            forceArrowsRoot.SetActive(false);

        if (histogramRoot != null)
            histogramRoot.SetActive(false);

        if (notebookRoot != null)
            notebookRoot.SetActive(false);
    }

    public void NotifyDropletTriggered()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 12 || currentStep == 20)
        {
            PlayTaskCompleteSound();
            NextStep();
        }
    }

    public void NotifyDropSelected()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 21)
        {
            PlayTaskCompleteSound();
            NextStep();
        }
    }

    public void NotifyVoltageSolved()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep != 22)
            return;

        measurementsCompleted++;
        UpdateFloatingCounter();
        PlayTaskCompleteSound();

        if (measurementsCompleted == 1 && !firstMeasurementExplanationShown)
        {
            firstMeasurementExplanationShown = true;
            currentStep = 23;
            ShowStep();
            return;
        }

        if (measurementsCompleted < requiredFloatingDroplets)
        {
            if (spraySpawner != null)
                spraySpawner.ResetAllDrops();

            currentStep = 20;
            ShowStep();
            return;
        }

        currentStep = 25;
        ShowStep();
    }

    private void PlayTaskCompleteSound()
    {
        if (taskCompleteSfxSource != null && taskCompleteSfx != null)
            taskCompleteSfxSource.PlayOneShot(taskCompleteSfx);
    }

    private void SetTutorialInputComponentsEnabled(bool enabled)
    {
        if (componentsToDisableDuringTutorial == null)
            return;

        for (int i = 0; i < componentsToDisableDuringTutorial.Length; i++)
        {
            if (componentsToDisableDuringTutorial[i] != null)
                componentsToDisableDuringTutorial[i].enabled = enabled;
        }
    }
}