using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ChopInteractive : MonoBehaviour
{
    [Header("Renderer thân hình & Material")]
    public Renderer shapeRenderer;
    public Material solidMaterial;
    public Material transparentMaterial;

    [Header("Material cho overlay")]
    public Material wireframeMaterial;
    public Material vertexDotMaterial;

    [Header("Tham chiếu mesh generator (đọc kích thước)")]
    public PyramidMeshGenerator meshGenerator;

    [Header("Kích thước overlay")]
    public float edgeThickness = 0.02f;
    public float dotSize = 0.06f;
    public float labelOffset = 0.08f;
    public float labelSize = 0.3f;

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
        // Đọc kích thước từ generator
        float baseSize = meshGenerator != null ? meshGenerator.baseSize : 1f;
        float height = meshGenerator != null ? meshGenerator.height : 1.5f;
        float h = baseSize * 0.5f;

        // Toạ độ trùng với mesh: đáy ở Y=0, đỉnh ở Y=height
        Vector3 S = new Vector3(0, height, 0);
        Vector3 A = new Vector3(-h, 0,  h);
        Vector3 B = new Vector3( h, 0,  h);
        Vector3 C = new Vector3( h, 0, -h);
        Vector3 D = new Vector3(-h, 0, -h);
        Vector3 O = new Vector3(0, 0, 0); // tâm đáy

        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(transform, false);

        // 6 chấm: S, A, B, C, D, O
        CreateDot("S_dot", S, "S", new Vector3(0, labelOffset, 0));
        CreateDot("A_dot", A, "A", new Vector3(-labelOffset, -labelOffset,  labelOffset));
        CreateDot("B_dot", B, "B", new Vector3( labelOffset, -labelOffset,  labelOffset));
        CreateDot("C_dot", C, "C", new Vector3( labelOffset, -labelOffset, -labelOffset));
        CreateDot("D_dot", D, "D", new Vector3(-labelOffset, -labelOffset, -labelOffset));
        CreateDot("O_dot", O, "O", new Vector3(labelOffset, -labelOffset * 0.3f, 0));

        // 4 cạnh đáy
        CreateEdge("AB", A, B);
        CreateEdge("BC", B, C);
        CreateEdge("CD", C, D);
        CreateEdge("DA", D, A);

        // 4 cạnh bên
        CreateEdge("SA", S, A);
        CreateEdge("SB", S, B);
        CreateEdge("SC", S, C);
        CreateEdge("SD", S, D);

        // Đường cao SO (mảnh hơn)
        var heightEdge = CreateEdge("Height_SO", S, O);
        heightEdge.transform.localScale = new Vector3(
            edgeThickness * 0.6f,
            heightEdge.transform.localScale.y,
            edgeThickness * 0.6f
        );

        CreateLabelAt("h_label", (S + O) * 0.5f + new Vector3(labelOffset, 0, 0), "h");
        CreateLabelAt("l_label", (S + A) * 0.5f + new Vector3(-labelOffset, 0,  labelOffset), "l");
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

    GameObject CreateEdge(string name, Vector3 from, Vector3 to)
    {
        Vector3 mid = (from + to) * 0.5f;
        Vector3 dir = to - from;
        float len = dir.magnitude;

        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(cyl.GetComponent<Collider>());
        cyl.name = name;
        cyl.transform.SetParent(overlay.transform, false);
        cyl.transform.localPosition = mid;
        cyl.transform.localScale = new Vector3(edgeThickness, len * 0.5f, edgeThickness);
        cyl.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        cyl.GetComponent<Renderer>().sharedMaterial = wireframeMaterial;
        return cyl;
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