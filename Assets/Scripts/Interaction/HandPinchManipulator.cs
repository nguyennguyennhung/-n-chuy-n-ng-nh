using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Two-hand pinch-based manipulator.
///
/// Quy ước CHUẨN cho học sinh:
///  - 1 tay pinch (chụm ngón cái-trỏ)   → DI CHUYỂN hình theo tay.
///  - 2 tay pinch cùng lúc              → SCALE theo khoảng cách + XOAY theo trục nối 2 tay.
///  - Tay PHẢI nắm (grip / full fist)   → XOAY tự do theo cổ tay.
///
/// Scale bị giới hạn bởi DiscreteScaleSteps (5 mức).
/// </summary>
[DisallowMultipleComponent]
public class HandPinchManipulator : MonoBehaviour
{
    [Header("Hand anchors")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Inputs (XR Hands binding: Pinch Value / Grip Value)")]
    public InputActionProperty pinchLeft;
    public InputActionProperty pinchRight;
    public InputActionProperty gripRight;

    [Header("Ngưỡng")]
    [Range(0.1f, 1f)] public float pinchThreshold = 0.7f;
    [Range(0.1f, 1f)] public float gripThreshold  = 0.7f;

    [Header("Tuỳ chọn")]
    public bool allowMove   = true;
    public bool allowRotate = true;
    public bool allowScale  = true;

    [Header("Scale 5 mức")]
    public float[] scaleSteps = { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f };
    public int   defaultScaleIndex = 2;
    public float snapDuration      = 0.2f;

    // Cached
    private Vector3 grabOffsetL, grabOffsetR;
    private Quaternion shapeRotAtGrab;
    private Vector3 shapeScaleAtGrab;
    private Vector3 axisAtGrab;
    private float   distAtGrab;
    private Quaternion handRotAtGrip;
    private Quaternion shapeRotAtGrip;

    private Vector3 baseScale;
    private bool snapping;
    private float snapTime;
    private Vector3 snapFrom, snapTo;

    private bool prevL, prevR, prevG;

    private void Awake()
    {
        // ★ FIX: chống IndexOutOfRange khi scaleSteps rỗng / defaultScaleIndex sai trong Inspector.
        if (scaleSteps == null || scaleSteps.Length == 0)
        {
            scaleSteps = new[] { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f };
        }
        defaultScaleIndex = Mathf.Clamp(defaultScaleIndex, 0, scaleSteps.Length - 1);

        baseScale = transform.localScale / Mathf.Max(0.001f, scaleSteps[defaultScaleIndex]);
        transform.localScale = baseScale * scaleSteps[defaultScaleIndex];
    }

    private void OnEnable()
    {
        pinchLeft.action?.Enable();
        pinchRight.action?.Enable();
        gripRight.action?.Enable();
    }
    private void OnDisable()
    {
        pinchLeft.action?.Disable();
        pinchRight.action?.Disable();
        gripRight.action?.Disable();
    }

    private void Update()
    {
        float lp = pinchLeft.action  != null ? pinchLeft.action.ReadValue<float>()  : 0f;
        float rp = pinchRight.action != null ? pinchRight.action.ReadValue<float>() : 0f;
        float rg = gripRight.action  != null ? gripRight.action.ReadValue<float>()  : 0f;
        bool L = lp > pinchThreshold;
        bool R = rp > pinchThreshold;
        bool G = rg > gripThreshold;

        // Rising-edge: cache state khi bắt đầu pinch/grip
        if ((!prevL && L) || (!prevR && R)) CachePinch();
        if (!prevG && G) CacheGrip();

        // Falling-edge khi cả 2 tay nhả → snap scale
        if ((prevL && prevR) && !(L && R)) SnapScale();

        if (L && R) ApplyTwoHand();
        else if (L) ApplyOneHand(leftHand,  grabOffsetL);
        else if (R) ApplyOneHand(rightHand, grabOffsetR);
        else if (G) ApplyGripRotate();

        TickSnap();

        prevL = L; prevR = R; prevG = G;
    }

    private void CachePinch()
    {
        if (leftHand)  grabOffsetL = transform.position - leftHand.position;
        if (rightHand) grabOffsetR = transform.position - rightHand.position;
        if (leftHand && rightHand)
        {
            Vector3 a = rightHand.position - leftHand.position;
            distAtGrab = a.magnitude;
            axisAtGrab = a.normalized;
            shapeRotAtGrab = transform.rotation;
            shapeScaleAtGrab = transform.localScale;
        }
    }

    private void CacheGrip()
    {
        if (!rightHand) return;
        handRotAtGrip  = rightHand.rotation;
        shapeRotAtGrip = transform.rotation;
    }

    private void ApplyOneHand(Transform h, Vector3 offset)
    {
        if (!h || !allowMove) return;
        transform.position = h.position + offset;
    }

    private void ApplyTwoHand()
    {
        if (!leftHand || !rightHand) return;
        Vector3 mid = (leftHand.position + rightHand.position) * 0.5f;
        Vector3 a = rightHand.position - leftHand.position;
        float d = a.magnitude;

        if (allowMove) transform.position = mid;
        if (allowRotate && distAtGrab > 1e-4f)
        {
            Quaternion delta = Quaternion.FromToRotation(axisAtGrab, a.normalized);
            transform.rotation = delta * shapeRotAtGrab;
        }
        if (allowScale && distAtGrab > 1e-4f)
        {
            float ratio = d / distAtGrab;
            Vector3 ns = shapeScaleAtGrab * ratio;
            // Clamp về min/max của scale steps
            float minV = baseScale.x * scaleSteps[0] * 0.5f;
            float maxV = baseScale.x * scaleSteps[scaleSteps.Length - 1] * 1.5f;
            float clamped = Mathf.Clamp(ns.x, minV, maxV);
            transform.localScale = Vector3.one * clamped;
        }
    }

    private void ApplyGripRotate()
    {
        if (!rightHand || !allowRotate) return;
        Quaternion dh = rightHand.rotation * Quaternion.Inverse(handRotAtGrip);
        transform.rotation = dh * shapeRotAtGrip;
    }

    private void SnapScale()
    {
        // Tìm step gần nhất
        float current = transform.localScale.x / baseScale.x;
        int nearest = 0; float bestD = float.MaxValue;
        for (int i = 0; i < scaleSteps.Length; i++)
        {
            float d = Mathf.Abs(scaleSteps[i] - current);
            if (d < bestD) { bestD = d; nearest = i; }
        }
        snapFrom = transform.localScale;
        snapTo   = baseScale * scaleSteps[nearest];
        snapTime = 0f;
        snapping = true;
    }

    private void TickSnap()
    {
        if (!snapping) return;
        snapTime += Time.deltaTime;
        float t = Mathf.Clamp01(snapTime / snapDuration);
        transform.localScale = Vector3.Lerp(snapFrom, snapTo, t);
        if (t >= 1f) snapping = false;
    }
}
