// Assets/Scripts/core/SafetyFloorMini.cs
using UnityEngine;

/// <summary>
/// Sàn an toàn KÍCH THƯỚC NHỎ — chỉ quanh SpawnAnchor.
///
/// Khác FloorEnsurer cũ (500x500m, gây conflict với HHti_Scean):
///   - Chỉ 20x20m, ở dưới SpawnAnchor.y - 0.05m
///   - Layer Default, không trigger
///   - Vô hình (không Renderer)
///   - KHÔNG DontDestroyOnLoad — tự dọn khi đổi scene
///
/// Gắn lên SpawnAnchor (cùng GameObject) → sàn xuất hiện ở dưới spawn.
/// </summary>
[DisallowMultipleComponent]
public class SafetyFloorMini : MonoBehaviour
{
    [Header("═══ Kích thước ═══")]
    public float floorSize = 20f;
    [Tooltip("Cách spawn anchor bao nhiêu mét xuống dưới")]
    public float floorOffsetY = 0.05f;
    public float floorThickness = 0.5f;

    [Header("═══ Layer ═══")]
    [Tooltip("Layer cho sàn — Default (0) đảm bảo CC va chạm")]
    public int floorLayer = 0;

    [Header("═══ Debug ═══")]
    public bool showLog = true;
    public bool visibleInScene = false; // bật để debug — sẽ thấy box trong Scene view

    GameObject _floor;

    void Start()
    {
        _floor = new GameObject("[SafetyFloor_NearSpawn]");
        _floor.transform.SetParent(null); // root level
        _floor.transform.position = new Vector3(
            transform.position.x,
            transform.position.y - floorOffsetY - floorThickness * 0.5f,
            transform.position.z);
        _floor.layer = floorLayer;

        var box = _floor.AddComponent<BoxCollider>();
        box.size = new Vector3(floorSize, floorThickness, floorSize);

        if (visibleInScene)
        {
            var mr = _floor.AddComponent<MeshRenderer>();
            var mf = _floor.AddComponent<MeshFilter>();
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mf.sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            _floor.transform.localScale = box.size;
            box.size = Vector3.one;
            DestroyImmediate(temp);
        }

        if (showLog)
            Debug.Log($"[SafetyFloorMini] ✅ Sàn {floorSize}x{floorSize}m tại {_floor.transform.position:F2}");
    }

    void OnDestroy()
    {
        if (_floor != null) Destroy(_floor);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = transform.position - Vector3.up * (floorOffsetY + floorThickness * 0.5f);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
        Gizmos.DrawCube(c, new Vector3(floorSize, floorThickness, floorSize));
    }
}
