using UnityEngine;

/// <summary>
/// Ring on the ground showing how close a player has to stand to grab the target.
/// Hidden while the target cannot be grabbed (e.g. both camera handles are taken).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrabRangeRing : MonoBehaviour
{
    [SerializeField] Grabbable target;
    [SerializeField] float lineWidth = 0.15f;
    [SerializeField] float groundHeight = 0.04f;
    [SerializeField] int segments = 48;

    MeshRenderer _renderer;

    void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        float outer = target.GrabRadius, inner = outer - lineWidth;

        var vertices = new Vector3[(segments + 1) * 2];
        var triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            vertices[i * 2] = direction * inner;
            vertices[i * 2 + 1] = direction * outer;
        }
        for (int i = 0; i < segments; i++)
        {
            int v = i * 2, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 3;
            triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }

        var mesh = new Mesh { name = "Grab Range Ring", vertices = vertices, triangles = triangles };
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        // Stay flat on the ground under the target, even while it swings or tumbles.
        _renderer.enabled = target.IsAvailable;
        Vector3 position = target.transform.position;
        transform.SetPositionAndRotation(new Vector3(position.x, groundHeight, position.z), Quaternion.identity);
    }
}
