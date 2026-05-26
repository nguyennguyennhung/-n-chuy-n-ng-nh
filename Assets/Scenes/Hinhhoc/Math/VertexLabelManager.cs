using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HIỆN TÊN ĐỈNH (A, B, C, S, O...) THEO PHONG CÁCH TOÁN HỌC.
/// Nhấn V hoặc dùng Wrist Menu để bật/tắt.
/// </summary>
public class VertexLabelManager : MonoBehaviour
{
    [Header("Cài đặt")]
    public float fontSize = 0.3f;
    public Color textColor = Color.white;

    private Dictionary<GameObject, List<GameObject>> labelMap = new Dictionary<GameObject, List<GameObject>>();
    private InteractionCore _core;
    private static readonly string[] LABELS = { "A", "B", "C", "D", "E", "F", "G", "H" };

    void Start()
    {
        _core = FindObjectOfType<InteractionCore>();
    }

    void Update()
    {
        // Lazy init — retry nếu lần đầu chưa tìm thấy
        if (_core == null)
        {
            _core = FindObjectOfType<InteractionCore>();
            if (_core == null) return;
        }
 
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (_core.Selected != null) ToggleLabels(_core.Selected.gameObject);
        }
    }

    public void ToggleLabels(GameObject target)
    {
        if (target == null) return;
        if (labelMap.ContainsKey(target)) RemoveLabels(target);
        else CreateLabels(target);
    }

    void CreateLabels(GameObject target)
    {
        List<Vector3> keyPoints = new List<Vector3>();
        string nameLower = target.name.ToLower();
        string type = "generic";

        if (target.GetComponent<ConeMeshGenerator>())
        {
            type = "cone";
            ConeMeshGenerator gen = target.GetComponent<ConeMeshGenerator>();
            keyPoints.Add(new Vector3(0, gen.height, 0));
            keyPoints.Add(Vector3.zero);
        }
        else if (target.GetComponent<PyramidMeshGenerator>())
        {
            type = "pyramid";
            PyramidMeshGenerator gen = target.GetComponent<PyramidMeshGenerator>();
            float s = gen.baseSize / 2f;
            keyPoints.Add(new Vector3(0, gen.height, 0));
            keyPoints.Add(new Vector3(-s, 0, s));
            keyPoints.Add(new Vector3(s, 0, s));
            keyPoints.Add(new Vector3(s, 0, -s));
            keyPoints.Add(new Vector3(-s, 0, -s));
        }
        else if (nameLower.Contains("cylinder") || nameLower.Contains("trụ"))
        {
            type = "cylinder";
            keyPoints.Add(new Vector3(0, -1f, 0));
            keyPoints.Add(new Vector3(0, 1f, 0));
        }
        else if (nameLower.Contains("sphere") || nameLower.Contains("cầu"))
        {
            type = "sphere";
            keyPoints.Add(Vector3.zero);
        }
        else if (nameLower.Contains("cube") || nameLower.Contains("box") || nameLower.Contains("hộp"))
        {
            type = "cube";
            float h = 0.5f;
            keyPoints.Add(new Vector3(-h, h, h));  keyPoints.Add(new Vector3(h, h, h));
            keyPoints.Add(new Vector3(h, h, -h));  keyPoints.Add(new Vector3(-h, h, -h));
            keyPoints.Add(new Vector3(-h, -h, h)); keyPoints.Add(new Vector3(h, -h, h));
            keyPoints.Add(new Vector3(h, -h, -h)); keyPoints.Add(new Vector3(-h, -h, -h));
        }

        List<GameObject> labels = new List<GameObject>();
        for (int i = 0; i < keyPoints.Count; i++)
        {
            string labelName = (i < LABELS.Length) ? LABELS[i] : "?";
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
        if (!labelMap.ContainsKey(target)) return;
        foreach (var label in labelMap[target])
            if (label != null) Destroy(label);
        labelMap.Remove(target);
    }

    void OnDestroy()
    {
        foreach (var kvp in labelMap)
            foreach (var label in kvp.Value)
                if (label != null) Destroy(label);
    }
}
