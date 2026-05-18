using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class ConeVisualizer : MonoBehaviour
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
    public Color labelColor = Color.white; // Màu chữ của các đỉnh (S, A, B, O)

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

        if (uniqueVertices.Count < 4)
        {
            Debug.LogWarning("Không đủ số đỉnh để nhận diện hình nón.");
            return;
        }

        // 2. Xác định đỉnh chóp S (Apex) và Tâm đáy O (Center)
        // Đỉnh chóp S có tọa độ Y cao nhất
        uniqueVertices.Sort((a, b) => b.y.CompareTo(a.y));
        Vector3 apex = uniqueVertices[0]; // Đỉnh S

        // Tìm Y thấp nhất để làm đáy
        float minY = uniqueVertices.Min(v => v.y);
        float centerX = uniqueVertices.Average(v => v.x);
        float centerZ = uniqueVertices.Average(v => v.z);
        Vector3 oCenter = new Vector3(centerX, minY, centerZ); // Tâm đáy O

        // 3. Lọc các đỉnh viền tròn đáy (Rim)
        List<Vector3> baseRim = uniqueVertices
            .Where(v => Mathf.Abs(v.y - minY) < 0.01f && Vector2.Distance(new Vector2(v.x, v.z), new Vector2(centerX, centerZ)) > 0.05f)
            .ToList();

        // Sắp xếp các đỉnh đáy theo vòng tròn
        baseRim.Sort((a, b) => Mathf.Atan2(a.z - centerZ, a.x - centerX).CompareTo(Mathf.Atan2(b.z - centerZ, b.x - centerX)));

        if (baseRim.Count < 2)
        {
            Debug.LogError("Lỗi lọc đỉnh đáy. Không đủ số lượng đỉnh đáy để hiển thị.");
            return;
        }

        // Tính khoảng cách offset tự động theo kích thước khối
        float autoOffset = mf.sharedMesh.bounds.extents.magnitude * 0.2f;

        // 4. Vẽ đường tròn viền đáy
        for (int i = 0; i < baseRim.Count; i++)
        {
            CreateLine("BaseRim_" + i, baseRim[i], baseRim[(i + 1) % baseRim.Count], edgeColor);
        }

        // 5. Xác định hai điểm đối xứng nhau ở đáy (A và B) để vẽ đường sinh bên
        Vector3 posA = baseRim[0];
        Vector3 posB = baseRim[baseRim.Count / 2];

        // Vẽ 2 đường sinh bên (SA và SB)
        CreateLine("Slant_Line_A", apex, posA, edgeColor);
        CreateLine("Slant_Line_B", apex, posB, edgeColor);

        // 6. Vẽ đường cao trục đứng SO và ký hiệu h
        if (showHeight)
        {
            CreateLine("Height_Line", oCenter, apex, heightLineColor);
            
            // Ký hiệu h màu vàng nằm giữa đường cao
            Vector3 hLabelPos = Vector3.Lerp(oCenter, apex, 0.5f) + Vector3.right * autoOffset * 0.5f;
            CreateLabel("h", hLabelPos, heightSymbolColor, fontSize * 0.8f);
        }

        // 7. Vẽ đường bán kính đáy OA và ký hiệu r
        if (showRadius)
        {
            CreateLine("Radius_Line", oCenter, posA, radiusLineColor);

            // Ký hiệu r màu xanh nằm giữa đường bán kính
            Vector3 rLabelPos = Vector3.Lerp(oCenter, posA, 0.5f) + Vector3.up * autoOffset * 0.4f;
            CreateLabel("r", rLabelPos, radiusSymbolColor, fontSize * 0.8f);
        }

        // 8. Dán nhãn các đỉnh chính S, A, B và tâm O
        CreateLabel("S", apex + Vector3.up * autoOffset, labelColor);
        CreateLabel("O", oCenter - Vector3.up * autoOffset, labelColor);

        // Đẩy nhãn A và B hướng ra xa tâm O để không bị chìm vào trong khối hình
        Vector3 pushDirA = (posA - oCenter).normalized;
        Vector3 pushDirB = (posB - oCenter).normalized;

        CreateLabel("A", posA + pushDirA * autoOffset, labelColor);
        CreateLabel("B", posB + pushDirB * autoOffset, labelColor);
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
