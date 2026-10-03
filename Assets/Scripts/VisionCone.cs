using UnityEngine;

/// <summary>
/// Flat wedge on the ground showing what the big camera can photograph
/// (the in-frame angle and the maximum subject distance).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    [SerializeField] PhotoCapture photoCamera;
    [SerializeField] float groundHeight = 0.05f;
    [SerializeField] int segments = 24;

    void Start()
    {
        float halfAngle = photoCamera.FrameHalfAngle * Mathf.Deg2Rad;
        float range = photoCamera.MaxSubjectDistance;

        var vertices = new Vector3[segments + 2];
        var triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
            vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * range;
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        var mesh = new Mesh { name = "Vision Cone", vertices = vertices, triangles = triangles };
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        // Stay flat on the ground under the lens, even while the camera swings or tumbles.
        Transform lens = photoCamera.Lens;
        Vector3 forward = Vector3.ProjectOnPlane(lens.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) return;
        transform.SetPositionAndRotation(new Vector3(lens.position.x, groundHeight, lens.position.z), Quaternion.LookRotation(forward));
    }
}
