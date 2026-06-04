// Assets/Scripts/core/PlayerSafetyClamp.cs
using UnityEngine;

/// <summary>
/// Gắn lên **XR Origin** (cùng GameObject với CharacterController).
/// 1) Clamp player vào bounds (chặn trôi ra vô tận khi tường thủng)
/// 2) Reset về spawnAnchor khi rơi xuống dưới fallY
/// 3) Warmup frames để bỏ qua giai đoạn tracking chưa ổn định
///
/// LƯU Ý: Không di chuyển player chủ động — chỉ phản ứng.
/// Locomotion thực tế đi qua Continuous Move Provider của XRI.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerSafetyClamp : MonoBehaviour
{
    [Header("═══ Bounds (vùng chơi cho phép) ═══")]
    public Vector3 boundsCenter = Vector3.zero;
    public Vector3 boundsSize = new Vector3(8, 3, 8);

    [Header("═══ Fall recovery ═══")]
    [Tooltip("Y thấp hơn ngưỡng này → teleport về spawnAnchor")]
    public float fallY = -1f;

    [Tooltip("Empty GameObject ở vị trí spawn an toàn")]
    public Transform spawnAnchor;

    [Tooltip("Delay (giây) trước khi reset, tránh false-positive")]
    public float fallResetDelay = 0.3f;

    [Header("═══ Warmup ═══")]
    [Tooltip("Bỏ qua N frame đầu (tracking chưa ổn định lúc khởi tạo)")]
    public int warmupFrames = 30;

    [Header("═══ Debug ═══")]
    public bool showGizmos = true;
    public bool showLog = false;

    CharacterController _cc;
    int _frame;
    float _outsideTimer;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        if (_cc == null || !_cc.enabled) return;

        if (_frame < warmupFrames) { _frame++; return; }

        Vector3 p = transform.position;

        // 1) Fall recovery
        if (p.y < fallY)
        {
            _outsideTimer += Time.deltaTime;
            if (_outsideTimer >= fallResetDelay)
            {
                ResetToSpawn();
                return;
            }
        }
        else
        {
            _outsideTimer = 0f;
        }

        // 2) Boundary clamp (chỉ khi vượt)
        Vector3 min = boundsCenter - boundsSize * 0.5f;
        Vector3 max = boundsCenter + boundsSize * 0.5f;
        Vector3 clamped = new Vector3(
            Mathf.Clamp(p.x, min.x, max.x),
            p.y,
            Mathf.Clamp(p.z, min.z, max.z));

        if ((clamped - p).sqrMagnitude > 0.0001f)
        {
            _cc.Move(clamped - p);
            if (showLog) Debug.Log($"[SafetyClamp] Đẩy vào bounds: {p} → {clamped}");
        }
    }

    public void ResetToSpawn()
    {
        if (spawnAnchor == null)
        {
            Debug.LogWarning("[SafetyClamp] spawnAnchor chưa gán → không reset được");
            return;
        }

        bool was = _cc.enabled;
        _cc.enabled = false;
        transform.SetPositionAndRotation(spawnAnchor.position, spawnAnchor.rotation);
        _cc.enabled = was;
        _outsideTimer = 0f;
        if (showLog) Debug.Log($"[SafetyClamp] Reset về spawn {spawnAnchor.position}");
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.25f);
        Gizmos.DrawCube(boundsCenter, boundsSize);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
        Gizmos.DrawWireCube(boundsCenter, boundsSize);

        Gizmos.color = Color.red;
        Vector3 fallMin = boundsCenter - new Vector3(boundsSize.x, 0, boundsSize.z) * 0.5f;
        fallMin.y = fallY;
        Vector3 fallMax = boundsCenter + new Vector3(boundsSize.x, 0, boundsSize.z) * 0.5f;
        fallMax.y = fallY;
        Gizmos.DrawLine(fallMin, new Vector3(fallMax.x, fallY, fallMin.z));
        Gizmos.DrawLine(new Vector3(fallMax.x, fallY, fallMin.z), fallMax);
        Gizmos.DrawLine(fallMax, new Vector3(fallMin.x, fallY, fallMax.z));
        Gizmos.DrawLine(new Vector3(fallMin.x, fallY, fallMax.z), fallMin);
    }
}
