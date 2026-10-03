using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Something the players can be asked to photograph. Needs a collider for the line-of-sight checks.
/// </summary>
public class PhotoSubject : MonoBehaviour
{
    public static readonly List<PhotoSubject> All = new List<PhotoSubject>();

    [SerializeField] string displayName = "Subject";

    Collider _collider;

    public string DisplayName => displayName;
    public Vector3 Center => _collider != null ? _collider.bounds.center : transform.position;

    void Awake() => _collider = GetComponentInChildren<Collider>();
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>True if nothing blocks the straight line from origin to this subject.</summary>
    public bool IsVisibleFrom(Vector3 origin, LayerMask obstructionMask)
    {
        if (!Physics.Linecast(origin, Center, out RaycastHit hit, obstructionMask, QueryTriggerInteraction.Ignore))
            return true;
        return hit.collider.GetComponentInParent<PhotoSubject>() == this;
    }
}
