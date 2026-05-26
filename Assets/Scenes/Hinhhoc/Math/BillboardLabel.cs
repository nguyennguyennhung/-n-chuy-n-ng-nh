using UnityEngine;

/// <summary>
/// Luôn quay text/label về phía camera. Dùng chung cho VertexLabel, EdgeLabel, MeasurementTool.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}
