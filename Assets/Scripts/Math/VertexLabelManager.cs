using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HIỆN TÊN CÁC ĐỈNH QUAN TRỌNG (A, B, C, S, O...) THEO PHONG CÁCH TOÁN HỌC.
/// </summary>
public class VertexLabelManager : MonoBehaviour
{
    [Header("Cài đặt nhãn đỉnh")]
    public float fontSize = 0.3f;
    public Color textColor = Color.white;

    private Dictionary<GameObject, List<GameObject>> labelMap = new Dictionary<GameObject, List<GameObject>>();
    private ObjectInteraction interaction;

    private static readonly string[] LABELS = { "A", "B", "C", "D", "E", "F", "G", "H" };

    void Start()
    {
        interaction = FindObjectOfType<ObjectInteraction>();
    }

    void Update()
    {
        if (interaction == null) return;

        // Nhấn V → bật/tắt nhãn đỉnh
        if (Input.GetKeyDown(KeyCode.V))
        {
            GeometryObject selected = interaction.GetSelectedObject();
            if (selected != null)
            {
                ToggleLabels(selected.gameObject);
            }
        }
    }

    public void ToggleLabels(GameObject target)
    {
        if (labelMap.ContainsKey(target))
            RemoveLabels(target);
        else
            CreateLabels(target);
    }

    void CreateLabels(GameObject target)
    {
        List<Vector3> keyPoints = new List<Vector3>();
        string nameLower = target.name.ToLower();
        string type = "generic";

        // 1. HÌNH NÓN
        if (target.GetComponent<ConeMeshGenerator>())
        {
            type = "cone";
            ConeMeshGenerator gen = target.GetComponent<ConeMeshGenerator>();
            keyPoints.Add(new Vector3(0, gen.height, 0)); // Đỉnh S
            keyPoints.Add(Vector3.zero); // Tâm đáy O
        }
        // 2. HÌNH CHÓP
        else if (target.GetComponent<PyramidMeshGenerator>())
        {
            type = "pyramid";
            PyramidMeshGenerator gen = target.GetComponent<PyramidMeshGenerator>();
            float s = gen.baseSize / 2f;
            keyPoints.Add(new Vector3(0, gen.height, 0)); // Đỉnh S
            keyPoints.Add(new Vector3(-s, 0, s));  // A
            keyPoints.Add(new Vector3(s, 0, s));   // B
            keyPoints.Add(new Vector3(s, 0, -s));  // C
            keyPoints.Add(new Vector3(-s, 0, -s)); // D
        }
        // 3. HÌNH TRỤ
        else if (nameLower.Contains("cylinder") || nameLower.Contains("trụ"))
        {
            type = "cylinder";
            float h = 1.0f;
            keyPoints.Add(new Vector3(0, -h, 0)); // O
            keyPoints.Add(new Vector3(0, h, 0));  // O'
        }
        // 4. HÌNH CẦU
        else if (nameLower.Contains("sphere") || nameLower.Contains("cầu"))
        {
            type = "sphere";
            keyPoints.Add(Vector3.zero); // O
        }
        // 5. HÌNH HỘP
        else if (nameLower.Contains("cube") || nameLower.Contains("box") || nameLower.Contains("hộp"))
        {
            type = "cube";
            float h = 0.5f;
            keyPoints.Add(new Vector3(-h, h, h)); keyPoints.Add(new Vector3(h, h, h));
            keyPoints.Add(new Vector3(h, h, -h)); keyPoints.Add(new Vector3(-h, h, -h));
            keyPoints.Add(new Vector3(-h, -h, h)); keyPoints.Add(new Vector3(h, -h, h));
            keyPoints.Add(new Vector3(h, -h, -h)); keyPoints.Add(new Vector3(-h, -h, -h));
        }

        // --- TẠO NHÃN ---
        List<GameObject> labels = new List<GameObject>();
        for (int i = 0; i < keyPoints.Count; i++)
        {
            string labelName = (i < LABELS.Length) ? LABELS[i] : "?";
            
            // Quy tắc đặt tên đặc biệt
            if (type == "cone") labelName = (i == 0) ? "S" : "O";
            else if (type == "pyramid" && i == 0) labelName = "S";
            else if (type == "cylinder") labelName = (i == 0) ? "O" : "O'";
            else if (type == "sphere") labelName = "O";

            GameObject labelObj = new GameObject("Label_" + labelName);
            labelObj.transform.SetParent(target.transform, false);
            labelObj.transform.localPosition = keyPoints[i];

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = labelName;
            tm.fontSize = 50;
            tm.characterSize = fontSize * 0.05f;
            tm.color = textColor;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;

            labelObj.AddComponent<BillboardLabel>();
            labels.Add(labelObj);
        }
        labelMap[target] = labels;
    }

    void RemoveLabels(GameObject target)
    {
        if (labelMap.ContainsKey(target))
        {
            foreach (GameObject label in labelMap[target]) if (label != null) Destroy(label);
            labelMap.Remove(target);
        }
    }
}

public class BillboardLabel : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}
