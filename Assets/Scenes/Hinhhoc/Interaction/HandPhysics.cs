using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CHỐNG TAY XUYÊN VẬT THỂ — v2 "VISUAL STOP"
///
/// CÁCH HOẠT ĐỘNG:
///   - Gắn SphereCollider (trigger) vào đầu ngón tay
///   - Khi ngón chạm vật → hiệu ứng:
///     1. Tay mờ dần (visual feedback)
///     2. Event OnHandTouch/OnHandRelease để code khác xử lý
///   - Vì VR tracking không thể chặn tay thật, ta dùng trigger
///     + visual feedback thay vì collision cứng
///
/// GẮN VÀO: Mỗi OVRHandPrefab (cả tay trái và tay phải).
/// </summary>
public class HandPhysics : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Bán kính collider đầu ngón tay (m)")]
    public float fingerTipRadius = 0.008f;

    [Tooltip("Bán kính collider lòng bàn tay (m)")]
    public float palmRadius = 0.025f;

    [Header("Visual Feedback")]
    [Tooltip("Độ mờ khi tay xuyên vật (0=ẩn hoàn toàn, 1=không mờ)")]
    [Range(0f, 1f)]
    public float penetrationAlpha = 0.2f;

    [Tooltip("Tốc độ chuyển đổi mờ/rõ")]
    public float fadeSpeed = 12f;

    [Header("Touch Detection")]
    [Tooltip("Layer chứa vật thể có thể chạm")]
    public LayerMask touchableLayer = ~0;

    // Bones cần gắn collider
    private static readonly OVRSkeleton.BoneId[] tipBones = {
        OVRSkeleton.BoneId.Hand_ThumbTip,
        OVRSkeleton.BoneId.Hand_IndexTip,
        OVRSkeleton.BoneId.Hand_MiddleTip,
        OVRSkeleton.BoneId.Hand_RingTip,
        OVRSkeleton.BoneId.Hand_PinkyTip
    };

    private OVRSkeleton skeleton;
    private OVRHand hand;
    private bool collidersAdded;
    private List<HandColliderTracker> trackers = new List<HandColliderTracker>();
    private SkinnedMeshRenderer handRenderer;
    private float currentAlpha = 1f;
    private MaterialPropertyBlock propBlock;

    // Public: vật đang chạm
    [HideInInspector] public bool isTouchingAny;
    [HideInInspector] public GameObject touchedObject;
    [HideInInspector] public Vector3 touchPoint;
    [HideInInspector] public Vector3 touchNormal;

    void Start()
    {
        skeleton = GetComponent<OVRSkeleton>();
        hand = GetComponent<OVRHand>();
        handRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        // Đợi skeleton init xong rồi mới gắn collider
        if (!collidersAdded && skeleton != null && skeleton.IsInitialized)
        {
            AddColliders();
            collidersAdded = true;
        }

        if (!collidersAdded) return;

        // Kiểm tra có ngón nào đang chạm vật không
        isTouchingAny = false;
        touchedObject = null;

        foreach (var tracker in trackers)
        {
            if (tracker != null && tracker.isTouching)
            {
                isTouchingAny = true;
                touchedObject = tracker.touchedObject;
                touchPoint = tracker.transform.position;
                touchNormal = tracker.touchNormal;
                break;
            }
        }

        // Fade effect
        float targetAlpha = isTouchingAny ? penetrationAlpha : 1f;
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // Áp dụng alpha lên tay
        if (handRenderer != null)
        {
            handRenderer.GetPropertyBlock(propBlock);
            Color c = propBlock.GetColor("_BaseColor");
            if (c == Color.clear) c = Color.white;
            c.a = currentAlpha;
            propBlock.SetColor("_BaseColor", c);
            handRenderer.SetPropertyBlock(propBlock);
        }
    }

    void AddColliders()
    {
        // Gắn collider vào 5 đầu ngón tay
        foreach (var boneId in tipBones)
        {
            Transform bone = FindBone(boneId);
            if (bone != null)
            {
                var tracker = AddSphereCollider(bone, fingerTipRadius);
                if (tracker != null) trackers.Add(tracker);
            }
        }

        // Gắn collider vào lòng bàn tay (WristRoot)
        Transform wrist = FindBone(OVRSkeleton.BoneId.Hand_WristRoot);
        if (wrist != null)
        {
            var tracker = AddSphereCollider(wrist, palmRadius);
            if (tracker != null) trackers.Add(tracker);
        }

        Debug.Log($"[HandPhysics] Đã gắn {trackers.Count} collider vào tay");
    }

    HandColliderTracker AddSphereCollider(Transform bone, float radius)
    {
        // Tạo child object để không ảnh hưởng bone gốc
        GameObject colliderObj = new GameObject("HandCollider_" + bone.name);
        colliderObj.transform.SetParent(bone, false);
        colliderObj.transform.localPosition = Vector3.zero;
        colliderObj.layer = gameObject.layer;

        // Rigidbody kinematic — di chuyển theo bone nhưng có va chạm
        Rigidbody rb = colliderObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Sphere collider — trigger để detect, không đẩy vật
        SphereCollider sc = colliderObj.AddComponent<SphereCollider>();
        sc.radius = radius;
        sc.isTrigger = true;

        // Tracker để biết có đang chạm không
        HandColliderTracker tracker = colliderObj.AddComponent<HandColliderTracker>();
        tracker.touchableLayer = touchableLayer;
        return tracker;
    }

    Transform FindBone(OVRSkeleton.BoneId id)
    {
        if (skeleton == null || skeleton.Bones == null) return null;
        foreach (var b in skeleton.Bones)
            if (b != null && b.Id == id) return b.Transform;
        return null;
    }

    /// <summary>
    /// Lấy ngón trỏ tracker (cho HandGestureInteraction)
    /// </summary>
    public HandColliderTracker GetIndexTipTracker()
    {
        // Index tip là phần tử thứ 2 (sau ThumbTip)
        if (trackers.Count >= 2) return trackers[1];
        return null;
    }

    /// <summary>
    /// Kiểm tra có đang chạm GeometryObject không
    /// (ưu tiên — duyệt qua TẤT CẢ tracker để tìm GeometryObject,
    ///  bỏ qua các vật không phải hình như tường/cửa sổ)
    /// </summary>
    public GeometryObject GetTouchedGeometry()
    {
        if (trackers == null) return null;

        foreach (var tracker in trackers)
        {
            if (tracker == null || !tracker.isTouching) continue;
            if (tracker.touchedObject == null) continue;

            // Kiểm tra cả parent vì collider có thể nằm trên child
            GeometryObject geo = tracker.touchedObject.GetComponent<GeometryObject>();
            if (geo == null) geo = tracker.touchedObject.GetComponentInParent<GeometryObject>();
            if (geo != null) return geo;
        }
        return null;
    }
}

/// <summary>
/// Theo dõi va chạm trên từng collider ngón tay.
/// Lưu thông tin vật đang chạm để code khác sử dụng.
/// </summary>
public class HandColliderTracker : MonoBehaviour
{
    [HideInInspector] public bool isTouching;
    [HideInInspector] public GameObject touchedObject;
    [HideInInspector] public Vector3 touchNormal;
    [HideInInspector] public LayerMask touchableLayer = ~0;

    private int touchCount;

    void OnTriggerEnter(Collider other)
    {
        // Bỏ qua va chạm với chính tay
        if (other.GetComponent<HandColliderTracker>() != null) return;
        if (other.isTrigger) return;

        // Kiểm tra layer
        if ((touchableLayer & (1 << other.gameObject.layer)) == 0) return;

        touchCount++;
        isTouching = true;
        touchedObject = other.gameObject;

        // Tính normal an toàn cho mọi loại Collider
        Vector3 closestPoint = transform.position;
        if (other is BoxCollider || other is SphereCollider || other is CapsuleCollider || (other is MeshCollider mc && mc.convex))
        {
            closestPoint = other.ClosestPoint(transform.position);
        }
        else
        {
            // Fallback cho TerrainCollider, non-convex MeshCollider, v.v.
            closestPoint = other.bounds.ClosestPoint(transform.position);
        }

        touchNormal = (transform.position - closestPoint).normalized;
        if (touchNormal == Vector3.zero) touchNormal = Vector3.up;

        // Highlight nếu là GeometryObject
        GeometryObject geo = other.GetComponent<GeometryObject>();
        if (geo != null && !geo.isSelected)
        {
            // Highlight nhẹ khi chạm (hover effect)
            Renderer rend = geo.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", geo.GetOriginalColor() * 0.15f);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<HandColliderTracker>() != null) return;
        if (other.isTrigger) return;
        if ((touchableLayer & (1 << other.gameObject.layer)) == 0) return;

        touchCount = Mathf.Max(0, touchCount - 1);
        isTouching = touchCount > 0;

        if (!isTouching)
        {
            // Bỏ highlight khi rời
            if (touchedObject != null)
            {
                GeometryObject geo = touchedObject.GetComponent<GeometryObject>();
                if (geo != null && !geo.isSelected)
                {
                    Renderer rend = geo.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material.DisableKeyword("_EMISSION");
                        rend.material.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
            touchedObject = null;
        }
    }
}
