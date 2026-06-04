using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class CubeInteractive : MonoBehaviour
{
    [Header("Renderer thân hình & Material")]
    public Renderer shapeRenderer;
    public Material solidMaterial;
    public Material transparentMaterial;

    [Header("Material cho overlay")]
    public Material wireframeMaterial;
    public Material vertexDotMaterial;

    [Header("Kích thước overlay (theo local của Cube)")]
    public float edgeThickness = 0.02f;
    public float dotSize = 0.06f;
    public float labelOffset = 0.1f;
    public float labelSize = 0.15f;

    static readonly string[] vName = { "A", "B", "C", "D", "E", "F", "G", "H" };
    static readonly Vector3[] vPos = {
        new Vector3(-0.5f,-0.5f,-0.5f), new Vector3( 0.5f,-0.5f,-0.5f),
        new Vector3( 0.5f,-0.5f, 0.5f), new Vector3(-0.5f,-0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f,-0.5f), new Vector3( 0.5f, 0.5f,-0.5f),
        new Vector3( 0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
    };
    static readonly int[,] edges = {
        {0,1},{1,2},{2,3},{3,0}, {4,5},{5,6},{6,7},{7,4}, {0,4},{1,5},{2,6},{3,7}
    };

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

        // 8 đỉnh A-H + nhãn
        for (int i = 0; i < 8; i++)
        {
            var group = new GameObject(vName[i]);
            group.transform.SetParent(overlay.transform, false);
            group.transform.localPosition = vPos[i];

            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dot.GetComponent<Collider>());
            dot.name = "Dot";
            dot.transform.SetParent(group.transform, false);
            dot.transform.localScale = Vector3.one * dotSize;
            dot.GetComponent<Renderer>().sharedMaterial = vertexDotMaterial;

            var label = new GameObject("Label");
            label.transform.SetParent(group.transform, false);
            label.transform.localPosition = new Vector3(labelOffset, labelOffset, 0);
            label.transform.localScale = Vector3.one * labelSize;
            var tmp = label.AddComponent<TextMeshPro>();
            tmp.text = vName[i];
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        // 12 cạnh
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            int a = edges[i, 0], b = edges[i, 1];
            Vector3 pa = vPos[a], pb = vPos[b];
            Vector3 mid = (pa + pb) * 0.5f;
            Vector3 dir = pb - pa;
            float len = dir.magnitude;

            var edge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(edge.GetComponent<Collider>());
            edge.name = $"{vName[a]}{vName[b]}";
            edge.transform.SetParent(overlay.transform, false);
            edge.transform.localPosition = mid;
            edge.transform.localScale = new Vector3(edgeThickness, len * 0.5f, edgeThickness);
            edge.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            edge.GetComponent<Renderer>().sharedMaterial = wireframeMaterial;
        }
    }
void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("FingerTip")) return;
    touching++;
    Debug.Log($"[Cube] touching = {touching}");   // ← thêm dòng này
    if (touching == 1) Reveal(true);
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
    var grabbable = GetComponent<Grabbable>();
    bool isBeingGrabbed = grabbable != null && grabbable.isHeld;

    if (on)
    {
        if (shapeRenderer != null)
            shapeRenderer.material = isBeingGrabbed ? solidMaterial : transparentMaterial;
        overlay.SetActive(true);
    }
    else
    {
        if (shapeRenderer != null)
            shapeRenderer.material = solidMaterial;
        overlay.SetActive(false);
    }
}
}