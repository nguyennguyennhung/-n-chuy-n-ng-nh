using UnityEngine;

/// <summary>
/// Mô tả 1 "mặt cắt hợp lệ" cho 1 hình.
/// Để gesture cắt có thể snap vào.
/// </summary>
[System.Serializable]
public class CrossSectionDefinition
{
    public enum SectionShape
    {
        Circle,        // Đường tròn
        Square,        // Hình vuông
        Rectangle,     // Hình chữ nhật
        TriangleEq,    // Tam giác đều
        TriangleIso,   // Tam giác cân
        Trapezoid,     // Hình thang cân
        Polygon        // Đa giác đều khác
    }

    [Tooltip("Tên hiển thị cho học sinh, vd: 'Hình tròn lớn'")]
    public string displayName = "Mặt cắt";

    [Tooltip("Loại mặt cắt — quyết định cách vẽ outline")]
    public SectionShape shape = SectionShape.Circle;

    [Tooltip("Điểm 1 trên mặt phẳng — local space của hình")]
    public Vector3 pointOnPlane = Vector3.zero;

    [Tooltip("Pháp tuyến của mặt phẳng — local space của hình")]
    public Vector3 normal = Vector3.up;

    [Tooltip("Bán kính (cho Circle) hoặc nửa-cạnh đặc trưng")]
    public float size = 0.5f;

    [Tooltip("Đỉnh của đa giác (cho Square/Rectangle/Triangle/...). Tính trong mặt phẳng cắt, local space của hình. Bỏ trống nếu là Circle.")]
    public Vector3[] polygonVertices;

    [Tooltip("Công thức diện tích để hiển thị")]
    public string areaFormula = "";
}
