using UnityEngine;

/// <summary>
/// An item one player can carry alone. It snaps to the player's hold point while grabbed.
/// Tapping the use button activates it; holding the use button charges a throw, which
/// happens when the use button or the grab button is released.
/// </summary>
public class CarryItem : Grabbable
{
    [Header("Throw (hold the use button)")]
    [Tooltip("Releasing the use button faster than this activates the item instead of throwing it.")]
    [SerializeField] float tapTime = 0.25f;
    [SerializeField] float throwChargeTime = 1f;
    [SerializeField] float minThrowSpeed = 4f;
    [SerializeField] float maxThrowSpeed = 14f;
    [SerializeField] float minThrowUpSpeed = 2f;
    [SerializeField] float maxThrowUpSpeed = 6f;

    float _useTime;

    public PlayerController Holder { get; private set; }
    public override bool IsAvailable => Holder == null;

    public override bool TryGrab(PlayerController player)
    {
        if (Holder != null) return false;

        Holder = player;
        _useTime = 0f;
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

        float charge = player.ThrowCharge;
        Vector3 velocity = player.Body.linearVelocity;
        if (charge > 0f)
            velocity += player.transform.forward * Mathf.Lerp(minThrowSpeed, maxThrowSpeed, charge)
                + Vector3.up * Mathf.Lerp(minThrowUpSpeed, maxThrowUpSpeed, charge);

        transform.SetParent(null);
        SetCollidersEnabled(true);
        Body.isKinematic = false;
        Body.linearVelocity = velocity;
        player.ThrowCharge = 0f;
        player.ClearHeld(this);
        Holder = null;
    }

    /// <summary>Called when the holding player taps the use button.</summary>
    protected virtual void Activate(PlayerController player) { }

    void Update()
    {
        if (Holder == null) return;

        if (Holder.UseHeld)
        {
            _useTime += Time.deltaTime;
            Holder.ThrowCharge = Mathf.Clamp01((_useTime - tapTime) / throwChargeTime);
        }
        else if (_useTime > 0f)
        {
            bool tapped = _useTime < tapTime;
            _useTime = 0f;
            if (tapped) Activate(Holder);
            else Release(Holder); // throws, because the holder has throw charge
        }
    }

    void SetCollidersEnabled(bool value)
    {
        foreach (var c in Colliders)
            c.enabled = value;
    }
}
