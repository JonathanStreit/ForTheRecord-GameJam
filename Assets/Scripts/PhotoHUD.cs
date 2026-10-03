using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen overlay: assignment list, score, round timer, short messages (countdown, errors) and the last photo.
/// </summary>
public class PhotoHUD : MonoBehaviour
{
    static PhotoHUD _instance;

    [Header("Assignments and score")]
    [SerializeField] Text taskText;
    [SerializeField] Text scoreText;

    [Header("Timer")]
    [SerializeField] RectTransform timerRoot;
    [Tooltip("Image with Image Type = Filled (Radial 360).")]
    [SerializeField] Image timerFill;
    [SerializeField] Text timerText;
    [SerializeField] Gradient timerColors;
    [Tooltip("The timer starts pulsing below this many seconds.")]
    [SerializeField] float hurrySeconds = 10f;

    [Header("Messages and photo")]
    [SerializeField] Text messageText;
    [Tooltip("Root object of the photo display, hidden when no photo is shown.")]
    [SerializeField] GameObject photoPanel;
    [SerializeField] RawImage photoImage;
    [SerializeField] Text verdictText;
    [SerializeField] float photoDisplaySeconds = 3f;

    [Header("Item pick-up prompt")]
    [Tooltip("Root object of the prompt, hidden when no prompt is shown.")]
    [SerializeField] GameObject promptPanel;
    [SerializeField] Text promptTitle;
    [SerializeField] Text promptText;

    float _messageUntil, _photoUntil, _promptUntil, _scorePunch, _timeLeft = float.MaxValue;

    void Awake()
    {
        _instance = this;
        messageText.text = "";
        photoPanel.SetActive(false);
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    void Update()
    {
        if (messageText.text.Length > 0 && Time.time >= _messageUntil)
            messageText.text = "";
        if (photoPanel.activeSelf && Time.time >= _photoUntil)
            photoPanel.SetActive(false);
        if (promptPanel != null && promptPanel.activeSelf && Time.time >= _promptUntil)
            promptPanel.SetActive(false);

        // Score pops when points are added, the timer pulses once per second when time runs out.
        _scorePunch = Mathf.MoveTowards(_scorePunch, 0f, Time.deltaTime * 2f);
        scoreText.transform.localScale = Vector3.one * (1f + 0.5f * _scorePunch);
        bool hurry = _timeLeft > 0f && _timeLeft <= hurrySeconds;
        timerRoot.localScale = Vector3.one * (hurry ? 1f + 0.2f * (_timeLeft % 1f) : 1f);
    }

    public static void SetTasks(string text)
    {
        if (_instance != null) _instance.taskText.text = text;
    }

    public static void SetScore(int score, bool animate)
    {
        if (_instance == null) return;
        _instance.scoreText.text = score.ToString();
        if (animate) _instance._scorePunch = 1f;
    }

    public static void SetTime(float secondsLeft, float totalSeconds)
    {
        if (_instance == null) return;
        float fraction = Mathf.Clamp01(secondsLeft / totalSeconds);
        _instance._timeLeft = secondsLeft;
        _instance.timerFill.fillAmount = fraction;
        _instance.timerFill.color = _instance.timerColors.Evaluate(fraction);
        int seconds = Mathf.CeilToInt(Mathf.Max(secondsLeft, 0f));
        _instance.timerText.text = seconds / 60 + ":" + (seconds % 60).ToString("00");
    }

    public static void ShowMessage(string text, float seconds)
    {
        if (_instance == null) return;
        _instance.messageText.text = text;
        _instance._messageUntil = Time.time + seconds;
    }

    public static void ShowPrompt(string title, string text, float seconds)
    {
        if (_instance == null || _instance.promptPanel == null) return;
        _instance.promptTitle.text = title;
        _instance.promptText.text = text;
        _instance.promptPanel.SetActive(true);
        _instance._promptUntil = Time.time + seconds;
    }

    public static void ShowPhoto(Texture photo, string verdict)
    {
        if (_instance == null) return;
        _instance.photoImage.texture = photo;
        _instance.verdictText.text = verdict;
        _instance.photoPanel.SetActive(true);
        _instance._photoUntil = Time.time + _instance.photoDisplaySeconds;
    }
}
