using UnityEngine;

public class Seat01TutorialController : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;
    public Transform seat01Point;
    public BottomTutorialController tutorialController;

    [Header("Seat Detection")]
    public float controlRadius = 0.9f;

    private bool wasInsideSeat01 = false;

    private void Update()
    {
        if (playerRoot == null || seat01Point == null || tutorialController == null)
            return;

        float distance = Vector3.Distance(playerRoot.position, seat01Point.position);
        bool isInsideSeat01 = distance <= controlRadius;

        if (isInsideSeat01 && !wasInsideSeat01)
        {
            wasInsideSeat01 = true;
            tutorialController.BeginTutorialSession();
        }
        else if (!isInsideSeat01 && wasInsideSeat01)
        {
            wasInsideSeat01 = false;
        }
    }
}