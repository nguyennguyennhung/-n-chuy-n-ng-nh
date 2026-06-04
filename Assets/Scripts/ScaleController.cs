using UnityEngine;

public class ScaleController : MonoBehaviour
{
    [Header("Phím Simulator")]
    public KeyCode scaleUpKey = KeyCode.Alpha9;
    public KeyCode scaleDownKey = KeyCode.Alpha0;

    [Header("Tốc độ scale (1.0 = 100%/giây)")]
    public float scaleSpeed = 0.5f;

    [Header("Giới hạn scale (so với scale gốc)")]
    public float minScaleMultiplier = 0.3f;
    public float maxScaleMultiplier = 3f;

    void Update()
    {
        Transform selected = ShapeSelector.Instance != null
            ? ShapeSelector.Instance.selectedShape
            : null;

        if (selected == null) return;

        bool up = Input.GetKey(scaleUpKey);
        bool down = Input.GetKey(scaleDownKey);

        if (!up && !down) return;

        var grabbable = selected.GetComponent<Grabbable>();
        Vector3 originalScale = (grabbable != null) ? grabbable.originalScale : selected.localScale;

        float currentMultiplier = selected.localScale.x / originalScale.x;
        float delta = scaleSpeed * Time.deltaTime * (up ? 1f : -1f);
        float newMultiplier = Mathf.Clamp(currentMultiplier + delta, minScaleMultiplier, maxScaleMultiplier);

        selected.localScale = originalScale * newMultiplier;
    }
}