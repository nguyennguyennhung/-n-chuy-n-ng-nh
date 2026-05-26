using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MENU 4 NÚT NEO VÀO CỔ TAY TRÁI — học sinh dùng đầu ngón trỏ phải poke vào để toggle.
///
/// Cử chỉ #5: nút "Trong suốt"   → ToggleTransparency
/// Cử chỉ #6: nút "Cạnh khối"     → ToggleWireframe
/// Cử chỉ #7: nút "Đỉnh A B C"    → ToggleVertexLabels
/// Cử chỉ #8: nút "Ký hiệu h R"   → ToggleEdgeLabels
///
/// CÁCH GẮN:
///   1. Cùng GameObject "GameManager" → Add Component → Hand Wrist Menu.
///   2. Kéo HandGestureInteraction (cùng GameObject) vào ô Gesture Controller.
///   3. Kéo OVRSkeleton tay TRÁI vào Left Skeleton.
///   4. Kéo OVRSkeleton tay PHẢI vào Right Skeleton (để biết vị trí ngón trỏ phải).
///
/// Menu tự tạo runtime — không cần dựng UI Canvas trong Editor.
/// </summary>
[DisallowMultipleComponent]
public class HandWristMenu : MonoBehaviour
{
    [Header("Tham chiếu")]
    public InteractionCore core;
    public OVRSkeleton leftSkeleton;
    public OVRSkeleton rightSkeleton;

    [Header("Cài đặt menu")]
    [Tooltip("Bán kính 'chạm nút' giữa đầu ngón trỏ phải và tâm nút (m).")]
    public float pokeRadius = 0.025f;

    [Tooltip("Thời gian chờ giữa 2 lần poke để tránh kích hoạt nhiều lần (giây).")]
    public float pokeCooldown = 0.6f;

    [Tooltip("Khoảng cách menu lệch khỏi cổ tay theo chiều mu bàn tay (m).")]
    public float menuOffsetUp = 0.06f;

    // 4 nút — tạo runtime
    private readonly string[] buttonLabels =
    {
        "Trong suot",   // #5 — TextMesh không vẽ được dấu tiếng Việt mặc định, dùng không dấu
        "Canh khoi",    // #6
        "Dinh A B C",   // #7
        "Ky hieu h R"   // #8
    };

    private readonly Color[] buttonColors =
    {
        new Color(1f, 0.6f, 0.2f),  // cam
        new Color(0.4f, 0.8f, 1f),  // xanh dương
        new Color(0.5f, 1f, 0.5f),  // xanh lá
        new Color(1f, 0.85f, 0.3f), // vàng
    };

    private List<GameObject> buttons = new List<GameObject>();
    private float[] lastPokeTime;
    private Transform leftWrist, rightIndexTip;
    private bool menuBuilt;

    void Update()
    {
        // Tìm bone cổ tay trái và đầu ngón trỏ phải
        if (leftWrist == null) leftWrist = FindBone(leftSkeleton, OVRSkeleton.BoneId.Hand_WristRoot);
        if (rightIndexTip == null) rightIndexTip = FindBone(rightSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);

        if (leftWrist == null) return;

        // Tạo menu lần đầu khi cổ tay đã có (skeleton init xong)
        if (!menuBuilt) BuildMenu();

        // Theo dõi: ngón trỏ phải có chạm nút nào không?
        if (rightIndexTip != null) CheckPoke();
    }

    // ====================================================
    // TẠO 4 NÚT GẮN VÀO CỔ TAY TRÁI
    // ====================================================
    void BuildMenu()
    {
        for (int i = 0; i < buttonLabels.Length; i++)
        {
            // Quả cầu nhỏ làm "nút"
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            btn.name = "WristBtn_" + buttonLabels[i];
            btn.transform.SetParent(leftWrist, false);

            // Sắp 4 nút thành hàng dọc theo cẳng tay (trục X local của bone wrist)
            // Cách nhau ~3.5cm để học sinh phân biệt được khi poke
            btn.transform.localPosition = new Vector3(-0.04f - i * 0.035f, menuOffsetUp, 0f);
            btn.transform.localScale = Vector3.one * 0.025f;

            // Bỏ collider (không cần — ta dùng khoảng cách đo trực tiếp)
            Destroy(btn.GetComponent<Collider>());

            // Màu nút
            Renderer rend = btn.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", buttonColors[i]);
            rend.material = mat;

            // Nhãn nổi bên cạnh nút
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btn.transform, false);
            labelObj.transform.localPosition = new Vector3(0, 0.03f, 0);

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = buttonLabels[i];
            tm.fontSize = 32;
            tm.characterSize = 0.008f;
            tm.color = Color.white;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            labelObj.AddComponent<BillboardLabel>();

            buttons.Add(btn);
        }

        lastPokeTime = new float[buttons.Count];
        menuBuilt = true;
    }

    // ====================================================
    // KIỂM TRA NGÓN TRỎ PHẢI POKE NÚT NÀO
    // ====================================================
    void CheckPoke()
    {
        if (core == null) core = FindObjectOfType<InteractionCore>();
        if (core == null) return;

        for (int i = 0; i < buttons.Count; i++)
        {
            GameObject btn = buttons[i];
            if (btn == null) continue;

            float d = Vector3.Distance(rightIndexTip.position, btn.transform.position);
            if (d <= pokeRadius && Time.time - lastPokeTime[i] >= pokeCooldown)
            {
                lastPokeTime[i] = Time.time;
                FireButton(i);

                // Phản hồi thị giác: phồng nút lên rồi co lại (bù cho thiếu haptic)
                StartCoroutine(PulseButton(btn));
            }
        }
    }

    void FireButton(int index)
    {
        switch (index)
        {
            case 0: core.ToggleTransparency(); break;  // #5
            case 1: core.ToggleWireframe();    break;  // #6
            case 2: core.ToggleVertexLabels(); break;  // #7
            case 3: core.ToggleEdgeLabels();   break;  // #8
        }
    }

    System.Collections.IEnumerator PulseButton(GameObject btn)
    {
        // Phóng to 1.5 lần trong 0.1s rồi về kích thước cũ — feedback thị giác
        Vector3 baseScale = btn.transform.localScale;
        float t = 0f;
        while (t < 0.1f)
        {
            float k = Mathf.Lerp(1f, 1.5f, t / 0.1f);
            btn.transform.localScale = baseScale * k;
            t += Time.deltaTime;
            yield return null;
        }
        t = 0f;
        while (t < 0.1f)
        {
            float k = Mathf.Lerp(1.5f, 1f, t / 0.1f);
            btn.transform.localScale = baseScale * k;
            t += Time.deltaTime;
            yield return null;
        }
        btn.transform.localScale = baseScale;
    }

    Transform FindBone(OVRSkeleton skel, OVRSkeleton.BoneId id)
    {
        if (skel == null || !skel.IsInitialized) return null;
        foreach (var b in skel.Bones) if (b.Id == id) return b.Transform;
        return null;
    }
}
