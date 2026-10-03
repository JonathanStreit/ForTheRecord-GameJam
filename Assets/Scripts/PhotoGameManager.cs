using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs one timed round: all assignments are open at once, every photo is judged against
/// the ones that are still missing, and points are added to the score.
/// </summary>
public class PhotoGameManager : MonoBehaviour
{
    enum State { WaitingForPlayers, Running, Over }

    [Tooltip("All subjects the players can photograph this round. Points are set on each subject.")]
    [SerializeField] PhotoSubject[] tasks;
    [SerializeField] float timeLimit = 180f;
    [Tooltip("The timer starts once this many players have joined.")]
    [SerializeField] int playersToStart = 2;
    [Tooltip("Seconds the final score is shown before the scene restarts.")]
    [SerializeField] float restartDelay = 6f;

    State _state;
    bool[] _done;
    int _score;
    float _timeLeft, _restartAt;

    void OnEnable() => PhotoCapture.PhotoTaken += OnPhotoTaken;
    void OnDisable() => PhotoCapture.PhotoTaken -= OnPhotoTaken;

    void Start()
    {
        _done = new bool[tasks.Length];
        _timeLeft = timeLimit;
        PhotoHUD.SetScore(0, false);
        PhotoHUD.SetTime(_timeLeft, timeLimit);
        ShowTasks();
    }

    void Update()
    {
        switch (_state)
        {
            case State.WaitingForPlayers:
                int joined = PlayerController.All.Count;
                if (joined >= playersToStart)
                    _state = State.Running;
                else
                    PhotoHUD.ShowMessage("Press a button to join (" + joined + "/" + playersToStart + ")", 0.2f);
                break;

            case State.Running:
                _timeLeft -= Time.deltaTime;
                PhotoHUD.SetTime(_timeLeft, timeLimit);
                if (_timeLeft <= 0f) EndRound("TIME'S UP!");
                break;

            case State.Over:
                if (Time.time >= _restartAt)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }

    void OnPhotoTaken(PhotoResult result)
    {
        if (_state == State.Over) return;
        if (_state == State.WaitingForPlayers)
        {
            PhotoHUD.ShowPhoto(result.Photo, "Practice shot - the round has not started.");
            return;
        }

        int gained = 0;
        bool tooDark = false;
        var names = new StringBuilder();
        for (int i = 0; i < tasks.Length; i++)
        {
            if (_done[i] || !result.InFrame.Contains(tasks[i])) continue;
            if (!result.Lit.Contains(tasks[i]))
            {
                tooDark = true;
                continue;
            }
            _done[i] = true;
            gained += tasks[i].Points;
            names.Append(names.Length > 0 ? ", " : "").Append(tasks[i].DisplayName);
        }

        string verdict;
        if (gained > 0)
        {
            _score += gained;
            PhotoHUD.SetScore(_score, true);
            ShowTasks();
            verdict = "Great shot of " + names + "!  +" + gained;
        }
        else if (tooDark)
            verdict = "Too dark! Light the subject with the flash.";
        else
            verdict = "No open assignment in the picture.";
        PhotoHUD.ShowPhoto(result.Photo, verdict);

        if (System.Array.TrueForAll(_done, done => done))
            EndRound("ALL PHOTOS TAKEN!");
    }

    void EndRound(string headline)
    {
        _state = State.Over;
        _restartAt = Time.time + restartDelay;
        PhotoHUD.ShowMessage(headline + "\n" + _score + " points", restartDelay);
    }

    void ShowTasks()
    {
        var text = new StringBuilder();
        for (int i = 0; i < tasks.Length; i++)
        {
            string line = tasks[i].DisplayName + "   " + tasks[i].Points;
            text.AppendLine(_done[i] ? "<color=#7CDB6B>" + line + "  - done</color>" : line);
        }
        PhotoHUD.SetTasks(text.ToString());
    }
}
