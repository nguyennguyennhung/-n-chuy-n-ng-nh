using UnityEngine;

/// <summary>
/// PROVIDER: META XR SIMULATOR
/// Chỉ xử lý Raycast và phím bấm, sau đó gọi lệnh sang InteractionCore.
/// </summary>
public class SimulatorInteraction : MonoBehaviour
{
    [Header("Core Reference")]
    public InteractionCore core;

    [Header("Raycast Settings")]
    public float rayLength = 15f;
    public float interactDist = 2f;
    public float touchDist = 0.5f;

    [Header("Visuals")]
    public LineRenderer rayLine;
    public GameObject reticle;

    private Transform _cam;
    private bool _isGrabbing;
    private float _grabDist;
    private Vector3 _grabOffset;

    void Start()
    {
        if (core == null) core = FindObjectOfType<InteractionCore>();
        _cam = Camera.main.transform;

        if (rayLine == null) rayLine = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (core == null || _cam == null) return;

        // 1. Raycast xác định mục tiêu
        Vector3 origin = _cam.position;
        Vector3 dir = _cam.forward;
        bool didHit = Physics.Raycast(origin, dir, out RaycastHit hit, rayLength);

        UpdateVisuals(origin, dir, didHit, hit);

        GeometryObject hitGeo = didHit ? hit.collider.GetComponentInParent<GeometryObject>() : null;

        // 2. Xử lý phím bấm
        HandleInput(hitGeo, hit);
    }

    void HandleInput(GeometryObject hitGeo, RaycastHit hit)
    {
        // [1] Hủy / reset
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            core.ClearAll();
            _isGrabbing = false;
        }

        // [3] Cầm (chỉ hoạt động khi đã có hình được chọn từ trước)
        if (Input.GetKeyDown(KeyCode.Alpha3) && core.Selected != null && hit.distance < interactDist)
        {
            _isGrabbing = true;
            _grabDist = hit.distance;
            _grabOffset = core.Selected.transform.position - hit.point;
            core.SetGrabbing(true);
        }

        // [Y] Xoay
        if (Input.GetKey(KeyCode.Y))
        {
            core.RotateSelected(45f);
        }

        // [4] Scale
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            bool isInside = (hitGeo != null && hit.distance < touchDist);
            core.ScaleSelected(!isInside); // Xa = To, Gần/Trong = Nhỏ
        }

        // Thực hiện di chuyển nếu đang cầm
        if (_isGrabbing && core.Selected != null)
        {
            Vector3 target = _cam.position + _cam.forward * _grabDist + _grabOffset;
            core.MoveSelected(target, 12f);
        }
    }

    void UpdateVisuals(Vector3 origin, Vector3 dir, bool didHit, RaycastHit hit)
    {
        if (rayLine)
        {
            rayLine.SetPosition(0, origin);
            rayLine.SetPosition(1, didHit ? hit.point : origin + dir * rayLength);
        }
        if (reticle)
        {
            reticle.SetActive(didHit);
            if (didHit) reticle.transform.position = hit.point;
        }
    }

    // --- Hàm bổ trợ bị thiếu ---
    bool IsPinching(OVRHand h)
    {
        if (h == null || !h.IsTracked) return false;
        return h.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.5f;
    }

    bool IsGrabbing(OVRHand h)
    {
        if (h == null || !h.IsTracked) return false;
        return h.GetFingerIsPinching(OVRHand.HandFinger.Middle);
    }
}
