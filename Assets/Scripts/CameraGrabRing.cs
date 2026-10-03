using UnityEngine;

/// <summary>
/// Ground circle around the big camera. The outline shows the grab range; each half fills with
/// the colour of the player holding the handle on that side. While carried, everything turns
/// red as the players get close to dropping the camera.
/// </summary>
public class CameraGrabRing : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    [SerializeField] HeavyCamera target;
    [Tooltip("Transparent unlit material; the colours are set per renderer.")]
    [SerializeField] Material material;
    [SerializeField] float lineWidth = 0.15f;
    [SerializeField] float groundHeight = 0.04f;
    [SerializeField] int segments = 48;
    [SerializeField] Color outlineColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] Color dangerColor = new Color(1f, 0.1f, 0.05f, 0.8f);
    [SerializeField, Range(0f, 1f)] float fillAlpha = 0.4f;

    MeshRenderer _outline, _leftFill, _rightFill;
    MaterialPropertyBlock _block;

    void Start()
    {
        _block = new MaterialPropertyBlock();
        float radius = target.GrabRadius;
        _outline = CreatePart("Outline", RingMesh.Create(radius - lineWidth, radius, 0f, 360f, segments));
        _rightFill = CreatePart("Right Fill", RingMesh.Create(0f, radius - lineWidth, 0f, 180f, segments / 2));
        _leftFill = CreatePart("Left Fill", RingMesh.Create(0f, radius - lineWidth, 180f, 360f, segments / 2));
    }

    MeshRenderer CreatePart(string partName, Mesh mesh)
    {
        var part = new GameObject(partName, typeof(MeshFilter), typeof(MeshRenderer));
        part.layer = gameObject.layer;
        part.transform.SetParent(transform, false);
        part.GetComponent<MeshFilter>().mesh = mesh;
        var partRenderer = part.GetComponent<MeshRenderer>();
        partRenderer.sharedMaterial = material;
        partRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return partRenderer;
    }

    void LateUpdate()
    {
        // Stay flat on the ground under the camera, turned so the halves match its handles.
        Vector3 position = target.transform.position;
        Vector3 forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up);
        Quaternion rotation = forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward) : transform.rotation;
        transform.SetPositionAndRotation(new Vector3(position.x, groundHeight, position.z), rotation);

        float stress = target.CarryStress;
        SetColor(_outline, Color.Lerp(outlineColor, dangerColor, stress));
        ShowHolder(_leftFill, target.LeftHolder, stress);
        ShowHolder(_rightFill, target.RightHolder, stress);
    }

    void ShowHolder(MeshRenderer fill, PlayerController holder, float stress)
    {
        fill.enabled = holder != null;
        if (holder == null) return;
        Color color = Color.Lerp(holder.Color, dangerColor, stress);
        color.a = fillAlpha;
        SetColor(fill, color);
    }

    void SetColor(MeshRenderer part, Color color)
    {
        _block.SetColor(BaseColor, color);
        part.SetPropertyBlock(_block);
    }
}
