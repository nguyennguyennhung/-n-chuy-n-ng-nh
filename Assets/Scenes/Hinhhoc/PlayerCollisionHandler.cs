using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CHỐNG XUYÊN TƯỜNG — CharacterController + RaycastAll Depenetration
/// GẮN VÀO: Camera Rig. Tự thêm CharacterController.
/// </summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CharacterController))]
public class PlayerCollisionHandler : MonoBehaviour
{
    [Header("=== Tham chiếu ===")]
    public Transform centerEyeAnchor;

    [Header("=== Collision ===")]
    public float safeRadius = 0.35f;
    public LayerMask wallLayer = ~0;

    [Header("=== Fade ===")]
    public bool enableFade = true;

    [Header("=== Debug ===")]
    public bool showDebugLog = true;

    // Internal
    CharacterController _cc;
    Vector3 _prevHeadLocal;
    bool _ready;
    int _warmup;
    float _currentFade;
    HashSet<Collider> _ownColliders = new HashSet<Collider>();

    // 12 hướng raycast (mỗi 30°)
    static readonly Vector3[] DIRS = new Vector3[12];
    static PlayerCollisionHandler()
    {
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            DIRS[i] = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
        }
    }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (centerEyeAnchor == null)
            centerEyeAnchor = FindDeepChild(transform, "CenterEyeAnchor");

        // Cache tất cả collider trên camera rig để bỏ qua khi raycast
        foreach (var col in GetComponentsInChildren<Collider>(true))
            _ownColliders.Add(col);

        // Setup CC
        _cc.height = 2f;
        _cc.radius = 0.2f;
        _cc.center = new Vector3(0, 1f, 0);
        _cc.slopeLimit = 0;
        _cc.stepOffset = 0;
        _cc.skinWidth = 0.01f;
    }

    void Start()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogError("[PlayerCollision] Không tìm thấy CenterEyeAnchor!");
            enabled = false;
            return;
        }
        if (showDebugLog)
            Debug.Log("[PlayerCollision] CC + Raycast OK");
    }

    // Chạy TRƯỚC OVR: ghi nhớ head local
    void Update()
    {
        if (centerEyeAnchor == null) return;

        if (!_ready)
        {
            _warmup++;
            if (_warmup < 15) { _prevHeadLocal = centerEyeAnchor.localPosition; return; }
            _ready = true;
            _prevHeadLocal = centerEyeAnchor.localPosition;
            if (showDebugLog) Debug.Log($"[PlayerCollision] Ready! Head={centerEyeAnchor.position:F2}");
            return;
        }

        _prevHeadLocal = centerEyeAnchor.localPosition;
    }

    // Chạy SAU OVR
    void LateUpdate()
    {
        if (!_ready || centerEyeAnchor == null) return;

        Vector3 currentHeadLocal = centerEyeAnchor.localPosition;
        Vector3 headDelta = currentHeadLocal - _prevHeadLocal;

        // Bảo vệ tracking nhảy
        if (headDelta.magnitude > 1f)
        {
            _cc.enabled = false;
            transform.position -= new Vector3(headDelta.x, 0, headDelta.z);
            _cc.enabled = true;
            return;
        }

        // === BƯỚC 1: CC.Move chặn tường ===
        Vector3 moveXZ = new Vector3(headDelta.x, 0, headDelta.z);
        Vector3 rootBefore = transform.position;
        _cc.Move(moveXZ);
        Vector3 actualMove = transform.position - rootBefore;

        // Nếu CC chặn (root di chuyển ít hơn dự kiến) → đẩy root lùi
        Vector3 overshoot = moveXZ - actualMove;
        if (overshoot.sqrMagnitude > 0.0001f)
        {
            _cc.enabled = false;
            transform.position -= overshoot;
            _cc.enabled = true;
        }

        // Giữ root Y cố định
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.y) > 0.01f)
        {
            _cc.enabled = false;
            transform.position = new Vector3(pos.x, 0, pos.z);
            _cc.enabled = true;
        }

        // Sync CC center theo đầu
        Vector3 headInRoot = transform.InverseTransformPoint(centerEyeAnchor.position);
        _cc.center = new Vector3(headInRoot.x, _cc.height * 0.5f, headInRoot.z);

        // === BƯỚC 2: RaycastAll depenetration — bắt lọt còn sót ===
        DepenetrateHead();

        // === BƯỚC 3: Fade ===
        if (enableFade) UpdateFade();
    }

    // Bắn 12 tia từ đầu, dùng RaycastAll để bỏ qua collider của mình
    void DepenetrateHead()
    {
        Vector3 headPos = centerEyeAnchor.position;
        Vector3 totalPush = Vector3.zero;
        int pushCount = 0;

        for (int i = 0; i < DIRS.Length; i++)
        {
            RaycastHit[] hits = Physics.RaycastAll(headPos, DIRS[i], safeRadius, wallLayer, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger) continue;
                if (_ownColliders.Contains(hit.collider)) continue;

                float penetration = safeRadius - hit.distance;
                if (penetration > 0.001f)
                {
                    Vector3 push = hit.normal;
                    push.y = 0;
                    if (push.sqrMagnitude < 0.01f) push = -DIRS[i];
                    push.Normalize();
                    totalPush += push * penetration;
                    pushCount++;
                }
            }
        }

        if (pushCount > 0)
        {
            Vector3 push = totalPush / pushCount;
            // Đẩy tức thì, không giới hạn
            _cc.enabled = false;
            transform.position += push;
            _cc.enabled = true;
        }
    }

    void UpdateFade()
    {
        Vector3 headPos = centerEyeAnchor.position;
        float targetFade = 0f;

        RaycastHit[] hits = Physics.RaycastAll(headPos, Vector3.forward, 0.05f, wallLayer, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (!h.collider.isTrigger && !_ownColliders.Contains(h.collider))
            { targetFade = 1f; break; }
        }
        if (targetFade < 1f)
        {
            for (int i = 0; i < DIRS.Length; i++)
            {
                hits = Physics.RaycastAll(headPos, DIRS[i], 0.05f, wallLayer, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (!h.collider.isTrigger && !_ownColliders.Contains(h.collider))
                    { targetFade = 1f; break; }
                }
                if (targetFade >= 1f) break;
            }
        }

        _currentFade = Mathf.MoveTowards(_currentFade, targetFade, Time.deltaTime * 8f);
    }

    void OnGUI()
    {
        if (!enableFade || _currentFade < 0.01f) return;
        GUI.color = new Color(0, 0, 0, _currentFade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
    }

    static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform c in parent)
        {
            if (c.name == name) return c;
            Transform f = FindDeepChild(c, name);
            if (f != null) return f;
        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (centerEyeAnchor == null) return;
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(centerEyeAnchor.position, safeRadius);
    }
}
