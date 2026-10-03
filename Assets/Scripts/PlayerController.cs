using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down player. Spawned by the Player Input Manager, reads its own Player Input actions.
/// Grab is hold-to-hold: letting go of the button releases whatever is held.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public static readonly List<PlayerController> All = new List<PlayerController>();

    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float acceleration = 40f;
    [SerializeField] float turnSpeed = 720f;
    [Tooltip("Distance from the player's centre down to the ground, plus a little slack.")]
    [SerializeField] float groundCheckDistance = 1.15f;
    [Tooltip("Dust puffs behind the player's steps (emits by distance moved).")]
    [SerializeField] ParticleSystem stepDust;

    [Header("Grabbing")]
    [Tooltip("Where one-handed items (flash, remote trigger) are held.")]
    [SerializeField] Transform holdPoint;
    [Tooltip("Renderers that get the player colour (e.g. the coat).")]
    [SerializeField] Renderer[] tintRenderers;

    [Header("Input action names")]
    [SerializeField] string moveActionName = "Move";
    [SerializeField] string grabActionName = "Interact";
    [SerializeField] string useActionName = "Attack";

    public Rigidbody Body { get; private set; }
    public Collider[] Colliders { get; private set; }
    public Transform HoldPoint => holdPoint != null ? holdPoint : transform;
    public Grabbable Held { get; private set; }
    public Color Color { get; private set; } = Color.white;
    /// <summary>Set by held objects to slow the player down (e.g. while carrying the camera). 0 = cannot move or turn.</summary>
    public float SpeedMultiplier { get; set; } = 1f;
    /// <summary>True while the use button is held down (charges a throw).</summary>
    public bool UseHeld => _use.IsPressed();
    /// <summary>0..1 throw power, set by whatever the player is holding and shown by the throw indicator.</summary>
    public float ThrowCharge { get; set; }
    /// <summary>True while the other player is holding this player.</summary>
    public bool IsBeingCarried { get; private set; }
    public bool IsGrounded { get; private set; }

    InputAction _move, _grab, _use;
    Vector3 _moveInput;
    Vector3 _resistDirection;
    float _resistAmount;
    bool _launched;
    float _launchTime;

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Body.constraints = RigidbodyConstraints.FreezeRotation;
        Colliders = GetComponentsInChildren<Collider>();

        var input = GetComponent<PlayerInput>();
        _move = input.actions[moveActionName];
        _grab = input.actions[grabActionName];
        _use = input.actions[useActionName];
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        // Players walk straight through player-only gates, the big camera does not.
        foreach (var gate in PlayerOnlyGate.All)
            foreach (var ownCollider in Colliders)
                Physics.IgnoreCollision(gate.Collider, ownCollider);
    }

    void Update()
    {
        Vector2 move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
        _moveInput = IsBeingCarried ? Vector3.zero : new Vector3(move.x, 0f, move.y);

        if (Held == null && !IsBeingCarried && _grab.WasPressedThisFrame())
            TryGrabNearest();
        else if (Held != null && !_grab.IsPressed())
            Held.Release(this);
    }

    void FixedUpdate()
    {
        bool grounded = Physics.Raycast(Body.position, Vector3.down, groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
        IsGrounded = grounded;
        if (stepDust != null)
        {
            var emission = stepDust.emission;
            emission.enabled = grounded && !IsBeingCarried;
        }

        if (IsBeingCarried) return;
        if (_launched)
        {
            // Thrown by the other player: no control until back on the ground.
            if (Time.time < _launchTime + 0.2f || !grounded) return;
            _launched = false;
        }

        Vector3 velocity = Body.linearVelocity;
        Vector3 target = _moveInput * (moveSpeed * SpeedMultiplier);
        float alongResist = Vector3.Dot(target, _resistDirection);
        if (alongResist > 0f)
            target -= _resistDirection * (alongResist * _resistAmount);
        Vector3 horizontal = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), target, acceleration * Time.fixedDeltaTime);
        Body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        if (_moveInput.sqrMagnitude > 0.01f && SpeedMultiplier > 0f)
        {
            Quaternion look = Quaternion.LookRotation(_moveInput, Vector3.up);
            Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, look, turnSpeed * Time.fixedDeltaTime));
        }
    }

    void TryGrabNearest()
    {
        // Every grabbable has its own grab radius; pick the one we are deepest inside of.
        // Low-priority grabbables (the other player) only count when nothing else is in reach.
        Grabbable nearest = null, nearestLowPriority = null;
        float nearestScore = 1f, nearestLowPriorityScore = 1f;
        foreach (var grabbable in Grabbable.All)
        {
            if (!grabbable.IsAvailable || grabbable.gameObject == gameObject) continue;
            Vector3 offset = Vector3.ProjectOnPlane(grabbable.transform.position - transform.position, Vector3.up);
            float score = offset.magnitude / grabbable.GrabRadius;
            if (grabbable.LowPriority)
            {
                if (score > nearestLowPriorityScore) continue;
                nearestLowPriorityScore = score;
                nearestLowPriority = grabbable;
            }
            else if (score <= nearestScore)
            {
                nearestScore = score;
                nearest = grabbable;
            }
        }
        if (nearest == null) nearest = nearestLowPriority;
        if (nearest != null)
            nearest.TryGrab(this);
    }

    public void SetColor(Color color)
    {
        Color = color;
        foreach (var r in tintRenderers)
            r.material.color = color;
    }

    /// <summary>Slows movement in the given direction by amount (0 = free, 1 = blocked).</summary>
    public void SetMoveResistance(Vector3 direction, float amount)
    {
        _resistDirection = direction;
        _resistAmount = amount;
    }

    /// <summary>Called when the other player picks this player up or lets go / throws.</summary>
    public void SetCarried(bool carried)
    {
        IsBeingCarried = carried;
        if (carried) return;
        _launched = true;
        _launchTime = Time.time;
    }

    // Called by Grabbables so they can also force a drop (e.g. the camera when players drift apart).
    public void SetHeld(Grabbable grabbable) => Held = grabbable;

    public void ClearHeld(Grabbable grabbable)
    {
        if (Held == grabbable) Held = null;
    }
}
