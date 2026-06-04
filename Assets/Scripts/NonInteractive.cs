using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class NonInteractive : MonoBehaviour
{
    [Header("Renderer thân hình & Material")]
    public Renderer shapeRenderer;
    public Material solidMaterial;
    public Material transparentMaterial;

    [Header("Material cho overlay")]
    public Material wireframeMaterial;
    public Material vertexDotMaterial;

    [Header("Tham chiếu mesh generator (đọc kích thước)")]
    public ConeMeshGenerator meshGenerator;

    [Header("Kích thước overlay")]
    public float ringThickness = 0.02f;
    public float dotSize = 0.06f;
    public float labelOffset = 0.08f;
    public float labelSize = 0.3f;
    public int ringSegments = 48;
    public int slantLines = 8;

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
        float radius = meshGenerator != null ? meshGenerator.radius : 0.5f;
        float height = meshGenerator != null ? meshGenerator.height : 1.5f;

        // Toạ độ trùng mesh: đáy Y=0, đỉnh Y=height
        Vector3 S = new Vector3(0, height, 0);
        Vector3 O = new Vector3(0, 0, 0);
        Vector3 M = new Vector3(radius, 0, 0);

        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(transform, false);

        CreateDot("S_dot", S, "S", new Vector3(0, labelOffset, 0));
        CreateDot("O_dot", O, "O", new Vector3(labelOffset, -labelOffset * 0.3f, 0));
        CreateDot("M_dot", M, "M", new Vector3(labelOffset, -labelOffset, 0));

        // Đường cao SO (mảnh)
        var hEdge = CreateEdge("Height_SO", S, O, ringThickness * 0.6f);
        CreateLabelAt("h_label", (S + O) * 0.5f + new Vector3(labelOffset, 0, 0), "h");

        // Bán kính OM
        CreateEdge("Radius_OM", O, M, ringThickness);
        CreateLabelAt("r_label", (O + M) * 0.5f + new Vector3(0, -labelOffset, 0), "r");

        // Đường sinh SM (đại diện)
        CreateEdge("Slant_SM", S, M, ringThickness);
        CreateLabelAt("l_label", (S + M) * 0.5f + new Vector3(labelOffset, 0, 0), "l");

        // Đường tròn đáy ở Y=0
        CreateCircle("Circle_Base", O, radius);

        // Các đường sinh quanh nón (mảnh hơn, bỏ qua SM vì đã có)
        for (int i = 1; i < slantLines; i++)
        {
            float angle = (i / (float)slantLines) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 basePoint = new Vector3(x, 0, z);
            CreateEdge($"Slant_{i}", S, basePoint, ringThickness * 0.7f);
        }
    }

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

    GameObject CreateEdge(string name, Vector3 from, Vector3 to, float thickness)
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
        cyl.GetComponent<Renderer>().sharedMaterial = wireframeMaterial;
        return cyl;
    }

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