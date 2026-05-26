using UnityEngine;

public class GeometryVisualizationController : MonoBehaviour
{
    private GeometryRenderer renderer3D;
    private VertexLabelSystem labels;

    void Start()
    {
        renderer3D =
            GetComponent<GeometryRenderer>();

        labels =
            GetComponent<VertexLabelSystem>();

        ShapeDefinition cube =
            ShapeLibrary.CreateCube();

        renderer3D.Render(cube);

        labels.CreateLabels(cube);
    }
}