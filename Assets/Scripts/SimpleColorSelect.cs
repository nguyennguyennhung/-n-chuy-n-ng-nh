using UnityEngine;

public class SimpleColorSelect : MonoBehaviour
{
    private Renderer rend;

    private Color originalColor;

    private bool selected = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null) { enabled = false; return; }

        // ★ FIX: không phải shader nào cũng có _BaseColor (Built-in dùng _Color).
        Material m = rend.material;
        if (m.HasProperty("_BaseColor"))      originalColor = m.GetColor("_BaseColor");
        else if (m.HasProperty("_Color"))     originalColor = m.color;
        else                                  originalColor = Color.white;
    }

    public void ToggleSelect()
    {
        if (rend == null) return;
        selected = !selected;

        Color target = selected ? Color.yellow : originalColor;
        Material m = rend.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", target);
        if (m.HasProperty("_Color"))     m.color = target;

        Debug.Log(selected ? "SELECT" : "DESELECT");
    }
}