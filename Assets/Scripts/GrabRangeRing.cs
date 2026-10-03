using UnityEngine;

/// <summary>
/// Ring on the ground showing how close a player has to stand to grab the target.
/// While a player holds the target, the circle is filled with that player's colour.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrabRangeRing : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    [SerializeField] CarryItem target;
    [SerializeField] float lineWidth = 0.1f;
    [SerializeField] float groundHeight = 0.04f;
    [SerializeField] int segments = 48;
    [SerializeField] Color freeColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField, Range(0f, 1f)] float fillAlpha = 0.4f;

    MeshRenderer _outline, _fill;
    MaterialPropertyBlock _block;

    void Start()
    {
        _block = new MaterialPropertyBlock();
        _outline = GetComponent<MeshRenderer>();
        float radius = target.GrabRadius;
        GetComponent<MeshFilter>().mesh = RingMesh.Create(radius - lineWidth, radius, 0f, 360f, segments);

        var fill = new GameObject("Fill", typeof(MeshFilter), typeof(MeshRenderer));
        fill.layer = gameObject.layer;
        fill.transform.SetParent(transform, false);
        fill.GetComponent<MeshFilter>().mesh = RingMesh.Create(0f, radius - lineWidth, 0f, 360f, segments);
        _fill = fill.GetComponent<MeshRenderer>();
        _fill.sharedMaterial = _outline.sharedMaterial;
        _fill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void LateUpdate()
    {
        // Stay flat on the ground under the target, even while it tumbles or is carried.
        Vector3 position = target.transform.position;
        transform.SetPositionAndRotation(new Vector3(position.x, groundHeight, position.z), Quaternion.identity);

        PlayerController holder = target.Holder;
        _fill.enabled = holder != null;
        Color outline = holder != null ? holder.Color : freeColor;
        outline.a = freeColor.a;
        SetColor(_outline, outline);
        if (holder == null) return;
        Color fillColor = holder.Color;
        fillColor.a = fillAlpha;
        SetColor(_fill, fillColor);
    }

    void SetColor(MeshRenderer part, Color color)
    {
        _block.SetColor(BaseColor, color);
        part.SetPropertyBlock(_block);
    }
}
