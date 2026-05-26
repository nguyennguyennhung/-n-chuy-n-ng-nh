using UnityEngine;

/// <summary>
/// HỆ THỐNG TƯƠNG TÁC VR — "RAY POINTER" v5
///
/// Tia laser (gaze ray) từ mắt + chấm 3D tại điểm chạm.
/// Hoạt động trên cả Simulator và Quest.
///
/// === SIMULATOR ===
///   WASD       = Di chuyển
///   Mũi tên    = Nhìn quanh
///   CHUỘT TRÁI = Chọn/Kéo hình
///   CHUỘT PHẢI = Xoay hình
///   SCROLL     = Phóng to/Thu nhỏ
///   SPACE      = Bỏ chọn
///
/// === QUEST ===
///   Nhìn vào hình + Pinch phải = Chọn/Kéo
///   Pinch trái = Xoay
///
/// GẮN VÀO: Bất kỳ GameObject nào
/// </summary>
public class RayPointerInteraction : MonoBehaviour
{
    [Header("=== Tham chiếu ===")]
    public OVRCameraRig cameraRig;
    public OVRHand rightHand;
    public OVRHand leftHand;

    [Header("=== Ray ===")]
    public float rayLength = 10f;
    public float rayWidth = 0.005f;

    [Header("=== Reticle 3D ===")]
    [Tooltip("Kích thước chấm tại điểm chạm (m)")]
    public float reticleRadius = 0.015f;

    [Header("=== Tương tác ===")]
    [Range(0.1f, 1f)] public float pinchThreshold = 0.3f;
    public float scaleStep = 0.5f;
    public float scaleMin = 0.05f;
    public float scaleMax = 10f;
    public float rotateSpeed = 90f;
    public float grabSmoothing = 12f;

    [Header("=== Debug ===")]
    public bool showDebugLog = true;

    // ========== Internal ==========
    private LineRenderer _line;
    private Transform _eyeCenter;
    private GameObject _reticleObj;          // Chấm 3D tại điểm chạm
    private MeshRenderer _reticleRenderer;
    private GeometryObject _hoveredGeo;
    private GeometryObject _selectedGeo;
    private bool _isGrabbing;
    private float _grabDist;
    private Vector3 _grabOffset;

    // Input
    private bool _selectBtn, _selectPrev;
    private bool _rotateBtn;
    private float _scaleInput;
    private bool _deselectBtn;

    // Debug
    private int _debugTimer;
    private string _lastHitName = "---";
    private string _inputSrc = "";
    private bool _handTracked;
    private float _pinchR, _pinchL;

    // Colors
    private static readonly Color COL_IDLE = new Color(0.8f, 0.8f, 1f, 0.5f);
    private static readonly Color COL_HOVER = new Color(0f, 1f, 0.5f, 0.9f);
    private static readonly Color COL_GRAB = new Color(1f, 0.5f, 0f, 0.95f);

    void Start()
    {
        // === LINE RENDERER (tia laser 3D — luôn hiện) ===
        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = rayWidth;
        _line.endWidth = rayWidth * 0.15f;
        _line.material = MakeUnlitMat(COL_IDLE);
        _line.startColor = COL_IDLE;
        _line.endColor = new Color(COL_IDLE.r, COL_IDLE.g, COL_IDLE.b, 0f);
        _line.receiveShadows = false;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === RETICLE 3D (chấm sáng tại điểm chạm) ===
        _reticleObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _reticleObj.name = "RayReticle";
        _reticleObj.transform.localScale = Vector3.one * reticleRadius * 2f;
        // Xóa collider để không ảnh hưởng raycast
        Collider col = _reticleObj.GetComponent<Collider>();
        if (col != null) Destroy(col);
        // Material phát sáng
        _reticleRenderer = _reticleObj.GetComponent<MeshRenderer>();
        _reticleRenderer.material = MakeUnlitMat(COL_IDLE);
        _reticleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _reticleRenderer.receiveShadows = false;
        _reticleObj.SetActive(false);

        // Tham chiếu
        if (cameraRig == null) cameraRig = FindObjectOfType<OVRCameraRig>();
        if (rightHand == null) rightHand = FindHand(true);
        if (leftHand == null) leftHand = FindHand(false);

        Debug.Log("[RayPointer] v5 OK — Nhìn vào hình + Click chuột trái = Chọn");
    }

    void Update()
    {
        // Camera
        if (_eyeCenter == null)
        {
            if (cameraRig != null && cameraRig.centerEyeAnchor != null)
                _eyeCenter = cameraRig.centerEyeAnchor;
            else if (Camera.main != null)
                _eyeCenter = Camera.main.transform;
            if (_eyeCenter == null) return;
        }

        ReadInput();

        // ===== RAY: Luôn từ mắt =====
        Vector3 origin = _eyeCenter.position;
        Vector3 dir = _eyeCenter.forward;
        bool hit3D = Physics.Raycast(origin, dir, out RaycastHit hit, rayLength);

        // Vẽ tia
        Vector3 rayStart = origin + dir * 0.15f; // Bắt đầu xa mắt 15cm
        Vector3 rayEnd = hit3D ? hit.point : origin + dir * rayLength;
        _line.SetPosition(0, rayStart);
        _line.SetPosition(1, rayEnd);

        // Reticle 3D — chấm tại điểm chạm
        if (hit3D)
        {
            _reticleObj.SetActive(true);
            _reticleObj.transform.position = hit.point + hit.normal * 0.002f;
            // Scale theo khoảng cách (nhỏ khi gần, to khi xa → cùng size trên mắt)
            float distScale = Mathf.Clamp(hit.distance * 0.01f, reticleRadius, reticleRadius * 3f);
            _reticleObj.transform.localScale = Vector3.one * distScale * 2f;
        }
        else
        {
            _reticleObj.SetActive(false);
        }

        // Tìm GeometryObject
        GeometryObject hitGeo = null;
        if (hit3D)
        {
            hitGeo = hit.collider.GetComponent<GeometryObject>();
            if (hitGeo == null) hitGeo = hit.collider.GetComponentInParent<GeometryObject>();
            _lastHitName = hit.collider.gameObject.name;
        }
        else
        {
            _lastHitName = "(trống)";
        }

        UpdateHover(hitGeo);

        // ===== SELECT — removed, rebuild here =====

        // DRAG
        if (_selectBtn && _isGrabbing && _selectedGeo != null)
        {
            Vector3 target = origin + dir * _grabDist + _grabOffset;
            _selectedGeo.transform.position = Vector3.Lerp(
                _selectedGeo.transform.position, target, Time.deltaTime * grabSmoothing);
        }

        if (!_selectBtn && _selectPrev) _isGrabbing = false;

        // ROTATE
        if (_rotateBtn && _selectedGeo != null)
            _selectedGeo.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // SCALE
        if (_scaleInput != 0 && _selectedGeo != null)
        {
            float s = Mathf.Clamp(
                _selectedGeo.transform.localScale.x + _scaleInput * scaleStep * Time.deltaTime,
                scaleMin, scaleMax);
            _selectedGeo.transform.localScale = Vector3.one * s;
        }

        // DESELECT
        if (_deselectBtn) DeselectCurrent();

        // ===== MÀU =====
        Color c = _isGrabbing ? COL_GRAB : (_hoveredGeo != null ? COL_HOVER : COL_IDLE);
        _line.startColor = c;
        _line.endColor = new Color(c.r, c.g, c.b, 0f);
        if (_reticleRenderer != null)
            _reticleRenderer.material.color = c;

        _selectPrev = _selectBtn;

        // ===== DEBUG LOG (mỗi 5s) =====
        _debugTimer++;
        if (showDebugLog && _debugTimer % 300 == 0)
        {
            string sel = _selectedGeo == null ? "---" : _selectedGeo.shapeName;
            Debug.Log($"[RayPointer] Ray→{_lastHitName} | Sel={sel} | Hand={(_handTracked ? "OK" : "NO")} PinchR={_pinchR:F2} PinchL={_pinchL:F2} | {_inputSrc}");
        }
    }

    // ==========================================
    // INPUT
    // ==========================================
    void ReadInput()
    {
        _selectBtn = false;
        _rotateBtn = false;
        _scaleInput = 0;
        _deselectBtn = false;
        _inputSrc = "";

        // 1. OVR Hand Pinch (Simulator click chuột trái → tự fire pinch)
        _handTracked = false;
        _pinchR = 0; _pinchL = 0;

        if (rightHand != null && rightHand.IsTracked)
        {
            _handTracked = true;
            _pinchR = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            if (_pinchR >= pinchThreshold) { _selectBtn = true; _inputSrc += $"RPinch({_pinchR:F1}) "; }
        }
        if (leftHand != null && leftHand.IsTracked)
        {
            _pinchL = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            if (_pinchL >= pinchThreshold) { _rotateBtn = true; _inputSrc += $"LPinch({_pinchL:F1}) "; }
        }

        // 2. OVRInput (controller mode)
        float tR = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);
        float tL = OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger);
        if (tR > 0.5f) { _selectBtn = true; _inputSrc += "RTrig "; }
        if (tL > 0.5f) { _rotateBtn = true; _inputSrc += "LTrig "; }
        if (OVRInput.Get(OVRInput.RawButton.A)) { _scaleInput = 5f; _inputSrc += "A "; }
        if (OVRInput.Get(OVRInput.RawButton.B)) { _scaleInput = -5f; _inputSrc += "B "; }

        // 3. Mouse fallback (nếu OVR không tracked)
        if (!_handTracked)
        {
            if (Input.GetMouseButton(0)) { _selectBtn = true; _inputSrc += "LMB "; }
        }
        if (Input.GetMouseButton(1)) { _rotateBtn = true; _inputSrc += "RMB "; }
        float scr = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scr) > 0.01f) { _scaleInput = scr * 20f; _inputSrc += "Scroll "; }
        if (Input.GetKeyDown(KeyCode.Space)) { _deselectBtn = true; _inputSrc += "Space "; }

        if (string.IsNullOrEmpty(_inputSrc)) _inputSrc = "none";
    }

    // ==========================================
    // HOVER
    // ==========================================
    void UpdateHover(GeometryObject newHover)
    {
        if (newHover == _hoveredGeo) return;

        if (_hoveredGeo != null && _hoveredGeo != _selectedGeo)
        {
            Renderer r = _hoveredGeo.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.DisableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", Color.black);
            }
        }
        _hoveredGeo = newHover;
        if (_hoveredGeo != null && _hoveredGeo != _selectedGeo)
        {
            Renderer r = _hoveredGeo.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", _hoveredGeo.GetOriginalColor() * 0.25f);
            }
        }
    }

    // ==========================================
    // SELECTION
    // ==========================================
    void SelectObject(GeometryObject geo)
    {
        if (_selectedGeo != null && _selectedGeo != geo) _selectedGeo.Deselect();
        _selectedGeo = geo;
        _selectedGeo.Select();
    }

    void DeselectCurrent()
    {
        if (_selectedGeo != null)
        {
            if (showDebugLog) Debug.Log($"[RayPointer] ✗ Bỏ chọn {_selectedGeo.shapeName}");
            _selectedGeo.Deselect();
        }
        _selectedGeo = null;
        _isGrabbing = false;
    }

    // ==========================================
    // PUBLIC API
    // ==========================================
    public GeometryObject GetSelectedObject() => _selectedGeo;
    public bool IsGrabbing() => _isGrabbing;

    // ==========================================
    // TIỆN ÍCH
    // ==========================================
    Material MakeUnlitMat(Color c)
    {
        // Tìm shader Unlit không cần ánh sáng
        string[] names = { "Unlit/Color", "Sprites/Default", "UI/Default" };
        foreach (string n in names)
        {
            Shader s = Shader.Find(n);
            if (s != null)
            {
                Material m = new Material(s);
                m.color = c;
                return m;
            }
        }
        Material fallback = new Material(Shader.Find("Standard"));
        fallback.color = c;
        return fallback;
    }

    OVRHand FindHand(bool isRight)
    {
        if (cameraRig == null) return null;
        OVRHand[] hands = cameraRig.GetComponentsInChildren<OVRHand>(true);
        foreach (var h in hands)
        {
            string n = h.gameObject.name.ToLower();
            if (isRight && (n.Contains("right") || n.Contains("r_"))) return h;
            if (!isRight && (n.Contains("left") || n.Contains("l_"))) return h;
        }
        if (hands.Length >= 2) return isRight ? hands[1] : hands[0];
        if (hands.Length == 1) return hands[0];
        return null;
    }

    void OnDestroy()
    {
        if (_reticleObj != null) Destroy(_reticleObj);
    }
}
