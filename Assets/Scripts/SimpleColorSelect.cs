using UnityEngine;

public class SimpleColorSelect : MonoBehaviour
{
    private Renderer rend;

    private Color originalColor;

    private bool selected = false;

    void Start()
    {
        rend = GetComponent<Renderer>();

        originalColor = rend.material.GetColor("_BaseColor");
    }

    public void ToggleSelect()
    {
        selected = !selected;

        if (selected)
        {
            rend.material.SetColor("_BaseColor", Color.yellow);

            Debug.Log("SELECT");
        }
        else
        {
            rend.material.SetColor("_BaseColor", originalColor);

            Debug.Log("DESELECT");
        }
    }
}