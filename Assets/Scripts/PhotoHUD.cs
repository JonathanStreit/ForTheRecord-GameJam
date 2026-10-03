using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen overlay: current task, short messages (countdown, errors) and the last photo.
/// </summary>
public class PhotoHUD : MonoBehaviour
{
    static PhotoHUD _instance;

    [SerializeField] Text taskText;
    [SerializeField] Text messageText;
    [Tooltip("Root object of the photo display, hidden when no photo is shown.")]
    [SerializeField] GameObject photoPanel;
    [SerializeField] RawImage photoImage;
    [SerializeField] Text verdictText;
    [SerializeField] float photoDisplaySeconds = 3f;

    float _messageUntil, _photoUntil;

    void Awake()
    {
        _instance = this;
        messageText.text = "";
        photoPanel.SetActive(false);
    }

    void Update()
    {
        if (messageText.text.Length > 0 && Time.time >= _messageUntil)
            messageText.text = "";
        if (photoPanel.activeSelf && Time.time >= _photoUntil)
            photoPanel.SetActive(false);
    }

    public static void SetTask(string text)
    {
        if (_instance != null) _instance.taskText.text = text;
    }

    public static void ShowMessage(string text, float seconds)
    {
        if (_instance == null) return;
        _instance.messageText.text = text;
        _instance._messageUntil = Time.time + seconds;
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
