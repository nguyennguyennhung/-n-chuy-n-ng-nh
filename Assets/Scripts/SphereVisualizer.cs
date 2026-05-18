using UnityEngine;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class SphereVisualizer : MonoBehaviour
{
    [Header("Line Settings")]
    public Color edgeColor = Color.white;
    public float edgeWidth = 0.005f; // Tối ưu cho VR (5mm)
    public bool drawEquator = true;   // Vẽ đường xích đạo (ngang)
    public bool drawMeridians = true; // Vẽ các đường kinh tuyến (dọc)

    [Header("Radius Settings (r)")]
    public bool showRadius = true;
    public Color radiusLineColor = Color.cyan;
    public Color radiusSymbolColor = Color.cyan;

    [Header("Label Settings")]
    public float fontSize = 2f;
    public Color labelColor = Color.white; // Màu chữ của tâm O và điểm O'

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

        // Tính toán bán kính và tâm thực tế từ Mesh Bounds
        Bounds bounds = mf.sharedMesh.bounds;
        Vector3 center = bounds.center;
        float radius = bounds.extents.x; // Lấy bán kính theo trục X làm chuẩn

        // Khoảng cách offset tự động theo kích thước khối
        float autoOffset = bounds.extents.magnitude * 0.2f;

        // 1. Vẽ các đường tròn tạo nên khung dây (wireframe) hình cầu chuẩn giáo trình
        if (drawEquator)
        {
            // Vẽ đường xích đạo ngang (mặt phẳng XZ)
            CreateProceduralCircle("Equator", center, radius, Vector3.up, edgeColor);
        }

        if (drawMeridians)
        {
            // Vẽ 2 đường kinh tuyến dọc vuông góc nhau (mặt phẳng XY và YZ)
            CreateProceduralCircle("Meridian_XY", center, radius, Vector3.forward, edgeColor);
            CreateProceduralCircle("Meridian_YZ", center, radius, Vector3.right, edgeColor);
        }

        // 2. Tạo điểm O' trên bề mặt hình cầu và vẽ bán kính r từ O -> O'
        // Chọn điểm O' nằm trên đường xích đạo nghiêng góc 30 độ để nhìn 3D rõ nhất
        float angleRad = 30f * Mathf.Deg2Rad;
        Vector3 oPrimeLocal = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * radius;
        Vector3 oPrime = center + oPrimeLocal;

        if (showRadius)
        {
            // Vẽ đường bán kính OO' màu xanh
            CreateLine("Radius_Line", center, oPrime, radiusLineColor);

            // Dán nhãn r màu xanh da trời ở giữa bán kính
            Vector3 rLabelPos = Vector3.Lerp(center, oPrime, 0.5f) + Vector3.up * autoOffset * 0.4f;
            CreateLabel("r", rLabelPos, radiusSymbolColor, fontSize * 0.8f);
        }

        // 3. Dán nhãn tâm O và điểm O'
        // Đẩy nhãn O hơi chệch xuống để không đè vào tâm
        Vector3 oLabelPos = center - Vector3.forward * autoOffset * 0.5f + Vector3.down * autoOffset * 0.3f;
        CreateLabel("O", oLabelPos, labelColor);

        // Đẩy nhãn O' hướng ra ngoài bề mặt để dễ nhìn
        Vector3 oPrimeLabelPos = oPrime + oPrimeLocal.normalized * autoOffset;
        CreateLabel("O'", oPrimeLabelPos, labelColor);
    }

    // Hàm vẽ đường tròn thủ tục mượt mà
    void CreateProceduralCircle(string name, Vector3 centerPoint, float circleRadius, Vector3 normal, Color color)
    {
        GameObject circleObj = new GameObject(name);
        circleObj.transform.SetParent(transform, false);
        circleObj.transform.localPosition = Vector3.zero;
        circleObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;

        float adjustedWidth = edgeWidth;
        if (transform.lossyScale.x > 10f) adjustedWidth = edgeWidth / transform.lossyScale.x * 2f;

        lr.startWidth = adjustedWidth;
        lr.endWidth = adjustedWidth;

        int segments = 60; // 60 điểm giúp vòng tròn cực kỳ mượt trong VR
        lr.positionCount = segments + 1;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i * 360f / segments) * Mathf.Deg2Rad;
            Vector3 localPoint = new Vector3(Mathf.Cos(angle) * circleRadius, 0f, Mathf.Sin(angle) * circleRadius);
            Vector3 rotatedPoint = rotation * localPoint + centerPoint;
            lr.SetPosition(i, rotatedPoint);
        }

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", color);
        lr.material = mat;
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
        mat.SetColor("_BaseColor", color); // URP-safe tinting
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
