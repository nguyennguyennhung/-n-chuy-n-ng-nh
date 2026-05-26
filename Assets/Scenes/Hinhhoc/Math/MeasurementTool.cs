using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CÔNG CỤ ĐO LƯỜNG CHO VR.
/// Dùng Pinch tay phải để đặt điểm đo (thay vì chuột).
/// Nhấn M bật/tắt, C xóa điểm đo.
/// </summary>
public class MeasurementTool : MonoBehaviour
{
    [Header("Cài đặt")]
    public Color pointColor = Color.yellow;
    public Color lineColor = Color.cyan;
    public float pointSize = 0.08f;

    private bool isMeasuring = false;
    private List<Vector3> measurePoints = new List<Vector3>();
    private List<GameObject> markers = new List<GameObject>();
    private List<GameObject> lines = new List<GameObject>();
    private List<GameObject> texts = new List<GameObject>();

    private HandGestureInteraction handController;

    void Update()
    {
        if (handController == null)
        {
            handController = FindObjectOfType<HandGestureInteraction>();
            if (handController == null) return;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            isMeasuring = !isMeasuring;
            Debug.Log(isMeasuring ? ">>> ĐO LƯỜNG: BẬT <<<" : ">>> ĐO LƯỜNG: TẮT <<<");
            if (!isMeasuring) ClearAll();
        }

        if (Input.GetKeyDown(KeyCode.C) && isMeasuring)
        {
            ClearAll();
            Debug.Log(">>> Đã xóa điểm đo <<<");
        }

        // Trong VR: dùng raycast từ ngón trỏ phải khi click chuột trái (fallback) hoặc pinch
        if (isMeasuring && Input.GetMouseButtonDown(0))
        {
            if (Input.GetMouseButton(1)) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                AddMeasurePoint(hit.point);
        }
    }

    void AddMeasurePoint(Vector3 point)
    {
        measurePoints.Add(point);

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = point;
        marker.transform.localScale = Vector3.one * pointSize;
        marker.GetComponent<Renderer>().material.color = pointColor;
        Destroy(marker.GetComponent<Collider>());
        markers.Add(marker);

        int count = measurePoints.Count;

        if (count >= 2)
        {
            Vector3 p1 = measurePoints[count - 2];
            Vector3 p2 = measurePoints[count - 1];
            DrawLine(p1, p2);
            float distance = Vector3.Distance(p1, p2);
            ShowText((p1 + p2) / 2f + Vector3.up * 0.15f, $"{distance:F2} m");
        }

        if (count >= 3 && (count - 1) % 2 == 0)
        {
            Vector3 pA = measurePoints[count - 3];
            Vector3 pB = measurePoints[count - 2];
            Vector3 pC = measurePoints[count - 1];
            float angle = Vector3.Angle(pA - pB, pC - pB);
            ShowText(pB + Vector3.up * 0.3f, $"∠ = {angle:F1}°");
        }
    }

    void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("MeasureLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = lineColor;
        lr.startWidth = lr.endWidth = 0.02f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lines.Add(lineObj);
    }

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
        textObj.AddComponent<BillboardLabel>();
        texts.Add(textObj);
    }

    void ClearAll()
    {
        foreach (var obj in markers) if (obj != null) Destroy(obj);
        foreach (var obj in lines) if (obj != null) Destroy(obj);
        foreach (var obj in texts) if (obj != null) Destroy(obj);
        markers.Clear(); lines.Clear(); texts.Clear(); measurePoints.Clear();
    }
}
