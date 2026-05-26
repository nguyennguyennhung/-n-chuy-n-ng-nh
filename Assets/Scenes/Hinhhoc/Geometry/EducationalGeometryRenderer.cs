using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// VẼ ĐƯỜNG VIỀN HÌNH HỌC THEO PHONG CÁCH SƯ PHẠM.
/// Nhấn W hoặc dùng API để bật/tắt.
/// </summary>
public class EducationalGeometryRenderer : MonoBehaviour
{
    [Header("Cấu hình")]
    public Color lineColor = new Color(0.7f, 0.95f, 0.85f);
    public float lineWidth = 0.003f;
    public Material lineMaterial;

    private Dictionary<GameObject, List<LineRenderer>> linesMap = new Dictionary<GameObject, List<LineRenderer>>();
    private InteractionCore _core;
    private Shader lineShader;

    void Start()
    {
        if (lineMaterial == null)
        {
            lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("Unlit/Color");
            lineMaterial = new Material(lineShader) { color = lineColor };
        }
    }

    void Update()
    {
        if (_core == null)
            _core = FindObjectOfType<InteractionCore>();
 
        if (_core != null && Input.GetKeyDown(KeyCode.W))
        {
            if (_core.Selected != null) ToggleOutlines(_core.Selected.gameObject);
        }
        SyncAppearance();
    }

    void SyncAppearance()
    {
        foreach (var pair in linesMap)
            foreach (var lr in pair.Value)
                if (lr != null)
                {
                    lr.startColor = lr.endColor = lineColor;
                    lr.startWidth = lr.endWidth = lineWidth;
                    if (lr.material != null) lr.material.color = lineColor;
                }
    }

    public void ToggleOutlines(GameObject target)
    {
        if (target == null) return;
        if (linesMap.ContainsKey(target)) ClearOutlines(target);
        else DrawOutlines(target);
    }

    void DrawOutlines(GameObject target)
    {
        List<LineRenderer> lrs = new List<LineRenderer>();
        if (target.GetComponent<PyramidMeshGenerator>()) DrawPyramid(target, lrs);
        else if (target.GetComponent<ConeMeshGenerator>()) DrawCone(target, lrs);
        else
        {
            string n = target.name.ToLower();
            if (n.Contains("sphere")) DrawSphere(target, lrs);
            else if (n.Contains("cylinder")) DrawCylinder(target, lrs);
            else if (n.Contains("cube") || n.Contains("box")) DrawCube(target, lrs);
        }
        linesMap[target] = lrs;
    }

    void DrawCylinder(GameObject t, List<LineRenderer> lrs)
    {
        float r = 0.5f, h = 1.0f;
        CreateCircle(t, new Vector3(0, -h, 0), r, Vector3.up, lrs);
        CreateCircle(t, new Vector3(0, h, 0), r, Vector3.up, lrs);
        CreateLine(t, new Vector3(0, -h, 0), new Vector3(0, h, 0), lrs);
        CreateLine(t, new Vector3(0, -h, 0), new Vector3(r, -h, 0), lrs);
        CreateLine(t, new Vector3(r, -h, 0), new Vector3(r, h, 0), lrs);
        CreateLine(t, new Vector3(-r, -h, 0), new Vector3(-r, h, 0), lrs);
    }

    void DrawCube(GameObject t, List<LineRenderer> lrs)
    {
        Vector3[] v = {
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,-0.5f),
            new Vector3(0.5f,-0.5f,0.5f), new Vector3(-0.5f,-0.5f,0.5f),
            new Vector3(-0.5f,0.5f,-0.5f), new Vector3(0.5f,0.5f,-0.5f),
            new Vector3(0.5f,0.5f,0.5f), new Vector3(-0.5f,0.5f,0.5f)
        };
        int[,] e = {{0,1},{1,2},{2,3},{3,0},{4,5},{5,6},{6,7},{7,4},{0,4},{1,5},{2,6},{3,7}};
        for (int i = 0; i < 12; i++) CreateLine(t, v[e[i,0]], v[e[i,1]], lrs);
    }

    void DrawSphere(GameObject t, List<LineRenderer> lrs)
    {
        float r = 0.5f;
        CreateCircle(t, Vector3.zero, r, Vector3.up, lrs);
        CreateCircle(t, Vector3.zero, r, Vector3.forward, lrs);
        CreateLine(t, new Vector3(-0.05f,0,0), new Vector3(0.05f,0,0), lrs);
        CreateLine(t, new Vector3(0,-0.05f,0), new Vector3(0,0.05f,0), lrs);
        CreateLine(t, Vector3.zero, new Vector3(r, 0, 0), lrs);
    }

    void DrawCone(GameObject t, List<LineRenderer> lrs)
    {
        var gen = t.GetComponent<ConeMeshGenerator>();
        float r = gen ? gen.radius : 0.5f, h = gen ? gen.height : 1f;
        CreateCircle(t, Vector3.zero, r, Vector3.up, lrs);
        CreateLine(t, Vector3.zero, new Vector3(0, h, 0), lrs);
        CreateLine(t, Vector3.zero, new Vector3(r, 0, 0), lrs);
        CreateLine(t, new Vector3(r, 0, 0), new Vector3(0, h, 0), lrs);
        CreateLine(t, new Vector3(-r, 0, 0), new Vector3(0, h, 0), lrs);
    }

    void DrawPyramid(GameObject t, List<LineRenderer> lrs)
    {
        var gen = t.GetComponent<PyramidMeshGenerator>();
        float s = gen ? gen.baseSize / 2f : 0.5f, h = gen ? gen.height : 1f;
        Vector3 apex = new Vector3(0, h, 0);
        Vector3 b1 = new Vector3(-s,0,s), b2 = new Vector3(s,0,s), b3 = new Vector3(s,0,-s), b4 = new Vector3(-s,0,-s);
        CreateLine(t, b1, b2, lrs); CreateLine(t, b2, b3, lrs);
        CreateLine(t, b3, b4, lrs); CreateLine(t, b4, b1, lrs);
        CreateLine(t, Vector3.zero, apex, lrs);
        CreateLine(t, b1, apex, lrs); CreateLine(t, b2, apex, lrs);
        CreateLine(t, b3, apex, lrs); CreateLine(t, b4, apex, lrs);
    }

    void CreateLine(GameObject parent, Vector3 start, Vector3 end, List<LineRenderer> lrs)
    {
        GameObject go = new GameObject("EduLine");
        go.transform.SetParent(parent.transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = new Material(lineMaterial);
        lr.startWidth = lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start); lr.SetPosition(1, end);
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
        int seg = 36;
        lr.positionCount = seg + 1;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        for (int i = 0; i <= seg; i++)
        {
            float a = i * (Mathf.PI * 2 / seg);
            Vector3 pos = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            lr.SetPosition(i, center + rot * pos);
        }
        lrs.Add(lr);
    }

    void ClearOutlines(GameObject target)
    {
        if (!linesMap.ContainsKey(target)) return;
        foreach (var lr in linesMap[target]) if (lr != null) Destroy(lr.gameObject);
        linesMap.Remove(target);
    }
}
