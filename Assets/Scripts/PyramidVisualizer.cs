using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PyramidVisualizer : MonoBehaviour
{
    public Color edgeColor = Color.white;
    public float edgeWidth = 0.1f;
    public bool showHeight = true;
    public Color heightColor = Color.yellow;

    private void Start()
    {
        // Give Unity a moment to initialize
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
        
        // Find distinct vertices (within a small tolerance)
        List<Vector3> uniqueVertices = new List<Vector3>();
        foreach (Vector3 v in vertices)
        {
            if (!uniqueVertices.Any(uv => Vector3.Distance(uv, v) < 0.01f))
            {
                uniqueVertices.Add(v);
            }
        }

        // Sort vertices: Highest is Apex, others are base
        uniqueVertices.Sort((a, b) => b.y.CompareTo(a.y));
        
        if (uniqueVertices.Count < 5)
        {
            Debug.LogWarning("Expected at least 5 vertices for a pyramid, found " + uniqueVertices.Count);
        }

        Vector3 apex = uniqueVertices[0];
        List<Vector3> baseVertices = uniqueVertices.Skip(1).ToList();

        // Sort base vertices circularly to draw edges correctly
        Vector3 center = Vector3.zero;
        foreach (var v in baseVertices) center += v;
        center /= baseVertices.Count;

        baseVertices.Sort((a, b) => {
            float angleA = Mathf.Atan2(a.z - center.z, a.x - center.x);
            float angleB = Mathf.Atan2(b.z - center.z, b.x - center.x);
            return angleA.CompareTo(angleB);
        });

        // Draw base edges
        for (int i = 0; i < baseVertices.Count; i++)
        {
            CreateLine("Edge_Base_" + i, baseVertices[i], baseVertices[(i + 1) % baseVertices.Count], edgeColor);
        }

        // Draw side edges
        for (int i = 0; i < baseVertices.Count; i++)
        {
            CreateLine("Edge_Side_" + i, apex, baseVertices[i], edgeColor);
        }

        // Draw Height
        if (showHeight)
        {
            CreateLine("Height_Line", apex, center, heightColor);
            CreateLabel("H", Vector3.Lerp(apex, center, 0.5f), heightColor);
        }

        // Add Labels
        CreateLabel("S", apex + Vector3.up * 0.1f);
        char labelChar = 'A';
        for (int i = 0; i < baseVertices.Count; i++)
        {
            CreateLabel(labelChar.ToString(), baseVertices[i] * 1.1f);
            labelChar++;
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
        lr.startWidth = edgeWidth;
        lr.endWidth = edgeWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
    }

    void CreateLabel(string text, Vector3 position, Color? color = null)
    {
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = position;
        
        TextMeshPro tm = labelObj.AddComponent<TextMeshPro>();
        tm.text = text;
        tm.fontSize = 2;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = color ?? Color.white;
        
        labelObj.AddComponent<BillboardLabel>();
    }
}

public class BillboardLabel : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }
}
