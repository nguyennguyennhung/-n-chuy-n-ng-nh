using UnityEngine;

/// <summary>
/// Bấm phím để reset tất cả hình về vị trí + scale gốc.
/// </summary>
public class KeyboardReset : MonoBehaviour
{
    [Tooltip("Phím bấm để reset (chọn phím Simulator không chiếm)")]
    public KeyCode resetKey = KeyCode.C;

    void Update()
{
    if (Input.anyKeyDown)
        Debug.Log("[KeyboardReset] Có phím được bấm: " + Input.inputString);

    if (Input.GetKeyDown(resetKey))
    {
        var all = FindObjectsOfType<Grabbable>();
        foreach (var g in all) g.ResetToOrigin();
        Debug.Log($"[KeyboardReset] Reset {all.Length} hình.");
    }
}
}
