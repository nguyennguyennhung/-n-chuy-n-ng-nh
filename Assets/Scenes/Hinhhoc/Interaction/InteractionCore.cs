using UnityEngine;

/// <summary>
/// TRUNG TÂM XỬ LÝ TƯƠNG TÁC (CORE)
/// Quản lý trạng thái: đang chọn hình nào, đang cầm hình nào.
/// Cung cấp các lệnh chuẩn cho cả Simulator và Quest.
/// </summary>
public class InteractionCore : MonoBehaviour
{
    [Header("Managers")]
    public WireframeRenderer wireframe;
    public VertexLabelManager vertexLabels;
    public EdgeLabelManager edgeLabels;

    private GeometryObject _selected;
    private bool _isGrabbing;

    public GeometryObject Selected => _selected;
    public bool IsGrabbing => _isGrabbing;

    void Awake()
    {
        // Tự tìm nếu chưa kéo thả
        if (!wireframe) wireframe = FindObjectOfType<WireframeRenderer>();
        if (!vertexLabels) vertexLabels = FindObjectOfType<VertexLabelManager>();
        if (!edgeLabels) edgeLabels = FindObjectOfType<EdgeLabelManager>();
    }

    // ═══════════════════════════════════
    // LỆNH CHỌN (Phím 2 / Pinch chạm)
    // ═══════════════════════════════════
    public void SelectObject(GeometryObject geo)
    {
        if (geo == null || _selected == geo) return;

        if (_selected != null) ClearAll();

        _selected = geo;
        _selected.Select();
        _selected.SetTransparency(true);

        if (wireframe) wireframe.ToggleWireframe(_selected.gameObject);
        if (vertexLabels) vertexLabels.ToggleLabels(_selected.gameObject);
        if (edgeLabels) edgeLabels.ToggleEdgeLabels(_selected.gameObject);

        Debug.Log($"[Core] Chọn: {geo.shapeName}");
    }

    // ═══════════════════════════════════
    // LỆNH HỦY (Phím 1 / Xòe tay)
    // ═══════════════════════════════════
    public void ClearAll()
    {
        if (_selected == null) return;

        if (wireframe) wireframe.ToggleWireframe(_selected.gameObject);
        if (vertexLabels) vertexLabels.ToggleLabels(_selected.gameObject);
        if (edgeLabels) edgeLabels.ToggleEdgeLabels(_selected.gameObject);

        
        _selected.SetTransparency(false);
        _selected.Deselect();
        _selected = null;
        _isGrabbing = false;
        Debug.Log("[Core] Đã hủy mọi lệnh");
    }

    // ═══════════════════════════════════
    // LỆNH XOAY (Phím Y / Grip tay phải)
    // ═══════════════════════════════════
    public void RotateSelected(float speed)
    {
        if (_selected == null) return;
        _selected.transform.Rotate(Vector3.up, -speed * Time.deltaTime, Space.World);
    }

    // ═══════════════════════════════════
    // LỆNH CẦM (Phím 3 / Pinch xuyên hình)
    // ═══════════════════════════════════
    public void SetGrabbing(bool state)
    {
        _isGrabbing = state;
    }

    public void MoveSelected(Vector3 targetPos, float smooth)
    {
        if (_selected == null || !_isGrabbing) return;
        Collider col = _selected.GetComponent<Collider>();
        float halfH = col != null ? col.bounds.extents.y : 0.3f;
        targetPos.y = Mathf.Max(targetPos.y, halfH);
        _selected.transform.position = Vector3.Lerp(_selected.transform.position, targetPos, Time.deltaTime * smooth);
    }

    // ═══════════════════════════════════
    // LỆNH SCALE (Phím 4 / Nắm tay)
    // ═══════════════════════════════════
    public void ScaleSelected(bool up)
    {
        if (_selected == null) return;
        if (up) _selected.ScaleUp();
        else _selected.ScaleDown();
    }

    // --- CÁC HÀM TOGGLE CHO MENU ---
    public void ToggleTransparency()
    {
        if (_selected == null) return;
        _selected.SetTransparency(!_selected.isTransparent);
    }

    public void ToggleWireframe()
    {
        if (_selected != null && wireframe != null)
            wireframe.ToggleWireframe(_selected.gameObject);
    }

    public void ToggleVertexLabels()
    {
        if (_selected != null && vertexLabels != null)
            vertexLabels.ToggleLabels(_selected.gameObject);
    }

    public void ToggleEdgeLabels()
    {
        if (_selected != null && edgeLabels != null)
            edgeLabels.ToggleEdgeLabels(_selected.gameObject);
    }
}
