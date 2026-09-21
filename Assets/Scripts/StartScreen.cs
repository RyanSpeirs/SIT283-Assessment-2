using TMPro;
using UnityEngine;

// Sits on a World Space canvas in the scene (not on the camera) so the XR ray interactors can press its buttons.
// Wire the buttons' OnClick events:
//   Easy / Medium / Hard -> SelectDifficulty(0 / 1 / 2)
//   Start                -> OnStartPressed()
public class StartScreen : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text difficultyLabel;
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private string[] levelNames = { "Easy", "Medium", "Hard" };
    [SerializeField] private float distanceFromPlayer = 1.5f;

    public void SelectDifficulty(int level)
    {
        gameManager.SetDifficulty(level);
        Refresh();
    }

    public void OnStartPressed()
    {
        gameManager.StartGame();
    }

    // bringTo is optional; pass the player's head to place the panel in front of them
    public void Show(string results, Transform bringTo)
    {
        gameObject.SetActive(true);
        resultsText.text = results;
        Refresh();

        if (bringTo != null)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(bringTo.forward, Vector3.up).normalized;
            transform.position = bringTo.position + flatForward * distanceFromPlayer;
            transform.rotation = Quaternion.LookRotation(flatForward);   // canvas faces the player
        }
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
