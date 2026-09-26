using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Counts down from startSeconds, updating a TextMeshPro label (on this same GameObject)
/// every frame, and loads a scene when it hits 0.
///
/// Setup:
/// 1. Attach to a GameObject that also has a TextMeshProUGUI (UI Text) or
///    TextMeshPro (3D Text) component — this script grabs it with GetComponent.
/// 2. Set startSeconds and sceneToLoad in the Inspector.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TIMER : MonoBehaviour
{
    [SerializeField] private float startSeconds = 300f;
    [SerializeField] private string sceneToLoad;

    private float timeRemaining;
    private TMP_Text label;
    private bool hasEnded;

    private void Awake()
    {
        // TMP_Text is the base class shared by both TextMeshProUGUI and TextMeshPro (3D),
        // so this works whichever one is on this GameObject.
        label = GetComponent<TMP_Text>();
        timeRemaining = startSeconds;
    }

    private void Start()
    {
        UpdateLabel();
    }

    private void Update()
    {
        if (hasEnded) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            hasEnded = true;
            UpdateLabel();
            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        UpdateLabel();
    }

    private void UpdateLabel()
    {
        // Formats as M:SS, e.g. "1:07". Change this if you want a different display.
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        label.text = $" Time Left: {minutes}:{seconds:00}";
    }
}