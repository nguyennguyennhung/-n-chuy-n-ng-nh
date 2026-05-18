using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class CubeVisualizer : MonoBehaviour
{
    [Header("Line Settings")]
    public Color edgeColor = Color.white;
    public float edgeWidth = 0.005f; // Tối ưu cho VR (5mm)

    [Header("Label Settings")]
    public float fontSize = 2f;
    public float labelOffset = 0.1f; // Khoảng cách đẩy chữ ra ngoài
    public Color labelColor = Color.white; // Màu chữ của các đỉnh (A, B, C...)

    [Header("Dimension Symbol Settings")]
    public bool showDimension = true;
    public string dimensionSymbol = "a";
    public Color dimensionColor = Color.yellow; // Mặc định màu vàng cho dễ nhìn

    private void Start()
    {
        // Khởi tạo sau 0.1s để đảm bảo Mesh đã load xong
        Invoke("InitializeVisuals", 0.1f);
    }

    void InitializeVisuals()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("No MeshFilter found on " + name);
            return;
        }

        Vector3[] vertices = mf.sharedMesh.vertices;
        
        // 1. Lọc lấy 8 đỉnh độc nhất
        List<Vector3> uniqueVertices = new List<Vector3>();
        foreach (Vector3 v in vertices)
        {
            if (!uniqueVertices.Any(uv => Vector3.Distance(uv, v) < 0.01f))
            {
                uniqueVertices.Add(v);
            }
        }

        if (uniqueVertices.Count < 8)
        {
            Debug.LogWarning("Một hình lập phương cần 8 đỉnh độc nhất. Tìm thấy: " + uniqueVertices.Count);
            return;
        }

        // 2. Chia thành 2 nhóm: 4 đỉnh phía trên (Top) và 4 đỉnh phía dưới (Bottom)
        float centerY = uniqueVertices.Average(v => v.y);
        List<Vector3> topVertices = uniqueVertices.Where(v => v.y > centerY).ToList();
        List<Vector3> bottomVertices = uniqueVertices.Where(v => v.y <= centerY).ToList();

        if (topVertices.Count != 4 || bottomVertices.Count != 4)
        {
            Debug.LogError("Lỗi chia đỉnh. Cần 4 đỉnh trên và 4 đỉnh dưới.");
            return;
        }

        // 3. Tính tâm của từng mặt để sắp xếp theo vòng tròn (tránh nối chéo)
        Vector3 topCenter = Vector3.zero;
        foreach (var v in topVertices) topCenter += v;
        topCenter /= 4f;

        topVertices.Sort((a, b) => {
            float angleA = Mathf.Atan2(a.z - topCenter.z, a.x - topCenter.x);
            float angleB = Mathf.Atan2(b.z - topCenter.z, b.x - topCenter.x);
            return angleA.CompareTo(angleB);
        });

        Vector3 bottomCenter = Vector3.zero;
        foreach (var v in bottomVertices) bottomCenter += v;
        bottomCenter /= 4f;

        bottomVertices.Sort((a, b) => {
            float angleA = Mathf.Atan2(a.z - bottomCenter.z, a.x - bottomCenter.x);
            float angleB = Mathf.Atan2(b.z - bottomCenter.z, b.x - bottomCenter.x);
            return angleA.CompareTo(angleB);
        });

        // Tính khoảng cách offset tự động theo kích thước khối
        float autoOffset = mf.sharedMesh.bounds.extents.magnitude * 0.2f;

        // Tên các đỉnh đáy dưới
        string[] bottomNames = { "A", "B", "C", "D" };

        // Vẽ cạnh đáy dưới
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            CreateLine("Edge_Bottom_" + i, bottomVertices[i], bottomVertices[next], edgeColor);
        }

        // Vẽ cạnh đáy trên
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            CreateLine("Edge_Top_" + i, topVertices[i], topVertices[next], edgeColor);
        }

        // Vẽ các cạnh đứng nối đáy trên và đáy dưới
        // Tìm đỉnh dưới tương ứng gần nhất với đỉnh trên để nối thẳng đứng
        int[] topToBottomMapping = new int[4];
        for (int i = 0; i < 4; i++)
        {
            int closestBottomIndex = 0;
            float minDist = float.MaxValue;
            for (int j = 0; j < 4; j++)
            {
                float d = Vector2.Distance(new Vector2(topVertices[i].x, topVertices[i].z), new Vector2(bottomVertices[j].x, bottomVertices[j].z));
                if (d < minDist)
                {
                    minDist = d;
                    closestBottomIndex = j;
                }
            }
            topToBottomMapping[i] = closestBottomIndex;
            CreateLine("Edge_Vertical_" + i, topVertices[i], bottomVertices[closestBottomIndex], edgeColor);
        }

        // 4. Tạo nhãn đỉnh đáy dưới (A, B, C, D)
        for (int i = 0; i < 4; i++)
        {
            Vector3 pushDir = (bottomVertices[i] - bottomCenter).normalized;
            CreateLabel(bottomNames[i], bottomVertices[i] + pushDir * autoOffset, labelColor);
        }

        // 5. Tạo nhãn đỉnh đáy trên (A', B', C', D' tương ứng thẳng hàng)
        for (int i = 0; i < 4; i++)
        {
            int bottomIdx = topToBottomMapping[i];
            string topName = bottomNames[bottomIdx] + "'";
            Vector3 pushDir = (topVertices[i] - topCenter).normalized;
            
            // Đẩy chữ lên phía trên trục Y một chút
            Vector3 offsetPos = topVertices[i] + pushDir * autoOffset + Vector3.up * (autoOffset * 0.5f);
            CreateLabel(topName, offsetPos, labelColor);
        }

        // 6. Gán ký hiệu độ dài cạnh "a" cho 1 cạnh đáy bất kì (ví dụ cạnh AB)
        if (showDimension)
        {
            Vector3 posA = bottomVertices[0];
            Vector3 posB = bottomVertices[1];
            Vector3 midPoint = Vector3.Lerp(posA, posB, 0.5f);
            
            // Hướng đẩy chữ ra ngoài đáy
            Vector3 pushDirection = (midPoint - bottomCenter).normalized;
            Vector3 labelPos = midPoint + pushDirection * (autoOffset * 0.6f);
            
            CreateLabel(dimensionSymbol, labelPos, dimensionColor, fontSize * 0.9f);
        }
    }

    void CreateLine(string name, Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform, false);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;

        float adjustedWidth = edgeWidth;
        if (transform.lossyScale.x > 10f) adjustedWidth = edgeWidth / transform.lossyScale.x * 2f;

        lr.startWidth = adjustedWidth;
        lr.endWidth = adjustedWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", color); // Gán màu trực tiếp vào Material URP để hoạt động tốt trên URP
        lr.material = mat;
    }

    void CreateLabel(string text, Vector3 position, Color? color = null, float customFontSize = -1)
    {
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = position;

        TextMeshPro tm = labelObj.AddComponent<TextMeshPro>();
        tm.text = text;

        float finalFontSize = customFontSize > 0 ? customFontSize : fontSize;
        if (transform.lossyScale.x > 10f) finalFontSize = finalFontSize / transform.lossyScale.x * 5f;

        tm.fontSize = finalFontSize;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = color ?? Color.white;

        labelObj.AddComponent<BillboardLabel>();
    }
}
