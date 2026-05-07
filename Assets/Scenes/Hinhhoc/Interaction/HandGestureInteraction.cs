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

    // Drag (cử chỉ #2)
    private Vector3 dragOffset;

    // Rotate (cử chỉ #3): mốc cổ tay khi bắt đầu pinch trái
    private Quaternion leftWristRefRotation;
    private bool leftRotateActive;

    // Scale (cử chỉ #4): khoảng cách 2 đầu ngón khi bắt đầu pinch cả 2 tay
    private float bimanualBaseDistance;
    private Vector3 bimanualBaseScale;
    private bool bimanualScaleActive;

    // Cache bone đầu ngón trỏ
    private Transform rightIndexTip, leftIndexTip;
    // Cache bone cổ tay (cho rotate)
    private Transform leftWrist;

    void Update()
    {
        // === 1) TRACKING & BONES ===
        bool rightOK = HandTracked(rightHand);
        bool leftOK = HandTracked(leftHand);

        if (rightIndexTip == null) rightIndexTip = FindBone(rightSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        if (leftIndexTip == null) leftIndexTip = FindBone(leftSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        if (leftWrist == null) leftWrist = FindBone(leftSkeleton, OVRSkeleton.BoneId.Hand_WristRoot);

        // === 2) PINCH STATE ===
        // Pitfall: Meta XR Simulator KHÔNG gửi giá trị qua GetFingerPinchStrength.
        // ⇒ Tự tính pinch theo khoảng cách thumb-index bone (hoạt động cả Quest thật lẫn Simulator).
        float rightPinchVal = ComputePinch(rightHand, rightSkeleton);
        float leftPinchVal = ComputePinch(leftHand, leftSkeleton);
        bool rightPinch = rightOK && rightPinchVal >= pinchThreshold;
        bool leftPinch = leftOK && leftPinchVal >= pinchThreshold;

        // === 3) BIMANUAL SCALE — ưu tiên cao nhất (cử chỉ #4) ===
        // Khi cả 2 tay đang pinch + có khối được chọn ⇒ scale theo khoảng cách 2 đầu ngón.
        // Pitfall: nếu 2 tay quá gần (<10cm) Quest hay nhầm pinch ⇒ tạm bỏ qua frame đó.
        if (rightPinch && leftPinch && selectedObject != null
            && rightIndexTip != null && leftIndexTip != null)
        {
            float dist = Vector3.Distance(rightIndexTip.position, leftIndexTip.position);
            if (dist < 0.1f)
            {
                bimanualScaleActive = false;   // Đợi 2 tay tách ra
            }
            else
            {
                if (!bimanualScaleActive)
                {
                    bimanualBaseDistance = dist;
                    bimanualBaseScale = selectedObject.transform.localScale;
                    bimanualScaleActive = true;
                }
                else
                {
                    float ratio = dist / bimanualBaseDistance;
                    Vector3 newScale = bimanualBaseScale * ratio;
                    float clamped = Mathf.Clamp(newScale.x, scaleMin, scaleMax);
                    selectedObject.transform.localScale = Vector3.one * clamped;
                }
            }
            wasRightPinching = wasLeftPinching = true;
            return;   // Đang scale thì không xử lý select/rotate (tránh chồng cử chỉ).
        }
        else
        {
            bimanualScaleActive = false;
        }

        // === 4) RIGHT PINCH — SELECT + DRAG (cử chỉ #1, #2, #9) ===
        if (rightOK && rightIndexTip != null)
        {
            // Cạnh xuống pinch (vừa pinch) → thử chọn khối.
            if (rightPinch && !wasRightPinching) TrySelectAtRightFinger();

            // Đang giữ pinch + có khối → kéo theo đầu ngón.
            if (rightPinch && selectedObject != null) DragSelected();
        }
        wasRightPinching = rightPinch;

        // === 5) LEFT PINCH — ROTATE (cử chỉ #3) ===
        // Khi vừa pinch trái + có khối được chọn → mốc cổ tay; sau đó cổ tay xoay
        // khoảng nào quanh Y, khối xoay khoảng đó.
        if (leftOK && selectedObject != null && leftWrist != null)
        {
            if (leftPinch && !wasLeftPinching)
            {
                leftWristRefRotation = leftWrist.rotation;
                leftRotateActive = true;
            }
            else if (!leftPinch && wasLeftPinching)
            {
                leftRotateActive = false;
            }

            if (leftRotateActive && leftPinch)
            {
                Quaternion delta = leftWrist.rotation * Quaternion.Inverse(leftWristRefRotation);
                float yawDeg = delta.eulerAngles.y;
                if (yawDeg > 180f) yawDeg -= 360f;   // Chuyển về [-180,180]

                selectedObject.transform.Rotate(Vector3.up, yawDeg * rotateSensitivity * Time.deltaTime, Space.World);
            }
        }
        else
        {
            leftRotateActive = false;
        }
        wasLeftPinching = leftPinch;
    }

    // ====================================================
    // CỬ CHỈ #1, #9: chọn khối / bỏ chọn khi pinch phải
    // ====================================================
    void TrySelectAtRightFinger()
    {
        // 1. Overlap sphere — chạm trực tiếp
        Collider[] hits = Physics.OverlapSphere(rightIndexTip.position, pinchSelectRadius);
        if (debugMode) Debug.Log($"[HandGesture] PINCH! tipPos={rightIndexTip.position} hits={hits.Length}");

        foreach (Collider c in hits)
        {
            GeometryObject geo = c.GetComponent<GeometryObject>();
            if (debugMode) Debug.Log($"[HandGesture]   hit collider '{c.name}' geometryObject={(geo != null)}");
            if (geo != null)
            {
                SelectObject(geo);
                dragOffset = geo.transform.position - rightIndexTip.position;
                return;
            }
        }

        // 2. Raycast forward — chọn khối ở xa (Distance Grab nhẹ)
        Ray ray = new Ray(rightIndexTip.position, rightIndexTip.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pinchRayDistance))
        {
            GeometryObject geo = hit.collider.GetComponent<GeometryObject>();
            if (debugMode) Debug.Log($"[HandGesture] RAY hit '{hit.collider.name}' geometryObject={(geo != null)}");
            if (geo != null)
            {
                SelectObject(geo);
                dragOffset = geo.transform.position - hit.point;
                return;
            }
        }
        else if (debugMode) Debug.Log("[HandGesture] RAY miss — không có collider trên đường đi");

        // 3. Pinch vào không trung → bỏ chọn (cử chỉ #9)
        DeselectCurrent();
    }

    // ====================================================
    // CỬ CHỈ #2: kéo khối theo đầu ngón
    // ====================================================
    void DragSelected()
    {
        Vector3 target = rightIndexTip.position + dragOffset;
        Rigidbody rb = selectedObject.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic) rb.MovePosition(target);
        else selectedObject.transform.position = target;
    }

    // ====================================================
    // SELECT / DESELECT
    // ====================================================
    void SelectObject(GeometryObject geo)
    {
        if (selectedObject != null && selectedObject != geo) selectedObject.Deselect();
        selectedObject = geo;
        selectedObject.Select();
    }

    void DeselectCurrent()
    {
        if (selectedObject != null) selectedObject.Deselect();
        selectedObject = null;
    }

    // ====================================================
    // PUBLIC API — cho Wrist Menu (cử chỉ #5–#8) gọi vào
    // ====================================================
    public GeometryObject GetSelectedObject() => selectedObject;

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

        // Nguồn 1: API chính thức
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
