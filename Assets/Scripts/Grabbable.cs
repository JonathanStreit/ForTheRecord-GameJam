using UnityEngine;

/// <summary>
/// Base class for everything a player can hold with the grab button.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class Grabbable : MonoBehaviour
{
    public Rigidbody Body { get; private set; }
    protected Collider[] Colliders { get; private set; }

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Colliders = GetComponentsInChildren<Collider>();
    }

    /// <summary>Returns true if the player is now holding this object.</summary>
    public abstract bool TryGrab(PlayerController player);

    /// <summary>Called when the player lets go of the grab button.</summary>
    public abstract void Release(PlayerController player);

    /// <summary>Called when the holding player presses the use button.</summary>
    public virtual void Use(PlayerController player) { }

    protected void IgnorePlayerCollision(PlayerController player, bool ignore)
    {
        foreach (var playerCollider in player.Colliders)
            foreach (var ownCollider in Colliders)
                Physics.IgnoreCollision(playerCollider, ownCollider, ignore);
    }
}
