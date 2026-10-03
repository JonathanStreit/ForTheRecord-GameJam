using UnityEngine;

/// <summary>
/// Builds flat ring / disc segment meshes in the XZ plane (used by the ground indicators).
/// Angles are in degrees, 0 = forward (+Z), 90 = right (+X).
/// </summary>
public static class RingMesh
{
    public static Mesh Create(float innerRadius, float outerRadius, float startAngle, float endAngle, int segments)
    {
        var vertices = new Vector3[(segments + 1) * 2];
        var triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)segments) * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            vertices[i * 2] = direction * innerRadius;
            vertices[i * 2 + 1] = direction * outerRadius;
        }
        for (int i = 0; i < segments; i++)
        {
            int v = i * 2, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 3;
            triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }

        var mesh = new Mesh { name = "Ring", vertices = vertices, triangles = triangles };
        mesh.RecalculateBounds();
        return mesh;
    }
}
