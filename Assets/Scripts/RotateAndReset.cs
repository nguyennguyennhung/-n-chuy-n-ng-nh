using UnityEngine;

public class RotateAndReset : MonoBehaviour
{
    public Vector3 rotateAxis = Vector3.up;
    public float rotateSpeed = 90f;

    void Update()
    {
        // Chỉ xoay khi 2 tay đứng yên (không đang kéo để scale)
        if (HandsDistanceTracker.Instance == null) return;
        if (HandsDistanceTracker.Instance.IsHandsMoving) return;

        foreach (var c in FindObjectsOfType<CubeInteractive>())
            if (c.touching >= 2) Rotate(c.transform);
        foreach (var c in FindObjectsOfType<SphereInteractive>())
            if (c.touching >= 2) Rotate(c.transform);
        foreach (var c in FindObjectsOfType<CylinderInteractive>())
            if (c.touching >= 2) Rotate(c.transform);
        foreach (var c in FindObjectsOfType<ChopInteractive>())
            if (c.touching >= 2) Rotate(c.transform);
        foreach (var c in FindObjectsOfType<NonInteractive>())
            if (c.touching >= 2) Rotate(c.transform);
    }

    void Rotate(Transform shape)
    {
        shape.Rotate(rotateAxis.normalized, rotateSpeed * Time.deltaTime, Space.Self);
    }
}