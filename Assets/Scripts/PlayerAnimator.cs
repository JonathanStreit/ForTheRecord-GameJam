using UnityEngine;

/// <summary>
/// Procedural walk cycle for the primitive player model: feet step, arms swing, the body bobs.
/// Also fades the looping footstep sound in while the player walks.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] Transform footLeft;
    [SerializeField] Transform footRight;
    [SerializeField] Transform armLeft;
    [SerializeField] Transform armRight;
    [SerializeField] Transform body;

    [Header("Walk cycle")]
    [Tooltip("Full step cycles (left + right) per metre walked.")]
    [SerializeField] float cyclesPerMeter = 0.45f;
    [SerializeField] float stride = 0.3f;
    [SerializeField] float footLift = 0.15f;
    [SerializeField] float armSwing = 0.2f;
    [SerializeField] float bodyBob = 0.06f;
    [Tooltip("Legs kick at this many cycles per second while the player is carried by the other one.")]
    [SerializeField] float carriedKickSpeed = 3f;

    [Header("Footsteps")]
    [Tooltip("Looping audio source; its volume follows the walking.")]
    [SerializeField] AudioSource footsteps;
    [SerializeField, Range(0f, 1f)] float footstepVolume = 0.5f;

    PlayerController _player;
    Vector3 _footLeftRest, _footRightRest, _armLeftRest, _armRightRest, _bodyRest;
    float _phase, _amount;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _footLeftRest = footLeft.localPosition;
        _footRightRest = footRight.localPosition;
        _armLeftRest = armLeft.localPosition;
        _armRightRest = armRight.localPosition;
        _bodyRest = body.localPosition;
        if (footsteps != null) footsteps.volume = 0f;
    }

    void Update()
    {
        float speed = Vector3.ProjectOnPlane(_player.Body.linearVelocity, Vector3.up).magnitude;
        bool walking = speed > 0.3f && _player.IsGrounded && !_player.IsBeingCarried;
        bool kicking = _player.IsBeingCarried;

        _phase += (kicking ? carriedKickSpeed : speed * cyclesPerMeter) * 2f * Mathf.PI * Time.deltaTime;
        _amount = Mathf.MoveTowards(_amount, walking || kicking ? 1f : 0f, Time.deltaTime * 8f);
        float swing = Mathf.Sin(_phase) * _amount;
        float lift = Mathf.Cos(_phase) * _amount;

        footLeft.localPosition = _footLeftRest + new Vector3(0f, Mathf.Max(0f, lift) * footLift, swing * stride);
        footRight.localPosition = _footRightRest + new Vector3(0f, Mathf.Max(0f, -lift) * footLift, -swing * stride);
        body.localPosition = _bodyRest + Vector3.up * (Mathf.Abs(swing) * bodyBob);

        // Arms swing against the feet, but stay still while the hands are full.
        float arm = _player.Held == null ? swing * armSwing : 0f;
        armLeft.localPosition = _armLeftRest + Vector3.back * arm;
        armRight.localPosition = _armRightRest + Vector3.forward * arm;

        if (footsteps != null)
            footsteps.volume = Mathf.MoveTowards(footsteps.volume, walking ? footstepVolume : 0f, Time.deltaTime * 4f);
    }
}
