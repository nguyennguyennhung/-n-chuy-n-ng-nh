using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trả về danh sách các CrossSectionDefinition hợp lệ cho mỗi loại hình.
/// GestureCuttingTool sẽ duyệt các định nghĩa này để snap mặt cắt người dùng đến gần nhất.
///
/// Đơn vị: tất cả tính theo local-space của hình, kích thước "đơn vị 1" (cube cạnh 1, cầu r=0.5...).
/// Khi áp dụng cho hình lớn hơn, cần nhân với scale.
/// </summary>
public static class ShapeCutLibrary
{
    /// <summary>Hình lập phương cạnh a (a = 2*halfSize)</summary>
    public static List<CrossSectionDefinition> Cube(float halfSize = 0.5f)
    {
        var list = new List<CrossSectionDefinition>();
        float a = halfSize;

        // 3 mặt phẳng vuông góc trục, đi qua tâm — mặt cắt là hình vuông cạnh a*2
        list.Add(SquareSection("Hình vuông (mp ngang)", Vector3.zero, Vector3.up,    a));
        list.Add(SquareSection("Hình vuông (mp trước)", Vector3.zero, Vector3.forward, a));
        list.Add(SquareSection("Hình vuông (mp bên)",   Vector3.zero, Vector3.right,   a));

        // Mặt phẳng chéo qua 4 đỉnh — hình chữ nhật cạnh a × a√2
        // Lấy mp đi qua cạnh AA' và CC' (xét cube A=(-a,-a,-a) ... C'=(a,a,a))
        // Pháp tuyến = (1, 0, -1) normalized
        list.Add(new CrossSectionDefinition
        {
            displayName = "Hình chữ nhật (mp chéo)",
            shape = CrossSectionDefinition.SectionShape.Rectangle,
            pointOnPlane = Vector3.zero,
            normal = new Vector3(1, 0, -1).normalized,
            polygonVertices = new []
            {
                new Vector3(-a, -a,  -a),
                new Vector3( a, -a,   a),
                new Vector3( a,  a,   a),
                new Vector3(-a,  a,  -a),
            },
            areaFormula = "S = a · a√2"
        });

        // Mặt phẳng qua 3 đỉnh A, B', D' → tam giác đều cạnh a√2
        list.Add(new CrossSectionDefinition
        {
            displayName = "Tam giác đều (3 đỉnh)",
            shape = CrossSectionDefinition.SectionShape.TriangleEq,
            pointOnPlane = new Vector3(-a, -a, -a),
            normal = new Vector3(1, 1, 1).normalized,
            polygonVertices = new []
            {
                new Vector3( a, -a, -a), // B
                new Vector3(-a,  a, -a), // A'
                new Vector3(-a, -a,  a), // D
            },
            areaFormula = "S = (a√2)² · √3/4"
        });

        return list;
    }

    /// <summary>Hình trụ — bán kính r, chiều cao h, trục Oy</summary>
    public static List<CrossSectionDefinition> Cylinder(float r = 0.5f, float h = 1f)
    {
        var list = new List<CrossSectionDefinition>();

        // Vuông góc trục, qua tâm → đường tròn bán kính r
        list.Add(CircleSection("Đường tròn (mp đáy)", Vector3.zero, Vector3.up, r));
        // Có thể thêm 1-2 mp vuông góc nữa ở y khác (y = +h/4, y = -h/4)
        list.Add(CircleSection("Đường tròn (1/4 trên)",  new Vector3(0,  h * 0.25f, 0), Vector3.up, r));
        list.Add(CircleSection("Đường tròn (1/4 dưới)",  new Vector3(0, -h * 0.25f, 0), Vector3.up, r));

        // Mp chứa trục → hình chữ nhật 2r × h
        list.Add(new CrossSectionDefinition
        {
            displayName = "Hình chữ nhật (chứa trục)",
            shape = CrossSectionDefinition.SectionShape.Rectangle,
            pointOnPlane = Vector3.zero,
            normal = Vector3.forward,
            polygonVertices = new []
            {
                new Vector3(-r, -h * 0.5f, 0),
                new Vector3( r, -h * 0.5f, 0),
                new Vector3( r,  h * 0.5f, 0),
                new Vector3(-r,  h * 0.5f, 0),
            },
            areaFormula = "S = 2r · h"
        });
        // 1 mp chứa trục, xoay 90°
        list.Add(new CrossSectionDefinition
        {
            displayName = "Hình chữ nhật (chứa trục - dọc)",
            shape = CrossSectionDefinition.SectionShape.Rectangle,
            pointOnPlane = Vector3.zero,
            normal = Vector3.right,
            polygonVertices = new []
            {
                new Vector3(0, -h * 0.5f, -r),
                new Vector3(0, -h * 0.5f,  r),
                new Vector3(0,  h * 0.5f,  r),
                new Vector3(0,  h * 0.5f, -r),
            },
            areaFormula = "S = 2r · h"
        });

        return list;
    }

    /// <summary>Hình nón — bán kính đáy r, chiều cao h, đỉnh S=(0,h,0)</summary>
    public static List<CrossSectionDefinition> Cone(float r = 0.5f, float h = 1f)
    {
        var list = new List<CrossSectionDefinition>();

        // Vuông góc trục — đường tròn bán kính tuyến tính
        // Tại y = 0           → r
        // Tại y = h/3         → 2r/3
        // Tại y = 2h/3        → r/3
        list.Add(CircleSection("Đường tròn (đáy)",       new Vector3(0, 0, 0),         Vector3.up, r));
        list.Add(CircleSection("Đường tròn (giữa nón)",  new Vector3(0, h * 0.5f, 0),  Vector3.up, r * 0.5f));
        list.Add(CircleSection("Đường tròn (sát đỉnh)",  new Vector3(0, h * 0.75f, 0), Vector3.up, r * 0.25f));

        // Mặt phẳng qua đỉnh và đường kính đáy → tam giác cân
        list.Add(new CrossSectionDefinition
        {
            displayName = "Tam giác cân (qua đỉnh)",
            shape = CrossSectionDefinition.SectionShape.TriangleIso,
            pointOnPlane = Vector3.zero,
            normal = Vector3.forward,
            polygonVertices = new []
            {
                new Vector3(-r, 0, 0),
                new Vector3( r, 0, 0),
                new Vector3( 0, h, 0),
            },
            areaFormula = "S = 1/2 · 2r · h"
        });

        return list;
    }

    /// <summary>Hình cầu tâm O bán kính r</summary>
    public static List<CrossSectionDefinition> Sphere(float r = 0.5f)
    {
        var list = new List<CrossSectionDefinition>();

        // 3 mặt phẳng cơ bản qua tâm → đường tròn lớn
        list.Add(CircleSection("Đường tròn lớn (xích đạo)", Vector3.zero, Vector3.up,      r));
        list.Add(CircleSection("Đường tròn lớn (kinh tuyến)", Vector3.zero, Vector3.forward, r));
        list.Add(CircleSection("Đường tròn lớn (kinh tuyến bên)", Vector3.zero, Vector3.right, r));

        // 2 đường tròn nhỏ song song xích đạo
        float d = r * 0.5f;                              // khoảng cách tâm tới mp
        float rSmall = Mathf.Sqrt(r * r - d * d);
        list.Add(CircleSection($"Đường tròn nhỏ (cách tâm {d:0.0})", new Vector3(0,  d, 0), Vector3.up, rSmall));
        list.Add(CircleSection($"Đường tròn nhỏ (cách tâm {d:0.0})", new Vector3(0, -d, 0), Vector3.up, rSmall));

        return list;
    }

    /// <summary>Chóp tứ giác đều S.ABCD, đáy cạnh a, cao h</summary>
    public static List<CrossSectionDefinition> Pyramid(float halfBase = 0.5f, float h = 1f)
    {
        var list = new List<CrossSectionDefinition>();
        float a = halfBase;

        // Mặt phẳng ngang qua giữa nón → hình vuông đồng dạng, cạnh giảm tuyến tính
        // Tại y=h/2: cạnh = a (size = a/2)
        list.Add(SquareSection("Hình vuông (mp ngang giữa)", new Vector3(0, h * 0.5f, 0), Vector3.up, a * 0.5f));
        list.Add(SquareSection("Hình vuông (mp ngang 1/4)",  new Vector3(0, h * 0.75f, 0), Vector3.up, a * 0.25f));

        // Mặt phẳng trung trực qua S, vuông góc AB → tam giác cân (S, trung điểm AB, trung điểm CD)
        list.Add(new CrossSectionDefinition
        {
            displayName = "Tam giác cân (mp trung trực)",
            shape = CrossSectionDefinition.SectionShape.TriangleIso,
            pointOnPlane = Vector3.zero,
            normal = Vector3.right,
            polygonVertices = new []
            {
                new Vector3(0, 0, -a),  // trung điểm AB
                new Vector3(0, 0,  a),  // trung điểm CD
                new Vector3(0, h,  0),  // S
            },
            areaFormula = "S = 1/2 · a · h"
        });

        // Mặt phẳng qua 2 cạnh đối xứng (vd: AB và đỉnh đối diện trên SD-SC) → hình thang cân
        // Đây là 1 ví dụ phổ biến trong sách giáo khoa
        list.Add(new CrossSectionDefinition
        {
            displayName = "Hình thang cân",
            shape = CrossSectionDefinition.SectionShape.Trapezoid,
            pointOnPlane = new Vector3(0, h * 0.4f, 0),
            normal = new Vector3(0, 1, 0.5f).normalized,
            polygonVertices = new []
            {
                new Vector3(-a, 0, -a),                 // A
                new Vector3( a, 0, -a),                 // B
                new Vector3( a * 0.4f, h * 0.6f, a * 0.4f),
                new Vector3(-a * 0.4f, h * 0.6f, a * 0.4f),
            },
            areaFormula = "S = 1/2·(đáy lớn + đáy bé)·h"
        });

        return list;
    }

    // ===================== Helpers =====================
    private static CrossSectionDefinition CircleSection(string name, Vector3 point, Vector3 normal, float r)
    {
        return new CrossSectionDefinition
        {
            displayName = name,
            shape = CrossSectionDefinition.SectionShape.Circle,
            pointOnPlane = point,
            normal = normal.normalized,
            size = r,
            areaFormula = "S = π · r²"
        };
    }

    private static CrossSectionDefinition SquareSection(string name, Vector3 point, Vector3 normal, float halfSize)
    {
        // Xây 4 đỉnh hình vuông trong mặt phẳng có normal
        Vector3 n = normal.normalized;
        Vector3 t = Vector3.Cross(n, Mathf.Abs(n.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 b = Vector3.Cross(n, t).normalized;
        return new CrossSectionDefinition
        {
            displayName = name,
            shape = CrossSectionDefinition.SectionShape.Square,
            pointOnPlane = point,
            normal = n,
            size = halfSize,
            polygonVertices = new []
            {
                point + (t + b) * halfSize,
                point + (t - b) * halfSize,
                point + (-t - b) * halfSize,
                point + (-t + b) * halfSize,
            },
            areaFormula = "S = a²"
        };
    }
}
