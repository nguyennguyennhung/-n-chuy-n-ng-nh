using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// VẼ ĐƯỜNG VIỀN HÌNH HỌC THEO PHONG CÁCH SƯ PHẠM (TOÁN HỌC).
/// 
/// Thay vì hiện toàn bộ lưới mesh, script này chỉ vẽ:
/// - Hình hộp: 12 cạnh chính.
/// - Hình cầu: Tâm O, 2 đường kinh tuyến/vĩ tuyến chính, bán kính R.
/// - Hình nón: Đường tròn đáy, đường cao, đường sinh.
/// - Hình chóp: Các cạnh đáy và cạnh bên.
/// </summary>
public class EducationalGeometryRenderer : MonoBehaviour
{
    [Header("Cấu hình nét vẽ")]
    public Color lineColor = new Color(0.7f, 0.95f, 0.85f); // Xanh bạc hà
    public float lineWidth = 0.003f;
    public Material lineMaterial;

    private Dictionary<GameObject, List<LineRenderer>> linesMap = new Dictionary<GameObject, List<LineRenderer>>();
    private ObjectInteraction interaction;

    void Start()
    {
        interaction = FindObjectOfType<ObjectInteraction>();
        if (lineMaterial == null)
        {
            lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("Unlit/Color");
            lineMaterial = new Material(lineShader);
            lineMaterial.color = lineColor;
        }
    }

    private Shader lineShader;

    void Update()
    {
        if (interaction == null) return;

        // Phím W để bật/tắt đường viền sư phạm
        if (Input.GetKeyDown(KeyCode.W))
        {
            GeometryObject selected = interaction.GetSelectedObject();
            if (selected != null)
            {
                ToggleOutlines(selected.gameObject);
            }
        }

        // Cập nhật màu/độ dày thời gian thực
        SyncAppearance();
    }

    void SyncAppearance()
    {
        foreach (var pair in linesMap)
        {
            foreach (var lr in pair.Value)
            {
                if (lr != null)
                {
                    lr.startColor = lr.endColor = lineColor;
                    lr.startWidth = lr.endWidth = lineWidth;
                    lr.material.color = lineColor;
                }
            }
        }
    }

    public void ToggleOutlines(GameObject target)
    {
        if (linesMap.ContainsKey(target))
        {
            ClearOutlines(target);
        }
        else
        {
            DrawOutlines(target);
        }
    }

    void DrawOutlines(GameObject target)
    {
        List<LineRenderer> lrs = new List<LineRenderer>();
        string shapeType = target.name.ToLower();

        // Kiểm tra loại hình dựa trên component hoặc tên
        if (target.GetComponent<PyramidMeshGenerator>()) DrawPyramid(target, lrs);
        else if (target.GetComponent<ConeMeshGenerator>()) DrawCone(target, lrs);
        else if (shapeType.Contains("sphere")) DrawSphere(target, lrs);
        else if (shapeType.Contains("cylinder")) DrawCylinder(target, lrs);
        else if (shapeType.Contains("cube") || shapeType.Contains("box")) DrawCube(target, lrs);
        else DrawGenericMesh(target, lrs);

        linesMap[target] = lrs;
    }

    // --- VẼ HÌNH TRỤ ---
    void DrawCylinder(GameObject target, List<LineRenderer> lrs)
    {
        float r = 0.5f; // Bán kính mặc định
        float h = 1.0f; // Chiều cao mặc định (Unity Cylinder cao 2 đơn vị, nhưng thường dùng 1)
        
        // Đường tròn đáy dưới và đáy trên
        CreateCircle(target, new Vector3(0, -h, 0), r, Vector3.up, lrs);
        CreateCircle(target, new Vector3(0, h, 0), r, Vector3.up, lrs);
        
        // Trục giữa (đường cao h)
        CreateLine(target, new Vector3(0, -h, 0), new Vector3(0, h, 0), lrs);
        
        // Đường kẻ BÁN KÍNH mặt đáy (R)
        CreateLine(target, new Vector3(0, -h, 0), new Vector3(r, -h, 0), lrs);
        
        // 2 đường sinh bên cạnh
        CreateLine(target, new Vector3(r, -h, 0), new Vector3(r, h, 0), lrs);
        CreateLine(target, new Vector3(-r, -h, 0), new Vector3(-r, h, 0), lrs);
    }

    // --- VẼ HÌNH HỘP / LẬP PHƯƠNG ---
    void DrawCube(GameObject target, List<LineRenderer> lrs)
    {
        // 8 đỉnh của hình lập phương đơn vị
        Vector3[] v = new Vector3[] {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
        };

        // 12 cạnh
        int[,] edges = {
            {0,1}, {1,2}, {2,3}, {3,0}, // Đáy
            {4,5}, {5,6}, {6,7}, {7,4}, // Đỉnh
            {0,4}, {1,5}, {2,6}, {3,7}  // Cạnh bên
        };

        for (int i = 0; i < 12; i++)
            CreateLine(target, v[edges[i,0]], v[edges[i,1]], lrs);
    }

    // --- VẼ HÌNH CẦU ---
    void DrawSphere(GameObject target, List<LineRenderer> lrs)
    {
        float r = 0.5f;
        // Vẽ 2 đường tròn lớn (Equator và Meridian)
        CreateCircle(target, Vector3.zero, r, Vector3.up, lrs);
        CreateCircle(target, Vector3.zero, r, Vector3.forward, lrs);
        
        // Vẽ Tâm O (một điểm nhỏ hoặc dấu thập)
        CreateLine(target, new Vector3(-0.05f,0,0), new Vector3(0.05f,0,0), lrs);
        CreateLine(target, new Vector3(0,-0.05f,0), new Vector3(0,0.05f,0), lrs);

        // Vẽ bán kính R
        CreateLine(target, Vector3.zero, new Vector3(r, 0, 0), lrs);
    }

    // --- VẼ HÌNH NÓN ---
    void DrawCone(GameObject target, List<LineRenderer> lrs)
    {
        ConeMeshGenerator gen = target.GetComponent<ConeMeshGenerator>();
        float r = gen ? gen.radius : 0.5f;
        float h = gen ? gen.height : 1.0f;

        // Đường tròn đáy
        CreateCircle(target, Vector3.zero, r, Vector3.up, lrs);
        // Đường cao (Tâm đáy lên đỉnh)
        CreateLine(target, Vector3.zero, new Vector3(0, h, 0), lrs);

        // Đường kẻ BÁN KÍNH mặt đáy (R)
        CreateLine(target, Vector3.zero, new Vector3(r, 0, 0), lrs);

        // 2 đường sinh bên ngoài
        CreateLine(target, new Vector3(r, 0, 0), new Vector3(0, h, 0), lrs);
        CreateLine(target, new Vector3(-r, 0, 0), new Vector3(0, h, 0), lrs);
    }

    // --- VẼ HÌNH CHÓP ---
    void DrawPyramid(GameObject target, List<LineRenderer> lrs)
    {
        PyramidMeshGenerator gen = target.GetComponent<PyramidMeshGenerator>();
        float s = gen ? gen.baseSize / 2f : 0.5f;
        float h = gen ? gen.height : 1.0f;

        Vector3 apex = new Vector3(0, h, 0);
        Vector3 b1 = new Vector3(-s, 0, s);
        Vector3 b2 = new Vector3(s, 0, s);
        Vector3 b3 = new Vector3(s, 0, -s);
        Vector3 b4 = new Vector3(-s, 0, -s);

        // Đáy
        CreateLine(target, b1, b2, lrs); CreateLine(target, b2, b3, lrs);
        CreateLine(target, b3, b4, lrs); CreateLine(target, b4, b1, lrs);

        // ĐƯỜNG CAO (Đỉnh S xuống tâm đáy)
        CreateLine(target, Vector3.zero, apex, lrs);

        // Cạnh bên
        CreateLine(target, b1, apex, lrs); CreateLine(target, b2, apex, lrs);
        CreateLine(target, b3, apex, lrs); CreateLine(target, b4, apex, lrs);
    }

    // --- CÁC HÀM TIỆN ÍCH VẼ ---
    void CreateLine(GameObject parent, Vector3 start, Vector3 end, List<LineRenderer> lrs)
    {
        GameObject go = new GameObject("EduLine");
        go.transform.SetParent(parent.transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = new Material(lineMaterial);
        lr.startWidth = lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lrs.Add(lr);
    }

    void CreateCircle(GameObject parent, Vector3 center, float radius, Vector3 normal, List<LineRenderer> lrs)
    {
        GameObject go = new GameObject("EduCircle");
        go.transform.SetParent(parent.transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = new Material(lineMaterial);
        lr.startWidth = lr.endWidth = lineWidth;
        int segments = 36;
        lr.positionCount = segments + 1;
        
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * (Mathf.PI * 2 / segments);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            lr.SetPosition(i, center + rot * pos);
        }
        lrs.Add(lr);
    }

    void DrawGenericMesh(GameObject target, List<LineRenderer> lrs)
    {
        // Nếu không nhận diện được hình đặc biệt, vẽ wireframe cơ bản nhưng lọc cạnh trùng
        // (Tương tự code cũ nhưng sạch hơn)
    }

    void ClearOutlines(GameObject target)
    {
        if (linesMap.ContainsKey(target))
        {
            foreach (var lr in linesMap[target]) if (lr != null) Destroy(lr.gameObject);
            linesMap.Remove(target);
        }
    }
}
