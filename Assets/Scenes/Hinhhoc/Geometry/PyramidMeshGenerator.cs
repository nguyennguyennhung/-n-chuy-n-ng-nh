using UnityEngine;

/// <summary>
/// TẠO HÌNH CHÓP TỨ GIÁC (PYRAMID) - BẢN FIX LỖI ONVALIDATE
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class PyramidMeshGenerator : MonoBehaviour
{
    [Header("Kích thước hình chóp")]
    public float baseSize = 1f;
    public float height = 1.5f;

    [Header("Vật liệu")]
    public Material material;

    void Start()
    {
        Generate();
    }

    void OnValidate()
    {
        // Chỉ chạy trong Editor khi Object đang hoạt động
        if (gameObject.activeInHierarchy)
        {
            // Delay việc vẽ một chút để tránh lỗi SendMessage của Unity
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null) Generate();
            };
            #endif
        }
    }

    void Generate()
    {
        Mesh mesh = CreatePyramidMesh();
        
        // Gán mesh vào Filter
        GetComponent<MeshFilter>().sharedMesh = mesh;

        // Gán mesh vào Collider (đã có sẵn nhờ RequireComponent)
        MeshCollider col = GetComponent<MeshCollider>();
        if (col != null) {
            col.sharedMesh = null; // Ép Unity xóa dữ liệu va chạm cũ
            col.sharedMesh = mesh; // Gán mesh mới để tính toán lại lớp vỏ
            col.convex = true;
        }

        // Xử lý Material
        MeshRenderer rend = GetComponent<MeshRenderer>();
        if (material != null) {
            rend.material = material;
        } else {
            // Tự tạo material URP nếu trống
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material defaultMat = new Material(shader);
            defaultMat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.2f));
            rend.material = defaultMat;
        }

        // Đảm bảo có GeometryObject
        GeometryObject geo = GetComponent<GeometryObject>();
        if (geo == null) geo = gameObject.AddComponent<GeometryObject>();
        geo.shapeName = "Hình chóp";
    }

    Mesh CreatePyramidMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "PyramidMesh";
        float h = baseSize / 2f;

        Vector3[] vertices = new Vector3[] {
            new Vector3(-h, 0,  h), new Vector3( h, 0,  h), new Vector3( h, 0, -h), new Vector3(-h, 0, -h),
            new Vector3(-h, 0, -h), new Vector3( h, 0, -h), new Vector3( 0, height, 0),
            new Vector3( h, 0, -h), new Vector3( h, 0,  h), new Vector3( 0, height, 0),
            new Vector3( h, 0,  h), new Vector3(-h, 0,  h), new Vector3( 0, height, 0),
            new Vector3(-h, 0,  h), new Vector3(-h, 0, -h), new Vector3( 0, height, 0)
        };

        int[] triangles = new int[] {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 7, 9, 8, 10, 12, 11, 13, 15, 14
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
