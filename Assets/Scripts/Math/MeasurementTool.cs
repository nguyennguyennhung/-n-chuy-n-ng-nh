using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CÔNG CỤ ĐO LƯỜNG HÌNH HỌC.
/// 
/// Chức năng:
/// - Nhấn M → bật/tắt chế độ đo lường
/// - Khi đang ở chế độ đo: click 2 điểm bất kỳ → hiện KHOẢNG CÁCH
/// - Click điểm thứ 3 → hiện GÓC giữa 3 điểm
/// - Nhấn C → xóa tất cả điểm đo, bắt đầu lại
/// 
/// GẮN VÀO: GameManager (cùng chỗ với ObjectInteraction).
/// </summary>
public class MeasurementTool : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Màu điểm đánh dấu")]
    public Color pointColor = Color.yellow;

    [Tooltip("Màu đường nối")]
    public Color lineColor = Color.cyan;

    [Tooltip("Kích thước điểm đánh dấu")]
    public float pointSize = 0.08f;

    // Trạng thái
    private bool isMeasuring = false;
    private List<Vector3> measurePoints = new List<Vector3>();
    private List<GameObject> markerObjects = new List<GameObject>();
    private List<GameObject> lineObjects = new List<GameObject>();
    private List<GameObject> textObjects = new List<GameObject>();

    void Update()
    {
        // === NHẤN M: BẬT/TẮT CHẾ ĐỘ ĐO ===
        if (Input.GetKeyDown(KeyCode.M))
        {
            isMeasuring = !isMeasuring;

            if (isMeasuring)
            {
                Debug.Log(">>> CHẾ ĐỘ ĐO: BẬT - Click vào các điểm để đo <<<");
            }
            else
            {
                Debug.Log(">>> CHẾ ĐỘ ĐO: TẮT <<<");
                ClearAll();
            }
        }

        // === NHẤN C: XÓA TẤT CẢ ĐIỂM ĐO ===
        if (Input.GetKeyDown(KeyCode.C) && isMeasuring)
        {
            ClearAll();
            Debug.Log(">>> Đã xóa tất cả điểm đo <<<");
        }

        // === CLICK CHUỘT TRÁI KHI ĐANG ĐO ===
        if (isMeasuring && Input.GetMouseButtonDown(0))
        {
            // Không xử lý nếu đang giữ chuột phải (xoay camera)
            if (Input.GetMouseButton(1)) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                // Thêm điểm đo tại vị trí click
                AddMeasurePoint(hit.point);
            }
        }
    }

    /// <summary>
    /// Thêm một điểm đo và tính toán kết quả.
    /// </summary>
    void AddMeasurePoint(Vector3 point)
    {
        measurePoints.Add(point);

        // Tạo hình cầu nhỏ đánh dấu điểm
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = point;
        marker.transform.localScale = Vector3.one * pointSize;
        marker.GetComponent<Renderer>().material.color = pointColor;

        // Tắt collider để không ảnh hưởng raycast
        Destroy(marker.GetComponent<Collider>());
        markerObjects.Add(marker);

        int count = measurePoints.Count;

        // === 2 ĐIỂM → ĐO KHOẢNG CÁCH ===
        if (count >= 2)
        {
            Vector3 p1 = measurePoints[count - 2];
            Vector3 p2 = measurePoints[count - 1];

            // Vẽ đường nối 2 điểm
            DrawLine(p1, p2);

            // Tính khoảng cách
            float distance = Vector3.Distance(p1, p2);

            // Hiện text khoảng cách ở giữa đường nối
            Vector3 midPoint = (p1 + p2) / 2f + Vector3.up * 0.15f;
            ShowText(midPoint, $"{distance:F2} m");

            Debug.Log($">>> Khoảng cách: {distance:F2} mét <<<");
        }

        // === 3 ĐIỂM → ĐO GÓC ===
        if (count >= 3 && (count - 1) % 2 == 0)
        {
            Vector3 pA = measurePoints[count - 3]; // Điểm đầu
            Vector3 pB = measurePoints[count - 2]; // Điểm góc (đỉnh góc)
            Vector3 pC = measurePoints[count - 1]; // Điểm cuối

            // Tính góc tại B (giữa BA và BC)
            Vector3 BA = pA - pB;
            Vector3 BC = pC - pB;
            float angle = Vector3.Angle(BA, BC);

            // Hiện text góc tại điểm B
            Vector3 textPos = pB + Vector3.up * 0.3f;
            ShowText(textPos, $"∠ = {angle:F1}°");

            Debug.Log($">>> Góc tại điểm giữa: {angle:F1}° <<<");
        }
    }

    /// <summary>
    /// Vẽ đường thẳng nối 2 điểm.
    /// </summary>
    void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("MeasureLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lineObjects.Add(lineObj);
    }

    /// <summary>
    /// Hiện text 3D tại một vị trí.
    /// </summary>
    void ShowText(Vector3 position, string text)
    {
        GameObject textObj = new GameObject("MeasureText");
        textObj.transform.position = position;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 40;
        tm.characterSize = 0.04f;
        tm.color = Color.white;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;

        // Luôn quay về phía camera
        textObj.AddComponent<BillboardLabel>();

        textObjects.Add(textObj);
    }

    /// <summary>
    /// Xóa tất cả điểm đo, đường nối, và text.
    /// </summary>
    void ClearAll()
    {
        foreach (var obj in markerObjects) if (obj != null) Destroy(obj);
        foreach (var obj in lineObjects) if (obj != null) Destroy(obj);
        foreach (var obj in textObjects) if (obj != null) Destroy(obj);

        markerObjects.Clear();
        lineObjects.Clear();
        textObjects.Clear();
        measurePoints.Clear();
    }
}
