using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HIỆN KÝ HIỆU TOÁN HỌC (h, a, R, D, l) TRÊN CÁC KHỐI HÌNH.
/// </summary>
public class EdgeLabelManager : MonoBehaviour
{
    [Header("Cài đặt nhãn")]
    public float fontSize = 0.3f;
    public Color edgeNameColor = new Color(1f, 0.85f, 0f); // Vàng

    private Dictionary<GameObject, List<GameObject>> labelMap = new Dictionary<GameObject, List<GameObject>>();
    private ObjectInteraction interaction;

    void Start()
    {
        interaction = FindObjectOfType<ObjectInteraction>();
    }

    void Update()
    {
        if (interaction == null) return;

        // Nhấn L → bật/tắt ký hiệu cạnh
        if (Input.GetKeyDown(KeyCode.L))
        {
            GeometryObject selected = interaction.GetSelectedObject();
            if (selected != null)
            {
                ToggleEdgeLabels(selected.gameObject);
            }
        }

        UpdateLabelScales();
    }

    void UpdateLabelScales()
    {
        foreach (var pair in labelMap)
        {
            foreach (GameObject labelObj in pair.Value)
            {
                if (labelObj != null)
                {
                    TextMesh tm = labelObj.GetComponent<TextMesh>();
                    tm.characterSize = fontSize * 0.05f;
                    tm.color = edgeNameColor;
                }
            }
        }
    }

    public void ToggleEdgeLabels(GameObject target)
    {
        if (labelMap.ContainsKey(target))
        {
            RemoveEdgeLabels(target);
        }
        else
        {
            CreateEdgeLabels(target);
        }
    }

    void CreateEdgeLabels(GameObject target)
    {
        List<GameObject> labels = new List<GameObject>();
        string nameLower = target.name.ToLower();

        // 1. HÌNH CHÓP (Nhận diện qua Script)
        if (target.GetComponent<PyramidMeshGenerator>())
        {
            PyramidMeshGenerator gen = target.GetComponent<PyramidMeshGenerator>();
            float s = gen.baseSize / 2f;
            AddSymbol(target, new Vector3(0, gen.height/2, 0.05f), "h", labels);
            AddSymbol(target, new Vector3(0, 0, s + 0.1f), "a", labels);
        }
        // 2. HÌNH NÓN (Nhận diện qua Script)
        else if (target.GetComponent<ConeMeshGenerator>())
        {
            ConeMeshGenerator gen = target.GetComponent<ConeMeshGenerator>();
            AddSymbol(target, new Vector3(0, gen.height/2, 0.05f), "h", labels);
            AddSymbol(target, new Vector3(gen.radius/2, 0, 0), "R", labels);
            AddSymbol(target, new Vector3(-gen.radius/2, 0, 0.1f), "D", labels);
            AddSymbol(target, new Vector3(gen.radius/2 + 0.15f, gen.height/2, 0), "l", labels);
        }
        // 3. HÌNH TRỤ (Nhận diện qua Tên)
        else if (nameLower.Contains("cylinder") || nameLower.Contains("trụ"))
        {
            float r = 0.5f;
            float h = 1.0f;
            AddSymbol(target, new Vector3(0.05f, 0, 0), "h", labels);
            AddSymbol(target, new Vector3(r/2, -h, 0), "R", labels);
            AddSymbol(target, new Vector3(-r/2, h, 0), "D", labels);
        }
        // 4. HÌNH CẦU (Nhận diện qua Tên)
        else if (nameLower.Contains("sphere") || nameLower.Contains("cầu"))
        {
            AddSymbol(target, new Vector3(0.25f, 0.05f, 0), "R", labels);
        }
        // 5. HÌNH HỘP/CUBE
        else if (nameLower.Contains("cube") || nameLower.Contains("box") || nameLower.Contains("hộp"))
        {
            float h = 0.5f;
            AddSymbol(target, new Vector3(0, h, h + 0.05f), "a", labels);
            AddSymbol(target, new Vector3(h + 0.05f, 0, h), "a", labels);
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
        if (labelMap.ContainsKey(target))
        {
            foreach (GameObject label in labelMap[target]) if (label != null) Destroy(label);
            labelMap.Remove(target);
        }
    }

    void OnDestroy()
    {
        foreach (var kvp in labelMap)
            foreach (var label in kvp.Value) if (label != null) Destroy(label);
    }
}
