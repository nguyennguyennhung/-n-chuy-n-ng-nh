using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PyramidVisualizer : MonoBehaviour
{
    [Header("Line Settings")]
    public Color edgeColor = Color.white;
    public float edgeWidth = 0.005f; // Tối ưu cho VR: để mỏng (5mm)
    
    [Header("Height Settings")]
    public bool showHeight = true;
    public Color heightLineColor = Color.yellow;
    public Color heightSymbolColor = Color.yellow; // Màu vàng cho chữ h (hoặc có thể chỉnh bất kỳ màu nào trong Inspector)

    [Header("Label Settings")]
    public float fontSize = 2f;
    public float labelOffset = 0.1f; // Đẩy text ra ngoài đỉnh 0.1 đơn vị

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
        
        // Lọc các đỉnh trùng lặp
        List<Vector3> uniqueVertices = new List<Vector3>();
        foreach (Vector3 v in vertices)
        {
            if (!uniqueVertices.Any(uv => Vector3.Distance(uv, v) < 0.01f))
            {
                uniqueVertices.Add(v);
            }
        }

        // Sắp xếp đỉnh theo chiều cao (y): Đỉnh cao nhất là chóp (Apex)
        uniqueVertices.Sort((a, b) => b.y.CompareTo(a.y));
        
        if (uniqueVertices.Count < 4)
        {
            Debug.LogWarning("Một hình chóp cần ít nhất 4 đỉnh. Tìm thấy: " + uniqueVertices.Count);
            return;
        }

        Vector3 apex = uniqueVertices[0];
        List<Vector3> baseVertices = uniqueVertices.Skip(1).ToList();

        // 1. Tìm tâm tạm thời của đáy
        Vector3 tempCenter = Vector3.zero;
        foreach (var v in baseVertices) tempCenter += v;
        tempCenter /= baseVertices.Count;

        // 2. Lọc bỏ điểm tâm dư thừa ở đáy (do ProBuilder tự sinh ra ở giữa)
        // Bằng cách chỉ lấy các đỉnh nằm ở viền (cách xa tâm)
        float maxRadius = baseVertices.Max(v => Vector3.Distance(new Vector3(v.x, 0, v.z), new Vector3(tempCenter.x, 0, tempCenter.z)));
        baseVertices = baseVertices.Where(v => Vector3.Distance(new Vector3(v.x, 0, v.z), new Vector3(tempCenter.x, 0, tempCenter.z)) > maxRadius * 0.5f).ToList();

        // Tìm tâm chính xác lại sau khi đã lọc
        Vector3 center = Vector3.zero;
        foreach (var v in baseVertices) center += v;
        center /= baseVertices.Count;

        // Sắp xếp các đỉnh đáy theo vòng tròn để nối viền không bị chéo
        baseVertices.Sort((a, b) => {
            float angleA = Mathf.Atan2(a.z - center.z, a.x - center.x);
            float angleB = Mathf.Atan2(b.z - center.z, b.x - center.x);
            return angleA.CompareTo(angleB);
        });

        // Tính toán khoảng cách an toàn (autoOffset) dựa trên kích thước thật của khối
        // Bằng cách này dù khối bị phóng to/thu nhỏ 100 lần trong Canvas thì chữ vẫn bám sát viền
        float autoOffset = mf.sharedMesh.bounds.extents.magnitude * 0.2f;

        // Tạo tên đỉnh đáy (A, B, C, D...)
        char labelChar = 'A';
        string[] baseNames = new string[baseVertices.Count];
        for (int i = 0; i < baseVertices.Count; i++)
        {
            baseNames[i] = labelChar.ToString();
            labelChar++;
        }

        // Vẽ cạnh đáy (Không tạo nhãn AB, BC, CD... để tránh rối mắt)
        for (int i = 0; i < baseVertices.Count; i++)
        {
            int nextIndex = (i + 1) % baseVertices.Count;
            Vector3 start = baseVertices[i];
            Vector3 end = baseVertices[nextIndex];
            
            CreateLine("Edge_Base_" + i, start, end, edgeColor);
        }

        // Vẽ cạnh bên (Không tạo nhãn SA, SB, SC... để tránh rối mắt)
        for (int i = 0; i < baseVertices.Count; i++)
        {
            Vector3 baseVertex = baseVertices[i];
            CreateLine("Edge_Side_" + i, apex, baseVertex, edgeColor);
        }

        // Vẽ đường cao và nhãn h
        if (showHeight)
        {
            CreateLine("Height_Line", apex, center, heightLineColor);
            
            // Chữ h màu xanh da trời ở giữa đường cao
            CreateLabel("h", Vector3.Lerp(apex, center, 0.5f) + Vector3.right * autoOffset * 0.5f, heightSymbolColor, fontSize * 0.8f);
        }

        // Dán nhãn các đỉnh (S, A, B, C, D)
        CreateLabel("S", apex + Vector3.up * autoOffset); // Đỉnh chóp đẩy lên trên
        
        for (int i = 0; i < baseVertices.Count; i++)
        {
            // Từng đỉnh đáy đẩy văng ra xa khỏi tâm để không bị chìm vào trong khối
            Vector3 outwardDir = (baseVertices[i] - center).normalized;
            CreateLabel(baseNames[i], baseVertices[i] + outwardDir * autoOffset);
        }
    }

    void CreateLine(string name, Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform, false);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        
        // Tự động thu nhỏ nét vẽ nếu Object bị phóng to trong Canvas
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
        
        // Tự động thu nhỏ chữ nếu Object bị phóng to trong Canvas
        float finalFontSize = customFontSize > 0 ? customFontSize : fontSize;
        if (transform.lossyScale.x > 10f) finalFontSize = finalFontSize / transform.lossyScale.x * 5f;
        
        tm.fontSize = finalFontSize;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = color ?? Color.white;
        
        labelObj.AddComponent<BillboardLabel>();
    }
}

public class BillboardLabel : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camTransform != null)
        {
            transform.LookAt(transform.position + camTransform.rotation * Vector3.forward, camTransform.rotation * Vector3.up);
        }
    }
}
