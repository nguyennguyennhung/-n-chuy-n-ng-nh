using UnityEngine;

/// <summary>
/// GẮN SCRIPT NÀY VÀO MỌI KHỐI HÌNH HỌC (Cube, Sphere, Cylinder, Pyramid, Cone).
/// Script này đánh dấu vật thể là "khối hình học" để các script khác nhận diện được.
/// Nó cũng quản lý: highlight khi chọn, trong suốt, hiện cạnh.
/// </summary>
public class GeometryObject : MonoBehaviour
{
    // ===== THÔNG TIN KHỐI HÌNH =====
    [Header("Thông tin khối hình")]
    [Tooltip("Tên hiển thị của khối, ví dụ: Hình lập phương")]
    public string shapeName = "Khối hình";

    // ===== TRẠNG THÁI =====
    [Header("Trạng thái (không cần chỉnh, tự động)")]
    public bool isSelected = false;      // Đang được chọn hay không?
    public bool isTransparent = false;   // Đang trong suốt hay không?

    [Header("Màu hiệu ứng")]
    [Tooltip("Màu khối khi đang được chọn/nắm (highlight). Mặc định = vàng.")]
    public Color grabColor = new Color(1f, 0.9f, 0.1f); // Vàng sáng

    // ===== BIẾN NỘI BỘ =====
    // Lưu lại màu gốc để khi bỏ chọn thì trả về màu cũ
    private Color originalColor;
    // Lưu lại chế độ render gốc
    private float originalRenderMode;

    /// <summary>
    /// Start() chạy 1 lần khi game bắt đầu.
    /// Ở đây ta lưu lại màu gốc của khối.
    /// </summary>
    private Rigidbody rb;

    void Awake()
    {
        // 1. Tự động thêm Rigidbody nếu chưa có
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // 2. Thiết lập vật lý Kinematic (Quan trọng để MeshCollider hoạt động ổn định khi kéo thả)
        rb.useGravity = false;
        rb.isKinematic = true; 
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = RigidbodyConstraints.FreezeRotation; 

        // 3. Đảm bảo có Collider
        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null) mc.convex = true;
    }

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && rend.material != null)
        {
            // URP dùng _BaseColor thay vì .color
            originalColor = rend.material.GetColor("_BaseColor");
        }
    }

    // ===== CHỌN KHỐI (HIGHLIGHT) =====
    /// <summary>
    /// Gọi hàm này khi người dùng click chọn khối.
    /// Khối sẽ phát sáng (emission) để nổi bật.
    /// </summary>
    public void Select()
    {
        isSelected = true;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Đổi màu sang grabColor (URP: dùng _BaseColor)
            Color c = grabColor;
            if (isTransparent) c.a = 0.3f;  // Giữ độ trong suốt nếu đang transparent
            rend.material.SetColor("_BaseColor", c);

            // Bật phát sáng (emission) để khối sáng rực
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", grabColor * 0.35f);
        }
    }

    // ===== BỎ CHỌN KHỐI =====
    /// <summary>
    /// Gọi khi người dùng click chỗ khác (bỏ chọn).
    /// Khối trở lại bình thường, không sáng nữa.
    /// </summary>
    public void Deselect()
    {
        isSelected = false;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Trả lại màu gốc (URP: dùng _BaseColor)
            Color c = originalColor;
            if (isTransparent) c.a = 0.3f;  // Giữ độ trong suốt nếu đang transparent
            rend.material.SetColor("_BaseColor", c);

            // Tắt phát sáng
            rend.material.DisableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", Color.black);
        }
    }

    // ===== BẬT/TẮT TRONG SUỐT =====
    /// <summary>
    /// Chuyển đổi giữa đặc và trong suốt.
    /// Dành cho URP (Universal Render Pipeline).
    /// </summary>
    public void ToggleTransparency()
    {
        isTransparent = !isTransparent;
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Material mat = rend.material;

        if (isTransparent)
        {
            // === CHUYỂN SANG TRONG SUỐT (URP) ===
            mat.SetFloat("_Surface", 1); // 1 = Transparent
            mat.SetFloat("_Blend", 0);   // 0 = Alpha Blend
            
            // Cài đặt các thông số kỹ thuật cho Alpha Blending
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0); // Tắt ghi vào Z-Buffer để nhìn xuyên qua
            
            // Bật các tính năng trong suốt của URP
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Đặt độ mờ (Alpha) = 0.3 (30% hiện hình)
            // URP dùng _BaseColor thay vì _Color
            Color c = mat.GetColor("_BaseColor");
            c.a = 0.3f; 
            mat.SetColor("_BaseColor", c);
        }
        else
        {
            // === TRỞ LẠI ĐẶC (OPAQUE) ===
            mat.SetFloat("_Surface", 0); // 0 = Opaque
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

            // Trả độ mờ về 1 (đặc hoàn toàn)
            Color c = mat.GetColor("_BaseColor");
            c.a = 1f;
            mat.SetColor("_BaseColor", c);
        }
    }

    /// <summary>
    /// Trả về màu gốc ban đầu.
    /// </summary>
    public Color GetOriginalColor()
    {
        return originalColor;
    }
}
