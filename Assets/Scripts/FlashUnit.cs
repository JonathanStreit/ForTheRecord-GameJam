using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One-player flash. Tapping use starts a 3-2-1 countdown, then the light stays on for a short time.
/// A subject counts as lit while it is inside the light cone with a free line of sight.
/// </summary>
public class FlashUnit : CarryItem
{
    public new static readonly List<FlashUnit> All = new List<FlashUnit>();

    [SerializeField] Light flashLight;
    [SerializeField] int countdownSeconds = 3;
    [SerializeField] float flashDuration = 2f;
    [SerializeField] float range = 10f;
    [SerializeField, Range(1f, 179f)] float coneAngle = 60f;
    [SerializeField] LayerMask obstructionMask = ~0;

    bool _busy;

    public bool IsFlashing { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        flashLight.type = LightType.Spot;
        flashLight.range = range;
        flashLight.spotAngle = coneAngle;
        flashLight.enabled = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        All.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        All.Remove(this);
    }

    protected override void Activate(PlayerController player)
    {
        if (!_busy) StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        _busy = true;
        for (int i = countdownSeconds; i > 0; i--)
        {
            PhotoHUD.ShowMessage(i.ToString(), 1f);
            yield return new WaitForSeconds(1f);
        }

        PhotoHUD.ShowMessage("FLASH!", flashDuration);
        IsFlashing = flashLight.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        IsFlashing = flashLight.enabled = false;
        _busy = false;
    }

    public bool Illuminates(PhotoSubject subject)
    {
        if (!IsFlashing) return false;

        Vector3 origin = flashLight.transform.position;
        Vector3 toSubject = subject.Center - origin;
        return toSubject.magnitude <= range
            && Vector3.Angle(flashLight.transform.forward, toSubject) <= coneAngle * 0.5f
            && subject.IsVisibleFrom(origin, obstructionMask);
    }

    public static bool AnyIlluminates(PhotoSubject subject)
    {
        foreach (var flash in All)
            if (flash.Illuminates(subject)) return true;
        return false;
    }
}
