// Assets/Scripts/core/WallLayerSetup.cs
using UnityEngine;

/// <summary>
/// Gắn lên GameObject phòng (HHti_Scean) → tự set layer "Walls"
/// cho tất cả collider con khi scene load.
///
/// Tránh phải mở từng prefab nested để chỉnh layer thủ công.
/// Nếu layer "Walls" không tồn tại → fallback "Default".
/// </summary>
[DisallowMultipleComponent]
public class WallLayerSetup : MonoBehaviour
{
    [Tooltip("Tên layer dành cho tường/sàn — phải khớp TagManager.asset")]
    public string wallLayerName = "Walls";

    [Tooltip("Áp dụng cho mọi child Renderer (true) hay chỉ child có Collider (false)")]
    public bool applyToAllChildren = false;

    [Tooltip("Log danh sách object đã đổi layer")]
    public bool verbose = false;

    void Awake()
    {
        int layer = LayerMask.NameToLayer(wallLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[WallLayerSetup] Layer '{wallLayerName}' không tồn tại → bỏ qua");
            return;
        }

        int changed = 0;
        if (applyToAllChildren)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.layer != layer)
                {
                    t.gameObject.layer = layer;
                    changed++;
                }
            }
        }
        else
        {
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                if (col.gameObject.layer != layer)
                {
                    col.gameObject.layer = layer;
                    changed++;
                }
            }
        }

        if (verbose) Debug.Log($"[WallLayerSetup] Đã set layer '{wallLayerName}' cho {changed} object con");
    }
}
