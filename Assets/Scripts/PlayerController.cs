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
    /// <summary>Set by held objects to slow the player down (e.g. while carrying the camera).</summary>
    public float SpeedMultiplier { get; set; } = 1f;
    /// <summary>True while the use button is held down (charges a throw).</summary>
    public bool UseHeld => _use.IsPressed();
    /// <summary>0..1 throw power, set by whatever the player is holding and shown by the throw indicator.</summary>
    public float ThrowCharge { get; set; }

    InputAction _move, _grab, _use;
    Vector3 _moveInput;

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
        _moveInput = new Vector3(move.x, 0f, move.y);

        if (Held == null && _grab.WasPressedThisFrame())
            TryGrabNearest();
        else if (Held != null && !_grab.IsPressed())
            Held.Release(this);
    }

    void FixedUpdate()
    {
        Vector3 velocity = Body.linearVelocity;
        Vector3 target = _moveInput * (moveSpeed * SpeedMultiplier);
        Vector3 horizontal = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), target, acceleration * Time.fixedDeltaTime);
        Body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(_moveInput, Vector3.up);
            Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, look, turnSpeed * Time.fixedDeltaTime));
        }
    }

    void TryGrabNearest()
    {
        // Every grabbable has its own grab radius; pick the one we are deepest inside of.
        Grabbable nearest = null;
        float nearestScore = 1f;
        foreach (var grabbable in Grabbable.All)
        {
            if (!grabbable.IsAvailable) continue;
            Vector3 offset = Vector3.ProjectOnPlane(grabbable.transform.position - transform.position, Vector3.up);
            float score = offset.magnitude / grabbable.GrabRadius;
            if (score <= nearestScore)
            {
                nearestScore = score;
                nearest = grabbable;
            }
        }
        if (nearest != null)
            nearest.TryGrab(this);
    }

    public void SetColor(Color color)
    {
        foreach (var r in tintRenderers)
            r.material.color = color;
    }

    // Called by Grabbables so they can also force a drop (e.g. the camera when players drift apart).
    public void SetHeld(Grabbable grabbable) => Held = grabbable;

    public void ClearHeld(Grabbable grabbable)
    {
        if (Held == grabbable) Held = null;
    }
}
