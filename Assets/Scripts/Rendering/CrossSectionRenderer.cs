using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Vẽ outline + nhãn của 1 mặt cắt trên hình.
///  - Circle    → vẽ polyline 64 đoạn
///  - Polygon   → vẽ closed polyline qua các đỉnh
///  - Vẽ thêm label tên + công thức diện tích
///
/// Dùng làm child của hình bị cắt. Đặt trong local-space để mặt cắt theo hình khi grab/scale.
/// </summary>
public class CrossSectionRenderer : MonoBehaviour
{
    public Material outlineMaterial;
    public Color outlineColor = Color.red;
    public float outlineWidth = 0.008f;

    [Tooltip("Số đoạn để vẽ đường tròn cho mịn")]
    public int circleSegments = 64;

    private readonly List<GameObject> spawned = new();
    private GameObject labelGo;

    /// <summary>
    /// Vẽ mặt cắt mới. Gọi lại sẽ xoá outline cũ.
    /// </summary>
    public void Draw(CrossSectionDefinition def)
    {
        Clear();
        if (def == null) return;

        if (def.shape == CrossSectionDefinition.SectionShape.Circle)
        {
            DrawCircle(def);
        }
        else if (def.polygonVertices != null && def.polygonVertices.Length >= 3)
        {
            DrawPolygon(def.polygonVertices);
        }

        DrawLabel(def);
    }

    public void Clear()
    {
        foreach (var g in spawned) if (g) Destroy(g);
        spawned.Clear();
        if (labelGo != null) Destroy(labelGo);
    }

    // ----- Internals -----
    private void DrawCircle(CrossSectionDefinition def)
    {
        Vector3 n = def.normal.normalized;
        Vector3 t = Vector3.Cross(n, Mathf.Abs(n.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 b = Vector3.Cross(n, t).normalized;

        GameObject go = new GameObject("Section_Circle");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = circleSegments;
        lr.startWidth = lr.endWidth = outlineWidth;
        lr.material = outlineMaterial != null ? outlineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = outlineColor;

        for (int i = 0; i < circleSegments; i++)
        {
            float ang = (i / (float)circleSegments) * Mathf.PI * 2f;
            Vector3 p = def.pointOnPlane + (t * Mathf.Cos(ang) + b * Mathf.Sin(ang)) * def.size;
            lr.SetPosition(i, p);
        }
        spawned.Add(go);
    }

    private void DrawPolygon(Vector3[] verts)
    {
        GameObject go = new GameObject("Section_Polygon");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = verts.Length;
        lr.startWidth = lr.endWidth = outlineWidth;
        lr.material = outlineMaterial != null ? outlineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = outlineColor;
        for (int i = 0; i < verts.Length; i++) lr.SetPosition(i, verts[i]);
        spawned.Add(go);
    }

    private void DrawLabel(CrossSectionDefinition def)
    {
        labelGo = new GameObject("Section_Label");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = def.pointOnPlane + def.normal.normalized * 0.05f;

        var tm = labelGo.AddComponent<TextMeshPro>();
        tm.text = string.IsNullOrEmpty(def.areaFormula)
            ? def.displayName
            : $"{def.displayName}\n{def.areaFormula}";
        tm.fontSize = 1.5f;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = outlineColor;

        if (labelGo.GetComponent<BillboardLabel>() == null)
            labelGo.AddComponent<BillboardLabel>();

        labelGo.transform.localScale = Vector3.one * 0.05f;
    }
}
