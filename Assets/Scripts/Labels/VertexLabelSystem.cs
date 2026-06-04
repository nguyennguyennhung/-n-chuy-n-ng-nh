using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class VertexLabelSystem : MonoBehaviour
{
    private List<GameObject> labels = new();

    public void CreateLabels(ShapeDefinition shape)
    {
        Clear();
        if (shape == null || shape.vertices == null) return;

        // ★ FIX: trước đây giả định labels.Length == vertices.Length — sai sẽ IndexOutOfRange.
        int n = shape.labels != null
            ? Mathf.Min(shape.vertices.Length, shape.labels.Length)
            : shape.vertices.Length;

        for (int i = 0; i < n; i++)
        {
            string txt = shape.labels != null ? shape.labels[i] : ((char)('A' + i)).ToString();
            CreateLabel(txt, shape.vertices[i]);
        }
    }

    void CreateLabel(string txt, Vector3 pos)
    {
        GameObject go = new GameObject(txt);

        go.transform.SetParent(transform, false);

        go.transform.localPosition = pos;

        TextMeshPro tm = go.AddComponent<TextMeshPro>();

        tm.text = txt;

        tm.fontSize = 3;

        tm.color = Color.yellow;

        tm.alignment = TextAlignmentOptions.Center;

        go.transform.localScale = Vector3.one * 0.07f;

        go.AddComponent<BillboardLabel>();

        labels.Add(go);
    }

    void Clear()
    {
        foreach (var l in labels)
            Destroy(l);

        labels.Clear();
    }
}