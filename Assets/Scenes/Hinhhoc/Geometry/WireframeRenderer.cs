using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HIỆN CÁC CẠNH (WIREFRAME) CỦA KHỐI HÌNH HỌC.
/// Nhấn W hoặc dùng Wrist Menu để bật/tắt.
/// </summary>
public class WireframeRenderer : MonoBehaviour
{
    [Header("Cài đặt")]
    public Color edgeColor = new Color(0.7f, 0.95f, 0.85f);
    public float edgeWidth = 0.004f;

    private Dictionary<GameObject, List<LineRenderer>> wireframeMap = new Dictionary<GameObject, List<LineRenderer>>();
    private InteractionCore _core;

    void Update()
    {
        if (_core == null) _core = FindObjectOfType<InteractionCore>();

        if (_core != null && Input.GetKeyDown(KeyCode.W))
        {
            if (_core.Selected != null) ToggleWireframe(_core.Selected.gameObject);
        }
        SyncColors();
    }

    void SyncColors()
    {
        foreach (var pair in wireframeMap)
            foreach (var lr in pair.Value)
                if (lr != null)
                {
                    lr.startColor = lr.endColor = edgeColor;
                    lr.startWidth = lr.endWidth = edgeWidth;
                    if (lr.material != null) lr.material.color = edgeColor;
                }
    }

    public void ToggleWireframe(GameObject target)
    {
        if (target == null) return;
        if (wireframeMap.ContainsKey(target)) RemoveWireframe(target);
        else CreateWireframe(target);
    }

    void CreateWireframe(GameObject target)
    {
        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (mf == null || mf.mesh == null) return;

        Mesh mesh = mf.mesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        HashSet<string> edgeSet = new HashSet<string>();
        List<Vector2Int> edges = new List<Vector2Int>();
        for (int i = 0; i < tris.Length; i += 3)
        {
            AddEdge(tris[i], tris[i + 1], verts, edgeSet, edges);
            AddEdge(tris[i + 1], tris[i + 2], verts, edgeSet, edges);
            AddEdge(tris[i + 2], tris[i], verts, edgeSet, edges);
        }

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        Material mat = new Material(sh) { color = edgeColor };

        List<LineRenderer> lrs = new List<LineRenderer>();
        foreach (var edge in edges)
        {
            GameObject go = new GameObject("EdgeLine");
            go.transform.SetParent(target.transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = mat;
            lr.startColor = lr.endColor = edgeColor;
            lr.startWidth = lr.endWidth = edgeWidth;
            lr.positionCount = 2;
            lr.SetPosition(0, verts[edge.x]);
            lr.SetPosition(1, verts[edge.y]);
            lrs.Add(lr);
        }
        wireframeMap[target] = lrs;
    }

    void RemoveWireframe(GameObject target)
    {
        if (!wireframeMap.ContainsKey(target)) return;
        foreach (var lr in wireframeMap[target])
            if (lr != null) Destroy(lr.gameObject);
        wireframeMap.Remove(target);
    }

    void AddEdge(int a, int b, Vector3[] verts, HashSet<string> set, List<Vector2Int> edges)
    {
        int lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
        string key = $"{verts[lo].x:F3},{verts[lo].y:F3},{verts[lo].z:F3}_{verts[hi].x:F3},{verts[hi].y:F3},{verts[hi].z:F3}";
        if (set.Add(key)) edges.Add(new Vector2Int(a, b));
    }

    void OnValidate() { if (Application.isPlaying) SyncColors(); }
}
