using TMPro;
using UnityEngine;

public class BottomTutorialController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dialogueText;
    public TMP_Text buttonHintText;
    public GameObject dialogueRoot;

    [Header("Panels")]
    public GameObject parameterPanelRoot;
    public GameObject guideUIRoot;
    public TMP_Text guideUIText;
    public HistogramPanel histogramPanel;
    public GameObject forceArrowsRoot;

    [Header("Scene Objects")]
    public GameObject notebookObject;

    [Header("References")]
    public SpraySpawner spraySpawner;
    public RadiusSliderController radiusSliderController;
    public DropSelectionManager selectionManager;
    public StoryMeasurementRecorder measurementRecorder;
    public Behaviour voltageKnobInput;
    public PrePostQuizController quizController;

    [Header("Return Point")]
    public Transform playerRoot;
    public Transform storyReturnPoint;

    [Header("Arrows")]
    public GameObject arrowSetup;
    public GameObject arrowSprayer;
    public GameObject arrowSelectDrop;
    public GameObject arrowLight;
    public GameObject arrowCapacitor;
    public GameObject arrowVoltageKnob;

    [Header("Radius Task")]
    public float radiusTargetTolerance = 0.08f;

    [Header("Measurement Task")]
    public int requiredFloatingDroplets = 5;

    [Header("Sounds")]
    public AudioSource taskCompleteSfxSource;
    public AudioClip taskCompleteSfx;
    public AudioSource pageSfxSource;
    public AudioClip bookPageSfx;

    [Header("Disable These During Tutorial")]
    public Behaviour[] componentsToDisableDuringTutorial;

    public static bool TutorialInputLocked { get; private set; }

    private bool tutorialSessionActive;
    private int currentStep;

    private int measurementsCompleted;
    private bool firstMeasurementExplanationShown;

    private int radiusTaskIndex = -1;
    private bool radiusTaskRadiusCorrect;
    private bool radiusTaskSprayed;
    private bool radiusTaskDropEnteredField;
    private bool currentRadiusTaskDone;

    private readonly float[] radiusTargets = { 0.5f, 1.0f, 1.5f };

    private const int LastStepIndex = 66;

    private void Start()
    {
        if (dialogueRoot == null)
            dialogueRoot = gameObject;

        ResetTutorialProgress();

        tutorialSessionActive = false;
        TutorialInputLocked = false;

        SetTutorialInputComponentsEnabled(true);
        SetVoltageInteraction(false);
        SetSelectionInteraction(false);
        HideAllTemporaryUI();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void Update()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 25 || currentStep == 26 || currentStep == 27)
            UpdateRadiusTask();

        if (OVRInput.GetDown(OVRInput.Button.One))
            TryAdvanceWithAButton();
    }

    public void BeginTutorialSession()
    {
        ResetTutorialProgress();

        tutorialSessionActive = true;
        TutorialInputLocked = true;

        SetTutorialInputComponentsEnabled(false);
        SetVoltageInteraction(false);
        SetSelectionInteraction(false);
        HideAllTemporaryUI();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);

        ShowStep();
    }

    private void EndTutorialSession()
    {
        tutorialSessionActive = false;
        TutorialInputLocked = false;

        SetTutorialInputComponentsEnabled(true);
        SetVoltageInteraction(false);
        SetSelectionInteraction(false);
        HideAllTemporaryUI();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        ReturnToStoryStart();
        StartPostQuiz();

        ResetTutorialProgress();
    }

    private void ResetTutorialProgress()
    {
        currentStep = 0;
        measurementsCompleted = 0;
        firstMeasurementExplanationShown = false;

        radiusTaskIndex = -1;
        radiusTaskRadiusCorrect = false;
        radiusTaskSprayed = false;
        radiusTaskDropEnteredField = false;
        currentRadiusTaskDone = false;

        if (measurementRecorder != null)
            measurementRecorder.ClearMeasurements();
    }

    private void TryAdvanceWithAButton()
    {
        if (IsBlockingStep(currentStep))
        {
            RefreshCurrentText();
            return;
        }

        if (currentStep == 25 || currentStep == 26 || currentStep == 27)
        {
            if (currentRadiusTaskDone)
                NextStep();
            else
                RefreshCurrentText();

            return;
        }

        if (currentStep == 40)
        {
            if (measurementsCompleted < requiredFloatingDroplets)
            {
                currentStep = 33;
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

    private void NextStep()
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
        UpdateArrowForStep(currentStep);
    }

    private void RefreshCurrentText()
    {
        if (dialogueText != null)
            dialogueText.text = GetDialogueForCurrentStep();

        if (buttonHintText != null)
            buttonHintText.text = GetButtonHintForCurrentStep();
    }

    private bool IsBlockingStep(int step)
    {
        return step == 17 || step == 33 || step == 34 || step == 35;
    }

    private void ApplyStepSideEffects(int step)
    {
        SetVoltageInteraction(false);
        SetSelectionInteraction(false);
        HideForceArrows();
        HideGuideUI();
        HideAllArrows();

        if (step >= 23 && step <= 40)
            ShowParameterPanel();
        else
            HideParameterPanel();

        switch (step)
        {
            case 0:
                HideAllTemporaryUI();
                break;

            case 17:
                if (spraySpawner != null)
                    spraySpawner.ReturnToRandomModeAndClearDrops();

                ShowGuideUI(GetGuideSprayer());
                break;

            case 22:
                ShowGuideUI(GetGuideGravityFormula());
                break;

            case 23:
                ShowParameterPanel();

                if (radiusSliderController != null)
                    radiusSliderController.StartRadiusTask();

                if (spraySpawner != null)
                    spraySpawner.EnableTutorialRadiusMode();

                ShowGuideUI(GetGuideRadiusSlider());
                break;

            case 25:
                StartRadiusTask(0);
                ShowGuideUI(GetGuideRadiusTask("0,5", "sehr langsam fallend"));
                break;

            case 26:
                StartRadiusTask(1);
                ShowGuideUI(GetGuideRadiusTask("1,0", "mittlere Geschwindigkeit"));
                break;

            case 27:
                StartRadiusTask(2);
                ShowGuideUI(GetGuideRadiusTask("1,5", "schnell fallend"));
                break;

            case 28:
                if (radiusSliderController != null)
                    radiusSliderController.EndRadiusTask();

                if (spraySpawner != null)
                    spraySpawner.ReturnToRandomModeAndClearDrops();

                HideGuideUI();
                break;

            case 33:
                StartNewMeasurement();
                ShowGuideUI(GetGuideMeasurementSpray());
                break;

            case 34:
                SetSelectionInteraction(true);
                ShowGuideUI(GetGuideMeasurementSelect());
                break;

            case 35:
                SetVoltageInteraction(true);
                ShowForceArrows();
                ShowGuideUI(GetGuideVoltageTask());
                break;

            case 37:
                ShowForceArrows();
                ShowGuideUI(GetGuideElectricFormula());
                break;

            case 38:
                ShowForceArrows();
                ShowGuideUI(GetGuideElectricFormula());
                break;

            case 39:
                ShowGuideUI(GetGuideParameterExplanation());
                break;

            case 41:
                SetVoltageInteraction(false);
                SetSelectionInteraction(false);

                if (spraySpawner != null)
                    spraySpawner.ResetAllDrops();

                ShowGuideUI(GetGuideHistogram());
                break;

            case 42:
            case 43:
            case 44:
            case 45:
            case 46:
                ShowGuideUI(GetGuideHistogram());
                break;

            case 47:
                if (notebookObject != null)
                    notebookObject.SetActive(true);

                PlayBookSound();
                HideGuideUI();
                break;

            case 58:
                if (notebookObject != null)
                    notebookObject.SetActive(false);

                HideAllTemporaryUI();
                break;
        }
    }

    private void StartNewMeasurement()
    {
        ShowParameterPanel();
        HideForceArrows();
        SetVoltageInteraction(false);
        SetSelectionInteraction(false);

        if (spraySpawner != null)
            spraySpawner.ReturnToRandomModeAndClearDrops();

        if (selectionManager != null)
            selectionManager.ClearSelectionAndHover();

        ResetVoltageToZero();
    }

    private void StartRadiusTask(int index)
    {
        radiusTaskIndex = index;
        radiusTaskRadiusCorrect = false;
        radiusTaskSprayed = false;
        radiusTaskDropEnteredField = false;
        currentRadiusTaskDone = false;

        ShowParameterPanel();

        if (radiusSliderController != null)
            radiusSliderController.StartRadiusTask();

        if (spraySpawner != null)
            spraySpawner.EnableTutorialRadiusMode();
    }

    private void UpdateRadiusTask()
    {
        if (currentRadiusTaskDone || radiusSliderController == null)
            return;

        float target = radiusTargets[radiusTaskIndex];
        float current = radiusSliderController.GetCurrentRadiusMicrometer();

        radiusTaskRadiusCorrect = Mathf.Abs(current - target) <= radiusTargetTolerance;

        if (radiusTaskRadiusCorrect && radiusTaskSprayed && radiusTaskDropEnteredField)
        {
            currentRadiusTaskDone = true;
            PlayTaskCompleteSound();
            RefreshCurrentText();
        }
    }

    public void NotifyDropletTriggered()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 17)
        {
            PlayTaskCompleteSound();
            NextStep();
            return;
        }

        if (currentStep == 25 || currentStep == 26 || currentStep == 27)
        {
            radiusTaskSprayed = true;
            RefreshCurrentText();
            return;
        }

        if (currentStep == 33)
        {
            PlayTaskCompleteSound();
            NextStep();
        }
    }

    public void NotifyDropEnteredField()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 25 || currentStep == 26 || currentStep == 27)
        {
            radiusTaskDropEnteredField = true;
            RefreshCurrentText();
        }
    }

    public void NotifyDropSelected()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 34)
        {
            PlayTaskCompleteSound();
            NextStep();
        }
    }

    public void NotifyVoltageSolved()
    {
        if (!tutorialSessionActive || currentStep != 35)
            return;

        measurementsCompleted++;

        if (measurementRecorder != null)
            measurementRecorder.RecordSelectedDrop(selectionManager);

        PlayTaskCompleteSound();
        SetVoltageInteraction(false);

        if (measurementsCompleted == 1 && !firstMeasurementExplanationShown)
        {
            firstMeasurementExplanationShown = true;
            currentStep = 36;
            ShowStep();
            return;
        }

        if (measurementsCompleted < requiredFloatingDroplets)
        {
            currentStep = 33;
            ShowStep();
            return;
        }

        currentStep = 41;
        ShowStep();
    }

    private string GetDialogueForCurrentStep()
    {
        switch (currentStep)
        {
            case 0:
                return "Ah. Ein Klassenzimmer. Gut. Das kenne ich.\nBei uns sahen sie etwas anders aus — aber das Prinzip ist dasselbe.\nMein Name ist Robert Andrews Millikan.\nIch war Physikprofessor an der University of Chicago — und später am California Institute of Technology.";

            case 1:
                return "Aber egal…Ich habe ein Problem.\nOder genauer gesagt: Ich habe eine Frage — und ich brauche jemanden, der mir hilft, sie zu beantworten.\nIst elektrische Ladung unteilbar?";

            case 2:
                return "Gibt es ein kleinstes elektrisches Paket — eine Art Atom der Ladung —\noder fließt Elektrizität einfach kontinuierlich, wie Wasser durch einen Schlauch?\nIch habe ein Experiment gebaut, das diese Frage beantworten kann.\nAber ich kann es nicht alleine durchführen.";

            case 3:
                return "Dazu brauche ich einen Assistenten - wie dich!\nDu müsstest die Geräte bedienen, während ich erkläre, was gerade passiert.\nAußerdem musst du gut aufpassen.\nGenau wie mein Doktorand Harvey Fletcher damals.\nBist du bereit diese Rolle zu übernehmen und mir zu helfen?";

            case 4:
                return "Ausgezeichnet. Dann legen wir los.\nKomm zum Experiment — ich zeige dir, womit wir es zu tun haben.";

            case 5:
                return "Gut. Dann fangen wir von vorne an.\nIch bin 1868 in Morrison, Illinois geboren.\nPhysik hat mich schon immer fasziniert.\nDie Frage, woraus Materie wirklich besteht.";

            case 6:
                return "Was Elektrizität eigentlich ist.\nWas hinter den Gleichungen steckt….\n1909 haben mein Doktorand Harvey Fletcher und ich begonnen, diesen Apparat hier zu entwickeln.";

            case 7:
                return "Fletcher hatte die entscheidende Idee: Statt Wasser — Öl.\nUnsere Tröpfchen bleiben stundenlang stabil.\nDas klingt banal.\nAber es hat alles verändert.";

            case 8:
                return "Was wir herausfinden wollten:\nGibt es eine kleinste Einheit elektrischer Ladung — oder ist Elektrizität so etwas wie eine Flüssigkeit, die man beliebig klein aufteilen kann?\nJ.J. Thomson hatte 1897 gezeigt, dass es Elektronen gibt — kleine, negativ geladene Teilchen.";

            case 9:
                return "Aber wie groß ist ihre Ladung?\nDas wusste niemand genau.\nIch wollte es wissen.\nUnd heute erfährst du, wie ich es gemessen habe.";

            case 10:
                return "Hier ist er. Mein Apparat.\nFünf Dinge arbeiten zusammen — und jedes einzelne ist entscheidend.\nIch erkläre dir jede einzelne Komponente nacheinander.";

            case 11:
                return "Dieser einfache Zerstäuber — fast wie ein Parfümflakon — ist der Anfang von allem.\nEin kurzer Druck, und Millionen winziger Öltröpfchen werden in die Messkammer geblasen.\nDurch die Reibung beim Zerstäuben laden sich viele davon elektrisch auf.\nGenau das sind die entscheidenden Öltröpfchen für uns.";

            case 12:
                return "Diese Tröpfchen sind viel zu klein, um sie direkt zu sehen.\nDas Mikroskop macht sie sichtbar — als helle Lichtpunkte auf dunklem Hintergrund.\nAber Vorsicht: Das Mikroskop spiegelt das Bild.";

            case 13:
                return "Was wir sehen, sinkt in Wirklichkeit — es sieht aus, als würde es steigen.\nDas verwirrt am Anfang.\nDeshalb haben wir hier in der Simulation die Öltröpfchen für das bloße Auge sichtbar gemacht und wir sehen direkt, ob die Öltröpfchen sinken oder steigen.";

            case 14:
                return "Das Licht kommt schräg von der Seite.\nOhne es würden wir gar nichts sehen.\nDie Tröpfchen streuen das Licht wie Staubkörner in einem Sonnenstrahl — plötzlich leuchten sie auf.";

            case 15:
                return "Das Herzstück.\nZwei Metallplatten, exakt 6 Millimeter auseinander.\nWenn ich eine Spannung anlege, entsteht zwischen ihnen ein elektrisches Feld — gleichmäßig, kontrolliert.\nDieses Feld wird auf unsere Tröpfchen wirken. Wie stark, das liegt in unserer Hand.";

            case 16:
                return "Dieser Regler ist unser wichtigstes Werkzeug.\nEr bestimmt, wie stark das elektrische Feld zwischen den Platten ist.\nDrehen wir ihn hoch — zieht das Feld geladene Tröpfchen nach oben.\nDrehen wir ihn auf null — fallen sie wieder frei.\nDie richtige Einstellung ist alles.";

            case 17:
                return "Aufgabe: Zerstäuber benutzen\n\nDas Feld ist ausgeschaltet.\nBitte geh mit deiner Hand zum Zerstäuber und drücke den rechten Trigger.";

            case 18:
                return "Wie du siehst werden die ersten Tröpfchen in den Apparat gesprüht.\nDie Tröpfchen fallen.\nLangsam — aber sie fallen.\nDie Schwerkraft zieht sie nach unten.";

            case 19:
                return "Das ist die erste Kraft, mit der wir es zu tun haben.\nWarum ist das wichtig?\nWeil sich aus der Fallgeschwindigkeit eines Tröpfchens der Radius r berechnen lässt.";

            case 20:
                return "Wir benötigen den Radius, um im nächsten Schritt die Ladung bestimmen zu können.\nAber das schauen wir uns später an.\nDie Dichte des Öls kenne ich — 875 Kilogramm pro Kubikmeter.\nDie Erdbeschleunigung kennst du.";

            case 21:
                return "Was ich nicht kenne: den Radius des Tröpfchens.\nDen messe ich aus der Fallgeschwindigkeit.\nSchnelleres Fallen bedeutet: größeres Tröpfchen.\nEinfacher Zusammenhang — aber fundamental wichtig.";

            case 22:
                return "Schau auf die Formel neben dem Experiment.\nDort siehst du die bekannten Werte.";

            case 23:
                return "Ein physischer Slider erscheint im VR-Raum.\nEr ist beschriftet: Tröpfchengröße r.\nLinks: 0,3 µm. Rechts: 2,0 µm.";

            case 24:
                return "Greifen Sie den Regler.\nSchieben Sie ihn nach rechts — das Tröpfchen wird größer.\nBeobachten Sie, wie es schneller fällt.\nNach links — kleiner, langsamer.\n\nDas ist das Stokes'sche Gesetz:\nDie Reibungskraft der Luft hängt vom Radius ab.";

            case 25:
                return GetRadiusTaskText("0,5", "sehr langsam fallend");

            case 26:
                return GetRadiusTaskText("1,0", "mittlere Geschwindigkeit");

            case 27:
                return GetRadiusTaskText("1,5", "schnell fallend");

            case 28:
                return "Gut. Sie verstehen jetzt: Der Radius bestimmt, wie schnell ein Tröpfchen fällt.\nUnd aus der Fallgeschwindigkeit können wir den Radius berechnen.\nJetzt kommt der eigentliche Schritt.";

            case 29:
                return "Wir sehen einen Regler, um die Spannung im Feld einzustellen.\nDreh den Regler mal hoch und achte auf das ausgewählte Tröpfchen.\nDas Tröpfchen verändert seine Geschwindigkeit.";

            case 30:
                return "Die elektrische Kraft — die Coulomb-Kraft — wirkt.\nJe mehr Spannung, desto stärker.";

            case 31:
                return "Dreh den Spannungsregler vor dir langsam nach oben.\nSchau mal, der grüne Pfeil wächst.\nDie elektrische Kraft wird stärker.";

            case 32:
                return "Das Tröpfchen verlangsamt sich.\nWenn die Spannung zu hoch wird, dann steigt das Tröpfchen auf einmal.";

            case 33:
                return "Aufgabe: Neues Öltröpfchen erzeugen\n\nRichte deine Hand auf den Zerstäuber.\nDrücke den rechten Trigger.\n\nMessung: " + measurementsCompleted + "/" + requiredFloatingDroplets;

            case 34:
                return "Aufgabe: Öltröpfchen auswählen\n\nZiele mit dem roten Strahl auf ein Tröpfchen.\nDrücke den rechten Trigger.\n\nMessung: " + measurementsCompleted + "/" + requiredFloatingDroplets;

            case 35:
                return "Aufgabe: Tröpfchen zum Schweben bringen\n\nVersuch es mal so einzustellen, dass du es zum Schweben bringst.\nSo, dass das Tröpfchen hängt, als würde die Zeit stillstehen.\nDas elektrische Feld hält es exakt gegen die Schwerkraft.\nDie beiden Kräfte heben sich exakt auf.\n\nMessung: " + measurementsCompleted + "/" + requiredFloatingDroplets;

            case 36:
                return "Das war deine erste Ladungsmessung.\nAber eine Messung ist noch keine Wissenschaft — das ist nur ein Datenpunkt.\nWas ich brauche, ist ein Muster.";

            case 37:
                return "Schau auf das Formel-Panel.\nDort siehst du das Kräftegleichgewicht und die Bedeutung der Kraftpfeile.";

            case 38:
                return "Und aus diesem Gleichgewicht folgt alles.\nWenn das Tröpfchen schwebt, weiß ich: Die elektrische Kraft ist gleich der Schwerkraft.\nIch kenne die Masse — aus dem Radius, den wir gerade gemessen haben.\nIch kenne den Plattenabstand: 6,00 Millimeter.\nUnd die Spannung lese ich ab. Damit berechne ich die Ladung q.";

            case 39:
                return "Auf dem Panel siehst du jetzt die Größen, die wir für die Berechnung brauchen.";

            case 40:
                return "Was ich brauche, ist ein Muster.\nAlso bring bitte noch 4 weitere Tröpfchen, die du per Klick auswählst, nacheinander zum Schweben.\nDu siehst dann wie die Aufgabenleiste sich füllt.";

            case 41:
                return "Alle fünf Messungen sind abgeschlossen.\nSchau dir nun das Histogramm an.\nDie gemessenen Ladungen werden dort eingetragen.";

            case 42:
                return "Ich habe nicht ein Tröpfchen gemessen.\nIch habe hunderte gemessen. Über Monate.\nUnd dabei etwas Erstaunliches beobachtet:\nDie Ladungen, die ich gemessen habe, waren nie zufällig verteilt.";

            case 43:
                return "Sie häuften sich immer an denselben Stellen.\nImmer ein Vielfaches derselben Grundeinheit.\nEinfach. Doppelt. Dreifach. Viermal. Nie dazwischen.\nDie Natur schien zu zählen — in ganzen Zahlen.";

            case 44:
                return "Elektrische Ladung ist nicht kontinuierlich.\nSie kommt in Paketen.\nDas kleinste Paket — das ist die Elementarladung e.\nJedes Tröpfchen trägt genau ein, zwei, drei oder mehr dieser Pakete.\nNie einen Bruchteil. Das nenne ich Ladungsquantisierung.";

            case 45:
                return "Du hast zuvor den Schwebeversuch bereits 5x durchgeführt\nund siehst deine Messwerte in dem Histogramm.\nSiehst du es?\nDie Ladungen häufen sich.\nNicht zufällig — bei bestimmten Werten.\nBei ganzzahligen Vielfachen.";

            case 46:
                return "Das Muster wird sichtbar.\nDas ist Wissenschaft.\nNicht eine Messung — sondern ein Muster aus vielen Messungen.\nUnd das Muster ist eindeutig:\nElektrische Ladung ist gequantelt.\nEs gibt eine kleinste Einheit.";

            case 47:
                return "Ich muss dir etwas zeigen.\nEtwas, das 1978 ein Historiker namens Gerald Holton entdeckt hat —\nin meinen Original-Notizbüchern aus den Jahren 1911 und 1912.";

            case 48:
                return "Er fand heraus, dass ich weit mehr Tröpfchen gemessen hatte\nals ich je veröffentlicht habe.";

            case 49:
                return "Millikan Notebook · 1911\n\n#47 — q = 1.613 ×10⁻¹⁹ C ✓\n#48 — q = 1.21 ×10⁻¹⁹ C Won't work\n#49 — q = 3.204 ×10⁻¹⁹ C (×2) ✓\n#50 — q = 0.94 ×10⁻¹⁹ C Schiefe Messung";

            case 50:
                return "Millikan Notebook · 1911\n\n#51 — q = 4.836 ×10⁻¹⁹ C (×3) ✓\n#52 — Tropfen verloren Error — discard\n#53 — q = 1.598 ×10⁻¹⁹ C ✓";

            case 51:
                return "Neben manchen Datenpunkten standen meine handschriftlichen Anmerkungen:\n'Won't work'.\n'Schiefe Messung'.\n'Error — discard'.";

            case 52:
                return "War das falsch?\nIch glaube: Nein.\nIch habe Messungen ausgeschlossen, bei denen ich technische Fehler erkannt habe —\nLuftzug, Erschütterungen, einen zitternden Tropfen.";

            case 53:
                return "Das ist kein Betrug.\nDas ist Urteilsvermögen.";

            case 54:
                return "Aber der Historiker Allan Franklin hat 1981 gezeigt:\nDie Daten, die ich wegließ, hätten meinen Endwert kaum verändert.\nNur die statistische Unsicherheit wäre größer geworden —\nvon 0,2% auf fast 2%.";

            case 55:
                return "Die Selektion hat meine Präzision verbessert,\naber nicht mein Ergebnis.";

            case 56:
                return "Die Frage, wann Datenselektion legitim ist,\nbeschäftigt Wissenschaftler bis heute.\nEs gibt keine einfache Antwort.\nAber es gibt eine klare Anforderung:\nTransparenz.";

            case 57:
                return "Was ich ausschließe — und warum —\ndas muss dokumentiert sein.";

            case 58:
                return "Es war 1913.\nIch publizierte meinen Endwert:\ne = 1,592 mal zehn hoch minus neunzehn Coulomb.\nUnsicherheit: 0,2 Prozent.";

            case 59:
                return "Es ist die genaueste Messung der Elementarladung,\ndie es bis dahin gibt.\nDer heute akzeptierte Wert ist 1,602.";

            case 60:
                return "Die Abweichung kommt aus einem leicht falschen Literaturwert\nfür die Luftviskosität,\nden ich damals verwendet habe.\nNicht aus meiner Methode.";

            case 61:
                return "Aber der eigentliche Beitrag ist nicht die Zahl.\nEs ist das Prinzip.\nElektrische Ladung ist gequantelt.";

            case 62:
                return "Es gibt keine halbe Elementarladung.\nKeine viertel Elementarladung.\nDie Natur zählt in ganzen Zahlen.";

            case 63:
                return "Das ist fundamental.\nDas ist eine der tiefsten Strukturen der Materie.";

            case 64:
                return "Seit 1995 haben automatisierte Experimente\nüber hundert Millionen Öltropfen vermessen.\nKein Hinweis auf Bruchladungen.\nKein einziger.";

            case 65:
                return "Du hast es mit deinen Messungen hier auch nochmal in der Neuzeit bewiesen,\ndurch die schwebenden Tröpfchen,\ndass meine Theorie stimmt.";

            case 66:
                return "Ich danke dir dafür.\nDu hast heute sehr viel gelernt,\nlass uns jetzt mal schauen wie viel du davon behalten hast.\nGern, darfst du jederzeit wieder vorbei kommen und mit mir experimentieren.\nIch freu mich drauf!";

            default:
                return "";
        }
    }

    private string GetRadiusTaskText(string target, string label)
    {
        if (currentRadiusTaskDone)
        {
            return "Aufgabe abgeschlossen:\n\n" +
                   "<b><color=#00AA00>r = " + target + " µm → " + label + "</color></b>\n\n" +
                   "Drücke A, um fortzufahren.";
        }

        return "Aufgabe:\n\n" +
               "r = " + target + " µm → " + label + "\n\n" +
               "Stelle den Radius mit dem rechten Controller ein.\n" +
               "Drücke danach den rechten Trigger am Zerstäuber.\n" +
               "Der Tropfen muss in den Plattenkondensator gelangen.\n" +
               "Du kannst mehrmals sprühen und die Tropfen beobachten.";
    }

    private string GetButtonHintForCurrentStep()
    {
        if (currentStep == 3)
            return "A: Ja, ich helfe dir!";

        if (currentStep == 4)
            return "A: Erst mehr erfahren — wer bist du?";

        if (IsBlockingStep(currentStep))
            return "";

        if ((currentStep == 25 || currentStep == 26 || currentStep == 27) && !currentRadiusTaskDone)
            return "";

        return "A: Weiter";
    }

    private string GetGuideSprayer()
    {
        return "Zerstäuber\n\n" +
               "Zielen + rechter Trigger:\n" +
               "Öltröpfchen erzeugen\n\n" +
               "Das elektrische Feld ist jetzt ausgeschaltet.";
    }

    private string GetGuideGravityFormula()
    {
        return "Schwerkraft und Radius\n\n" +
               "F_G = m · g\n" +
               "m = ρ_Öl · (4/3)πr³\n\n" +
               "ρ_Öl = 875 kg/m³\n" +
               "g = 9,81 m/s²\n\n" +
               "Nur r ist unbekannt.";
    }

    private string GetGuideRadiusSlider()
    {
        return "Tröpfchengröße r\n\n" +
               "Slider links: 0,3 µm\n" +
               "Slider rechts: 2,0 µm\n\n" +
               "Rechter Controller:\n" +
               "auf den Slider zeigen und Trigger halten.\n\n" +
               "Nach rechts: größer\n" +
               "Nach links: kleiner";
    }

    private string GetGuideRadiusTask(string target, string label)
    {
        return "Radius-Aufgabe\n\n" +
               "Ziel:\n" +
               "r = " + target + " µm\n" +
               label + "\n\n" +
               "1. Radius einstellen\n" +
               "2. Öltröpfchen sprühen\n" +
               "3. Tropfen muss in den Plattenkondensator gelangen\n\n" +
               "Du kannst mehrmals sprühen.";
    }

    private string GetGuideMeasurementSpray()
    {
        return "Messung starten\n\n" +
               "Zerstäuber:\n" +
               "Zielen + rechter Trigger\n\n" +
               "Erzeuge neue Öltröpfchen.\n\n" +
               "Danach wählst du ein Tröpfchen aus.";
    }

    private string GetGuideMeasurementSelect()
    {
        return "Tröpfchen auswählen\n\n" +
               "Roter Strahl + rechter Trigger:\n" +
               "Tröpfchen auswählen\n\n" +
               "Die Auswahl wird gelb markiert.\n\n" +
               "Die Daten erscheinen im Parameterpanel.";
    }

    private string GetGuideVoltageTask()
    {
        return "Schwebezustand\n\n" +
               "Spannungsregler greifen\n" +
               "Hand links / rechts bewegen:\n" +
               "Spannung ändern\n\n" +
               "X halten:\n" +
               "Feineinstellung\n\n" +
               "Ziel:\n" +
               "Das Tröpfchen soll weder deutlich steigen noch deutlich fallen.";
    }

    private string GetGuideElectricFormula()
    {
        return "Kräftegleichgewicht\n\n" +
               "F_el = F_G\n\n" +
               "F_el = q · E\n" +
               "E = U / d\n\n" +
               "q · U / d = m · g\n" +
               "q = m · g · d / U\n\n" +
               "Grün: elektrische Kraft F_el\n" +
               "Blau: Gewichtskraft F_G";
    }

    private string GetGuideParameterExplanation()
    {
        return "Messgrößen\n\n" +
               "q = Ladung des Tröpfchens [C]\n" +
               "U = Spannung [V]\n" +
               "d = Plattenabstand = 6,00 mm\n" +
               "m = Masse des Tröpfchens [kg]\n" +
               "g = 9,81 m/s²\n" +
               "E = U / d";
    }

    private string GetGuideHistogram()
    {
        if (histogramPanel != null)
            return histogramPanel.GetHistogramText();

        return "Ladungsverteilung\n\nNoch keine Messwerte vorhanden.";
    }

    private void UpdateArrowForStep(int step)
    {
        HideAllArrows();

        switch (step)
        {
            case 10:
                if (arrowSetup != null) arrowSetup.SetActive(true);
                break;

            case 11:
            case 17:
            case 33:
                if (arrowSprayer != null) arrowSprayer.SetActive(true);
                break;

            case 14:
                if (arrowLight != null) arrowLight.SetActive(true);
                break;

            case 15:
            case 29:
            case 30:
            case 31:
            case 32:
            case 37:
            case 38:
                if (arrowCapacitor != null) arrowCapacitor.SetActive(true);
                break;

            case 34:
                if (arrowSelectDrop != null) arrowSelectDrop.SetActive(true);
                break;

            case 35:
                if (arrowVoltageKnob != null) arrowVoltageKnob.SetActive(true);
                break;
        }
    }

    private void HideAllTemporaryUI()
    {
        HideParameterPanel();
        HideGuideUI();
        HideForceArrows();
        HideAllArrows();

        if (notebookObject != null)
            notebookObject.SetActive(false);
    }

    private void ShowParameterPanel()
    {
        if (parameterPanelRoot != null)
            parameterPanelRoot.SetActive(true);
    }

    private void HideParameterPanel()
    {
        if (parameterPanelRoot != null)
            parameterPanelRoot.SetActive(false);
    }

    private void ShowGuideUI(string text)
    {
        if (guideUIRoot != null)
            guideUIRoot.SetActive(true);

        if (guideUIText != null)
            guideUIText.text = text;
    }

    private void HideGuideUI()
    {
        if (guideUIRoot != null)
            guideUIRoot.SetActive(false);
    }

    private void ShowForceArrows()
    {
        if (forceArrowsRoot != null)
            forceArrowsRoot.SetActive(true);
    }

    private void HideForceArrows()
    {
        if (forceArrowsRoot != null)
            forceArrowsRoot.SetActive(false);
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

    private void SetVoltageInteraction(bool enabled)
    {
        if (voltageKnobInput != null)
            voltageKnobInput.enabled = enabled;
    }

    private void SetSelectionInteraction(bool enabled)
    {
        if (selectionManager != null)
            selectionManager.SetSelectionEnabled(enabled);
    }

    private void ResetVoltageToZero()
    {
        VoltageKnobInput v = voltageKnobInput as VoltageKnobInput;

        if (v != null)
            v.ResetVoltageToZero();
    }

    private void ReturnToStoryStart()
    {
        if (playerRoot != null && storyReturnPoint != null)
        {
            playerRoot.position = storyReturnPoint.position;
            playerRoot.rotation = storyReturnPoint.rotation;
        }
    }

    private void StartPostQuiz()
    {
        if (quizController != null)
            quizController.StartPostQuiz();
        else
            Debug.LogWarning("[BottomTutorialController] QuizController is not assigned.");
    }

    private void PlayBookSound()
    {
        if (pageSfxSource != null && bookPageSfx != null)
            pageSfxSource.PlayOneShot(bookPageSfx);
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