using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class CylinderVisualizer : MonoBehaviour
{
    [Header("Line Settings")]
    public Color edgeColor = Color.white;
    public float edgeWidth = 0.005f; // Tối ưu cho VR (5mm)

    [Header("Height Settings (h)")]
    public bool showHeight = true;
    public Color heightLineColor = Color.yellow;
    public Color heightSymbolColor = Color.yellow;

    [Header("Radius Settings (r)")]
    public bool showRadius = true;
    public Color radiusLineColor = Color.cyan;
    public Color radiusSymbolColor = Color.cyan;

    [Header("Label Settings")]
    public float fontSize = 2f;
    public Color labelColor = Color.white; // Màu chữ của các tâm O, O'

    private void Start()
    {
        // Khởi tạo sau 0.1s để đảm bảo Mesh đã load xong
        Invoke("InitializeVisuals", 0.1f);
    }

    void InitializeVisuals()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("No MeshFilter found on " + name);
            return;
        }

        Vector3[] vertices = mf.sharedMesh.vertices;
        
        // 1. Lọc lấy các đỉnh độc nhất
        List<Vector3> uniqueVertices = new List<Vector3>();
        foreach (Vector3 v in vertices)
        {
            if (!uniqueVertices.Any(uv => Vector3.Distance(uv, v) < 0.01f))
            {
                uniqueVertices.Add(v);
            }
        }

        if (uniqueVertices.Count < 6)
        {
            Debug.LogWarning("Không đủ số đỉnh để nhận diện hình trụ.");
            return;
        }

        // 2. Tính toán các thông số Y và Tâm
        float maxY = uniqueVertices.Max(v => v.y);
        float minY = uniqueVertices.Min(v => v.y);
        float centerX = uniqueVertices.Average(v => v.x);
        float centerZ = uniqueVertices.Average(v => v.z);

        // Tâm đáy trên O' và đáy dưới O
        Vector3 oPrime = new Vector3(centerX, maxY, centerZ); // O'
        Vector3 oCenter = new Vector3(centerX, minY, centerZ); // O

        // 3. Lọc lấy các đỉnh nằm trên đường viền tròn (Rim) đáy trên và dưới
        List<Vector3> topRim = uniqueVertices
            .Where(v => Mathf.Abs(v.y - maxY) < 0.01f && Vector2.Distance(new Vector2(v.x, v.z), new Vector2(centerX, centerZ)) > 0.05f)
            .ToList();
            
        List<Vector3> bottomRim = uniqueVertices
            .Where(v => Mathf.Abs(v.y - minY) < 0.01f && Vector2.Distance(new Vector2(v.x, v.z), new Vector2(centerX, centerZ)) > 0.05f)
            .ToList();

        // Sắp xếp các đỉnh viền theo hình tròn để vẽ nét đứt/liền xoay vòng
        topRim.Sort((a, b) => Mathf.Atan2(a.z - centerZ, a.x - centerX).CompareTo(Mathf.Atan2(b.z - centerZ, b.x - centerX)));
        bottomRim.Sort((a, b) => Mathf.Atan2(a.z - centerZ, a.x - centerX).CompareTo(Mathf.Atan2(b.z - centerZ, b.x - centerX)));

        // Tính khoảng cách offset tự động theo kích thước khối
        float autoOffset = mf.sharedMesh.bounds.extents.magnitude * 0.2f;

        // 4. Vẽ đường tròn đáy trên
        for (int i = 0; i < topRim.Count; i++)
        {
            CreateLine("TopRim_" + i, topRim[i], topRim[(i + 1) % topRim.Count], edgeColor);
        }

        // 5. Vẽ đường tròn đáy dưới
        for (int i = 0; i < bottomRim.Count; i++)
        {
            CreateLine("BottomRim_" + i, bottomRim[i], bottomRim[(i + 1) % bottomRim.Count], edgeColor);
        }

        // 6. Vẽ 2 đường sinh bên (để khối hình trụ rõ ràng cấu trúc)
        if (topRim.Count > 0 && bottomRim.Count > 0)
        {
            // Nối đỉnh đầu tiên và đỉnh đối diện ở giữa danh sách vòng tròn
            int indexOpposite = topRim.Count / 2;
            
            CreateLine("Side_Line_1", topRim[0], bottomRim[0], edgeColor);
            CreateLine("Side_Line_2", topRim[indexOpposite], bottomRim[indexOpposite], edgeColor);
        }

        // 7. Vẽ trục/đường cao OO' và ký hiệu h
        if (showHeight)
        {
            CreateLine("Height_Line", oCenter, oPrime, heightLineColor);
            
            // Nhãn chữ h màu vàng ở giữa trục đứng
            Vector3 hLabelPos = Vector3.Lerp(oCenter, oPrime, 0.5f) + Vector3.right * autoOffset * 0.5f;
            CreateLabel("h", hLabelPos, heightSymbolColor, fontSize * 0.8f);
        }

        // 8. Vẽ bán kính r ở đáy dưới
        if (showRadius && bottomRim.Count > 0)
        {
            // Chọn đỉnh đáy bất kỳ để nối từ tâm O ra
            Vector3 targetBottomPoint = bottomRim[0];
            CreateLine("Radius_Line", oCenter, targetBottomPoint, radiusLineColor);

            // Nhãn chữ r màu xanh ở giữa đường bán kính
            Vector3 rLabelPos = Vector3.Lerp(oCenter, targetBottomPoint, 0.5f) + Vector3.up * autoOffset * 0.4f;
            CreateLabel("r", rLabelPos, radiusSymbolColor, fontSize * 0.8f);
        }

        // 9. Dán nhãn tâm O và O'
        CreateLabel("O'", oPrime + Vector3.up * autoOffset, labelColor);
        CreateLabel("O", oCenter - Vector3.up * autoOffset, labelColor);
    }

    void CreateLine(string name, Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform, false);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;

        float adjustedWidth = edgeWidth;
        if (transform.lossyScale.x > 10f) adjustedWidth = edgeWidth / transform.lossyScale.x * 2f;

        lr.startWidth = adjustedWidth;
        lr.endWidth = adjustedWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", color); // Gán màu trực tiếp vào Material URP để hoạt động tốt trên URP
        lr.material = mat;
    }

    void CreateLabel(string text, Vector3 position, Color? color = null, float customFontSize = -1)
    {
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = position;

        TextMeshPro tm = labelObj.AddComponent<TextMeshPro>();
        tm.text = text;

        float finalFontSize = customFontSize > 0 ? customFontSize : fontSize;
        if (transform.lossyScale.x > 10f) finalFontSize = finalFontSize / transform.lossyScale.x * 5f;

        tm.fontSize = finalFontSize;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = color ?? Color.white;

        labelObj.AddComponent<BillboardLabel>();
    }
}
