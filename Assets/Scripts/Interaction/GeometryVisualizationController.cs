using UnityEngine;

public class GeometryVisualizationController : MonoBehaviour
{
    private GeometryRenderer renderer3D;
    private VertexLabelSystem labels;

    void Start()
    {
        renderer3D = GetComponent<GeometryRenderer>();
        labels = GetComponent<VertexLabelSystem>();

        ShapeDefinition cube = ShapeLibrary.CreateCube();

        // ★ FIX: thiếu component sẽ NRE — log rõ thay vì crash.
        if (renderer3D == null) Debug.LogError("[GeometryViz] GeometryRenderer chưa được gắn cùng GameObject.", this);
        else renderer3D.Render(cube);

        if (labels == null) Debug.LogError("[GeometryViz] VertexLabelSystem chưa được gắn cùng GameObject.", this);
        else labels.CreateLabels(cube);
    }
}