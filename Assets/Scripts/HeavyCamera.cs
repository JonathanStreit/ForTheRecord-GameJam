using UnityEngine;

/// <summary>
/// The big old-time camera. It is a normal rigidbody that only lifts off the ground while
/// BOTH players hold a handle. While carried it hangs on a soft spring between the players,
/// so it swings with their movement and faces perpendicular to the line between them.
/// To throw it, each player holds the use button to charge their own power and both release
/// at the same time; unequal power skews the throw towards the stronger player's side.
/// </summary>
public class HeavyCamera : Grabbable
{
    [Header("Handles")]
    [SerializeField] Transform leftHandle;
    [SerializeField] Transform rightHandle;

    [Header("Carrying")]
    [SerializeField] float carryHeight = 0.9f;
    [Tooltip("The camera drops when the two players are further apart than this.")]
    [SerializeField] float maxPlayerDistance = 6.5f;
    [Tooltip("A player holding a handle alone lets go when further away than this.")]
    [SerializeField] float maxHandleDistance = 3.5f;
    [SerializeField, Range(0.1f, 1f)] float carrySpeedMultiplier = 0.7f;
    [SerializeField] float turnSpeed = 8f;

    [Header("Swing (inertia between the players)")]
    [Tooltip("Swings per second. Lower = heavier, lazier swing.")]
    [SerializeField] float swingFrequency = 0.9f;
    [Tooltip("0 = swings forever, 1 = no swing at all.")]
    [SerializeField, Range(0f, 1f)] float swingDamping = 0.15f;
    [Tooltip("The camera slips out of the players' hands when it swings further than this from their midpoint.")]
    [SerializeField] float maxSwingOffset = 3f;

    [Header("Random sway")]
    [Tooltip("Adds a wandering pull that the players have to counterbalance.")]
    [SerializeField] bool randomSway = true;
    [SerializeField] float swayStrength = 8f;
    [SerializeField] float swayChangeSpeed = 0.5f;

    [Header("Throw (both hold the use button, then release together)")]
    [Tooltip("Seconds both players have to hold the throw button for full strength.")]
    [SerializeField] float throwChargeTime = 1.5f;
    [Tooltip("Both players must release the throw button within this many seconds, otherwise the throw is cancelled.")]
    [SerializeField] float throwWindow = 0.2f;
    [SerializeField] float minThrowSpeed = 4f;
    [SerializeField] float maxThrowSpeed = 14f;
    [SerializeField] float minThrowUpSpeed = 2f;
    [SerializeField] float maxThrowUpSpeed = 6f;
    [Tooltip("How far (degrees) the throw veers towards the player with more power when one is fully charged and the other not at all.")]
    [SerializeField] float maxThrowSkewAngle = 45f;

    const float HeightFrequency = 3f;

    PlayerController _left, _right;
    // Per-player throw state, index 0 = left handle, 1 = right handle.
    readonly float[] _charge = new float[2];
    readonly bool[] _wasHeld = new bool[2];
    readonly float[] _releaseTime = { -1f, -1f };
    float _swaySeed;

    public PlayerController LeftHolder => _left;
    public PlayerController RightHolder => _right;
    /// <summary>0..1 throw power of the player on the left / right handle.</summary>
    public float LeftCharge => _charge[0];
    public float RightCharge => _charge[1];
    public bool IsCarried => _left != null && _right != null;
    public bool IsHeld => _left != null || _right != null;
    public bool IsResting => !IsCarried && Body.linearVelocity.sqrMagnitude < 0.05f;

    protected override void Awake()
    {
        base.Awake();
        _swaySeed = Random.value * 100f;
    }

    public override bool TryGrab(PlayerController player)
    {
        bool leftFree = _left == null, rightFree = _right == null;
        if (!leftFree && !rightFree) return false;

        bool takeLeft = leftFree;
        if (leftFree && rightFree)
        {
            Vector3 p = player.transform.position;
            takeLeft = (leftHandle.position - p).sqrMagnitude <= (rightHandle.position - p).sqrMagnitude;
        }

        if (takeLeft) _left = player; else _right = player;
        player.SetHeld(this);
        IgnorePlayerCollision(player, true);

        if (IsCarried)
        {
            Body.useGravity = false;
            _left.SpeedMultiplier = _right.SpeedMultiplier = carrySpeedMultiplier;
        }
        return true;
    }

    public override void Release(PlayerController player)
    {
        if (player != _left && player != _right) return;

        Detach(player);
        Body.useGravity = true;
        CancelThrow();
    }

    /// <summary>Forces both players to let go without a throw.</summary>
    public void Drop()
    {
        if (_left != null) Detach(_left);
        if (_right != null) Detach(_right);
        Body.useGravity = true;
        CancelThrow();
    }

    void Update()
    {
        if (!IsCarried) return;

        UpdateCharge(0, _left.UseHeld);
        UpdateCharge(1, _right.UseHeld);

        bool leftReleased = _releaseTime[0] >= 0f, rightReleased = _releaseTime[1] >= 0f;
        if (leftReleased && rightReleased)
            Throw();
        else if (leftReleased || rightReleased)
        {
            float firstRelease = leftReleased ? _releaseTime[0] : _releaseTime[1];
            if (Time.time - firstRelease > throwWindow)
                CancelThrow(); // not released together
        }
    }

    void UpdateCharge(int side, bool held)
    {
        if (held)
        {
            _charge[side] = Mathf.MoveTowards(_charge[side], 1f, Time.deltaTime / throwChargeTime);
            _releaseTime[side] = -1f;
        }
        else if (_wasHeld[side])
        {
            _releaseTime[side] = Time.time;
        }
        _wasHeld[side] = held;
    }

    void Throw()
    {
        float power = (_charge[0] + _charge[1]) * 0.5f;
        // More power on the left -> veers left, more on the right -> veers right.
        float skew = (_charge[1] - _charge[0]) * maxThrowSkewAngle;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 direction = Quaternion.AngleAxis(skew, Vector3.up) * forward;
        Drop();
        Body.linearVelocity = direction * Mathf.Lerp(minThrowSpeed, maxThrowSpeed, power)
            + Vector3.up * Mathf.Lerp(minThrowUpSpeed, maxThrowUpSpeed, power);
    }

    void CancelThrow()
    {
        _charge[0] = _charge[1] = 0f;
        _releaseTime[0] = _releaseTime[1] = -1f;
    }

    void Detach(PlayerController player)
    {
        if (player == _left) _left = null; else _right = null;
        var other = _left != null ? _left : _right;
        if (other != null) other.SpeedMultiplier = 1f;
        player.SpeedMultiplier = 1f;
        player.ClearHeld(this);
        IgnorePlayerCollision(player, false);
    }

    void FixedUpdate()
    {
        if (!IsCarried)
        {
            DropLoneHolderIfTooFar(_left, leftHandle);
            DropLoneHolderIfTooFar(_right, rightHandle);
            return;
        }

        Vector3 leftPos = _left.Body.position, rightPos = _right.Body.position;
        Vector3 span = Vector3.ProjectOnPlane(rightPos - leftPos, Vector3.up);
        if (span.magnitude > maxPlayerDistance)
        {
            Drop();
            return;
        }

        Vector3 anchor = (leftPos + rightPos) * 0.5f + Vector3.up * carryHeight;
        Vector3 offset = anchor - Body.position;
        Vector3 horizontalOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        if (horizontalOffset.magnitude > maxSwingOffset)
        {
            Drop();
            return;
        }

        // Horizontal: soft, underdamped spring to the midpoint -> the camera lags behind and swings.
        Vector3 carrierVelocity = Vector3.ProjectOnPlane((_left.Body.linearVelocity + _right.Body.linearVelocity) * 0.5f, Vector3.up);
        Vector3 velocity = Body.linearVelocity;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float w = swingFrequency * 2f * Mathf.PI;
        Vector3 accel = w * w * horizontalOffset + 2f * swingDamping * w * (carrierVelocity - horizontalVelocity);

        if (randomSway)
        {
            float t = Time.time * swayChangeSpeed;
            accel += new Vector3(Mathf.PerlinNoise(t, _swaySeed) - 0.5f, 0f, Mathf.PerlinNoise(_swaySeed, t) - 0.5f) * (2f * swayStrength);
        }

        // Vertical: stiff, critically damped spring so it stays at carry height.
        float wy = HeightFrequency * 2f * Mathf.PI;
        accel.y = wy * wy * offset.y - 2f * wy * velocity.y;
        Body.AddForce(accel, ForceMode.Acceleration);

        // Rotation: face perpendicular to the line between the players, so they aim it by walking around each other.
        if (span.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(Vector3.Cross(span.normalized, Vector3.up), Vector3.up);
            (target * Quaternion.Inverse(Body.rotation)).ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            Body.angularVelocity = float.IsFinite(axis.x) ? axis * (angle * Mathf.Deg2Rad * turnSpeed) : Vector3.zero;
        }
    }

    void DropLoneHolderIfTooFar(PlayerController player, Transform handle)
    {
        if (player != null && (player.transform.position - handle.position).magnitude > maxHandleDistance)
            Detach(player);
    }
}
