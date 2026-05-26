using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HIỆN KÝ HIỆU TOÁN HỌC (h, a, R, D, l) TRÊN CÁC KHỐI HÌNH.
/// Nhấn L hoặc dùng Wrist Menu để bật/tắt.
/// </summary>
public class EdgeLabelManager : MonoBehaviour
{
    [Header("Cài đặt")]
    public float fontSize = 0.3f;
    public Color edgeNameColor = new Color(1f, 0.85f, 0f);

    private Dictionary<GameObject, List<GameObject>> labelMap = new Dictionary<GameObject, List<GameObject>>();
    private InteractionCore _core;

    void Update()
    {
        if (_core == null)
        {
            _core = FindObjectOfType<InteractionCore>();
            if (_core == null) return;
        }
 
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (_core.Selected != null) ToggleEdgeLabels(_core.Selected.gameObject);
        }
        UpdateLabelScales();
    }

    void UpdateLabelScales()
    {
        foreach (var pair in labelMap)
            foreach (var labelObj in pair.Value)
                if (labelObj != null)
                {
                    TextMesh tm = labelObj.GetComponent<TextMesh>();
                    if (tm != null) { tm.characterSize = fontSize * 0.05f; tm.color = edgeNameColor; }
                }
    }

    public void ToggleEdgeLabels(GameObject target)
    {
        if (target == null) return;
        if (labelMap.ContainsKey(target)) RemoveEdgeLabels(target);
        else CreateEdgeLabels(target);
    }

    void CreateEdgeLabels(GameObject target)
    {
        List<GameObject> labels = new List<GameObject>();

        if (target.GetComponent<PyramidMeshGenerator>())
        {
            var gen = target.GetComponent<PyramidMeshGenerator>();
            float s = gen.baseSize / 2f;
            AddSymbol(target, new Vector3(0, gen.height / 2, 0.05f), "h", labels);
            AddSymbol(target, new Vector3(0, 0, s + 0.1f), "a", labels);
        }
        else if (target.GetComponent<ConeMeshGenerator>())
        {
            var gen = target.GetComponent<ConeMeshGenerator>();
            AddSymbol(target, new Vector3(0, gen.height / 2, 0.05f), "h", labels);
            AddSymbol(target, new Vector3(gen.radius / 2, 0, 0), "R", labels);
            AddSymbol(target, new Vector3(-gen.radius / 2, 0, 0.1f), "D", labels);
            AddSymbol(target, new Vector3(gen.radius / 2 + 0.15f, gen.height / 2, 0), "l", labels);
        }
        else
        {
            string n = target.name.ToLower();
            if (n.Contains("cylinder") || n.Contains("trụ"))
            {
                AddSymbol(target, new Vector3(0.05f, 0, 0), "h", labels);
                AddSymbol(target, new Vector3(0.25f, -1f, 0), "R", labels);
            }
            else if (n.Contains("sphere") || n.Contains("cầu"))
                AddSymbol(target, new Vector3(0.25f, 0.05f, 0), "R", labels);
            else if (n.Contains("cube") || n.Contains("box") || n.Contains("hộp"))
            {
                AddSymbol(target, new Vector3(0, 0.5f, 0.55f), "a", labels);
                AddSymbol(target, new Vector3(0.55f, 0, 0.5f), "a", labels);
            }
        }

        labelMap[target] = labels;
    }

    void AddSymbol(GameObject parent, Vector3 localPos, string symbol, List<GameObject> labels)
    {
        GameObject labelObj = new GameObject("Symbol_" + symbol);
        labelObj.transform.SetParent(parent.transform, false);
        labelObj.transform.localPosition = localPos;

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = symbol;
        tm.fontSize = 50;
        tm.color = edgeNameColor;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;

        labelObj.AddComponent<BillboardLabel>();
        labels.Add(labelObj);
    }

    void RemoveEdgeLabels(GameObject target)
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
