using UnityEngine;

/// <summary>
/// PROVIDER: QUEST 3 HAND TRACKING
/// Chỉ xử lý cử chỉ tay và va chạm, sau đó gọi lệnh sang InteractionCore.
/// </summary>
public class HandGestureInteraction : MonoBehaviour
{
    [Header("Core Reference")]
    public InteractionCore core;

    [Header("Hands")]
    public OVRHand rightHand;
    public OVRHand leftHand;

    [Header("Settings")]
    public float interactDist = 0.15f; // Chạm trực tiếp
    public float rotateSpeed = 60f;

    private GeometryObject _collidingGeo;
    private bool _isGrabbing;

    void Start()
    {
        if (core == null) core = FindObjectOfType<InteractionCore>();
    }

    void Update()
    {
        if (core == null) return;

        bool rPinch = IsPinching(rightHand);
        bool lPinch = IsPinching(leftHand);
        bool rGrip = IsGripping(rightHand);
        bool lGrip = IsGripping(leftHand);

        // 1. HỦY (Xòe cả 2 tay - không pinch không grip)
        if (!rPinch && !lPinch && !rGrip && !lGrip)
        {
            // core.ClearAll(); // Tùy chọn: có thể để người dùng bấm nút menu để hủy thay vì xòe tay
        }

        // 3. CẦM (Pinch xuyên hình - dùng HandPhysics để báo xuyên)
        // Lưu ý: Logic cầm tay thực tế sẽ cần vị trí xương tay. 
        // Ở đây ta gọi đơn giản để minh họa cấu trúc tách rời.
        if (rPinch && _isHandInside) 
        {
            core.SetGrabbing(true);
            core.MoveSelected(rightHand.transform.position, 20f);
        }
        else if (!rPinch)
        {
            core.SetGrabbing(false);
        }

        // 4. XOAY (Grip tay phải)
        if (rGrip && core.Selected != null)
        {
            core.RotateSelected(rotateSpeed);
        }

        // 5. SCALE (Grip 2 tay = to, Nắm tay trong hình = nhỏ)
        // Logic này bạn có thể tinh chỉnh thêm dựa trên trạng thái tay
    }

    private bool _isHandInside;
    public void SetHandInside(bool inside, GeometryObject geo)
    {
        _isHandInside = inside;
        _collidingGeo = geo;
    }

    bool IsPinching(OVRHand h) => h && h.GetFingerIsPinching(OVRHand.HandFinger.Index);
    bool IsGripping(OVRHand h) => h && h.GetFingerPinchStrength(OVRHand.HandFinger.Middle) > 0.8f;
}
