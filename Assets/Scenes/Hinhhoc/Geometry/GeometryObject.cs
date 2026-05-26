using UnityEngine;

/// <summary>
/// GẮN SCRIPT NÀY VÀO MỌI KHỐI HÌNH HỌC (Cube, Sphere, Cylinder, Pyramid, Cone).
/// Script này đánh dấu vật thể là "khối hình học" để các script khác nhận diện được.
/// Nó cũng quản lý: highlight khi chọn, trong suốt, hiện cạnh.
/// </summary>
public enum GeometryShapeType
{
    Cube,
    Sphere,
    Cylinder,
    Cone,
    Pyramid
}

public class GeometryObject : MonoBehaviour
{
    // ===== THÔNG TIN KHỐI HÌNH =====
    [Header("Thông tin khối hình")]
    [Tooltip("Tên hiển thị của khối, ví dụ: Hình lập phương")]
    public string shapeName = "Khối hình";

    [Tooltip("Loại hình học")]
    public GeometryShapeType shapeType;

    [Tooltip("Bán kính hoặc nửa cạnh")]
    public float radius = 0.5f;

    [Tooltip("Chiều cao")]
    public float height = 1f;

    // ===== TRẠNG THÁI =====
    [Header("Trạng thái (tự động, không cần chỉnh)")]
    public bool isSelected = false;
    public bool isTransparent = false;

    // ===== SCALE =====
    [Header("Scale")]
    public float scaleUpStep = 0.3f;
    public float scaleDownStep = 0.2f;

    // ===== BIẾN NỘI BỘ =====
    private Color _originalColor;
    private Material _matInstance;   // Material instance riêng — tránh sửa sharedMaterial
    private Renderer _rend;
    private Rigidbody _rb;

    // =============================================
    // AWAKE — Setup Rigidbody + Collider
    // =============================================
    void Awake()
    {
        _rend = GetComponent<Renderer>();

        // Tự động thêm Rigidbody nếu chưa có
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();

        // Thiết lập Kinematic — bắt buộc cho MeshCollider convex
        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Đảm bảo MeshCollider convex
        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null) mc.convex = true;
    }

    // =============================================
    // START — Lưu màu gốc, tạo material instance
    // =============================================
    void Start()
    {
        if (_rend == null) _rend = GetComponent<Renderer>();
        if (_rend != null && _rend.material != null)
        {
            // Tạo material instance riêng — QUAN TRỌNG: không sửa sharedMaterial
            _matInstance = _rend.material; // Unity tự clone khi truy cập .material
            if (_matInstance.HasProperty("_BaseColor"))
            {
                _originalColor = _matInstance.GetColor("_BaseColor");
            }
            else
            {
                _originalColor = _matInstance.color;
            }
        }
    }

    // =============================================
    // CHỌN — Bật highlight (emission)
    // =============================================
    public void Select()
    {
        isSelected = true;
        if (_rend == null) return;

        Material mat = _rend.material;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", _originalColor * 0.4f);
    }

    // =============================================
    // BỎ CHỌN — Tắt highlight, trả về trạng thái gốc
    // =============================================
    public void Deselect()
    {
        isSelected = false;
        if (_rend == null) return;

        Material mat = _rend.material;
        mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);
    }

    // =============================================
    // TRANSPARENCY — Chuyển đổi Opaque ↔ Transparent (URP Lit)
    //
    // ĐÂY LÀ HÀM QUAN TRỌNG NHẤT — FIX MỌI LỖI "TÀNG HÌNH"
    //
    // Nguyên nhân gốc gây invisible:
    //   1. Không set _BaseColor (chỉ set mat.color → URP Lit dùng _BaseColor)
    //   2. Khi opaque không DisableKeyword → shader vẫn ở Transparent
    //   3. Không set _AlphaClip đúng
    //   4. _Blend không set → mặc định sai
    //   5. RenderQueue sai khiến vật bị render sau background
    //   6. Dùng mat.color thay vì mat.SetColor("_BaseColor")
    //   7. Không set RenderType override tag
    // =============================================
    // =============================================
    // SIMPLE SELECT COLOR
    // KHÔNG transparency
    // CHỈ đổi màu khi chọn
    // ỔN ĐỊNH TRÊN QUEST
    // =============================================
    public void SetTransparency(bool selected)
    {
        if (_rend == null) return;

        Material mat = _rend.material;

        // GIỮ nguyên shader hiện tại
        // KHÔNG đổi surface
        // KHÔNG đổi blend
        // KHÔNG đổi render queue

        if (selected)
        {
            // Màu khi được chọn
            Color selectedColor = Color.cyan;

            // Nếu shader có _BaseColor
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", selectedColor);
            }

            // fallback cho shader thường
            if (mat.HasProperty("_Color"))
            {
                mat.color = selectedColor;
            }

            // emission sáng nhẹ
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", selectedColor * 0.5f);
        }
        else
        {
            // trả lại màu gốc
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", _originalColor);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.color = _originalColor;
            }

            // tắt emission
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }
    }
    public void ScaleUp()
    {
        Vector3 s = transform.localScale;
        s += Vector3.one * scaleUpStep;
        // Giới hạn max
        s = Vector3.Min(s, Vector3.one * 3f);
        transform.localScale = s;
    }

    public void ScaleDown()
    {
        Vector3 s = transform.localScale;
        s -= Vector3.one * scaleDownStep;
        // Giới hạn min
        s = Vector3.Max(s, Vector3.one * 0.2f);
        transform.localScale = s;
    }

    // =============================================
    // TIỆN ÍCH
    // =============================================
    public Color GetOriginalColor()
    {
        return _originalColor;
    }
}