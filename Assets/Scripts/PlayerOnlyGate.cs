using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A barrier that players walk through but that blocks everything else (the big camera).
/// Use it to close the gaps in a fence so the camera has to be thrown over.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlayerOnlyGate : MonoBehaviour
{
    public static readonly List<PlayerOnlyGate> All = new List<PlayerOnlyGate>();

    public Collider Collider { get; private set; }

    void Awake() => Collider = GetComponent<Collider>();
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);
}
