using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class CylinderInteractive : MonoBehaviour
{
    [Header("Renderer thân hình & Material")]
    public Renderer shapeRenderer;
    public Material solidMaterial;
    public Material transparentMaterial;

    [Header("Material cho overlay")]
    public Material wireframeMaterial;
    public Material vertexDotMaterial;

    [Header("Kích thước overlay")]
    public float ringThickness = 0.015f;
    public float dotSize = 0.06f;
    public float labelOffset = 0.08f;
    public float labelSize = 0.3f;
    public int ringSegments = 48;
    public int sideLines = 8;   // số đường sinh dọc (đường thẳng đứng quanh trụ)

    // Cylinder primitive trong Unity: cao 2, bán kính 0.5 (local)
    // Đáy dưới Y = -1, đáy trên Y = +1, bán kính = 0.5
    const float CYL_HEIGHT_HALF = 1f;
    const float CYL_RADIUS = 0.5f;

    GameObject overlay;
public int touching = 0;
    void Awake()
    {
        BuildOverlay();
        if (shapeRenderer != null && solidMaterial != null)
            shapeRenderer.material = solidMaterial;
        overlay.SetActive(false);
    }
    void Update()
{
    var grabbable = GetComponent<Grabbable>();
    if (grabbable != null && grabbable.isHeld)
    {
        // Đang cầm: luôn hiện overlay + giữ solid
        if (!overlay.activeSelf)
        {
            overlay.SetActive(true);
            if (shapeRenderer != null)
                shapeRenderer.material = solidMaterial;
        }
    }
    else if (touching == 0 && overlay.activeSelf)
    {
        // Không cầm, không chạm → ẩn overlay
        // (đề phòng OnTriggerExit miss event)
        overlay.SetActive(false);
        if (shapeRenderer != null)
            shapeRenderer.material = solidMaterial;
    }
}

    void BuildOverlay()
    {
        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(transform, false);

        Vector3 topCenter = new Vector3(0, CYL_HEIGHT_HALF, 0);
        Vector3 bottomCenter = new Vector3(0, -CYL_HEIGHT_HALF, 0);

        // 1) Tâm đáy trên O1, tâm đáy dưới O2
        CreateDot("O1_dot", topCenter, "O1", new Vector3(labelOffset, labelOffset, 0));
        CreateDot("O2_dot", bottomCenter, "O2", new Vector3(labelOffset, -labelOffset, 0));

        // 2) Đường cao h (nối O1 O2), nhãn "h" ở giữa
        CreateCylinderEdge("Height_O1O2", topCenter, bottomCenter, ringThickness, wireframeMaterial);
        CreateLabelAt("h_label", new Vector3(labelOffset * 1.5f, 0, 0), "h");

        // 3) Điểm M trên mép đáy dưới (để vẽ bán kính r)
        Vector3 mPos = new Vector3(CYL_RADIUS, -CYL_HEIGHT_HALF, 0);
        CreateDot("M_dot", mPos, "M", new Vector3(labelOffset, -labelOffset, 0));

        // 4) Bán kính r (đoạn O2 → M ở đáy dưới)
        CreateCylinderEdge("Radius_O2M", bottomCenter, mPos, ringThickness, wireframeMaterial);
        CreateLabelAt("r_label", (bottomCenter + mPos) * 0.5f + new Vector3(0, -labelOffset, 0), "r");

        // 5) 2 đường tròn đáy
        CreateCircle("Circle_Top", topCenter, CYL_RADIUS);
        CreateCircle("Circle_Bottom", bottomCenter, CYL_RADIUS);

        // 6) Các đường sinh dọc (nối mép 2 đáy)
        for (int i = 0; i < sideLines; i++)
        {
            float angle = (i / (float)sideLines) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * CYL_RADIUS;
            float z = Mathf.Sin(angle) * CYL_RADIUS;
            Vector3 top = new Vector3(x, CYL_HEIGHT_HALF, z);
            Vector3 bot = new Vector3(x, -CYL_HEIGHT_HALF, z);
            CreateCylinderEdge($"Side_{i}", top, bot, ringThickness * 0.7f, wireframeMaterial);
        }
    }

    // ===== Helpers =====

    void CreateDot(string name, Vector3 localPos, string labelText, Vector3 labelOffsetVec)
    {
        var group = new GameObject(name);
        group.transform.SetParent(overlay.transform, false);
        group.transform.localPosition = localPos;

        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(dot.GetComponent<Collider>());
        dot.name = "Dot";
        dot.transform.SetParent(group.transform, false);
        dot.transform.localScale = Vector3.one * dotSize;
        dot.GetComponent<Renderer>().sharedMaterial = vertexDotMaterial;

        var label = new GameObject("Label");
        label.transform.SetParent(group.transform, false);
        label.transform.localPosition = labelOffsetVec;
        label.transform.localScale = Vector3.one * labelSize;
        var tmp = label.AddComponent<TextMeshPro>();
        tmp.text = labelText;
        tmp.fontSize = 4;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    void CreateLabelAt(string name, Vector3 localPos, string text)
    {
        var label = new GameObject(name);
        label.transform.SetParent(overlay.transform, false);
        label.transform.localPosition = localPos;
        label.transform.localScale = Vector3.one * labelSize;
        var tmp = label.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 4;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    void CreateCylinderEdge(string name, Vector3 from, Vector3 to, float thickness, Material mat)
    {
        Vector3 mid = (from + to) * 0.5f;
        Vector3 dir = to - from;
        float len = dir.magnitude;

        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(cyl.GetComponent<Collider>());
        cyl.name = name;
        cyl.transform.SetParent(overlay.transform, false);
        cyl.transform.localPosition = mid;
        cyl.transform.localScale = new Vector3(thickness, len * 0.5f, thickness);
        cyl.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        cyl.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // Vẽ đường tròn nằm ngang ở độ cao center.y, bán kính cho trước
    void CreateCircle(string name, Vector3 center, float radius)
    {
        var circle = new GameObject(name);
        circle.transform.SetParent(overlay.transform, false);
        circle.transform.localPosition = center;

        Vector3 prev = new Vector3(radius, 0, 0);
        for (int i = 1; i <= ringSegments; i++)
        {
            float angle = (i / (float)ringSegments) * Mathf.PI * 2f;
            Vector3 curr = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(seg.GetComponent<Collider>());
            seg.name = "seg";
            seg.transform.SetParent(circle.transform, false);

            Vector3 mid = (prev + curr) * 0.5f;
            Vector3 dir = curr - prev;
            float len = dir.magnitude;
            seg.transform.localPosition = mid;
            seg.transform.localScale = new Vector3(ringThickness, len * 0.5f, ringThickness);
            seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            seg.GetComponent<Renderer>().sharedMaterial = wireframeMaterial;

            prev = curr;
        }
    }

    // ===== Trigger logic =====

void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("FingerTip")) return;

    touching++;
    if (touching == 1) Reveal(true);

    // Đăng ký hình này là hình đang được chạm
    if (ShapeSelector.Instance != null)
        ShapeSelector.Instance.SetSelected(transform);
}

void OnTriggerExit(Collider other)
{
    if (!other.CompareTag("FingerTip")) return;

    touching = Mathf.Max(0, touching - 1);
    if (touching == 0) Reveal(false);

    if (ShapeSelector.Instance != null)
        ShapeSelector.Instance.ClearSelected(transform);
}

    void Reveal(bool on)
{
    // Kiểm tra: hình có đang được cầm không?
    var grabbable = GetComponent<Grabbable>();
    bool isBeingGrabbed = grabbable != null && grabbable.isHeld;

    if (on)
    {
        // Đang chạm hoặc đang cầm
        if (isBeingGrabbed)
        {
            // Cầm: hình ĐẶC + overlay hiện
            if (shapeRenderer != null)
                shapeRenderer.material = solidMaterial;
        }
        else
        {
            // Chạm thường: hình TRONG SUỐT + overlay hiện
            if (shapeRenderer != null)
                shapeRenderer.material = transparentMaterial;
        }
        overlay.SetActive(true);
    }
    else
    {
        // Không chạm, không cầm
        if (shapeRenderer != null)
            shapeRenderer.material = solidMaterial;
        overlay.SetActive(false);
    }
}
}
