using System.Collections;
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
    [SerializeField] float maxHandleDistance = 4.5f;
    [SerializeField, Range(0.1f, 1f)] float carrySpeedMultiplier = 0.7f;
    [SerializeField] float turnSpeed = 8f;
    [Tooltip("Fraction of the drop distances at which the warning (red circle) and the resistance start.")]
    [SerializeField, Range(0.1f, 1f)] float edgeStart = 0.7f;
    [Tooltip("How much walking further apart is slowed right before the camera drops (1 = cannot walk apart at all).")]
    [SerializeField, Range(0f, 1f)] float edgeResistance = 0.75f;

    [Header("Impact (dropped or thrown camera hitting something)")]
    [Tooltip("Scene particle system that is moved to the impact point and played.")]
    [SerializeField] ParticleSystem landingShockwave;
    [SerializeField] float minImpactSpeed = 2f;
    [SerializeField] float impactShake = 0.5f;
    [SerializeField] AudioClip impactSound;

    [Header("Pick-up animation (when the second player grabs)")]
    [SerializeField] float pickupPopSeconds = 0.25f;
    [Tooltip("How much bigger the camera gets at the peak of the pop (0.2 = 20%).")]
    [SerializeField] float pickupPopScale = 0.2f;

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

    [Header("Throw (both hold the use button, then release use or grab together)")]
    [Tooltip("Seconds a player has to hold the throw button for full power.")]
    [SerializeField] float throwChargeTime = 1.5f;
    [Tooltip("Both players must release (throw button or grab button) within this many seconds, otherwise the throw is cancelled.")]
    [SerializeField] float throwWindow = 0.5f;
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
    // True when that player released by letting go of grab; they stay attached until the throw happens or times out.
    readonly bool[] _grabReleased = new bool[2];
    float _swaySeed;
    float _lastImpactTime;
    Coroutine _pickupPop;

    public override bool IsAvailable => _left == null || _right == null;
    public PlayerController LeftHolder => _left;
    public PlayerController RightHolder => _right;
    /// <summary>0 = carried comfortably, 1 = about to drop (players too far apart or camera swinging too far out).</summary>
    public float CarryStress { get; private set; }
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
            if (_pickupPop != null) StopCoroutine(_pickupPop);
            _pickupPop = StartCoroutine(PickupPop());
        }
        else
        {
            // Holding the camera alone: stand still until the partner grabs the other handle.
            player.SpeedMultiplier = 0f;
        }
        return true;
    }

    public override void Release(PlayerController player)
    {
        if (player != _left && player != _right) return;

        // Letting go of grab with throw power counts as this player's half of the throw.
        // The player keeps calling Release every frame, so after a cancel the camera simply drops.
        int side = player == _left ? 0 : 1;
        if (IsCarried && _charge[side] > 0f)
        {
            if (_releaseTime[side] < 0f) _releaseTime[side] = Time.time;
            _grabReleased[side] = true;
            return;
        }

        Detach(player);
        Body.useGravity = true;
        CancelThrow();
    }

    // Quick squash-and-stretch pop that sells the moment both players lift the camera.
    IEnumerator PickupPop()
    {
        for (float t = 0f; t < pickupPopSeconds; t += Time.deltaTime)
        {
            float pop = Mathf.Sin(t / pickupPopSeconds * Mathf.PI) * pickupPopScale;
            transform.localScale = new Vector3(1f - pop * 0.5f, 1f + pop, 1f - pop * 0.5f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        _pickupPop = null;
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
        _left.ThrowCharge = _charge[0];
        _right.ThrowCharge = _charge[1];

        bool leftReleased = _releaseTime[0] >= 0f, rightReleased = _releaseTime[1] >= 0f;
        if (leftReleased && rightReleased)
            Throw();
        else if (leftReleased || rightReleased)
        {
            float firstRelease = leftReleased ? _releaseTime[0] : _releaseTime[1];
            if (Time.time - firstRelease > throwWindow)
            {
                // Not released together: no throw. Whoever let go of grab really lets go now.
                bool leftLetGo = _grabReleased[0], rightLetGo = _grabReleased[1];
                CancelThrow();
                if (leftLetGo) Detach(_left);
                if (rightLetGo) Detach(_right);
                if (leftLetGo || rightLetGo) Body.useGravity = true;
            }
        }
    }

    void UpdateCharge(int side, bool held)
    {
        if (_grabReleased[side]) return;

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
        CoopCamera.Shake(0.25f, 0.15f);
        Body.linearVelocity = direction * Mathf.Lerp(minThrowSpeed, maxThrowSpeed, power)
            + Vector3.up * Mathf.Lerp(minThrowUpSpeed, maxThrowUpSpeed, power);
    }

    void CancelThrow()
    {
        _charge[0] = _charge[1] = 0f;
        _releaseTime[0] = _releaseTime[1] = -1f;
        _grabReleased[0] = _grabReleased[1] = false;
        if (_left != null) _left.ThrowCharge = 0f;
        if (_right != null) _right.ThrowCharge = 0f;
    }

    void Detach(PlayerController player)
    {
        if (player == _left) _left = null; else _right = null;
        var other = _left != null ? _left : _right;
        if (other != null)
        {
            // The remaining player now holds the camera alone and cannot move.
            other.SpeedMultiplier = 0f;
            other.SetMoveResistance(Vector3.zero, 0f);
        }
        CarryStress = 0f;
        player.SpeedMultiplier = 1f;
        player.SetMoveResistance(Vector3.zero, 0f);
        player.ThrowCharge = 0f;
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

        // Close to dropping: warn (CarryStress drives the red circle) and make walking further apart harder.
        float spanStress = Mathf.InverseLerp(maxPlayerDistance * edgeStart, maxPlayerDistance, span.magnitude);
        float swingStress = Mathf.InverseLerp(maxSwingOffset * edgeStart, maxSwingOffset, horizontalOffset.magnitude);
        CarryStress = Mathf.Max(spanStress, swingStress);
        Vector3 apart = span.normalized;
        _left.SetMoveResistance(-apart, spanStress * edgeResistance);
        _right.SetMoveResistance(apart, spanStress * edgeResistance);

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

    void OnCollisionEnter(Collision collision)
    {
        // Screen shake and a dust shockwave when the dropped or thrown camera hits something.
        if (IsCarried || Time.time - _lastImpactTime < 0.3f) return;
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;
        if (collision.collider.GetComponentInParent<PlayerController>() != null) return;

        _lastImpactTime = Time.time;
        CoopCamera.Shake(impactShake, 0.3f);
        GameAudio.Play(impactSound);
        if (landingShockwave != null)
        {
            landingShockwave.transform.position = collision.GetContact(0).point + Vector3.up * 0.1f;
            landingShockwave.Play();
        }
    }

    void DropLoneHolderIfTooFar(PlayerController player, Transform handle)
    {
        if (player != null && (player.transform.position - handle.position).magnitude > maxHandleDistance)
            Detach(player);
    }
}
