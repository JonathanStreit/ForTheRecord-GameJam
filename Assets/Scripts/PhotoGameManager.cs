using UnityEngine;

/// <summary>
/// Hands out the photo tasks one after another and judges every photo that is taken.
/// </summary>
public class PhotoGameManager : MonoBehaviour
{
    [Tooltip("Subjects to photograph, in order.")]
    [SerializeField] PhotoSubject[] tasks;

    int _current;

    PhotoSubject CurrentTask => _current < tasks.Length ? tasks[_current] : null;

    void OnEnable() => PhotoCapture.PhotoTaken += OnPhotoTaken;
    void OnDisable() => PhotoCapture.PhotoTaken -= OnPhotoTaken;
    void Start() => ShowTask();

    void OnPhotoTaken(PhotoResult result)
    {
        var task = CurrentTask;
        if (task == null) return;

        string verdict;
        if (!result.InFrame.Contains(task))
            verdict = task.DisplayName + " is not in the picture!";
        else if (!result.Lit.Contains(task))
            verdict = "Too dark! " + task.DisplayName + " needs the flash.";
        else
        {
            verdict = "Great shot of " + task.DisplayName + "!";
            _current++;
            ShowTask();
        }
        PhotoHUD.ShowPhoto(result.Photo, verdict);
    }

    void ShowTask()
    {
        var task = CurrentTask;
        PhotoHUD.SetTask(task != null ? "Photograph: " + task.DisplayName : "All photos taken!");
    }
}
