using UnityEngine;

/// <summary>
/// SCRIPT 2 — CHẠY TRÊN META QUEST 3
/// Dùng hand tracking (bàn tay thật) để tương tác với hình khối.
/// Mục đích: Sản phẩm chính thức, học sinh thao tác bằng tay.
///
/// CỬ CHỈ:
///   Nhìn vào hình + PINCH tay phải     → CHỌN hình
///   Giữ pinch + di tay phải            → KÉO hình
///   PINCH tay trái (khi đã chọn)       → XOAY hình
///   Pinch CẢ 2 tay + dãn/co khoảng cách → PHÓNG TO / THU NHỎ
///   Pinch phải vào khoảng trống        → BỎ CHỌN 
///
/// GẮN VÀO: Bất kỳ GameObject (ví dụ GameManager)
/// CHÚ Ý: Tắt SimulatorInteraction khi dùng script này
/// </summary>
public class QuestHandInteraction : MonoBehaviour
{
    [Header("=== Tham chiếu tay ===")]
    public OVRCameraRig cameraRig;
    public OVRHand rightHand;
    public OVRSkeleton rightSkeleton;
    public OVRHand leftHand;
    public OVRSkeleton leftSkeleton;

    [Header("=== Ray ===")]
    public float rayLength = 10f;
    public float rayWidth = 0.004f;

    [Header("=== Reticle ===")]
    public float reticleSize = 0.012f;

    [Header("=== Tương tác ===")]
    [Range(0.1f, 1f)] public float pinchThreshold = 0.3f;
    public float grabSmooth = 12f;
    public float rotateSpeed = 100f;
    public float scaleMin = 0.05f;
    public float scaleMax = 10f;

    [Header("=== Debug ===")]
    public bool logEnabled = true;

    // Internal
    private Transform _cam;
    private LineRenderer _ray;
    private GameObject _dot;
    private MeshRenderer _dotRenderer;
    private GeometryObject _hovered;
    private GeometryObject _selected;
    private bool _grabbing;
    private float _grabDist;
    private Vector3 _grabOffset;
    private bool _rPinchPrev, _lPinchPrev;
    private int _logTimer;

    // Two-hand scale
    private bool _bimanualActive;
    private float _bimanualBaseDist;
    private Vector3 _bimanualBaseScale;

    // Bone cache
    private Transform _rIndexTip, _lIndexTip;
    private bool _bonesCached;

    // Màu
    static readonly Color C_IDLE = new Color(0.5f, 0.8f, 1f, 0.4f);
    static readonly Color C_HOVER = new Color(0f, 1f, 0.4f, 0.85f);
    static readonly Color C_GRAB = new Color(1f, 0.4f, 0f, 0.9f);

    void Start()
    {
        if (cameraRig == null) cameraRig = FindObjectOfType<OVRCameraRig>();

        // Auto-find hands
        if (rightHand == null || leftHand == null)
            FindHands();

        // Tia laser
        _ray = gameObject.AddComponent<LineRenderer>();
        _ray.positionCount = 2;
        _ray.startWidth = rayWidth;
        _ray.endWidth = rayWidth * 0.1f;
        _ray.material = MakeMat(C_IDLE);
        _ray.receiveShadows = false;
        _ray.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Chấm 3D
        _dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _dot.name = "_QuestReticle";
        _dot.transform.localScale = Vector3.one * reticleSize;
        Destroy(_dot.GetComponent<Collider>());
        _dotRenderer = _dot.GetComponent<MeshRenderer>();
        _dotRenderer.material = MakeMat(C_IDLE);
        _dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _dot.SetActive(false);

        Debug.Log("[Quest] Script Hand Tracking — HOẠT ĐỘNG");
        Debug.Log("[Quest] Pinch phải = Chọn/Kéo | Pinch trái = Xoay | 2 tay = Scale");
    }

    void Update()
    {
        // Camera
        if (_cam == null)
        {
            if (cameraRig != null && cameraRig.centerEyeAnchor != null)
                _cam = cameraRig.centerEyeAnchor;
            else _cam = Camera.main?.transform;
            if (_cam == null) return;
        }

        // Cache bones
        if (!_bonesCached) TryCacheBones();

        // Pinch detection
        bool rTracked = rightHand != null && rightHand.IsTracked;
        bool lTracked = leftHand != null && leftHand.IsTracked;
        float rPinchVal = rTracked ? rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) : 0f;
        float lPinchVal = lTracked ? leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) : 0f;
        bool rPinch = rPinchVal >= pinchThreshold;
        bool lPinch = lPinchVal >= pinchThreshold;

        // =====================
        // RAY — từ mắt (gaze)
        // =====================
        Vector3 origin = _cam.position;
        Vector3 dir = _cam.forward;

        // Nếu tay phải có PointerPose → dùng nó (tia từ tay)
        if (rTracked && rightHand.PointerPose != null)
        {
            origin = rightHand.PointerPose.position;
            dir = rightHand.PointerPose.forward;
        }

        bool didHit = Physics.Raycast(origin, dir, out RaycastHit hit, rayLength);

        // Vẽ tia + chấm
        _ray.SetPosition(0, origin);
        _ray.SetPosition(1, didHit ? hit.point : origin + dir * rayLength);

        if (didHit)
        {
            _dot.SetActive(true);
            _dot.transform.position = hit.point + hit.normal * 0.001f;
            float s = Mathf.Clamp(hit.distance * 0.008f, reticleSize * 0.5f, reticleSize * 4f);
            _dot.transform.localScale = Vector3.one * s;
        }
        else _dot.SetActive(false);

        // GeometryObject
        GeometryObject hitGeo = null;
        if (didHit)
        {
            hitGeo = hit.collider.GetComponent<GeometryObject>();
            if (hitGeo == null) hitGeo = hit.collider.GetComponentInParent<GeometryObject>();
        }
        DoHover(hitGeo);

        // =====================
        // 2 TAY SCALE (ưu tiên cao nhất)
        // =====================
        if (rPinch && lPinch && _selected != null && _rIndexTip != null && _lIndexTip != null)
        {
            float dist = Vector3.Distance(_rIndexTip.position, _lIndexTip.position);
            if (dist >= 0.08f)
            {
                if (!_bimanualActive)
                {
                    _bimanualBaseDist = dist;
                    _bimanualBaseScale = _selected.transform.localScale;
                    _bimanualActive = true;
                }
                else
                {
                    float ratio = dist / _bimanualBaseDist;
                    float s = Mathf.Clamp(_bimanualBaseScale.x * ratio, scaleMin, scaleMax);
                    _selected.transform.localScale = Vector3.one * s;
                }
            }
            _rPinchPrev = rPinch;
            _lPinchPrev = lPinch;

            SetRayColor(C_GRAB);
            return;
        }
        _bimanualActive = false;

        // =====================
        // PINCH PHẢI: Chọn / Kéo
        // =====================
        if (rPinch && !_rPinchPrev) // Vừa pinch
        {
            if (hitGeo != null)
            {
                DoSelect(hitGeo);
                _grabbing = true;
                _grabDist = hit.distance;
                _grabOffset = hitGeo.transform.position - hit.point;
            }
            else
            {
                DoDeselect();
            }
        }

        // Giữ pinch → kéo
        if (rPinch && _grabbing && _selected != null)
        {
            Vector3 target = origin + dir * _grabDist + _grabOffset;
            _selected.transform.position = Vector3.Lerp(
                _selected.transform.position, target, Time.deltaTime * grabSmooth);
        }

        // Thả pinch → dừng kéo
        if (!rPinch && _rPinchPrev) _grabbing = false;

        // =====================
        // PINCH TRÁI: Xoay
        // =====================
        if (lPinch && _selected != null)
        {
            _selected.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        // Lưu
        _rPinchPrev = rPinch;
        _lPinchPrev = lPinch;

        // Màu
        Color c = _grabbing ? C_GRAB : (_hovered != null ? C_HOVER : C_IDLE);
        SetRayColor(c);

        // Log
        if (logEnabled)
        {
            _logTimer++;
            if (_logTimer % 300 == 0)
            {
                string sel = _selected == null ? "---" : _selected.shapeName;
                Debug.Log($"[Quest] HandR={rTracked} PinchR={rPinchVal:F2} PinchL={lPinchVal:F2} | Sel={sel} | Grab={_grabbing}");
            }
        }
    }

    // ===== HOVER =====
    void DoHover(GeometryObject geo)
    {
        if (geo == _hovered) return;
        if (_hovered != null && _hovered != _selected)
        {
            Renderer r = _hovered.GetComponent<Renderer>();
            if (r) { r.material.DisableKeyword("_EMISSION"); r.material.SetColor("_EmissionColor", Color.black); }
        }
        _hovered = geo;
        if (_hovered != null && _hovered != _selected)
        {
            Renderer r = _hovered.GetComponent<Renderer>();
            if (r) { r.material.EnableKeyword("_EMISSION"); r.material.SetColor("_EmissionColor", _hovered.GetOriginalColor() * 0.2f); }
        }
    }

    // ===== SELECT =====
    void DoSelect(GeometryObject geo)
    {
        if (_selected != null && _selected != geo) _selected.Deselect();
        _selected = geo;
        _selected.Select();
        if (logEnabled) Debug.Log($"[Quest] ★ CHỌN: {geo.shapeName}");
    }

    void DoDeselect()
    {
        if (_selected != null)
        {
            if (logEnabled) Debug.Log($"[Quest] ✗ Bỏ chọn: {_selected.shapeName}");
            _selected.Deselect();
        }
        _selected = null;
        _grabbing = false;
    }

    // ===== PUBLIC =====
    public GeometryObject GetSelected() => _selected;

    // ===== UTIL =====
    void SetRayColor(Color c)
    {
        _ray.startColor = c;
        _ray.endColor = new Color(c.r, c.g, c.b, 0f);
        if (_dotRenderer != null) _dotRenderer.material.color = c;
    }

    void TryCacheBones()
    {
        if (rightSkeleton != null && rightSkeleton.IsInitialized)
            _rIndexTip = FindBone(rightSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        if (leftSkeleton != null && leftSkeleton.IsInitialized)
            _lIndexTip = FindBone(leftSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        _bonesCached = (_rIndexTip != null && _lIndexTip != null);
    }

    Transform FindBone(OVRSkeleton skel, OVRSkeleton.BoneId id)
    {
        if (skel == null || skel.Bones == null) return null;
        foreach (var b in skel.Bones)
            if (b != null && b.Id == id) return b.Transform;
        return null;
    }

    void FindHands()
    {
        if (cameraRig == null) return;
        OVRHand[] hands = cameraRig.GetComponentsInChildren<OVRHand>(true);
        foreach (var h in hands)
        {
            string n = h.gameObject.name.ToLower();
            if (rightHand == null && (n.Contains("right") || n.Contains("r_")))
            {
                rightHand = h;
                rightSkeleton = h.GetComponent<OVRSkeleton>();
            }
            if (leftHand == null && (n.Contains("left") || n.Contains("l_")))
            {
                leftHand = h;
                leftSkeleton = h.GetComponent<OVRSkeleton>();
            }
        }
    }

    Material MakeMat(Color c)
    {
        foreach (string n in new[] { "Unlit/Color", "Sprites/Default" })
        {
            Shader s = Shader.Find(n);
            if (s != null) { Material m = new Material(s); m.color = c; return m; }
        }
        return new Material(Shader.Find("Standard")) { color = c };
    }

    void OnDestroy()
    {
        if (_dot != null) Destroy(_dot);
    }
}
