using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// VR CHARACTER CONTROLLER — Chống xuyên tường hoàn chỉnh cho CameraRig
/// ══════════════════════════════════════════════════════════════════════
///
/// GẮN VÀO: CameraRig (OVRCameraRig / [BuildingBlock] Camera Rig)
///
/// Hierarchy:
///   CameraRig                        ← script gắn ở đây
///     └── TrackingSpace
///           ├── CenterEyeAnchor      ← tự tìm
///           ├── LeftHandAnchor
///           └── RightHandAnchor
///
/// NGUYÊN LÝ HOẠT ĐỘNG:
///   1. Mỗi frame đo "đầu người chơi di chuyển bao nhiêu" (room-scale tracking)
///   2. Dùng CharacterController.Move() để áp dụng chuyển động đó
///   3. CC tự động chặn tường — nếu bị chặn, đẩy CameraRig lùi lại
///   4. Thumbstick locomotion cũng đi qua CC.Move() → cũng bị chặn tường
///   5. Depenetration bổ sung: bắn raycast 360° từ đầu để đẩy ra nếu lọt sót
///   6. Fade màn hình đen khi đầu quá gần tường → tránh nhìn xuyên
///
/// THAY THẾ: CharacterPhysicsHandler + PlayerCollisionHandler + HeadCollisionFader
/// (Gỡ 3 script cũ, chỉ dùng script này)
/// </summary>
[DefaultExecutionOrder(-100)]   // Chạy trước OVR tracking
[RequireComponent(typeof(CharacterController))]
public class VRCharacterController : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    //  INSPECTOR — Tham chiếu
    // ══════════════════════════════════════════════════════

    [Header("═══ Tham chiếu (để trống = tự tìm) ═══")]
    [Tooltip("CenterEyeAnchor — vị trí mắt thực tế")]
    public Transform centerEyeAnchor;

    [Tooltip("TrackingSpace — dùng xác định hướng đi")]
    public Transform trackingSpace;

    // ══════════════════════════════════════════════════════
    //  INSPECTOR — CharacterController
    // ══════════════════════════════════════════════════════

    [Header("═══ CharacterController ═══")]
    [Tooltip("Bán kính CC (nên 0.2–0.35)")]
    public float ccRadius = 0.25f;

    [Tooltip("skinWidth CC (nên 0.01–0.02)")]
    public float ccSkinWidth = 0.015f;

    // ══════════════════════════════════════════════════════
    //  INSPECTOR — Thumbstick Locomotion
    // ══════════════════════════════════════════════════════

    [Header("═══ Di chuyển (Thumbstick) ═══")]
    [Tooltip("Tốc độ di chuyển bằng thumbstick (m/s)")]
    public float moveSpeed = 2.5f;

    [Tooltip("Tay đọc thumbstick: LTouch = tay trái")]
    public OVRInput.Controller moveController = OVRInput.Controller.LTouch;

    [Tooltip("Trọng lực áp dụng mỗi giây (âm = kéo xuống). Đặt 0 nếu sàn VR luôn bằng phẳng.")]
    public float gravity = 0f;   // VR thường không cần gravity vì sàn phẳng

    // ══════════════════════════════════════════════════════
    //  INSPECTOR — Depenetration (bắn tia bổ sung)
    // ══════════════════════════════════════════════════════

    [Header("═══ Depenetration bổ sung ═══")]
    [Tooltip("Bán kính an toàn quanh đầu (m). Nếu tường nằm trong vùng này → đẩy ra")]
    public float safeHeadRadius = 0.3f;

    [Tooltip("Layer tường/vật cản")]
    public LayerMask wallLayer = ~0;

    [Tooltip("Số hướng raycast quanh đầu (12 = mỗi 30°, 8 = mỗi 45°)")]
    [Range(6, 24)]
    public int raycastDirections = 12;

    // ══════════════════════════════════════════════════════
    //  INSPECTOR — Fade (chống nhìn xuyên tường)
    // ══════════════════════════════════════════════════════

    [Header("═══ Fade khi gần tường ═══")]
    [Tooltip("Bật/tắt fade màn hình đen")]
    public bool enableFade = true;

    [Tooltip("Khoảng cách bắt đầu fade (m)")]
    public float fadeStartDistance = 0.15f;

    [Tooltip("Tốc độ fade in/out")]
    public float fadeSpeed = 10f;

    // ══════════════════════════════════════════════════════
    //  INSPECTOR — Vignette (chống say tàu xe)
    // ══════════════════════════════════════════════════════

    [Header("═══ Vignette (Motion Sickness) ═══")]
    public bool enableVignette = true;

    [Range(0f, 1f)]
    public float maxVignetteIntensity = 0.35f;
    public float vignetteFadeSpeed = 6f;

    // ══════════════════════════════════════════════════════
    //  INSPECTOR — Debug
    // ══════════════════════════════════════════════════════

    [Header("═══ Debug ═══")]
    public bool showDebugLog = true;
    public bool showGizmos = true;

    // ══════════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════════

    CharacterController _cc;
    OVRVignette _vignette;          // optional
    HashSet<Collider> _ownColliders = new HashSet<Collider>();
    Vector3[] _dirs;                // mảng hướng raycast

    Vector3 _prevHeadLocal;         // vị trí đầu frame trước (local space)
    float _verticalVelocity;        // vận tốc dọc (gravity)
    float _currentFade;             // opacity fade hiện tại (0→1)
    float _vignetteVal;             // vignette intensity hiện tại
    float _smoothHeight;            // chiều cao capsule đã smooth

    bool _ready;
    int _warmupFrames;
    const int WARMUP_COUNT = 30;    // bỏ qua 30 frame đầu (tracking chưa ổn định)
    const float MAX_GRAVITY_VEL = -5f;  // Giới hạn tốc độ rơi
    const float MAX_PUSH_PER_FRAME = 0.05f; // Giới hạn đẩy depenetration mỗi frame

    // ══════════════════════════════════════════════════════
    //  AWAKE — Khởi tạo
    // ══════════════════════════════════════════════════════

    void Awake()
    {
        // --- CharacterController ---
        _cc = GetComponent<CharacterController>();
        SetupCharacterController();

        // ★ TẮT CC cho đến khi warmup xong → tránh bị đẩy bật lúc khởi tạo
        _cc.enabled = false;

        // --- Tự tìm tham chiếu ---
        if (centerEyeAnchor == null)
            centerEyeAnchor = FindDeepChild(transform, "CenterEyeAnchor");

        if (trackingSpace == null)
            trackingSpace = FindDeepChild(transform, "TrackingSpace");

        // --- Vignette (optional) ---
        _vignette = FindObjectOfType<OVRVignette>();

        // --- Cache collider bản thân để bỏ qua khi raycast ---
        foreach (var col in GetComponentsInChildren<Collider>(true))
            _ownColliders.Add(col);

        // --- Tạo mảng hướng raycast ---
        BuildRaycastDirections();

        // Init smooth height
        _smoothHeight = 1.7f;

        if (showDebugLog)
            Debug.Log("[VRCharController] Awake OK (CC disabled until warmup)");
    }

    void Start()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogError("[VRCharController] Không tìm thấy CenterEyeAnchor! Vô hiệu hóa.", this);
            enabled = false;
            return;
        }
        if (trackingSpace == null)
        {
            Debug.LogWarning("[VRCharController] Không tìm thấy TrackingSpace — thumbstick sẽ không hoạt động.", this);
        }

        if (showDebugLog)
            Debug.Log($"[VRCharController] Start OK — CenterEye={centerEyeAnchor.name}, CC.radius={_cc.radius}");
    }

    // ══════════════════════════════════════════════════════
    //  UPDATE — Ghi nhớ vị trí đầu trước khi OVR cập nhật
    // ══════════════════════════════════════════════════════

    void Update()
    {
        if (centerEyeAnchor == null) return;

        // Warmup: bỏ qua vài frame đầu vì tracking chưa ổn định
        if (!_ready)
        {
            _warmupFrames++;
            _prevHeadLocal = centerEyeAnchor.localPosition;

            if (_warmupFrames < WARMUP_COUNT)
                return;

            // ★ Warmup xong → bật CC, sync capsule trước
            _ready = true;
            _smoothHeight = Mathf.Max(centerEyeAnchor.localPosition.y, 0.5f);
            SyncCapsuleWithHead(); // Sync 1 lần trước khi bật
            _cc.enabled = true;

            if (showDebugLog)
                Debug.Log($"[VRCharController] Ready! Head={centerEyeAnchor.position:F2}, Height={_smoothHeight:F2}");
            return;
        }

        // Ghi nhớ vị trí đầu hiện tại (local) để so sánh ở LateUpdate
        _prevHeadLocal = centerEyeAnchor.localPosition;
    }

    // ══════════════════════════════════════════════════════
    //  LATE UPDATE — Xử lý chính (sau khi OVR cập nhật tracking)
    // ══════════════════════════════════════════════════════

    void LateUpdate()
    {
        if (!_ready || centerEyeAnchor == null) return;

        // ────────────────────────────────────────────────
        //  BƯỚC 1: Tính delta đầu (room-scale tracking)
        // ────────────────────────────────────────────────
        Vector3 currentHeadLocal = centerEyeAnchor.localPosition;
        Vector3 headDelta = currentHeadLocal - _prevHeadLocal;

        // Bảo vệ: tracking nhảy đột ngột (teleport / reset guardian)
        if (headDelta.sqrMagnitude > 1f)
        {
            if (showDebugLog)
                Debug.LogWarning($"[VRCharController] Tracking nhảy quá xa ({headDelta.magnitude:F2}m), bỏ qua frame này");
            return;
        }

        // ────────────────────────────────────────────────
        //  BƯỚC 2: CharacterController.Move() cho room-scale
        // ────────────────────────────────────────────────
        Vector3 roomScaleMove = new Vector3(headDelta.x, 0f, headDelta.z);

        // ★ Giới hạn di chuyển tối đa mỗi frame (tránh nhảy xa)
        if (roomScaleMove.sqrMagnitude > 0.25f) // > 0.5m/frame = bất thường
        {
            roomScaleMove = roomScaleMove.normalized * 0.5f;
        }

        Vector3 posBefore = transform.position;
        _cc.Move(roomScaleMove);

        // Tính phần bị tường chặn
        Vector3 actualMove = transform.position - posBefore;
        Vector3 blocked = roomScaleMove - actualMove;

        // Nếu bị chặn → offset CameraRig ngược lại để đầu không xuyên tường
        if (blocked.sqrMagnitude > 0.0001f)
        {
            // ★ Chỉ offset XZ, KHÔNG đổi Y
            Vector3 correction = new Vector3(blocked.x, 0f, blocked.z);
            SetPositionDirect(transform.position - correction);
        }

        // ────────────────────────────────────────────────
        //  BƯỚC 3: Thumbstick locomotion (cũng qua CC.Move)
        // ────────────────────────────────────────────────
        HandleThumbstickMovement();

        // ────────────────────────────────────────────────
        //  BƯỚC 4: Gravity
        // ────────────────────────────────────────────────
        HandleGravity();

        // ────────────────────────────────────────────────
        //  BƯỚC 5: Sync CC capsule với vị trí đầu
        // ────────────────────────────────────────────────
        SyncCapsuleWithHead();

        // ────────────────────────────────────────────────
        //  BƯỚC 6: Depenetration bổ sung (raycast 360°)
        // ────────────────────────────────────────────────
        DepenetrateHead();

        // ────────────────────────────────────────────────
        //  BƯỚC 7: Giữ CameraRig không rơi dưới sàn
        // ────────────────────────────────────────────────
        ClampToFloor();

        // ────────────────────────────────────────────────
        //  BƯỚC 8: Hiệu ứng
        // ────────────────────────────────────────────────
        if (enableFade) UpdateFade();
        if (enableVignette) UpdateVignette();
    }

    // ══════════════════════════════════════════════════════
    //  THUMBSTICK MOVEMENT
    // ══════════════════════════════════════════════════════

    void HandleThumbstickMovement()
    {
        if (trackingSpace == null) return;

        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, moveController);
        if (stick.sqrMagnitude < 0.01f) return;

        // Hướng đi theo TrackingSpace (không bị lệch khi cúi đầu)
        Vector3 dir = trackingSpace.TransformDirection(new Vector3(stick.x, 0f, stick.y));
        dir.y = 0f;
        dir.Normalize();

        Vector3 move = dir * moveSpeed * Time.deltaTime;
        _cc.Move(move);
    }

    // ══════════════════════════════════════════════════════
    //  GRAVITY
    // ══════════════════════════════════════════════════════

    void HandleGravity()
    {
        // ★ Nếu gravity = 0 (VR sàn phẳng) → bỏ qua hoàn toàn
        if (Mathf.Approximately(gravity, 0f))
        {
            _verticalVelocity = 0f;
            return;
        }

        if (_cc.isGrounded)
        {
            _verticalVelocity = -0.1f; // Nhẹ nhàng giữ dính mặt đất
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
            // ★ Giới hạn tốc độ rơi tối đa
            _verticalVelocity = Mathf.Max(_verticalVelocity, MAX_GRAVITY_VEL);
        }

        _cc.Move(new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f));
    }

    // ══════════════════════════════════════════════════════
    //  SYNC CAPSULE — Cập nhật CC theo vị trí đầu
    // ══════════════════════════════════════════════════════

    void SyncCapsuleWithHead()
    {
        if (centerEyeAnchor == null) return;

        // Vị trí đầu trong local space của CameraRig
        Vector3 headLocal = transform.InverseTransformPoint(centerEyeAnchor.position);

        // ★ SMOOTH chiều cao — thay đổi từ từ, tránh CC bị overlap đột ngột → đẩy bật
        float targetHeight = Mathf.Max(headLocal.y + 0.1f, 0.5f);
        _smoothHeight = Mathf.Lerp(_smoothHeight, targetHeight, Time.deltaTime * 5f);

        // ★ Đảm bảo height >= radius * 2 (yêu cầu của CC)
        float finalHeight = Mathf.Max(_smoothHeight, _cc.radius * 2.1f);
        _cc.height = finalHeight;

        // Center theo XZ của đầu, Y = nửa chiều cao
        _cc.center = new Vector3(headLocal.x, finalHeight * 0.5f, headLocal.z);
    }

    // ══════════════════════════════════════════════════════
    //  DEPENETRATE — Bắn tia 360° từ đầu, đẩy ra nếu lọt
    // ══════════════════════════════════════════════════════

    void DepenetrateHead()
    {
        if (centerEyeAnchor == null) return;

        Vector3 headPos = centerEyeAnchor.position;
        Vector3 totalPush = Vector3.zero;
        int pushCount = 0;

        for (int i = 0; i < _dirs.Length; i++)
        {
            // RaycastAll để có thể lọc bỏ collider của mình
            RaycastHit[] hits = Physics.RaycastAll(
                headPos, _dirs[i], safeHeadRadius,
                wallLayer, QueryTriggerInteraction.Ignore
            );

            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger) continue;
                if (_ownColliders.Contains(hit.collider)) continue;

                float penetration = safeHeadRadius - hit.distance;
                if (penetration > 0.001f)
                {
                    // Đẩy theo hướng normal của mặt va chạm
                    Vector3 push = hit.normal;
                    push.y = 0f;
                    if (push.sqrMagnitude < 0.01f)
                        push = -_dirs[i]; // Fallback nếu normal gần thẳng đứng

                    push.Normalize();
                    totalPush += push * penetration;
                    pushCount++;
                }
            }
        }

        if (pushCount > 0)
        {
            Vector3 push = totalPush / pushCount;

            // ★ GIỚI HẠN đẩy tối đa mỗi frame → tránh bật xa
            if (push.magnitude > MAX_PUSH_PER_FRAME)
                push = push.normalized * MAX_PUSH_PER_FRAME;

            // ★ Chỉ đẩy XZ, KHÔNG đẩy Y
            push.y = 0f;

            SetPositionDirect(transform.position + push);
        }
    }

    // ══════════════════════════════════════════════════════
    //  CLAMP TO FLOOR — Không cho rơi dưới sàn
    // ══════════════════════════════════════════════════════

    void ClampToFloor()
    {
        Vector3 pos = transform.position;

        // ★ Clamp cả dưới sàn VÀ trên cao (tránh bị bật lên trời)
        if (pos.y < -0.01f || pos.y > 3f)
        {
            float clampedY = Mathf.Clamp(pos.y, 0f, 0f); // VR: luôn giữ Y = 0
            SetPositionDirect(new Vector3(pos.x, clampedY, pos.z));
            _verticalVelocity = 0f;

            if (showDebugLog && (pos.y < -0.5f || pos.y > 3f))
                Debug.LogWarning($"[VRCharController] Clamp Y: {pos.y:F2} → 0");
        }
    }

    // ══════════════════════════════════════════════════════
    //  FADE — Màn hình đen khi đầu gần tường
    // ══════════════════════════════════════════════════════

    void UpdateFade()
    {
        if (centerEyeAnchor == null) return;

        Vector3 headPos = centerEyeAnchor.position;
        float targetFade = 0f;

        // Kiểm tra 12+ hướng
        for (int i = 0; i < _dirs.Length; i++)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                headPos, _dirs[i], fadeStartDistance,
                wallLayer, QueryTriggerInteraction.Ignore
            );

            foreach (var h in hits)
            {
                if (!h.collider.isTrigger && !_ownColliders.Contains(h.collider))
                {
                    // Tính fade intensity dựa trên khoảng cách
                    float closeness = 1f - (h.distance / fadeStartDistance);
                    targetFade = Mathf.Max(targetFade, closeness);
                }
            }

            if (targetFade >= 1f) break; // Đã max, không cần kiểm tra thêm
        }

        _currentFade = Mathf.MoveTowards(_currentFade, targetFade, Time.deltaTime * fadeSpeed);
    }

    // ══════════════════════════════════════════════════════
    //  VIGNETTE — Giảm say tàu xe khi di chuyển
    // ══════════════════════════════════════════════════════

    void UpdateVignette()
    {
        if (_vignette == null) return;

        // Kiểm tra thumbstick có đang di chuyển
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, moveController);
        bool isMoving = stick.sqrMagnitude > 0.05f;

        float target = isMoving ? maxVignetteIntensity : 0f;
        _vignetteVal = Mathf.Lerp(_vignetteVal, target, Time.deltaTime * vignetteFadeSpeed);

        // Điều chỉnh FOV vignette (60 = full view, giảm = thu hẹp)
        _vignette.VignetteFieldOfView = 60f - (_vignetteVal * 40f);
    }

    // ══════════════════════════════════════════════════════
    //  OnGUI — Vẽ fade overlay
    // ══════════════════════════════════════════════════════

    void OnGUI()
    {
        if (!enableFade || _currentFade < 0.01f) return;

        GUI.color = new Color(0f, 0f, 0f, _currentFade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
    }

    // ══════════════════════════════════════════════════════
    //  SETUP
    // ══════════════════════════════════════════════════════

    void SetupCharacterController()
    {
        _cc.radius     = ccRadius;
        _cc.skinWidth  = ccSkinWidth;
        _cc.height     = 1.8f;                         // Sẽ auto-sync mỗi frame
        _cc.center     = new Vector3(0f, 0.9f, 0f);    // Sẽ auto-sync mỗi frame

        // ★★★ QUAN TRỌNG: Đặt = 0 cho VR ★★★
        // slopeLimit > 0 làm CC trượt trên mặt nghiêng → bị đẩy bật
        // stepOffset > 0 làm CC "nhảy lên" khi gặp gờ → bị bật cao
        _cc.slopeLimit = 0f;
        _cc.stepOffset = 0f;

        _cc.minMoveDistance = 0f;
    }

    void BuildRaycastDirections()
    {
        _dirs = new Vector3[raycastDirections];
        for (int i = 0; i < raycastDirections; i++)
        {
            float angle = i * (360f / raycastDirections) * Mathf.Deg2Rad;
            _dirs[i] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }
    }

    // ══════════════════════════════════════════════════════
    //  UTILITY — Đặt vị trí trực tiếp (tắt CC tạm thời)
    // ══════════════════════════════════════════════════════

    void SetPositionDirect(Vector3 newPos)
    {
        _cc.enabled = false;
        transform.position = newPos;
        _cc.enabled = true;
    }

    // ══════════════════════════════════════════════════════
    //  UTILITY — Tìm child theo tên (đệ quy)
    // ══════════════════════════════════════════════════════

    static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════
    //  PUBLIC API — Cho các script khác gọi
    // ══════════════════════════════════════════════════════

    /// <summary>Teleport người chơi đến vị trí mới (an toàn)</summary>
    public void Teleport(Vector3 worldPos)
    {
        SetPositionDirect(worldPos);
        _verticalVelocity = 0f;
        _prevHeadLocal = centerEyeAnchor != null
            ? centerEyeAnchor.localPosition
            : Vector3.zero;

        if (showDebugLog)
            Debug.Log($"[VRCharController] Teleport → {worldPos:F2}");
    }

    /// <summary>Bật/tắt di chuyển thumbstick</summary>
    public void SetLocomotionEnabled(bool enabled)
    {
        moveSpeed = enabled ? 2.5f : 0f;
    }

    /// <summary>Người chơi có đang đứng trên mặt đất?</summary>
    public bool IsGrounded => _cc != null && _cc.isGrounded;

    // ══════════════════════════════════════════════════════
    //  GIZMOS — Debug trong Scene View
    // ══════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Vẽ vùng an toàn quanh đầu
        if (centerEyeAnchor != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Gizmos.DrawWireSphere(centerEyeAnchor.position, safeHeadRadius);

            // Đường nối gốc → mắt
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, centerEyeAnchor.position);
            Gizmos.DrawWireSphere(centerEyeAnchor.position, 0.04f);
        }

        // Capsule CC
        if (_cc != null && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 center = transform.TransformPoint(_cc.center);
            Gizmos.DrawWireSphere(center, _cc.radius);

            // Trạng thái: xanh = grounded, đỏ = bay
            Gizmos.color = _cc.isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.05f, 0.07f);
        }

        // Vẽ các hướng raycast
        if (_dirs != null && centerEyeAnchor != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Vector3 head = centerEyeAnchor.position;
            for (int i = 0; i < _dirs.Length; i++)
            {
                Gizmos.DrawRay(head, _dirs[i] * safeHeadRadius);
            }
        }
    }
}
