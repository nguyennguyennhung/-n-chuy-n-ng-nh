using UnityEngine;
using System.Collections.Generic;

public class GeometryRenderer : MonoBehaviour
{
    public Material lineMaterial;

    private List<GameObject> spawned = new();

    public void Render(ShapeDefinition shape)
    {
        Clear();

        for (int i = 0; i < shape.edges.GetLength(0); i++)
        {
            int a = shape.edges[i, 0];
            int b = shape.edges[i, 1];

            CreateEdge(
                shape.vertices[a],
                shape.vertices[b]
            );
        }
    }

    void CreateEdge(Vector3 a, Vector3 b)
    {
        GameObject go = new GameObject("Edge");

        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();

        lr.material = lineMaterial;

        lr.positionCount = 2;

        lr.useWorldSpace = false;

        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;

        lr.SetPosition(0, a);
        lr.SetPosition(1, b);

        spawned.Add(go);
    }

    public void Clear()
    {
        foreach (var g in spawned)
            Destroy(g);

        spawned.Clear();
    }
}