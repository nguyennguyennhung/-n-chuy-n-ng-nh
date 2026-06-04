// Assets/Scripts/core/XROriginYLock.cs
using UnityEngine;

/// <summary>
/// Khóa Y của XR Origin tại 1 độ cao cố định.
///
/// MỤC ĐÍCH:
///   - Chống jitter do tracking không ổn định (HMD không đeo, sensor che)
///   - Đảm bảo player luôn ở độ cao đúng dù Quest report position kì lạ
///   - X, Z vẫn cho phép di chuyển (qua teleport hoặc tracking)
///
/// HOẠT ĐỘNG:
///   - Mỗi LateUpdate, ghi đè transform.position.y = targetY
///   - Chạy SAU mọi script khác (DefaultExecutionOrder = 9999)
///
/// Gắn lên XR Origin Hands (XR Rig).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(9999)]  // sau cả PlayerSafetyClamp
public class XROriginYLock : MonoBehaviour
{
    [Header("═══ Y Lock ═══")]
    [Tooltip("Độ cao Y muốn khóa XR Origin tại")]
    public float targetY = 0.05f;

    [Tooltip("Nếu Y khác targetY > threshold → snap về")]
    public float snapThreshold = 0.001f;

    [Header("═══ Chế độ ═══")]
    [Tooltip("Lock cả X, Z (cấm di chuyển ngang) — tùy chọn")]
    public bool lockXZ = false;
    public Vector3 lockedXZ = Vector3.zero;

    [Header("═══ Auto-disable khi tracking ổn định ═══")]
    [Tooltip("Tự tắt khi HMD đeo + tay được track. Nếu false, luôn lock.")]
    public bool autoDisableWhenTracking = false;

    [Header("═══ Debug ═══")]
    public bool showLog = false;

    CharacterController _cc;
    int _lockCount;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        if (autoDisableWhenTracking && IsTrackingHealthy()) return;

        Vector3 p = transform.position;
        Vector3 target = new Vector3(
            lockXZ ? lockedXZ.x : p.x,
            targetY,
            lockXZ ? lockedXZ.z : p.z);

        if ((target - p).sqrMagnitude < snapThreshold * snapThreshold) return;

        // Snap qua CC nếu enabled, nếu không thì set thẳng transform
        if (_cc != null && _cc.enabled)
        {
            _cc.enabled = false;
            transform.position = target;
            _cc.enabled = true;
        }
        else
        {
            transform.position = target;
        }

        _lockCount++;
        if (showLog && _lockCount % 60 == 0)
            Debug.Log($"[XROriginYLock] Đã lock {_lockCount} lần — pos y giờ = {targetY}");
    }

    bool IsTrackingHealthy()
    {
        // Heuristic: nếu hand tracking 2 tay đều OK → tracking ổn
        var subs = new System.Collections.Generic.List<UnityEngine.XR.Hands.XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subs);
        if (subs.Count == 0 || !subs[0].running) return false;
        return subs[0].leftHand.isTracked && subs[0].rightHand.isTracked;
    }
}
