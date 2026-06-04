using UnityEngine;

/// <summary>
/// Đánh dấu hình này có thể bị cắt.
/// Cần MeshFilter + MeshRenderer (hầu hết các hình đã có).
/// </summary>
public class Sliceable : MonoBehaviour
{
    [Tooltip("Material dùng cho mặt cắt (sẽ làm sáng nổi bật ở Phase 2)")]
    public Material crossSectionMaterial;
}