using UnityEngine;

/// <summary>
/// HIỆN BẢNG HƯỚNG DẪN CỬ CHỈ TAY trong scene cho học sinh đọc.
///
/// Tự tạo 1 bảng 3D float trước mặt người chơi khi scene bắt đầu.
/// Bảng và chữ đã được tinh chỉnh tỉ lệ cân đối — có thể đổi các hằng số ở đầu file
/// nếu muốn co/giãn theo sở thích.
///
/// CÁCH GẮN: GameObject "GameManager" → Add Component → Gesture Guide Display.
/// </summary>
[DisallowMultipleComponent]
public class GestureGuideDisplay : MonoBehaviour
{
    // ========== TỈ LỆ BẢNG (chỉnh ở đây nếu muốn co/giãn) ==========
    [Header("Vị trí bảng so với CenterEyeAnchor")]
    [Tooltip("Khoảng cách trước mặt người chơi (m). Bảng to thì cần đẩy ra xa.")]
    public float forwardDistance = 1.8f;

    [Tooltip("Lệch xuống dưới so với mắt (m).")]
    public float downOffset = 0.15f;

    [Header("Kích thước bảng (m) — đã phóng to")]
    public float panelWidth = 1.80f;
    public float panelHeight = 1.10f;

    [Header("Kích thước chữ (m) — đã thu nhỏ")]
    [Tooltip("Chiều cao chữ TIÊU ĐỀ — to gấp ~1.9 lần body.")]
    public float titleSize = 0.038f;
    [Tooltip("Chiều cao chữ NỘI DUNG.")]
    public float bodySize = 0.020f;
    [Tooltip("Khoảng cách giữa các dòng (m). Nên ≥ bodySize × 2.")]
    public float lineSpacing = 0.045f;

    [Header("Lề")]
    [Tooltip("Cách mép trái panel (m).")]
    public float leftPadding = 0.08f;
    [Tooltip("Cách mép trên panel (m) — vị trí tiêu đề.")]
    public float topPadding = 0.10f;
    [Tooltip("Khoảng cách từ tiêu đề xuống dòng đầu tiên của body.")]
    public float titleToBodyGap = 0.10f;

    [Header("Màu sắc")]
    public Color titleColor = new Color(1f, 0.85f, 0.1f);   // vàng
    public Color textColor = Color.white;
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.9f);

    [Header("Tự động ẩn (tùy chọn)")]
    [Tooltip("Tự ẩn bảng sau X giây để không che khối hình. Đặt 0 = không tự ẩn.")]
    public float autoHideAfterSeconds = 0f;

    private GameObject panel;

    void Start()
    {
        BuildGuidePanel();

        // Tự ẩn sau X giây (nếu autoHideAfterSeconds > 0)
        if (autoHideAfterSeconds > 0f)
            Invoke(nameof(HideGuide), autoHideAfterSeconds);
    }

    void BuildGuidePanel()
    {
        // Tìm điểm gắn — CenterEyeAnchor (Quest) hoặc Camera.main fallback.
        Transform anchor = null;
        GameObject cea = GameObject.Find("CenterEyeAnchor");
        if (cea != null) anchor = cea.transform;
        else if (Camera.main != null) anchor = Camera.main.transform;
        if (anchor == null) return;

        // === PANEL NỀN ===
        panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = "GestureGuidePanel";
        panel.transform.SetParent(anchor, false);
        panel.transform.localPosition = new Vector3(0, -downOffset, forwardDistance);
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(panelWidth, panelHeight, 1f);
        Destroy(panel.GetComponent<Collider>());

        // Material nền (URP Unlit, hỗ trợ alpha)
        Renderer rend = panel.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material bgMat = new Material(shader);
        bgMat.SetColor("_BaseColor", backgroundColor);

        // BẬT CHẾ ĐỘ TRANSPARENT cho URP Unlit (nếu không bật, alpha sẽ bị bỏ qua)
        bgMat.SetFloat("_Surface", 1);   // 1 = Transparent
        bgMat.SetFloat("_Blend", 0);     // 0 = Alpha Blend
        bgMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bgMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bgMat.SetInt("_ZWrite", 0);
        bgMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bgMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        rend.material = bgMat;

        // === TÍNH MỐC TỌA ĐỘ THEO TỈ LỆ PANEL ===
        // Panel ở (0,0,0) local; mép trên y = +panelHeight/2, mép trái x = -panelWidth/2.
        float halfH = panelHeight * 0.5f;
        float halfW = panelWidth * 0.5f;

        float titleY = halfH - topPadding;                  // Tiêu đề: cách mép trên topPadding
        float firstBodyY = titleY - titleToBodyGap;         // Dòng đầu body: cách tiêu đề titleToBodyGap
        float textX = -halfW + leftPadding;                 // Mọi chữ canh trái cùng cột này

        // === TIÊU ĐỀ (canh giữa) ===
        AddText(panel.transform, "HUONG DAN CU CHI TAY",
                new Vector3(0f, titleY, -0.001f),
                titleSize, titleColor, TextAnchor.UpperCenter, TextAlignment.Center);

        // === BODY — 9 dòng, tất cả canh trái cùng cột textX ===
        string[] lines =
        {
            "1. Pinch tro PHAI vao khoi    >> CHON",
            "2. Giu pinch + di tay         >> KEO khoi",
            "3. Pinch tro TRAI + xoay tay  >> XOAY khoi",
            "4. Pinch CA 2 TAY + dan/co    >> PHONG TO / THU NHO",
            "5. Cham nut Trong suot        >> trong suot ON/OFF",
            "6. Cham nut Canh khoi         >> hien duong canh",
            "7. Cham nut Dinh A B C        >> hien ten dinh",
            "8. Cham nut Ky hieu h R       >> hien ky hieu canh",
            "(Pinch vao khong trung = bo chon)"
        };

        float y = firstBodyY;
        foreach (string line in lines)
        {
            AddText(panel.transform, line,
                    new Vector3(textX, y, -0.001f),
                    bodySize, textColor, TextAnchor.UpperLeft, TextAlignment.Left);
            y -= lineSpacing;
        }

        // Cảnh báo runtime nếu nội dung tràn panel — giúp dev biết mà chỉnh hằng số.
        float bottomY = y + lineSpacing;     // y của dòng cuối
        if (bottomY < -halfH)
        {
            Debug.LogWarning($"[GestureGuideDisplay] Nội dung tràn panel " +
                             $"(bottomY={bottomY:F3} < -halfH={-halfH:F3}). " +
                             $"Tăng panelHeight hoặc giảm lineSpacing/bodySize.");
        }
    }

    void AddText(Transform parent, string text, Vector3 localPos,
                 float charSize, Color color, TextAnchor anchor, TextAlignment align)
    {
        GameObject obj = new GameObject("Line_" + text);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;

        TextMesh tm = obj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 90;            // Pixel mật độ glyph — cao = chữ sắc nét, không ảnh hưởng kích thước thực.
        tm.characterSize = charSize; // ĐÂY mới là kích thước thực (m).
        tm.color = color;
        tm.alignment = align;
        tm.anchor = anchor;
        tm.richText = false;
    }

    /// <summary>Gọi từ ngoài để ẩn bảng (ví dụ sau khi HS đã hiểu).</summary>
    public void HideGuide()
    {
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>Gọi từ ngoài để hiện lại bảng (nếu HS muốn xem lại).</summary>
    public void ShowGuide()
    {
        if (panel != null) panel.SetActive(true);
    }
}