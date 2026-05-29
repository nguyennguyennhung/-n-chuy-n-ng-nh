using UnityEngine;

/// <summary>
/// BỘ ĐIỀU KHIỂN TAY TỔNG cho khối hình học — thay thế chuột/phím.
///
/// CỬ CHỈ XỬ LÝ TRONG FILE NÀY:
///   #1 Pinch trỏ phải vào khối     → CHỌN
///   #2 Giữ pinch phải + di tay     → KÉO
///   #3 Pinch trỏ trái + xoay cổ tay→ XOAY khối quanh Y
///   #4 Pinch trỏ cả 2 tay + dãn/co → PHÓNG TO / THU NHỎ
///   #9 Pinch trỏ phải vào không trung → BỎ CHỌN
///
/// CỬ CHỈ #5–#8 (T/W/V/L) ở file riêng HandWristMenu.cs — gọi
/// các public methods InvokeToggle... bên dưới.
///
/// CÁCH GẮN:
///   1. GameObject "GameManager" → Add Component → Hand Gesture Interaction.
///   2. Kéo OVRHand prefab tay PHẢI và TRÁI vào Right/Left Hand.
///   3. Cùng GameObject đó có OVRSkeleton — kéo vào Right/Left Skeleton.
///   4. TẮT (uncheck) component ObjectInteraction cũ trong cùng GameObject.
/// </summary>
[DisallowMultipleComponent]
public class HandGestureInteraction : MonoBehaviour
{
    // ====================================================
    // THAM CHIẾU TAY (kéo vào trong Inspector)
    // ====================================================
    [Header("Tham chiếu tay phải")]
    public OVRHand rightHand;
    public OVRSkeleton rightSkeleton;

    [Header("Tham chiếu tay trái")]
    public OVRHand leftHand;
    public OVRSkeleton leftSkeleton;

    // ====================================================
    // CÀI ĐẶT CỬ CHỈ
    // ====================================================
    [Header("Cài đặt cử chỉ chung")]
    [Tooltip("Pinch strength tối thiểu để coi là pinch (0–1). Giảm xuống ~0.6 nếu test trong Meta XR Simulator hoặc HS pinch khó kích hoạt.")]
    [Range(0.3f, 1f)] public float pinchThreshold = 0.7f;

    [Tooltip("Bán kính overlap quanh đầu ngón để chọn khối khi chạm trực tiếp (m).")]
    public float pinchSelectRadius = 0.08f;

    [Tooltip("Khoảng cách raycast từ đầu ngón ra trước (m) để chọn khối ở xa.")]
    public float pinchRayDistance = 1.5f;

    [Tooltip("Yêu cầu tracking confidence cao. TẮT khi test trong Meta XR Simulator (nó luôn trả về low confidence).")]
    public bool requireHighConfidence = false;

    [Tooltip("Bật để in log debug mỗi 1s: tracking, pinch strength, bone status. TẮT trong build production.")]
    public bool debugMode = true;

    [Header("Cài đặt xoay (cử chỉ #3)")]
    [Tooltip("Hệ số nhân tốc độ xoay theo cử động cổ tay.")]
    public float rotateSensitivity = 1.5f;

    [Header("Cài đặt phóng to/thu nhỏ (cử chỉ #4)")]
    public float scaleMin = 0.1f;
    public float scaleMax = 10f;

    // ====================================================
    // TRẠNG THÁI NỘI BỘ
    // ====================================================
    private GeometryObject selectedObject;     // Khối đang được chọn
    private bool wasRightPinching, wasLeftPinching;

    // Kéo thả (1 tay bất kỳ)
    private Transform grabTip; 
    private Vector3 dragOffset;

    // Thao tác 2 tay (Scale & Rotate)
    private bool bimanualActive;
    private float bimanualStartTime;
    private Transform freeTip;           // Ngón tay thứ 2 (dùng để điều khiển)
    private Vector3 freeTipStartPos;     // Vị trí bắt đầu của tay thứ 2
    private Vector3 bimanualBaseScale;
    private Quaternion bimanualBaseRot;

    // Cache bone đầu ngón trỏ
    private Transform rightIndexTip, leftIndexTip;

    void Update()
    {
        // === 1) TRACKING & BONES ===
        bool rightOK = HandTracked(rightHand);
        bool leftOK = HandTracked(leftHand);

        if (rightIndexTip == null) rightIndexTip = FindBone(rightSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        if (leftIndexTip == null) leftIndexTip = FindBone(leftSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);

        // === 2) PINCH STATE ===
        float rightPinchVal = ComputePinch(rightHand, rightSkeleton);
        float leftPinchVal = ComputePinch(leftHand, leftSkeleton);
        bool rightPinch = rightOK && rightPinchVal >= pinchThreshold;
        bool leftPinch = leftOK && leftPinchVal >= pinchThreshold;

        // === 3) THAO TÁC 2 TAY: SCALE & ROTATE (Kiểu Joystick cực dễ) ===
        if (rightPinch && leftPinch && selectedObject != null && rightIndexTip != null && leftIndexTip != null)
        {
            if (!bimanualActive)
            {
                // Xác định tay nào là tay gắp, tay nào là tay tự do
                freeTip = (grabTip == rightIndexTip) ? leftIndexTip : rightIndexTip;
                if (freeTip == null) freeTip = leftIndexTip; // Fallback an toàn
                
                freeTipStartPos = freeTip.position;
                bimanualBaseScale = selectedObject.transform.localScale;
                bimanualBaseRot = selectedObject.transform.rotation;
                bimanualActive = true;
                bimanualStartTime = Time.time;
            }
            else
            {
                // Tính khoảng cách tay tự do di chuyển so với lúc mới bấm
                Vector3 delta = freeTip.position - freeTipStartPos;

                // Kéo tay LÊN/XUỐNG để Phóng to / Thu nhỏ (10cm = x1.3)
                float scaleMultiplier = 1f + (delta.y * 3f); 
                float clampedScale = Mathf.Clamp((bimanualBaseScale * scaleMultiplier).x, scaleMin, scaleMax);
                selectedObject.transform.localScale = Vector3.one * clampedScale;

                // Kéo tay TRÁI/PHẢI để Xoay khối (Xoay quanh trục Y)
                float rotAngle = delta.x * -300f; // Kéo sang phải -> xoay phải
                selectedObject.transform.rotation = Quaternion.Euler(0, rotAngle, 0) * bimanualBaseRot;
            }
            wasRightPinching = wasLeftPinching = true;
            return;
        }

        // Nếu vừa nhả 1 tay ra khỏi trạng thái 2 tay
        if (bimanualActive && (!rightPinch || !leftPinch))
        {
            // TÀNG HÌNH KHỐI: Bấm nhấp tay thứ 2 thật nhanh (< 0.3s)
            if (Time.time - bimanualStartTime < 0.3f && selectedObject != null)
            {
                selectedObject.ToggleTransparency();
            }

            bimanualActive = false;
            
            // Cập nhật lại mốc kéo cho tay đang giữ khối để khối không bị giật
            if (rightPinch) grabTip = rightIndexTip;
            else if (leftPinch) grabTip = leftIndexTip;
            else grabTip = null;

            if (grabTip != null && selectedObject != null)
                dragOffset = selectedObject.transform.position - grabTip.position;
        }

        // === 4) THAO TÁC 1 TAY BẤT KỲ: CHỌN & KÉO ===
        if (!bimanualActive)
        {
            // Bắt khối
            if (rightPinch && !wasRightPinching && rightIndexTip != null) TryGrab(rightIndexTip);
            else if (leftPinch && !wasLeftPinching && leftIndexTip != null) TryGrab(leftIndexTip);

            // Kéo khối (di chuyển)
            if (selectedObject != null)
            {
                if (grabTip == rightIndexTip && !rightPinch) DeselectCurrent();
                else if (grabTip == leftIndexTip && !leftPinch) DeselectCurrent();
                else if (grabTip != null) DragSelected();
            }
        }

        wasRightPinching = rightPinch;
        wasLeftPinching = leftPinch;
    }

    // ====================================================
    // CỬ CHỈ GẮP KHỐI BẰNG TAY BẤT KỲ
    // ====================================================
    void TryGrab(Transform tip)
    {
        // 1. Chạm trực tiếp
        Collider[] hits = Physics.OverlapSphere(tip.position, pinchSelectRadius);
        foreach (Collider c in hits)
        {
            GeometryObject geo = c.GetComponent<GeometryObject>();
            if (geo != null)
            {
                SelectObject(geo, tip);
                return;
            }
        }

        // 2. Chạm từ xa (Raycast)
        Ray ray = new Ray(tip.position, tip.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pinchRayDistance))
        {
            GeometryObject geo = hit.collider.GetComponent<GeometryObject>();
            if (geo != null)
            {
                SelectObject(geo, tip);
                return;
            }
        }

        // 3. Pinch vào không trung → bỏ chọn
        DeselectCurrent();
    }

    // ====================================================
    // CỬ CHỈ #2: kéo khối theo đầu ngón
    // ====================================================
    void DragSelected()
    {
        if (grabTip == null) return;
        Vector3 target = grabTip.position + dragOffset;
        Rigidbody rb = selectedObject.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic) rb.MovePosition(target);
        else selectedObject.transform.position = target;
    }

    // ====================================================
    // SELECT / DESELECT
    // ====================================================
    void SelectObject(GeometryObject geo, Transform tip)
    {
        if (selectedObject != null && selectedObject != geo) selectedObject.Deselect();
        selectedObject = geo;
        grabTip = tip;
        dragOffset = selectedObject.transform.position - tip.position;
        selectedObject.Select();
    }

    void DeselectCurrent()
    {
        if (selectedObject != null) selectedObject.Deselect();
        selectedObject = null;
        grabTip = null;
    }

    // ====================================================
    // PUBLIC API — cho Wrist Menu (cử chỉ #5–#8) gọi vào
    // ====================================================
    public GeometryObject GetSelectedObject() => selectedObject;

    /// <summary>Trả về true nếu tay phải đang pinch (dùng để WristMenu tránh poke nhầm khi đang kéo khối).</summary>
    public bool IsRightPinching() => wasRightPinching;

    /// <summary>Cử chỉ #5: poke nút "Trong suốt".</summary>
    public void InvokeToggleTransparency()
    {
        if (selectedObject != null) selectedObject.ToggleTransparency();
    }

    /// <summary>Cử chỉ #6: poke nút "Cạnh khối".</summary>
    public void InvokeToggleWireframe()
    {
        if (selectedObject == null) return;
        WireframeRenderer wr = FindObjectOfType<WireframeRenderer>();
        if (wr != null) wr.ToggleWireframe(selectedObject.gameObject);
    }

    /// <summary>Cử chỉ #7: poke nút "Đỉnh A B C".</summary>
    public void InvokeToggleVertexLabels()
    {
        if (selectedObject == null) return;
        VertexLabelManager vlm = FindObjectOfType<VertexLabelManager>();
        if (vlm != null) vlm.ToggleLabels(selectedObject.gameObject);
    }

    /// <summary>Cử chỉ #8: poke nút "Ký hiệu h, R".</summary>
    public void InvokeToggleEdgeLabels()
    {
        if (selectedObject == null) return;
        EdgeLabelManager elm = FindObjectOfType<EdgeLabelManager>();
        if (elm != null) elm.ToggleEdgeLabels(selectedObject.gameObject);
    }

    // ====================================================
    // TIỆN ÍCH
    // ====================================================
    bool HandTracked(OVRHand h)
    {
        if (h == null || !h.IsTracked) return false;
        if (requireHighConfidence && !h.IsDataHighConfidence) return false;
        return true;
    }

    /// <summary>
    /// Tính độ pinch (0–1) theo 2 nguồn:
    /// 1) OVRHand.GetFingerPinchStrength (Quest thật trả giá trị; Simulator trả 0).
    /// 2) Khoảng cách thumb_tip ↔ index_tip — fallback cho Simulator.
    /// </summary>
    float ComputePinch(OVRHand hand, OVRSkeleton skel)
    {
        if (hand == null) return 0f;

        // Nguồn 0: Boolean từ API (Hoạt động hoàn hảo trong Meta XR Simulator khi bấm phím Pinch)
        if (hand.GetFingerIsPinching(OVRHand.HandFinger.Index)) return 1.0f;

        // Nguồn 1: API chính thức (Pinch Strength)
        float api = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        if (api > 0.05f) return api;

        // Nguồn 2: tự đo khoảng cách 2 đầu ngón
        if (skel == null || !skel.IsInitialized) return 0f;
        Transform thumbTip = FindBone(skel, OVRSkeleton.BoneId.Hand_ThumbTip);
        Transform indexTip = FindBone(skel, OVRSkeleton.BoneId.Hand_IndexTip);
        if (thumbTip == null || indexTip == null) return 0f;

        float dist = Vector3.Distance(thumbTip.position, indexTip.position);
        // Mapping: 0cm = pinch 1.0, 5cm = pinch 0.0 (tuyến tính)
        return Mathf.Clamp01(1f - dist / 0.05f);
    }

    // ====================================================
    // DEBUG LOG — in Console mỗi 1 giây để chẩn đoán "tay không chọn được hình"
    // ====================================================
    private float nextDebugTime;
    void LateUpdate()
    {
        if (!debugMode) return;
        if (Time.time < nextDebugTime) return;
        nextDebugTime = Time.time + 1f;

        string r = DescribeHand("RIGHT", rightHand, rightSkeleton, rightIndexTip);
        string l = DescribeHand("LEFT ", leftHand, leftSkeleton, leftIndexTip);
        Debug.Log($"[HandGesture] {r} | {l} | selected={(selectedObject == null ? "NONE" : selectedObject.name)}");
    }

    string DescribeHand(string tag, OVRHand h, OVRSkeleton s, Transform tip)
    {
        if (h == null) return $"{tag}: HAND_REF=NULL (chưa kéo OVRHand vào Inspector)";
        string tracked = h.IsTracked ? "TRACKED" : "NO_TRACK";
        string conf = h.IsDataHighConfidence ? "HighConf" : "LowConf";
        float pinchApi = h.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float pinchCalc = ComputePinch(h, s);
        string skelTxt = (s == null) ? "SKEL=NULL" : (s.IsInitialized ? "SkelOK" : "SkelInit?");
        string tipTxt = (tip == null) ? "TIP=NULL" : "TipOK";
        return $"{tag}: {tracked} {conf} pinchApi={pinchApi:F2} pinchCalc={pinchCalc:F2} {skelTxt} {tipTxt}";
    }

    Transform FindBone(OVRSkeleton skel, OVRSkeleton.BoneId id)
    {
        if (skel == null || !skel.IsInitialized) return null;
        foreach (var bone in skel.Bones) if (bone.Id == id) return bone.Transform;
        return null;
    }
}
