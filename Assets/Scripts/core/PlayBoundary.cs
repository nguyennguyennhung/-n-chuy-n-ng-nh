using UnityEngine;

/// <summary>
/// Định nghĩa "hộp phòng học" — XR Origin luôn bị clamp về trong hộp này.
/// Tránh việc người chơi (do teleport sai, do giật tay, do glitch vật lý) bay ra ngoài.
///
/// Đặt 1 GameObject empty trong scene, gắn script này, set bounds.
/// SafePlayerLocomotion sẽ tìm và dùng nó.
/// </summary>
public class PlayBoundary : MonoBehaviour
{
    [Header("Tâm hộp giới hạn (world)")]
    public Vector3 center = Vector3.zero;

    [Header("Kích thước hộp (mét). Y = chiều cao trần.")]
    public Vector3 size = new Vector3(8f, 3f, 8f);

    [Header("Y tối thiểu — dưới mức này coi như rơi, sẽ reset")]
    public float fallThresholdY = -1f;

    /// <summary>
    /// Trả về vị trí đã được clamp vào trong hộp.
    /// </summary>
    public Vector3 Clamp(Vector3 worldPos)
    {
        Vector3 min = center - size * 0.5f;
        Vector3 max = center + size * 0.5f;
        return new Vector3(
            Mathf.Clamp(worldPos.x, min.x, max.x),
            Mathf.Clamp(worldPos.y, min.y, max.y),
            Mathf.Clamp(worldPos.z, min.z, max.z)
        );
    }

    public bool IsFallen(Vector3 worldPos)
    {
        return worldPos.y < fallThresholdY;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
