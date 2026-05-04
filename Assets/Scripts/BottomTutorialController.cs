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

    [Header("Spray Teaching Radius")]
    public SpraySpawner spraySpawner;

    [Header("Disable These During Tutorial")]
    public Behaviour[] componentsToDisableDuringTutorial;

    public static bool TutorialInputLocked { get; private set; }

    public bool IsSessionActive => tutorialSessionActive;

    private bool tutorialSessionActive = false;
    private int currentStep = 0;

    private const int LastStepIndex = 43;

    private void Start()
    {
        if (dialogueRoot == null)
            dialogueRoot = gameObject;

        ResetTutorialProgress();

        tutorialSessionActive = false;
        TutorialInputLocked = false;

        SetTutorialInputComponentsEnabled(true);
        HideAllArrows();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void Update()
    {
        if (!tutorialSessionActive)
            return;

        if (OVRInput.GetDown(OVRInput.Button.One))
            TryAdvanceFromPlayerReply();
    }

    public void BeginTutorialSession()
    {
        ResetTutorialProgress();

        tutorialSessionActive = true;
        TutorialInputLocked = true;

        SetTutorialInputComponentsEnabled(false);
        HideAllArrows();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);

        ShowStep();
    }

    public void EndTutorialSession()
    {
        tutorialSessionActive = false;
        TutorialInputLocked = false;

        if (spraySpawner != null)
            spraySpawner.ReturnToRandomModeAndClearDrops();

        SetTutorialInputComponentsEnabled(true);
        HideAllArrows();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        ResetTutorialProgress();
    }

    private void ResetTutorialProgress()
    {
        currentStep = 0;
    }

    private void TryAdvanceFromPlayerReply()
    {
        if (IsTaskStep(currentStep))
        {
            RefreshTaskHint();
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

        if (dialogueText != null)
            dialogueText.text = GetDialogueForCurrentStep();

        if (buttonHintText != null)
            buttonHintText.text = GetButtonHintForCurrentStep();

        UpdateArrowForStep(currentStep);
    }

    private void ApplyStepSideEffects(int step)
    {
        if (spraySpawner == null)
        {
            if (step == 15 || step == 17 || step == 19 || step == 21)
                Debug.LogWarning("[BottomTutorialController] SpraySpawner is not assigned.");

            return;
        }

        switch (step)
        {
            case 15:
                spraySpawner.SetTeachingRadiusStep(0);
                break;

            case 17:
                spraySpawner.SetTeachingRadiusStep(1);
                break;

            case 19:
                spraySpawner.SetTeachingRadiusStep(2);
                break;

            case 21:
                spraySpawner.ReturnToRandomModeAndClearDrops();
                break;
        }
    }

    private bool IsTaskStep(int step)
    {
        return step == 15 || step == 17 || step == 19 || step == 23 || step == 25 || step == 29;
    }

    private void RefreshTaskHint()
    {
        if (buttonHintText != null)
            buttonHintText.text = GetButtonHintForCurrentStep();
    }

    private string GetDialogueForCurrentStep()
    {
        switch (currentStep)
        {
            case 0:
                return "Chicago, Herbst 1909. In einem Kellerlabor der University of Chicago untersuchte ich einen einzelnen Öltropfen. Der echte Apparat war viel kleiner als das, was du hier siehst.";

            case 1:
                return "Die Messkammer war kaum größer als ein Schuhkarton. In dieser VR-Version ist der Aufbau stark vergrößert, damit du die Platten, die Lichtquelle und die Öltröpfchen besser erkennen und selbst bedienen kannst.";

            case 2:
                return "Mein Name ist Robert Andrews Millikan. Ich wollte herausfinden, ob elektrische Ladung beliebig teilbar ist oder ob sie aus kleinsten Einheiten besteht.";

            case 3:
                return "Mein Doktorand Harvey Fletcher hatte eine entscheidende Idee: Öl statt Wasser. Wasser verdunstet zu schnell. Öltröpfchen bleiben länger stabil.";

            case 4:
                return "Du übernimmst heute die Rolle meines Assistenten. Du bedienst den Apparat, ich erkläre dir, was dabei geschieht. Wenn wir sorgfältig arbeiten, können wir die Elementarladung sichtbar machen.";

            case 5:
                return "Bevor wir messen, sehen wir uns den Aufbau an. Fünf Bestandteile arbeiten zusammen: Zerstäuber, Mikroskop, Lichtquelle, Plattenkondensator und Spannungsquelle.";

            case 6:
                return "Das ist der Zerstäuber. Mit ihm erzeugen wir winzige Öltröpfchen. Beim Zerstäuben laden sich einige Tröpfchen elektrisch auf. Genau solche geladenen Tröpfchen brauchen wir.";

            case 7:
                return "Das Mikroskop machte die Tröpfchen im echten Experiment sichtbar. Die Beobachtung war damals schwieriger als hier, denn die Tröpfchen waren viel zu klein, um sie direkt mit bloßem Auge zu sehen.";

            case 8:
                return "In dieser VR-Version vereinfachen wir die Beobachtung. Du siehst direkt, ob ein Tröpfchen fällt, schwebt oder steigt.";

            case 9:
                return "Die Lichtquelle beleuchtet die Kammer von der Seite. Ohne Licht würden wir die Tröpfchen kaum erkennen. Mit Licht erscheinen sie als kleine helle Punkte.";

            case 10:
                return "Das ist der Plattenkondensator. Zwischen den beiden Platten entsteht ein elektrisches Feld. Der Abstand beträgt in dieser Simulation 6 Millimeter.";

            case 11:
                return "Dieser Regler ist unser wichtigstes Werkzeug. Mit ihm ändern wir die Spannung. Je höher die Spannung, desto stärker wird die elektrische Kraft auf das geladene Tröpfchen.";

            case 12:
                return "Bevor wir das elektrische Feld benutzen, beobachten wir den Tropfen ohne Feld. Ohne elektrische Kraft fällt er nach unten. Die Schwerkraft zieht ihn zur Erde.";

            case 13:
                return "Ein kleiner Tropfen fällt in Luft nicht einfach immer schneller. Die Luft bremst ihn. Nach kurzer Zeit fällt er fast mit konstanter Geschwindigkeit.";

            case 14:
                return "Diese Fallgeschwindigkeit ist wichtig. Sie verrät uns etwas über den Radius des Tröpfchens. Aus dem Radius folgt die Masse, und ohne Masse können wir später die Ladung nicht berechnen.";

            case 15:
                return "Zuerst erzeugen wir eine Gruppe sehr kleiner Tröpfchen mit r = 0,5 µm. Du musst hier noch keine Spannung einstellen. Beobachte nur, wie langsam diese kleinen Tröpfchen fallen.";

            case 16:
                return "Gut. Diese kleinen Tröpfchen haben nur eine sehr geringe Masse. Die Gewichtskraft ist entsprechend klein, und die Fallbewegung ist langsam.";

            case 17:
                return "Jetzt erzeugen wir eine neue Gruppe mit r = 1,0 µm. Die alten Tröpfchen werden automatisch entfernt. Vergleiche nur die Fallbewegung mit der vorherigen Gruppe.";

            case 18:
                return "Du siehst: Wenn der Radius größer wird, wächst die Masse deutlich. Für eine Kugel hängt die Masse vom Volumen ab, und das Volumen wächst mit r hoch 3.";

            case 19:
                return "Nun erzeugen wir eine dritte Gruppe mit r = 1,5 µm. Auch hier musst du noch keine Spannung einstellen. Vergleiche wieder nur die Fallgeschwindigkeit.";

            case 20:
                return "Der Radius ist also nicht nur eine Größenangabe. Er bestimmt die Masse, die Gewichtskraft und die spätere Schwebe-Spannung. Darum ist r ein wichtiger Messwert.";

            case 21:
                return "Die Radius-Vergleiche sind abgeschlossen. Für die eigentliche Messung verwenden wir wieder normale, zufällig erzeugte Öltröpfchen.";

            case 22:
                return "Jetzt beginnen wir mit der eigentlichen Messung. Wir brauchen ein geladenes Öltröpfchen im Apparat.";

            case 23:
                return "Erzeuge nun eine neue zufällige Gruppe von Öltröpfchen. Ab jetzt sind Radius und Ladung wieder zufällig, so wie es im echten Experiment auch der Fall wäre.";

            case 24:
                return "Sehr gut. Jetzt wählen wir ein einzelnes Tröpfchen aus, damit wir seine Bewegung genauer verfolgen können.";

            case 25:
                return "Wähle nun ein Tröpfchen mit dem roten Strahl aus. Sobald es ausgewählt ist, können wir seine Daten sehen und die Spannung einstellen.";

            case 26:
                return "Jetzt sehen wir Radius, Masse, Ladung und Spannung des ausgewählten Tröpfchens. Diese Werte gehören zu genau diesem einen Tropfen.";

            case 27:
                return "Wenn wir eine Spannung anlegen, entsteht zwischen den Platten ein elektrisches Feld. Auf ein geladenes Tröpfchen wirkt dann eine elektrische Kraft.";

            case 28:
                return "Beobachte die Kräfte: Die Gewichtskraft zeigt nach unten. Der Auftrieb und die elektrische Kraft zeigen nach oben. Wenn die oberen Kräfte zusammen gleich groß sind wie die Gewichtskraft, schwebt der Tropfen.";

            case 29:
                return "Stelle nun die Spannung so ein, dass das Tröpfchen schwebt. Fällt es, ist die elektrische Kraft zu klein. Steigt es, ist sie zu groß. Dein Ziel ist der Schwebezustand.";

            case 30:
                return "Sehr gut. Jetzt schwebt das Tröpfchen. Das bedeutet: Die elektrische Kraft gleicht die Gewichtskraft aus.";

            case 31:
                return "Aus diesem Gleichgewicht folgt die Ladung. Wenn der Tropfen schwebt, gilt F_el = F_G. Daraus folgt: q = m · g · d / U.";

            case 32:
                return "Eine Messung allein reicht nicht. Ich habe viele Tröpfchen gemessen, nicht eines und nicht zehn, sondern Hunderte.";

            case 33:
                return "Jedes Tröpfchen liefert eine Ladung q. Wenn man viele Werte sammelt, entsteht ein Muster.";

            case 34:
                return "Die Ladungen liegen nicht beliebig verteilt. Sie erscheinen als Vielfache desselben Grundwertes: einmal, zweimal, dreimal. Nie dazwischen.";

            case 35:
                return "Die Natur zählt hier in ganzen Zahlen. Elektrische Ladung ist nicht kontinuierlich, sondern gequantelt. Das kleinste Paket nennen wir Elementarladung: e.";

            case 36:
                return "Heute wissen wir: e beträgt ungefähr 1,602 x 10^-19 Coulomb. Jedes Tröpfchen trägt ein ganzzahliges Vielfaches davon: 1e, 2e, 3e und so weiter.";

            case 37:
                return "Viele Jahre später untersuchte der Wissenschaftshistoriker Gerald Holton meine Originalnotizbücher. Er fand mehr Messungen, als ich veröffentlicht hatte.";

            case 38:
                return "Manche Messungen waren technisch fehlerhaft: Luftzug, Erschütterung oder ein zitternder Tropfen. Solche Daten auszuschließen kann wissenschaftlich begründet sein.";

            case 39:
                return "Später zeigte Allan Franklin, dass die ausgelassenen Werte den Endwert kaum verändert hätten. Aber die Unsicherheit wäre größer gewesen. Deshalb ist Transparenz wichtig.";

            case 40:
                return "1913 veröffentlichte ich meinen Messwert für die Elementarladung: e = 1,592 x 10^-19 Coulomb. Der heute akzeptierte Wert liegt bei etwa 1,602 x 10^-19 Coulomb.";

            case 41:
                return "1923 erhielt ich dafür den Nobelpreis für Physik. Aber das Wichtigste war nicht nur die Zahl. Das Wichtigste war der Nachweis, dass elektrische Ladung gequantelt ist.";

            case 42:
                return "Auch Harvey Fletcher gehört zu dieser Geschichte. Ohne seine Idee, Öl statt Wasser zu verwenden, wäre dieser Versuch kaum möglich gewesen.";

            case 43:
                return "Heute hast du gesehen, wie aus einem schwebenden Tropfen ein Beweis für eine grundlegende Eigenschaft der Natur wird. Das ist die Idee der Elementarladung.";

            default:
                return "";
        }
    }

    private string GetButtonHintForCurrentStep()
    {
        switch (currentStep)
        {
            case 15:
                return "Aufgabe: Sprühe eine Gruppe mit r = 0,5 µm.\nRichte auf den Zerstäuber.\nDrücke den Trigger.";

            case 17:
                return "Aufgabe: Sprühe eine Gruppe mit r = 1,0 µm.\nRichte auf den Zerstäuber.\nDrücke den Trigger.";

            case 19:
                return "Aufgabe: Sprühe eine Gruppe mit r = 1,5 µm.\nRichte auf den Zerstäuber.\nDrücke den Trigger.";

            case 23:
                return "Aufgabe: Sprühe eine zufällige Gruppe.\nRichte auf den Zerstäuber.\nDrücke den Trigger.";

            case 25:
                return "Aufgabe: Wähle ein Öltröpfchen aus.\nZiele mit dem roten Strahl darauf.\nDrücke den Trigger.";

            case 29:
                return "Aufgabe: Stelle die Schwebe-Spannung ein.\nGreife den Spannungsregler.\nMit X kannst du feiner nachregeln.";

            default:
                return "A: Weiter";
        }
    }

    private void UpdateArrowForStep(int step)
    {
        HideAllArrows();

        switch (step)
        {
            case 5:
                if (arrowSetup != null) arrowSetup.SetActive(true);
                break;

            case 6:
            case 15:
            case 17:
            case 19:
            case 23:
                if (arrowSprayer != null) arrowSprayer.SetActive(true);
                break;

            case 9:
                if (arrowLight != null) arrowLight.SetActive(true);
                break;

            case 10:
            case 12:
            case 27:
            case 28:
                if (arrowCapacitor != null) arrowCapacitor.SetActive(true);
                break;

            case 11:
            case 29:
                if (arrowVoltageKnob != null) arrowVoltageKnob.SetActive(true);
                break;

            case 25:
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

    public void NotifyDropletTriggered()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 15 || currentStep == 17 || currentStep == 19 || currentStep == 23)
            NextStep();
    }

    public void NotifyDropSelected()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 25)
            NextStep();
    }

    public void NotifyVoltageSolved()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 29)
            NextStep();
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