using UnityEngine;

/// <summary>
/// QUẢN LÝ TƯƠNG TÁC VỚI CÁC KHỐI HÌNH HỌC BẰNG CHUỘT.
/// 
/// Chức năng:
/// - Click trái chuột vào khối → CHỌN khối (highlight sáng lên)
/// - Giữ chuột trái + kéo → DI CHUYỂN khối theo chuột
/// - Nhấn R khi đang chọn → XOAY khối
/// - Nhấn Q khi đang chọn → THU NHỎ khối
/// - Nhấn E khi đang chọn → PHÓNG TO khối
/// - Nhấn T khi đang chọn → BẬT/TẮT TRONG SUỐT
/// - Click vào vùng trống → BỎ CHỌN
/// 
/// CÁCH GẮN:
/// 1. GameObject → Create Empty → đặt tên "GameManager"
/// 2. Kéo script này thả vào Inspector của GameManager
/// </summary>
public class ObjectInteraction : MonoBehaviour
{
    [Header("Cài đặt tương tác")]
    [Tooltip("Tốc độ xoay khối khi nhấn R (độ/giây)")]
    public float rotateSpeed = 90f;

    [Tooltip("Tốc độ thay đổi kích thước khi nhấn Q/E")]
    public float scaleSpeed = 0.5f;

    [Tooltip("Kích thước tối thiểu (không cho nhỏ hơn)")]
    public float minScale = 0.2f;

    [Tooltip("Kích thước tối đa (không cho to hơn)")]
    public float maxScale = 5f;

    // ===== BIẾN NỘI BỘ =====
    // Khối đang được chọn (null = chưa chọn gì)
    private GeometryObject selectedObject = null;

    // Biến phục vụ kéo thả (drag)
    private bool isDragging = false;
    private float dragDistance;        // Khoảng cách từ camera đến khối khi bắt đầu kéo
    private Vector3 dragOffset;        // Độ lệch giữa điểm click và tâm khối

    void Update()
    {
        HandleSelection();    // Xử lý chọn/bỏ chọn
        HandleDragging();     // Xử lý kéo thả
        HandleRotation();     // Xử lý xoay
        HandleScaling();      // Xử lý phóng to/thu nhỏ
        HandleTransparency(); // Xử lý trong suốt
    }

    // ====================================================
    // 1. CHỌN / BỎ CHỌN KHỐI
    // ====================================================
    void HandleSelection()
    {
        // Chỉ xử lý khi NHẤN chuột trái (không phải giữ)
        if (!Input.GetMouseButtonDown(0)) return;

        // Đang giữ chuột phải để xoay camera → không xử lý click
        if (Input.GetMouseButton(1)) return;

        // === BẮN TIA (Raycast) TỪ CAMERA QUA CHUỘT ===
        // Tưởng tượng: bạn dùng đèn pin chiếu từ camera qua vị trí chuột.
        // Tia gặp vật thể nào đầu tiên → đó là vật bạn click vào.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            // Tia đã chạm vào một vật thể!
            // Kiểm tra xem vật thể đó có phải là khối hình học không
            GeometryObject geo = hit.collider.GetComponent<GeometryObject>();

            if (geo != null)
            {
                // === CHỌN KHỐI MỚI ===
                // Nếu đang chọn khối khác → bỏ chọn khối cũ
                if (selectedObject != null && selectedObject != geo)
                {
                    selectedObject.Deselect();
                }

                // Chọn khối mới
                selectedObject = geo;
                selectedObject.Select();

                // Chuẩn bị cho kéo thả
                isDragging = true;
                dragDistance = hit.distance;
                dragOffset = selectedObject.transform.position - hit.point;

                return;
            }
        }

        // === CLICK VÀO VÙNG TRỐNG → BỎ CHỌN ===
        if (selectedObject != null)
        {
            selectedObject.Deselect();
            selectedObject = null;
        }
    }

    // ====================================================
    // 2. KÉO THẢ (DRAG) KHỐI
    // ====================================================
    void HandleDragging()
    {
        if (Input.GetMouseButtonUp(0)) isDragging = false;
        if (!isDragging || selectedObject == null || Input.GetMouseButton(1)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPos = ray.GetPoint(dragDistance) + dragOffset;
        
        Rigidbody rb = selectedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // --- KỸ THUẬT QUÉT VA CHẠM (SWEEP TEST) ĐỂ CHỐNG XUYÊN BÀN ---
            Vector3 direction = targetPos - rb.position;
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                RaycastHit hit;
                // Quét xem trên đường đi có vật cản (cái bàn) không
                if (rb.SweepTest(direction.normalized, out hit, distance))
                {
                    // Nếu chạm bàn, chỉ cho phép đi đến sát mặt bàn
                    rb.MovePosition(rb.position + direction.normalized * (hit.distance - 0.01f));
                }
                else
                {
                    // Nếu đường trống, đi thẳng tới vị trí chuột
                    rb.MovePosition(targetPos);
                }
            }
        }
        else
        {
            selectedObject.transform.position = targetPos;
        }
    }

    // ====================================================
    // 3. XOAY KHỐI (nhấn R)
    // ====================================================
    void HandleRotation()
    {
        if (selectedObject == null) return;

        // Nhấn giữ R → xoay khối quanh trục Y
        if (Input.GetKey(KeyCode.R))
        {
            selectedObject.transform.Rotate(
                Vector3.up,
                rotateSpeed * Time.deltaTime,
                Space.World
            );
        }
    }

    // ====================================================
    // 4. PHÓNG TO / THU NHỎ
    // ====================================================
    void HandleScaling()
    {
        if (selectedObject == null) return;

        // Reset kích thước (Phím S)
        if (Input.GetKeyDown(KeyCode.S))
        {
            selectedObject.transform.localScale = Vector3.one;
            return;
        }

        float scaleChange = 1f;

        // 1. Con lăn chuột (Rất nhanh)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) scaleChange = 1.1f;
        else if (scroll < 0) scaleChange = 0.9f;

        // 2. Phím I (Phóng to) / K (Thu nhỏ)
        if (Input.GetKey(KeyCode.I)) scaleChange = 1.05f;
        if (Input.GetKey(KeyCode.K)) scaleChange = 0.95f;

        if (scaleChange != 1f)
        {
            Vector3 newScale = selectedObject.transform.localScale * scaleChange;
            
            // Ép giới hạn tối thiểu 0.1 và tối đa 10.0 (Thoải mái hơn)
            float finalMin = 0.1f;
            float finalMax = 10f;

            if (scaleChange > 1f && newScale.x > finalMax) return;
            if (scaleChange < 1f && newScale.x < finalMin) return;

            selectedObject.transform.localScale = newScale;
        }
    }

    // ====================================================
    // 5. BẬT/TẮT TRONG SUỐT (nhấn T)
    // ====================================================
    void HandleTransparency()
    {
        if (selectedObject == null) return;

        // Nhấn T (chỉ 1 lần, không phải giữ)
        if (Input.GetKeyDown(KeyCode.T))
        {
            selectedObject.ToggleTransparency();
        }
    }

    // ====================================================
    // TIỆN ÍCH: Lấy khối đang chọn (cho script khác dùng)
    // ====================================================
    public GeometryObject GetSelectedObject()
    {
        return selectedObject;
    }
}
