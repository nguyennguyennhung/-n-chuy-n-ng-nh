using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PyramidMesh : MonoBehaviour
{
    void Awake()
    {
        var mesh = new Mesh();
        mesh.name = "PyramidMesh";

        // 5 đỉnh hình học
        Vector3 S = new Vector3(0,  0.5f, 0);
        Vector3 A = new Vector3(-0.5f, -0.5f, -0.5f);
        Vector3 B = new Vector3( 0.5f, -0.5f, -0.5f);
        Vector3 C = new Vector3( 0.5f, -0.5f,  0.5f);
        Vector3 D = new Vector3(-0.5f, -0.5f,  0.5f);

        // 18 vertices: mỗi mặt giữ vertex riêng để normal đẹp (không chia sẻ)
        // 4 mặt bên × 3 + đáy 2 tam giác × 3 = 18
        Vector3[] vertices =
        {
            // Mặt SAB (0,1,2)
            S, A, B,
            // Mặt SBC (3,4,5)
            S, B, C,
            // Mặt SCD (6,7,8)
            S, C, D,
            // Mặt SDA (9,10,11)
            S, D, A,
            // Đáy tam giác 1 ABC (12,13,14)
            A, B, C,
            // Đáy tam giác 2 ACD (15,16,17)
            A, C, D,
        };

        // Triangles: 4 mặt bên facing ra ngoài, 2 tam giác đáy facing xuống
        int[] triangles =
        {
            // 4 mặt bên — chiều thuận, normal hướng ngoài
            0, 1, 2,
            3, 4, 5,
            6, 7, 8,
            9, 10, 11,
            // 2 tam giác đáy — ngược chiều (A,C,B và A,D,C) để normal xuống
            12, 14, 13,
            15, 17, 16,
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}