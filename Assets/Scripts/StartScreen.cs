using TMPro;
using UnityEngine;

// Sits on a World Space canvas in the scene (not on the camera) so the XR ray interactors can press its buttons.
//   Easy - Medium - Hard -> Difficulty = 0 / 1 / 2
public class StartScreen : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text difficultyLabel;
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private string[] levelNames = { "Easy", "Medium", "Hard" };
    [SerializeField] private float distanceFromPlayer = 1.5f;
    [SerializeField] private LayerMask blockingMask = ~0;   // layers that block: bins, trees, terrain
    [SerializeField] private float clearRadius = 0.4f;

    private static readonly float[] candidateAngles = { 0f, 40f, -40f, 80f, -80f, 180f };


    public void SelectDifficulty(int level)
    {
        gameManager.SetDifficulty(level);
        Refresh();
    }

    public void OnStartPressed()
    {
        gameManager.StartGame();
    }

    //  pass the player's head to place the panel in front of them
    public void Show(string results, Transform bringTo)
    {
        gameObject.SetActive(true);
        resultsText.text = results;
        Refresh();

        if (bringTo == null) return;

        Vector3 flatForward = Vector3.ProjectOnPlane(bringTo.forward, Vector3.up).normalized;
        Vector3 chosen = flatForward;   // if every direction is blocked, fall back to straight ahead

        foreach (float angle in candidateAngles)
        {
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * flatForward;
            bool blocked = Physics.SphereCast(bringTo.position, clearRadius, dir, out _,
                distanceFromPlayer, blockingMask, QueryTriggerInteraction.Ignore);
            if (!blocked)
            {
                chosen = dir;
                break;
            }
        }

        transform.position = bringTo.position + chosen * distanceFromPlayer;
        transform.rotation = Quaternion.LookRotation(chosen);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        int level = Mathf.Clamp(gameManager.Difficulty, 0, levelNames.Length - 1);
        difficultyLabel.text = $"Difficulty: {levelNames[level]}";
    }
}
