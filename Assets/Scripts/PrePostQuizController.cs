using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class PrePostQuizController : MonoBehaviour
{
    [Serializable]
    public class QuizQuestion
    {
        [TextArea(2, 5)] public string question;
        [TextArea(1, 3)] public string answerA;
        [TextArea(1, 3)] public string answerB;
        [TextArea(1, 3)] public string answerC;
        [Range(0, 2)] public int correctIndex;
    }

    public enum QuizMode
    {
        PreQuiz,
        PostQuiz
    }

    [Header("Root")]
    public GameObject wallRoot;
    public GameObject quizGroup;
    public GameObject resultGroup;

    [Header("Texts")]
    public TMP_Text modeText;
    public TMP_Text questionCounterText;
    public TMP_Text questionText;
    public TMP_Text answerTextA;
    public TMP_Text answerTextB;
    public TMP_Text answerTextC;
    public TMP_Text scoreText;
    public TMP_Text resultText;
    public TMP_Text continueText;

    [Header("Buttons")]
    public Button answerButtonA;
    public Button answerButtonB;
    public Button answerButtonC;
    public Button continueButton;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public float postFeedbackSeconds = 0.8f;
    public bool keepInspectorHighlightColor = true;

    [Header("Flow")]
    public bool startPreQuizOnStart = true;
    public Behaviour[] behavioursToEnableAfterPreQuiz;
    public GameObject[] objectsToEnableAfterPreQuiz;

    [Header("Events")]
    public UnityEvent onPreQuizCompleted;
    public UnityEvent onPostQuizCompleted;

    private readonly List<QuizQuestion> questions = new List<QuizQuestion>();
    private readonly List<int> preAnswers = new List<int>();
    private readonly List<int> postAnswers = new List<int>();

    private QuizMode currentMode;
    private int currentQuestionIndex;
    private int correctPostCount;
    private bool waitingForPostFeedback;
    private Coroutine feedbackRoutine;

    private Image imageA;
    private Image imageB;
    private Image imageC;

    public IReadOnlyList<int> PreAnswers => preAnswers;
    public IReadOnlyList<int> PostAnswers => postAnswers;
    public int CorrectPostCount => correctPostCount;
    public int QuestionCount => questions.Count;
    public QuizMode CurrentMode => currentMode;

    private void Awake()
    {
        if (wallRoot == null)
            wallRoot = gameObject;

        CacheButtonImages();
        SetupQuestions();
        SetupButtons();

        if (keepInspectorHighlightColor)
            KeepHighlightedColorFromInspector();
    }

    private void Start()
    {
        if (startPreQuizOnStart)
            StartPreQuiz();
        else
            HideWall();
    }

    private void CacheButtonImages()
    {
        if (answerButtonA != null)
            imageA = answerButtonA.GetComponent<Image>();

        if (answerButtonB != null)
            imageB = answerButtonB.GetComponent<Image>();

        if (answerButtonC != null)
            imageC = answerButtonC.GetComponent<Image>();
    }

    private void SetupButtons()
    {
        if (answerButtonA != null)
        {
            answerButtonA.onClick.RemoveAllListeners();
            answerButtonA.onClick.AddListener(() => SelectAnswer(0));
        }

        if (answerButtonB != null)
        {
            answerButtonB.onClick.RemoveAllListeners();
            answerButtonB.onClick.AddListener(() => SelectAnswer(1));
        }

        if (answerButtonC != null)
        {
            answerButtonC.onClick.RemoveAllListeners();
            answerButtonC.onClick.AddListener(() => SelectAnswer(2));
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(ContinueAfterResult);
        }
    }

    private void KeepHighlightedColorFromInspector()
    {
        // 不再覆盖 highlightedColor。
        // 只把 selectedColor 调成 normalColor，避免按钮点完后一直停留在选中颜色。
        KeepHighlight(answerButtonA);
        KeepHighlight(answerButtonB);
        KeepHighlight(answerButtonC);
        KeepHighlight(continueButton);
    }

    private void KeepHighlight(Button button)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
    }

    public void StartPreQuiz()
    {
        StopFeedbackRoutineIfNeeded();

        currentMode = QuizMode.PreQuiz;
        currentQuestionIndex = 0;
        correctPostCount = 0;
        waitingForPostFeedback = false;

        preAnswers.Clear();

        SetAfterPreQuizUnlocked(false);
        ShowWall();
        ShowQuizGroup();
        ShowCurrentQuestion();
    }

    public void StartPostQuiz()
    {
        StopFeedbackRoutineIfNeeded();

        currentMode = QuizMode.PostQuiz;
        currentQuestionIndex = 0;
        correctPostCount = 0;
        waitingForPostFeedback = false;

        postAnswers.Clear();

        ShowWall();
        ShowQuizGroup();
        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        if (currentQuestionIndex < 0 || currentQuestionIndex >= questions.Count)
        {
            FinishQuiz();
            return;
        }

        ClearButtonSelection();
        ResetButtonColors();
        SetAnswerButtonsInteractable(true);

        QuizQuestion q = questions[currentQuestionIndex];

        if (modeText != null)
        {
            modeText.text = currentMode == QuizMode.PreQuiz
                ? "Vorwissenstest"
                : "Wissenstest nach dem Experiment";
        }

        if (questionCounterText != null)
            questionCounterText.text = "Frage " + (currentQuestionIndex + 1) + "/" + questions.Count;

        if (questionText != null)
            questionText.text = q.question;

        if (answerTextA != null)
            answerTextA.text = "A. " + q.answerA;

        if (answerTextB != null)
            answerTextB.text = "B. " + q.answerB;

        if (answerTextC != null)
            answerTextC.text = "C. " + q.answerC;

        RefreshScoreText();

        Canvas.ForceUpdateCanvases();
    }

    private void SelectAnswer(int selectedIndex)
    {
        if (waitingForPostFeedback)
            return;

        if (currentQuestionIndex < 0 || currentQuestionIndex >= questions.Count)
            return;

        QuizQuestion q = questions[currentQuestionIndex];

        if (currentMode == QuizMode.PreQuiz)
        {
            preAnswers.Add(selectedIndex);
            ClearButtonSelection();

            currentQuestionIndex++;
            ShowCurrentQuestion();
            return;
        }

        postAnswers.Add(selectedIndex);

        bool isCorrect = selectedIndex == q.correctIndex;

        if (isCorrect)
            correctPostCount++;

        ShowPostFeedback(selectedIndex, q.correctIndex);
        RefreshScoreText();

        waitingForPostFeedback = true;
        SetAnswerButtonsInteractable(false);

        feedbackRoutine = StartCoroutine(GoToNextPostQuestionAfterDelay());
    }

    private IEnumerator GoToNextPostQuestionAfterDelay()
    {
        yield return new WaitForSeconds(postFeedbackSeconds);

        waitingForPostFeedback = false;
        feedbackRoutine = null;

        ClearButtonSelection();

        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    private void ShowPostFeedback(int selectedIndex, int correctIndex)
    {
        ResetButtonColors();

        SetButtonColor(correctIndex, correctColor);

        if (selectedIndex != correctIndex)
            SetButtonColor(selectedIndex, wrongColor);
    }

    private void RefreshScoreText()
    {
        if (scoreText == null)
            return;

        if (currentMode == QuizMode.PreQuiz)
        {
            scoreText.text = "";
            return;
        }

        scoreText.text = "Richtig: " + correctPostCount + "/" + questions.Count;
    }

    private void FinishQuiz()
    {
        StopFeedbackRoutineIfNeeded();
        ClearButtonSelection();
        ResetButtonColors();
        SetAnswerButtonsInteractable(false);

        if (currentMode == QuizMode.PreQuiz)
        {
            SetAfterPreQuizUnlocked(true);
            HideWall();
            onPreQuizCompleted?.Invoke();
            return;
        }

        ShowResultGroup();

        if (modeText != null)
            modeText.text = "Wissenstest nach dem Experiment";

        if (scoreText != null)
            scoreText.text = "Richtig: " + correctPostCount + "/" + questions.Count;

        if (resultText != null)
            resultText.text = "Ergebnis: " + correctPostCount + "/" + questions.Count + " richtig.";

        if (continueText != null)
            continueText.text = "Weiter";
    }

    private void ContinueAfterResult()
    {
        ClearButtonSelection();
        HideWall();

        if (currentMode == QuizMode.PostQuiz)
            onPostQuizCompleted?.Invoke();
    }

    private void ShowWall()
    {
        if (wallRoot != null)
            wallRoot.SetActive(true);
    }

    private void HideWall()
    {
        if (wallRoot != null)
            wallRoot.SetActive(false);
    }

    private void ShowQuizGroup()
    {
        if (quizGroup != null)
            quizGroup.SetActive(true);

        if (resultGroup != null)
            resultGroup.SetActive(false);
    }

    private void ShowResultGroup()
    {
        if (quizGroup != null)
            quizGroup.SetActive(false);

        if (resultGroup != null)
            resultGroup.SetActive(true);
    }

    private void ResetButtonColors()
    {
        if (imageA != null)
            imageA.color = normalColor;

        if (imageB != null)
            imageB.color = normalColor;

        if (imageC != null)
            imageC.color = normalColor;
    }

    private void SetButtonColor(int index, Color color)
    {
        if (index == 0 && imageA != null)
            imageA.color = color;
        else if (index == 1 && imageB != null)
            imageB.color = color;
        else if (index == 2 && imageC != null)
            imageC.color = color;
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        if (answerButtonA != null)
            answerButtonA.interactable = interactable;

        if (answerButtonB != null)
            answerButtonB.interactable = interactable;

        if (answerButtonC != null)
            answerButtonC.interactable = interactable;
    }

    private void ClearButtonSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (answerButtonA != null)
            answerButtonA.OnDeselect(null);

        if (answerButtonB != null)
            answerButtonB.OnDeselect(null);

        if (answerButtonC != null)
            answerButtonC.OnDeselect(null);

        if (continueButton != null)
            continueButton.OnDeselect(null);
    }

    private void StopFeedbackRoutineIfNeeded()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        waitingForPostFeedback = false;
    }

    private void SetAfterPreQuizUnlocked(bool unlocked)
    {
        if (behavioursToEnableAfterPreQuiz != null)
        {
            for (int i = 0; i < behavioursToEnableAfterPreQuiz.Length; i++)
            {
                if (behavioursToEnableAfterPreQuiz[i] != null)
                    behavioursToEnableAfterPreQuiz[i].enabled = unlocked;
            }
        }

        if (objectsToEnableAfterPreQuiz != null)
        {
            for (int i = 0; i < objectsToEnableAfterPreQuiz.Length; i++)
            {
                if (objectsToEnableAfterPreQuiz[i] != null)
                    objectsToEnableAfterPreQuiz[i].SetActive(unlocked);
            }
        }
    }

    private void SetupQuestions()
    {
        questions.Clear();

        questions.Add(new QuizQuestion
        {
            question = "Wie war die zentrale Forschungsfrage, die Robert Millikan mit seinem Experiment beantworten wollte?",
            answerA = "Können Öltröpfchen durch reine Lichtenergie in der Schwebe gehalten werden?",
            answerB = "Wie groß ist die Masse eines Elektrons im Vergleich zu einem Öltröpfchen?",
            answerC = "Ist elektrische Ladung diskret in unteilbaren Einheiten aufgebaut oder kontinuierlich?",
            correctIndex = 2
        });

        questions.Add(new QuizQuestion
        {
            question = "Welche Beobachtung macht man beim Blick durch das Mikroskop auf die fallenden Tröpfchen?",
            answerA = "Die Tröpfchen scheinen nach oben zu steigen, da das Mikroskop das Bild spiegelt.",
            answerB = "Man kann die exakte Anzahl der Elektronen auf der Oberfläche des Tropfens sehen.",
            answerC = "Die Tröpfchen erscheinen als farbige Ringe aufgrund der Lichtbrechung.",
            correctIndex = 0
        });

        questions.Add(new QuizQuestion
        {
            question = "Warum schlug Millikans Doktorand Harvey Fletcher die Verwendung von Öl anstelle von Wasser vor?",
            answerA = "Öltröpfchen bleiben stundenlang stabil und verdunsten nicht so schnell wie Wasser.",
            answerB = "Öl lässt sich durch Reibung wesentlich schneller aufladen als Wasser.",
            answerC = "Die Dichte von Öl entspricht exakt der Erdbeschleunigung g.",
            correctIndex = 0
        });

        questions.Add(new QuizQuestion
        {
            question = "Wie wird im Millikan-Versuch der Radius r eines Öltröpfchens bestimmt?",
            answerA = "Aus der Fallgeschwindigkeit des Tröpfchens bei ausgeschaltetem elektrischen Feld.",
            answerB = "Durch die Messung der Zeit, die das Tröpfchen zum Schweben benötigt.",
            answerC = "Durch das Ablesen an einer Skala direkt auf der Oberfläche des Zerstäubers.",
            correctIndex = 0
        });

        questions.Add(new QuizQuestion
        {
            question = "Welcher Zustand muss erreicht sein, damit ein Tröpfchen in der Kammer schwebt?",
            answerA = "Der Zerstäuber muss einen konstanten Luftstrom erzeugen, der das Tröpfchen trägt.",
            answerB = "Die elektrische Kraft Fel muss die Gewichtskraft FG exakt ausgleichen.",
            answerC = "Die Spannung muss auf den maximalen Wert der Spannungsquelle eingestellt sein.",
            correctIndex = 1
        });

        questions.Add(new QuizQuestion
        {
            question = "Was besagt das Prinzip der Ladungsquantisierung?",
            answerA = "Elektrische Ladung kann in beliebig kleine Bruchstücke unterteilt werden.",
            answerB = "Jede gemessene Ladung ist immer ein ganzzahliges Vielfaches der Elementarladung e.",
            answerC = "Die Ladung eines Tröpfchens nimmt stetig ab, je länger es in der Kammer schwebt.",
            correctIndex = 1
        });

        questions.Add(new QuizQuestion
        {
            question = "Warum schloss Millikan bestimmte Messwerte aus seinen Veröffentlichungen aus, wie in seinen Notizbüchern entdeckt wurde?",
            answerA = "Er wollte die Ergebnisse seines Doktoranden Harvey Fletcher absichtlich fälschen.",
            answerB = "Er erkannte technische Fehler wie Luftzüge oder Erschütterungen während dieser Messungen.",
            answerC = "Die ausgeschlossenen Werte waren mathematisch nicht berechenbar.",
            correctIndex = 1
        });

        questions.Add(new QuizQuestion
        {
            question = "Welcher physikalische Parameter war für die leichte Abweichung von Millikans Wert (1,592 * 10-19 C) zum heutigen Standardwert verantwortlich?",
            answerA = "Schwankungen im Magnetfeld der Erde in Chicago.",
            answerB = "Ein ungenauer Literaturwert für die Luftviskosität η.",
            answerC = "Die fehlerhafte Zählung der Tröpfchen im Histogramm.",
            correctIndex = 1
        });

        questions.Add(new QuizQuestion
        {
            question = "Welche Rolle spielte Harvey Fletcher im Zusammenhang mit dem Nobelpreis von 1923?",
            answerA = "Er war der schärfste Kritiker der Schwebemethode und versuchte den Versuch zu verhindern.",
            answerB = "Er verzichtete vertraglich auf die Autorenschaft und wurde daher nicht mit dem Nobelpreis ausgezeichnet.",
            answerC = "Er erhielt den Nobelpreis gemeinsam mit Millikan für die Entdeckung des Elektrons.",
            correctIndex = 1
        });

        questions.Add(new QuizQuestion
        {
            question = "In welcher Einheit wird die Elementarladung e typischerweise angegeben?",
            answerA = "Volt pro Meter (V/m).",
            answerB = "Newton pro Kilogramm (N/kg).",
            answerC = "Coulomb (C).",
            correctIndex = 2
        });

        questions.Add(new QuizQuestion
        {
            question = "Welcher physikalische Zusammenhang wird durch das Stokes’sche Gesetz im Experiment genutzt?",
            answerA = "Die elektrische Kraft auf ein Teilchen nimmt quadratisch mit der Entfernung zum Kondensator ab.",
            answerB = "Die Masse eines Tröpfchens verringert sich proportional zu seiner Fallzeit.",
            answerC = "Die Reibungskraft der Luft auf eine Kugel hängt direkt von deren Radius ab.",
            correctIndex = 2
        });
    }
}