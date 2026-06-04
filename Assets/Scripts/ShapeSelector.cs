using UnityEngine;

public class ShapeSelector : MonoBehaviour
{
    public static ShapeSelector Instance { get; private set; }
    [HideInInspector] public Transform selectedShape;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public void SetSelected(Transform shape) { selectedShape = shape; }

    public void ClearSelected(Transform shape)
    {
        if (selectedShape == shape) selectedShape = null;
    }
}
