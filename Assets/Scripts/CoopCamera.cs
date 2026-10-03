using UnityEngine;

/// <summary>
/// Top-down game camera that follows the middle of all players and zooms out
/// the further they (and any extra targets) are apart.
/// </summary>
public class CoopCamera : MonoBehaviour
{
    [Tooltip("Where to look while no player has joined yet.")]
    [SerializeField] Vector3 defaultFocus;
    [Tooltip("Also kept in view, e.g. the big camera.")]
    [SerializeField] Transform[] extraTargets;
    [SerializeField] float pitch = 55f;
    [SerializeField] float minDistance = 16f;
    [SerializeField] float maxDistance = 45f;
    [Tooltip("Extra camera distance per metre the targets are apart.")]
    [SerializeField] float zoomPerMeter = 1.1f;
    [SerializeField] float smoothTime = 0.35f;

    Vector3 _focus, _focusVelocity;
    float _distance, _distanceVelocity;

    void Start()
    {
        _focus = defaultFocus;
        _distance = minDistance;
        Apply();
    }

    void LateUpdate()
    {
        Vector3 targetFocus = defaultFocus;
        float spread = 0f;

        if (PlayerController.All.Count > 0)
        {
            var bounds = new Bounds(PlayerController.All[0].transform.position, Vector3.zero);
            foreach (var player in PlayerController.All)
                bounds.Encapsulate(player.transform.position);
            foreach (var target in extraTargets)
                if (target != null) bounds.Encapsulate(target.position);
            targetFocus = bounds.center;
            spread = bounds.size.magnitude;
        }

        float targetDistance = Mathf.Clamp(minDistance + spread * zoomPerMeter, minDistance, maxDistance);
        _focus = Vector3.SmoothDamp(_focus, targetFocus, ref _focusVelocity, smoothTime);
        _distance = Mathf.SmoothDamp(_distance, targetDistance, ref _distanceVelocity, smoothTime);
        Apply();
    }

    void Apply()
    {
        Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.SetPositionAndRotation(_focus - rotation * Vector3.forward * _distance, rotation);
    }
}
