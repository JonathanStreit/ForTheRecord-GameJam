using UnityEngine;

/// <summary>
/// Lets the other player pick this player up and throw them, exactly like a carry item:
/// grab to lift, hold use to charge, release to throw.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCarryable : CarryItem
{
    PlayerController _self;

    // A player busy with the big camera cannot be picked up.
    public override bool IsAvailable => base.IsAvailable && !(_self.Held is HeavyCamera);
    public override bool LowPriority => true;

    protected override void Awake()
    {
        base.Awake();
        _self = GetComponent<PlayerController>();
    }

    public override bool TryGrab(PlayerController player)
    {
        if (player == _self || !IsAvailable || !base.TryGrab(player)) return false;
        _self.SetCarried(true);
        return true;
    }

    public override void Release(PlayerController player)
    {
        if (player != Holder) return;
        base.Release(player);
        _self.SetCarried(false);
    }
}
