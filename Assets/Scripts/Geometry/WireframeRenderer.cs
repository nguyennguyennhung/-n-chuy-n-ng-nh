using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HIỆN CÁC CẠNH (WIREFRAME) CỦA KHỐI HÌNH HỌC.
/// 
/// Khi bật wireframe, các cạnh của khối sẽ được vẽ bằng đường kẻ màu đen,
/// giúp nhìn rõ cấu trúc hình học (đặc biệt hữu ích khi khối đang trong suốt).
/// 
/// CÁCH GẮN:
/// Gắn script này vào MỖI khối hình học (Cube, Sphere, Cylinder, Pyramid, Cone).
/// Hoặc tốt hơn: gắn vào GameManager và nó sẽ tự quản lý.
/// 
/// CÁCH DÙNG: Nhấn phím W khi đang chọn 1 khối → bật/tắt hiện cạnh.
/// </summary>
public class WireframeRenderer : MonoBehaviour
{
    [Header("Cài đặt đường cạnh")]
    [Tooltip("Màu đường cạnh")]
    public Color edgeColor = new Color(0.7f, 0.95f, 0.85f); // Màu xanh bạc hà dịu mắt

    [Tooltip("Độ dày đường cạnh")]
    public float edgeWidth = 0.004f;

    // Lưu các đường cạnh đã tạo cho từng khối
    // Key = GameObject (khối hình), Value = danh sách LineRenderer
    private Dictionary<GameObject, List<LineRenderer>> wireframeMap
        = new Dictionary<GameObject, List<LineRenderer>>();

    // Tham chiếu đến ObjectInteraction để biết khối nào đang chọn
    private ObjectInteraction interaction;

    void Start()
    {
        interaction = FindObjectOfType<ObjectInteraction>();
    }

    void Update()
    {
        if (interaction == null) return;

        // Nhấn W → bật/tắt wireframe cho khối đang chọn
        if (Input.GetKeyDown(KeyCode.W))
        {
            GeometryObject selected = interaction.GetSelectedObject();
            if (selected != null)
            {
                ToggleWireframe(selected.gameObject);
            }
        }

        // CẬP NHẬT MÀU SẮC THỜI GIAN THỰC
        // Giúp người dùng thấy thay đổi ngay lập tức khi chỉnh màu trong Inspector
        SyncColors();
    }

    /// <summary>
    /// Đồng bộ màu sắc từ biến edgeColor vào các LineRenderer đang hiển thị.
    /// </summary>
    void SyncColors()
    {
        foreach (var pair in wireframeMap)
        {
            foreach (LineRenderer lr in pair.Value)
            {
                if (lr != null)
                {
                    lr.startColor = edgeColor;
                    lr.endColor = edgeColor;
                    lr.startWidth = edgeWidth;
                    lr.endWidth = edgeWidth;
                    if (lr.material != null) lr.material.color = edgeColor;
                }
            }
        }
    }

    /// <summary>
    /// Bật/tắt wireframe cho một khối.
    /// </summary>
    void ToggleWireframe(GameObject target)
    {
        if (wireframeMap.ContainsKey(target))
        {
            // Đã có wireframe → XÓA (tắt)
            RemoveWireframe(target);
        }
        else
        {
            // Chưa có → TẠO (bật)
            CreateWireframe(target);
        }
    }

    /// <summary>
    /// Tạo wireframe (vẽ các cạnh) cho một khối.
    /// </summary>
    void CreateWireframe(GameObject target)
    {
        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (mf == null || mf.mesh == null) return;

        Mesh mesh = mf.mesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        // === TÌM TẤT CẢ CÁC CẠNH DUY NHẤT ===
        HashSet<string> edgeSet = new HashSet<string>();
        List<Vector2Int> edges = new List<Vector2Int>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            AddEdge(tris[i], tris[i + 1], verts, edgeSet, edges);
            AddEdge(tris[i + 1], tris[i + 2], verts, edgeSet, edges);
            AddEdge(tris[i + 2], tris[i], verts, edgeSet, edges);
        }

        // === TẠO LINERENDERER CHO MỖI CẠNH ===
        List<LineRenderer> lineRenderers = new List<LineRenderer>();

        // Tạo material mới cho mỗi khối (để tránh ảnh hưởng lẫn nhau)
        // Dùng shader Sprites/Default hoặc Unlit/Color để tương thích tốt nhất
        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null) lineShader = Shader.Find("Unlit/Color");
        
        Material lineMat = new Material(lineShader);
        lineMat.color = edgeColor;

        foreach (Vector2Int edge in edges)
        {
            GameObject lineObj = new GameObject("EdgeLine");
            lineObj.transform.SetParent(target.transform, false);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = lineMat;
            lr.startColor = edgeColor;
            lr.endColor = edgeColor;
            lr.startWidth = edgeWidth;
            lr.endWidth = edgeWidth;
            lr.positionCount = 2;
            lr.SetPosition(0, verts[edge.x]);
            lr.SetPosition(1, verts[edge.y]);

            lineRenderers.Add(lr);
        }

        wireframeMap[target] = lineRenderers;
    }

    /// <summary>
    /// Xóa wireframe (ẩn cạnh) của một khối.
    /// </summary>
    void RemoveWireframe(GameObject target)
    {
        if (!wireframeMap.ContainsKey(target)) return;

        foreach (LineRenderer lr in wireframeMap[target])
        {
            if (lr != null && lr.gameObject != null) Destroy(lr.gameObject);
        }

        wireframeMap.Remove(target);
    }

    /// <summary>
    /// Thêm một cạnh vào danh sách (nếu chưa có).
    /// </summary>
    void AddEdge(int idx1, int idx2, Vector3[] verts, HashSet<string> edgeSet, List<Vector2Int> edges)
    {
        int a = Mathf.Min(idx1, idx2);
        int b = Mathf.Max(idx1, idx2);

        string key = RoundVec(verts[a]) + "_" + RoundVec(verts[b]);

        if (!edgeSet.Contains(key))
        {
            edgeSet.Add(key);
            edges.Add(new Vector2Int(idx1, idx2));
        }
    }

    string RoundVec(Vector3 v)
    {
        return $"{v.x:F3},{v.y:F3},{v.z:F3}";
    }

    // Tự động cập nhật trong Editor khi chỉnh thông số
    void OnValidate()
    {
        if (Application.isPlaying) SyncColors();
    }
}
