using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class SphereInteractive : MonoBehaviour
{
    [Header("Renderer thân hình & Material")]
    public Renderer shapeRenderer;
    public Material solidMaterial;
    public Material transparentMaterial;

    [Header("Material cho overlay")]
    public Material wireframeMaterial;
    public Material vertexDotMaterial;

    [Header("Kích thước overlay (theo local của Sphere)")]
    public float ringThickness = 0.015f;     // độ dày 3 đường tròn lớn
    public float dotSize = 0.06f;            // kích thước chấm tâm O và điểm trên mặt cầu
    public float labelOffset = 0.08f;
    public float labelSize = 0.25f;
    public int ringSegments = 48;            // số đoạn để vẽ 1 đường tròn (càng cao càng mượt)

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

        // 1) Tâm O ở giữa
        CreateDot("O_dot", Vector3.zero, "O", new Vector3(labelOffset, labelOffset, 0));

        // 2) Điểm M trên mặt cầu (để vẽ bán kính r)
        // Sphere primitive có bán kính 0.5 trong local, đặt M ở (0.5, 0, 0)
        Vector3 mPos = new Vector3(0.5f, 0f, 0f);
        CreateDot("M_dot", mPos, "M", new Vector3(labelOffset, labelOffset, 0));

        // 3) Đoạn thẳng OM = bán kính r, có nhãn "r" ở giữa
        CreateCylinder("Radius_OM", Vector3.zero, mPos, ringThickness, wireframeMaterial);
        var rLabel = CreateLabel("r_label", (mPos) * 0.5f + new Vector3(0, labelOffset, 0), "r");
        rLabel.transform.localScale = Vector3.one * labelSize;

        // 4) 3 đường tròn lớn theo 3 mặt phẳng
        // Sphere primitive bán kính 0.5
        CreateCircle("Circle_XZ", 0.5f, Vector3.up);     // mặt phẳng nằm ngang
        CreateCircle("Circle_XY", 0.5f, Vector3.forward); // mặt phẳng đứng (chính diện)
        CreateCircle("Circle_YZ", 0.5f, Vector3.right);  // mặt phẳng đứng (cạnh)
    }

    // ===== Helper functions =====

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

        var label = CreateLabel("Label", labelOffsetVec, labelText);
        label.transform.SetParent(group.transform, false);
        label.transform.localPosition = labelOffsetVec;
        label.transform.localScale = Vector3.one * labelSize;
    }

    GameObject CreateLabel(string name, Vector3 localPos, string text)
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
        return label;
    }

    // Vẽ một đoạn thẳng bằng cylinder mỏng
    void CreateCylinder(string name, Vector3 from, Vector3 to, float thickness, Material mat)
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

    // Vẽ một đường tròn (3 đường tròn lớn của sphere)
    // axis = trục vuông góc với mặt phẳng chứa đường tròn
    void CreateCircle(string name, float radius, Vector3 axis)
    {
        var circle = new GameObject(name);
        circle.transform.SetParent(overlay.transform, false);

        // Tạo từng đoạn thẳng nhỏ nối liền thành vòng tròn
        // Mặt phẳng vuông góc với axis → tìm 2 vector vuông góc với axis
        Vector3 u, v;
        if (Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.99f)
        {
            u = Vector3.Cross(axis, Vector3.up).normalized;
        }
        else
        {
            u = Vector3.Cross(axis, Vector3.right).normalized;
        }
        v = Vector3.Cross(axis, u).normalized;

        Vector3 prev = u * radius;
        for (int i = 1; i <= ringSegments; i++)
        {
            float angle = (i / (float)ringSegments) * Mathf.PI * 2f;
            Vector3 curr = (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * radius;

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
