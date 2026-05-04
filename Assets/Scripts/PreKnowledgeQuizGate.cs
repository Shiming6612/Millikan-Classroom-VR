using TMPro;
using UnityEngine;

public class PreKnowledgeQuizGate : MonoBehaviour
{
    [Header("Panel-Gruppen")]
    public GameObject panelRoot;
    public GameObject welcomeGroup;
    public GameObject quizGroup;
    public GameObject feedbackGroup;

    [Header("Nach Abschluss ausblenden")]
    public GameObject objectToHideAfterQuiz;

    [Header("Feedback")]
    public TMP_Text feedbackText;
    public string wrongFeedback = "Nicht ganz. Bitte versuche es noch einmal.";
    public string correctFeedback = "Danke für deine Teilnahme, bitte geh jetzt zum Experimentaufbau.";

    [Header("Spieler blockieren")]
    public Transform playerRoot;
    public bool lockPlayerUntilQuizCompleted = true;

    [Header("Nach dem Quiz aktivieren")]
    public Behaviour[] behavioursToEnableAfterQuiz;
    public GameObject[] objectsToEnableAfterQuiz;

    private bool quizCompleted;
    private Vector3 lockedPosition;
    private Quaternion lockedRotation;

    private void Start()
    {
        quizCompleted = false;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (objectToHideAfterQuiz == null)
            objectToHideAfterQuiz = panelRoot;

        if (playerRoot != null)
        {
            lockedPosition = playerRoot.position;
            lockedRotation = playerRoot.rotation;
        }

        SetUnlockedState(false);
        ShowWelcome();
    }

    private void LateUpdate()
    {
        if (quizCompleted)
            return;

        if (!lockPlayerUntilQuizCompleted)
            return;

        if (playerRoot == null)
            return;

        playerRoot.position = lockedPosition;
        playerRoot.rotation = lockedRotation;
    }

    public void ShowWelcome()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (welcomeGroup != null)
            welcomeGroup.SetActive(true);

        if (quizGroup != null)
            quizGroup.SetActive(false);

        if (feedbackGroup != null)
            feedbackGroup.SetActive(false);
    }

    public void ShowQuiz()
    {
        if (welcomeGroup != null)
            welcomeGroup.SetActive(false);

        if (quizGroup != null)
            quizGroup.SetActive(true);

        if (feedbackGroup != null)
            feedbackGroup.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = "";
    }

    public void SelectWrongAnswer()
    {
        if (feedbackText != null)
            feedbackText.text = wrongFeedback;
    }

    public void SelectCorrectAnswer()
    {
        if (feedbackText != null)
            feedbackText.text = correctFeedback;

        if (quizGroup != null)
            quizGroup.SetActive(false);

        if (feedbackGroup != null)
            feedbackGroup.SetActive(true);
    }

    public void CompleteQuiz()
    {
        if (quizCompleted)
            return;

        quizCompleted = true;

        SetUnlockedState(true);

        if (objectToHideAfterQuiz != null)
            objectToHideAfterQuiz.SetActive(false);
        else if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void SetUnlockedState(bool unlocked)
    {
        if (behavioursToEnableAfterQuiz != null)
        {
            for (int i = 0; i < behavioursToEnableAfterQuiz.Length; i++)
            {
                if (behavioursToEnableAfterQuiz[i] != null)
                    behavioursToEnableAfterQuiz[i].enabled = unlocked;
            }
        }

        if (objectsToEnableAfterQuiz != null)
        {
            for (int i = 0; i < objectsToEnableAfterQuiz.Length; i++)
            {
                if (objectsToEnableAfterQuiz[i] != null)
                    objectsToEnableAfterQuiz[i].SetActive(unlocked);
            }
        }
    }
}