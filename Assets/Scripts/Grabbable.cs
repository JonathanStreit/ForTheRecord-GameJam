using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for everything a player can hold with the grab button.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class Grabbable : MonoBehaviour
{
    public static readonly List<Grabbable> All = new List<Grabbable>();

    [Tooltip("A player can grab this when standing within this distance (measured on the ground from this object's position).")]
    [SerializeField] float grabRadius = 1.8f;

    public Rigidbody Body { get; private set; }
    public float GrabRadius => grabRadius;
    /// <summary>False while nobody else can grab it (already held / no free handle).</summary>
    public abstract bool IsAvailable { get; }
    /// <summary>Low-priority grabbables are only grabbed when nothing else is in reach.</summary>
    public virtual bool LowPriority => false;
    protected Collider[] Colliders { get; private set; }

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Colliders = GetComponentsInChildren<Collider>();
    }

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    /// <summary>Returns true if the player is now holding this object.</summary>
    public abstract bool TryGrab(PlayerController player);

    /// <summary>Called when the player lets go of the grab button.</summary>
    public abstract void Release(PlayerController player);

    protected void IgnorePlayerCollision(PlayerController player, bool ignore)
    {
        foreach (var playerCollider in player.Colliders)
            foreach (var ownCollider in Colliders)
                Physics.IgnoreCollision(playerCollider, ownCollider, ignore);
    }
}
