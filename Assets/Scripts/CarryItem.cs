using UnityEngine;

/// <summary>
/// An item one player can carry alone. It snaps to the player's hold point while grabbed.
/// </summary>
public class CarryItem : Grabbable
{
    public PlayerController Holder { get; private set; }

    public override bool TryGrab(PlayerController player)
    {
        if (Holder != null) return false;

        Holder = player;
        Body.isKinematic = true;
        SetCollidersEnabled(false);
        transform.SetParent(player.HoldPoint);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        player.SetHeld(this);
        return true;
    }

    public override void Release(PlayerController player)
    {
        if (player != Holder) return;

        transform.SetParent(null);
        SetCollidersEnabled(true);
        Body.isKinematic = false;
        Body.linearVelocity = player.Body.linearVelocity;
        player.ClearHeld(this);
        Holder = null;
    }

    void SetCollidersEnabled(bool value)
    {
        foreach (var c in Colliders)
            c.enabled = value;
    }
}
