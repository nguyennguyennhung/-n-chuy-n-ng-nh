using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConeMesh : MonoBehaviour
{
    public int segments = 32;  // số mặt quanh nón

    void Awake()
    {
        var mesh = new Mesh();
        mesh.name = "ConeMesh";

        float radius = 0.5f;
        float halfHeight = 0.5f;

        // Vertices: đỉnh S + segments điểm trên mép đáy + tâm đáy
        var vertices = new Vector3[segments + 2];
        vertices[0] = new Vector3(0, halfHeight, 0);  // đỉnh S
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                -halfHeight,
                Mathf.Sin(angle) * radius
            );
        }
        vertices[segments + 1] = new Vector3(0, -halfHeight, 0);  // tâm đáy

        // Triangles: segments tam giác mặt bên + segments tam giác đáy
        var triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            // Mặt bên: đỉnh S — i — next
            triangles[i * 3 + 0] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = next + 1;

            // Đáy: tâm — next — i (ngược chiều để facing xuống)
            triangles[segments * 3 + i * 3 + 0] = segments + 1;
            triangles[segments * 3 + i * 3 + 1] = next + 1;
            triangles[segments * 3 + i * 3 + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}