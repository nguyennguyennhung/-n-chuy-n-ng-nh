using UnityEngine;

/// <summary>
/// Base class chung cho mọi mesh generator (chóp, nón, sau này thêm các hình khác).
/// Class con chỉ cần override CreateMesh() để định nghĩa mesh đặc trưng.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public abstract class ShapeMeshGenerator : MonoBehaviour
{
    [Header("Vật liệu (để trống = tự tạo màu mặc định)")]
    public Material material;

    // Mesh tự tạo, giữ tham chiếu để destroy đúng cách (tránh leak)
    Mesh generatedMesh;
    Material generatedMaterial;

    // Class con tự định nghĩa
    protected abstract Mesh CreateMesh();
    protected abstract Color DefaultColor { get; }

    void Start()
    {
        Generate();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Hoãn 1 frame để tránh lỗi SendMessage của Unity khi đang serialize
        if (!gameObject.activeInHierarchy) return;
        UnityEditor.EditorApplication.delayCall += SafeGenerateInEditor;
    }

    void SafeGenerateInEditor()
    {
        // Component có thể đã bị destroy giữa lúc delayCall và lúc chạy
        if (this == null) return;
        if (gameObject == null) return;
        if (!gameObject.activeInHierarchy) return;
        Generate();
    }
#endif

    void OnDestroy()
    {
        // Dọn dẹp để tránh leak khi spam OnValidate trong Editor
        if (generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
        if (generatedMaterial != null)
        {
            if (Application.isPlaying) Destroy(generatedMaterial);
            else DestroyImmediate(generatedMaterial);
        }
    }

    void Generate()
    {
        // 1) Mesh — destroy mesh cũ trước khi tạo mới
        if (generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
        generatedMesh = CreateMesh();

        GetComponent<MeshFilter>().sharedMesh = generatedMesh;

        // 2) MeshCollider — cảnh báo nếu mesh quá phức tạp cho Convex
        var col = GetComponent<MeshCollider>();
        if (col != null)
        {
            col.sharedMesh = null;
            col.sharedMesh = generatedMesh;

            // Convex Collider giới hạn 255 verts. Nếu vượt → tự tắt Convex và cảnh báo
            if (generatedMesh.vertexCount > 255)
            {
                col.convex = false;
                Debug.LogWarning(
                    $"[{name}] Mesh có {generatedMesh.vertexCount} verts (>255), " +
                    "đã tắt Convex. Trigger có thể không hoạt động đúng — giảm segments xuống."
                );
            }
            else
            {
                col.convex = true;
            }
        }

        // 3) Material
        var rend = GetComponent<MeshRenderer>();
        if (material != null)
        {
            rend.sharedMaterial = material;
        }
        else
        {
            // Chỉ tạo material default 1 lần, không tạo lại mỗi lần OnValidate
            if (generatedMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
    ?? Shader.Find("Standard");
                generatedMaterial = new Material(shader) { name = name + "_DefaultMat" };
                // URP dùng _BaseColor, built-in dùng _Color → set cả hai cho an toàn
                if (generatedMaterial.HasProperty("_BaseColor"))
                    generatedMaterial.SetColor("_BaseColor", DefaultColor);
                if (generatedMaterial.HasProperty("_Color"))
                    generatedMaterial.SetColor("_Color", DefaultColor);
            }
            rend.sharedMaterial = generatedMaterial;
        }
    }
}