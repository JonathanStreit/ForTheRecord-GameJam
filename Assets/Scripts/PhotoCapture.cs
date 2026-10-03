using System;
using System.Collections.Generic;
using UnityEngine;

public class PhotoResult
{
    public Texture Photo;
    public readonly List<PhotoSubject> InFrame = new List<PhotoSubject>();
    public readonly List<PhotoSubject> Lit = new List<PhotoSubject>();
}

/// <summary>
/// The lens of the big camera. Renders the photo into a texture and checks which subjects
/// are in frame (viewport check + line of sight) and lit by a flash.
/// </summary>
public class PhotoCapture : MonoBehaviour
{
    public static event Action<PhotoResult> PhotoTaken;

    [Tooltip("Camera component used as the lens. It is disabled and only rendered when a photo is taken.")]
    [SerializeField] Camera lens;
    [SerializeField] Vector2Int resolution = new Vector2Int(640, 480);
    [SerializeField] float maxSubjectDistance = 12f;
    [Tooltip("How far from the edge of the picture a subject has to be (0 = anywhere in the picture).")]
    [SerializeField, Range(0f, 0.4f)] float frameMargin = 0.1f;
    [SerializeField] LayerMask obstructionMask = ~0;

    RenderTexture _photo;

    public Transform Lens => lens.transform;
    public float MaxSubjectDistance => maxSubjectDistance;
    /// <summary>Half of the horizontal angle (degrees) in which a subject counts as in frame.</summary>
    public float FrameHalfAngle
    {
        get
        {
            float horizontalFov = Camera.VerticalToHorizontalFieldOfView(lens.fieldOfView, (float)resolution.x / resolution.y);
            return Mathf.Atan(Mathf.Tan(horizontalFov * 0.5f * Mathf.Deg2Rad) * (1f - 2f * frameMargin)) * Mathf.Rad2Deg;
        }
    }

    void Awake()
    {
        _photo = new RenderTexture(resolution.x, resolution.y, 24);
        lens.enabled = false;
        lens.targetTexture = _photo;
    }

    void OnDestroy()
    {
        if (_photo != null) _photo.Release();
    }

    public PhotoResult TakePhoto()
    {
        lens.Render();

        var result = new PhotoResult { Photo = _photo };
        foreach (var subject in PhotoSubject.All)
        {
            if (!IsInFrame(subject)) continue;
            result.InFrame.Add(subject);
            if (FlashUnit.AnyIlluminates(subject))
                result.Lit.Add(subject);
        }

        PhotoTaken?.Invoke(result);
        return result;
    }

    public bool IsInFrame(PhotoSubject subject)
    {
        Vector3 viewport = lens.WorldToViewportPoint(subject.Center);
        return viewport.z > 0f && viewport.z <= maxSubjectDistance
            && viewport.x >= frameMargin && viewport.x <= 1f - frameMargin
            && viewport.y >= frameMargin && viewport.y <= 1f - frameMargin
            && subject.IsVisibleFrom(lens.transform.position, obstructionMask);
    }
}
