using UnityEngine;

/// <summary>
/// Moves back and forth between its start position and start + travel, facing the way it walks.
/// </summary>
public class PatrolMover : MonoBehaviour
{
    [Tooltip("Offset from the start position to the turning point.")]
    [SerializeField] Vector3 travel = new Vector3(8f, 0f, 0f);
    [SerializeField] float speed = 2f;
    [Tooltip("Seconds to wait at each end.")]
    [SerializeField] float pause = 0.5f;

    Vector3 _start, _end;
    bool _toEnd = true;
    float _waitUntil;

    void Start()
    {
        _start = transform.position;
        _end = _start + travel;
    }

    void Update()
    {
        if (Time.time < _waitUntil) return;

        Vector3 goal = _toEnd ? _end : _start;
        transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
        Vector3 direction = goal - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            _toEnd = !_toEnd;
            _waitUntil = Time.time + pause;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 from = Application.isPlaying ? _start : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(from, from + travel);
    }
}
