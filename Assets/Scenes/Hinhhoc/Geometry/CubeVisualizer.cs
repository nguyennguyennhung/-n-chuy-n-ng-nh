using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CubeVisualizer : MonoBehaviour
{
    public Material transparentMaterial;
    public Material lineMaterial;

    Renderer rend;
    Material originalMat;
    Color originalColor;
    Transform root;

    bool created = false;

    void Start()
    {
        rend = GetComponent<Renderer>();

        originalMat = rend.material;
        originalColor = rend.material.color;
    }

    // GỌI TỪ EVENT SELECT
    public void OnSelect_ShowGeometry()
    {
        if (!created)
        {
            CreateGeometry();

            created = true;
        }

        root.gameObject.SetActive(true);

        SetTransparent(true);
        rend.material.EnableKeyword("_EMISSION");

    rend.material.SetColor(
    "_EmissionColor",
    originalColor * 0.5f);
    }

    void CreateGeometry()
    {
        root = new GameObject("CubeGeometryRoot").transform;

        root.SetParent(transform, false);

        root.localPosition = Vector3.zero;

root.gameObject.SetActive(false);
        float s = 0.5f;

        // 8 ĐỈNH
        Vector3 A  = new Vector3(-s,-s,-s);
        Vector3 B  = new Vector3( s,-s,-s);
        Vector3 C  = new Vector3( s,-s, s);
        Vector3 D  = new Vector3(-s,-s, s);

        Vector3 A2 = new Vector3(-s, s,-s);
        Vector3 B2 = new Vector3( s, s,-s);
        Vector3 C2 = new Vector3( s, s, s);
        Vector3 D2 = new Vector3(-s, s, s);

        var P = new Dictionary<string, Vector3>()
        {
            {"A",A},
            {"B",B},
            {"C",C},
            {"D",D},

            {"A'",A2},
            {"B'",B2},
            {"C'",C2},
            {"D'",D2},
        };

        // TẠO LABEL ĐỈNH
        foreach(var kv in P)
        {
            CreateLabel(kv.Key, kv.Value, Color.yellow, 0.07f);
        }

        // TÂM O
        CreateLabel("O", Vector3.zero, Color.cyan, 0.08f);

        // 12 CẠNH
        CreateEdge(A,B);
        CreateEdge(B,C);
        CreateEdge(C,D);
        CreateEdge(D,A);

        CreateEdge(A2,B2);
        CreateEdge(B2,C2);
        CreateEdge(C2,D2);
        CreateEdge(D2,A2);

        CreateEdge(A,A2);
        CreateEdge(B,B2);
        CreateEdge(C,C2);
        CreateEdge(D,D2);

        // CẠNH a
        CreateMidLabel("a", A, B, Color.green, 0.06f);

        // ĐƯỜNG AA'
        CreateSpecialLine(A, A2, "AA'", Color.magenta);

        // ĐƯỜNG CHÉO AC'
        CreateSpecialLine(A, C2, "AC'", Color.red);
    }

    void CreateEdge(Vector3 a, Vector3 b)
    {
        var go = new GameObject("Edge");

        go.transform.SetParent(root, false);

        var lr = go.AddComponent<LineRenderer>();

        lr.material = lineMaterial;

        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;

        lr.positionCount = 2;

        lr.useWorldSpace = false;

        lr.SetPosition(0, a);

        lr.SetPosition(1, b);
    }

    void CreateLabel(string txt, Vector3 pos, Color c, float scale)
    {
        var go = new GameObject(txt);

        go.transform.SetParent(root);

        go.transform.localPosition = pos;

        var t = go.AddComponent<TextMeshPro>();

        t.text = txt;

        t.fontSize = 3;

        t.alignment = TextAlignmentOptions.Center;

        t.color = c;

        go.transform.localScale = Vector3.one * scale;
        t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.2f;
    }

    void CreateMidLabel(string txt,
        Vector3 a,
        Vector3 b,
        Color c,
        float scale)
    {
        CreateLabel(txt, (a + b) * 0.5f, c, scale);
    }

    void CreateSpecialLine(
    Vector3 a,
    Vector3 b,
    string label,
    Color c)
{
    var go = new GameObject(label);

    go.transform.SetParent(root, false);

    var lr = go.AddComponent<LineRenderer>();

    lr.material = new Material(lineMaterial);

    lr.startColor = c;
    lr.endColor = c;

    lr.startWidth = 0.015f;
    lr.endWidth = 0.015f;

    lr.positionCount = 2;

    lr.useWorldSpace = false;

    lr.SetPosition(0, a);

    lr.SetPosition(1, b);

    CreateMidLabel(label, a, b, c, 0.06f);
}

    // LABEL LUÔN NHÌN CAMERA
    void Update()
    {
        if (root == null) return;

        var cam = Camera.main;

        foreach (var t in root.GetComponentsInChildren<TextMeshPro>())
        {
            t.transform.forward = cam.transform.forward;
        }
    }
    void SetTransparent(bool transparent)
{
    Material mat = rend.material;

    Color c = originalColor;

    if (transparent)
    {
        c.a = 0.25f;

        // URP TRANSPARENT
        mat.SetFloat("_Surface", 1);

        mat.SetOverrideTag("RenderType", "Transparent");

        mat.SetInt("_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

        mat.SetInt("_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        mat.SetInt("_ZWrite", 0);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        mat.renderQueue =
            (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
    else
    {
        c.a = 1f;

        // URP OPAQUE
        mat.SetFloat("_Surface", 0);

        mat.SetOverrideTag("RenderType", "Opaque");

        mat.SetInt("_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.One);

        mat.SetInt("_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.Zero);

        mat.SetInt("_ZWrite", 1);

        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

        mat.renderQueue =
            (int)UnityEngine.Rendering.RenderQueue.Geometry;
    }

    // QUAN TRỌNG NHẤT
    if (mat.HasProperty("_BaseColor"))
    {
        mat.SetColor("_BaseColor", c);
    }
    else
    {
        mat.color = c;
    }
}
public void OnUnselect_HideGeometry()
{
    if (root != null)
    {
        root.gameObject.SetActive(false);
    }

    SetTransparent(false);
    rend.material.DisableKeyword("_EMISSION");
}

}