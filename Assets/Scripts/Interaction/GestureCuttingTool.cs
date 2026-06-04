using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Công cụ cắt thiết diện bằng gesture tay với CONSTRAINT.
///
/// Cách dùng:
///  1. Gắn lên cùng GameObject với hình (cube/cylinder/...).
///  2. Set shapeType = loại hình.
///  3. Bind pinchRight / pinchLeft (InputAction trả về float 0..1).
///  4. Kéo rightHandAnchor / leftHandAnchor.
///  5. Kéo crossSectionRenderer vào field tương ứng.
///
/// Flow gesture:
///   - Pinch tay PHẢI > 0.7  → bắt đầu "preview cut", tạo plane theo hướng tay
///   - Hệ thống tìm valid section gần nhất → snap preview + highlight xanh
///   - Pinch tay TRÁI > 0.7 trong khi vẫn pinch phải → CONFIRM cắt, hiển thị đỏ
///   - Pinch tay PHẢI 1 lần nữa khi không có cắt nào → xóa
///
/// Constraint: KHÔNG cho cắt tự do — luôn snap về 1 trong các mặt định nghĩa
/// trong ShapeCutLibrary.
/// </summary>
public class GestureCuttingTool : MonoBehaviour
{
    public enum ShapeType { Cube, Cylinder, Cone, Sphere, Pyramid }

    [Header("Cấu hình hình")]
    public ShapeType shapeType = ShapeType.Cube;
    public float halfSize = 0.5f;
    public float height   = 1f;

    [Header("Renderer hiển thị mặt cắt — đặt làm child cùng tâm hình")]
    public CrossSectionRenderer crossSectionRenderer;

    [Header("Hand anchors (kéo từ XR Origin > Hand)")]
    public Transform rightHandAnchor;
    public Transform leftHandAnchor;

    [Header("Pinch inputs (XR Hands binding)")]
    public InputActionProperty pinchRight;
    public InputActionProperty pinchLeft;

    [Header("Ngưỡng pinch coi là 'đang pinch'")]
    [Range(0.1f, 1f)] public float pinchThreshold = 0.7f;

    [Header("Ngưỡng snap")]
    [Tooltip("Sai khác góc giữa pháp tuyến tay và pháp tuyến mp hợp lệ — ° (độ)")]
    public float angleSnapDegrees    = 25f;
    [Tooltip("Khoảng cách từ điểm tay đến mặt phẳng để snap — local-space units")]
    public float distanceSnapMeters  = 0.15f;

    [Header("Materials")]
    public Color previewColor = new Color(0f, 1f, 0.5f, 0.7f);
    public Color confirmColor = new Color(1f, 0.2f, 0.2f, 1f);

    private List<CrossSectionDefinition> validSections;
    private CrossSectionDefinition currentPreview;
    private CrossSectionDefinition currentConfirmed;

    private bool wasRightPinch, wasLeftPinch;

    private void Awake()
    {
        RebuildLibrary();
    }

    private void OnEnable()
    {
        pinchRight.action?.Enable();
        pinchLeft.action?.Enable();
    }
    private void OnDisable()
    {
        pinchRight.action?.Disable();
        pinchLeft.action?.Disable();
    }

    public void RebuildLibrary()
    {
        switch (shapeType)
        {
            case ShapeType.Cube:     validSections = ShapeCutLibrary.Cube(halfSize); break;
            case ShapeType.Cylinder: validSections = ShapeCutLibrary.Cylinder(halfSize, height); break;
            case ShapeType.Cone:     validSections = ShapeCutLibrary.Cone(halfSize, height); break;
            case ShapeType.Sphere:   validSections = ShapeCutLibrary.Sphere(halfSize); break;
            case ShapeType.Pyramid:  validSections = ShapeCutLibrary.Pyramid(halfSize, height); break;
        }
    }

    private void Update()
    {
        float rp = pinchRight.action != null ? pinchRight.action.ReadValue<float>() : 0f;
        float lp = pinchLeft.action  != null ? pinchLeft.action.ReadValue<float>()  : 0f;
        bool rightPinch = rp > pinchThreshold;
        bool leftPinch  = lp > pinchThreshold;

        // Click logic cho tay phải: trạng thái "rising edge" khi không có confirmed cut
        bool rightJustPinched = !wasRightPinch && rightPinch;

        // CASE 1: Đang có 1 cắt được confirmed → tay phải pinch lần nữa → xoá
        if (currentConfirmed != null && rightJustPinched)
        {
            currentConfirmed = null;
            crossSectionRenderer?.Clear();
            wasRightPinch = rightPinch;
            wasLeftPinch  = leftPinch;
            return;
        }

        // CASE 2: Đang preview + tay trái vừa pinch → CONFIRM
        if (currentPreview != null && !wasLeftPinch && leftPinch)
        {
            currentConfirmed = currentPreview;
            currentPreview   = null;
            if (crossSectionRenderer != null)
            {
                crossSectionRenderer.outlineColor = confirmColor;
                crossSectionRenderer.Draw(currentConfirmed);
            }
        }
        // CASE 3: Tay phải đang pinch → preview cut
        // ★ FIX: chỉ vẽ lại khi mục tiêu snap thay đổi — tránh tạo hàng trăm LineRenderer/frame
        else if (rightPinch && currentConfirmed == null)
        {
            CrossSectionDefinition next = FindBestSnap();
            if (next != currentPreview)
            {
                currentPreview = next;
                if (currentPreview != null && crossSectionRenderer != null)
                {
                    crossSectionRenderer.outlineColor = previewColor;
                    crossSectionRenderer.Draw(currentPreview);
                }
                else if (currentPreview == null)
                {
                    crossSectionRenderer?.Clear();
                }
            }
        }
        // CASE 4: Tay phải nhả khi đang preview → clear preview
        else if (!rightPinch && currentPreview != null)
        {
            currentPreview = null;
            crossSectionRenderer?.Clear();
        }

        wasRightPinch = rightPinch;
        wasLeftPinch  = leftPinch;
    }

    /// <summary>
    /// Tìm valid section có "khoảng cách + góc" tổng hợp gần nhất với mặt phẳng tay phải tạo ra.
    /// Trả về null nếu không có cái nào trong ngưỡng snap.
    /// </summary>
    private CrossSectionDefinition FindBestSnap()
    {
        if (rightHandAnchor == null || validSections == null || validSections.Count == 0) return null;

        // Plane của tay phải: điểm = vị trí tay, normal = trục up local của tay
        Vector3 handPosLocal = transform.InverseTransformPoint(rightHandAnchor.position);
        Vector3 handNormalLocal = transform.InverseTransformDirection(rightHandAnchor.up).normalized;

        CrossSectionDefinition best = null;
        float bestScore = float.MaxValue;

        foreach (var sec in validSections)
        {
            // Distance từ điểm tay tới mp
            Vector3 toPlane = handPosLocal - sec.pointOnPlane;
            float dist = Mathf.Abs(Vector3.Dot(toPlane, sec.normal));
            if (dist > distanceSnapMeters) continue;

            // Angle giữa 2 pháp tuyến (lấy abs vì ngược chiều vẫn coi là cùng plane)
            float dot = Mathf.Abs(Vector3.Dot(handNormalLocal, sec.normal));
            float angle = Mathf.Acos(Mathf.Clamp(dot, 0f, 1f)) * Mathf.Rad2Deg;
            if (angle > angleSnapDegrees) continue;

            // Score = trọng số cộng dồn — chuẩn hoá
            float score = (dist / distanceSnapMeters) + (angle / angleSnapDegrees);
            if (score < bestScore)
            {
                bestScore = score;
                best = sec;
            }
        }
        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (validSections == null) RebuildLibrary();
        if (validSections == null) return;
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        foreach (var s in validSections)
        {
            Vector3 worldP = transform.TransformPoint(s.pointOnPlane);
            Vector3 worldN = transform.TransformDirection(s.normal);
            Gizmos.DrawLine(worldP, worldP + worldN * 0.2f);
        }
    }
#endif
}
