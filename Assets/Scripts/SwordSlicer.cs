using UnityEngine;
using EzySlice;

[RequireComponent(typeof(Collider))]
public class SwordSlicer : MonoBehaviour
{
    [Header("Material mặt cắt")]
    public Material crossSectionMaterial;

    [Header("Tham số")]
    public float separationDistance = 0.15f;

    [Header("Chế độ kích cắt")]
    [Tooltip("Bật khi test Simulator — chạm kiếm là cắt, không cần vung nhanh")]
    public bool sliceOnContact = true;
    public float minSwingVelocity = 0.5f;

    [Header("Debug")]
    public bool debugLog = true;

    private Vector3 lastPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        var sliceable = other.GetComponentInParent<Sliceable>();
        if (sliceable == null) return;

        if (debugLog)
            Debug.Log($"[SwordSlicer] Chạm Sliceable: {sliceable.name}, sliceOnContact={sliceOnContact}");

        float speed = currentVelocity.magnitude;

        if (!sliceOnContact && speed < minSwingVelocity)
        {
            if (debugLog) Debug.Log($"[SwordSlicer] Vung quá chậm ({speed:F2} m/s).");
            return;
        }

        TrySlice(sliceable.transform, speed);
    }

    void TrySlice(Transform shape, float speed)
    {
        if (debugLog) Debug.Log($"[SwordSlicer] Bắt đầu TrySlice {shape.name}.");

        // Tìm MeshFilter — kể cả con đang inactive
        MeshFilter mf = shape.GetComponent<MeshFilter>();
        if (mf == null)
        {
            // Tìm cả các con đang tắt (true)
            var allMfs = shape.GetComponentsInChildren<MeshFilter>(true);
            if (allMfs.Length > 0)
            {
                mf = allMfs[0];
                if (debugLog)
                {
                    Debug.Log($"[SwordSlicer] Tìm thấy {allMfs.Length} MeshFilter:");
                    foreach (var m in allMfs)
                        Debug.Log($"  - {m.gameObject.name} (active={m.gameObject.activeInHierarchy})");
                }
            }
        }

        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning($"[SwordSlicer] Không thấy MeshFilter hoặc Mesh trên {shape.name} (kể cả children).");
            return;
        }

        GameObject meshObj = mf.gameObject;
        if (debugLog) Debug.Log($"[SwordSlicer] Dùng MeshFilter ở: {meshObj.name}");

        // Pháp tuyến mặt cắt: vuông góc với cả hướng vung VÀ trục lưỡi
// → mặt cắt chứa lưỡi kiếm và đi theo hướng vung (đúng như chém thật)
Vector3 swordAxis = transform.forward;  // trục dọc lưỡi
Vector3 swingDir;

if (currentVelocity.magnitude > 0.05f)
{
    swingDir = currentVelocity.normalized;
}
else
{
    // Kiếm đứng yên → dùng "right" của kiếm làm hướng vung giả lập
    swingDir = transform.right;
}

Vector3 planeNormal = Vector3.Cross(swordAxis, swingDir).normalized;

// Đề phòng vector 0 (sword axis song song với swing dir)
if (planeNormal.sqrMagnitude < 0.001f)
    planeNormal = transform.up;
Vector3 planeWorldPos = transform.position;

if (debugLog)
    Debug.Log($"[SwordSlicer] PlanePos={planeWorldPos} SwordAxis={swordAxis} SwingDir={swingDir} Normal={planeNormal}");
// Điểm cắt = vị trí lưỡi kiếm tại thời điểm chạm (entry point)
// Đây là cách Beat Saber/Fruit Ninja làm
        // Slice
        SlicedHull hull = meshObj.Slice(planeWorldPos, planeNormal, crossSectionMaterial);
        if (hull == null)
        {
            Debug.LogWarning($"[SwordSlicer] EzySlice trả về null — plane không cắt qua mesh hoặc mesh không hợp lệ.");
            return;
        }

        GameObject upper = hull.CreateUpperHull(meshObj, crossSectionMaterial);
        GameObject lower = hull.CreateLowerHull(meshObj, crossSectionMaterial);
        if (upper == null || lower == null)
        {
            Debug.LogWarning($"[SwordSlicer] Không tạo được hull (upper={upper}, lower={lower}).");
            return;
        }

        SetupPiece(upper, meshObj.transform, planeNormal * separationDistance * 0.5f);
        SetupPiece(lower, meshObj.transform, -planeNormal * separationDistance * 0.5f);

        // Ẩn hình gốc
        shape.gameObject.SetActive(false);

        Debug.Log($"[SwordSlicer] Cắt {shape.name} thành công (speed = {speed:F2} m/s).");
    }

    void SetupPiece(GameObject piece, Transform source, Vector3 offset)
    {
        piece.transform.position = source.position + offset;
        piece.transform.rotation = source.rotation;
        piece.transform.localScale = source.lossyScale;

        var col = piece.AddComponent<MeshCollider>();
        col.convex = true;

        var rb = piece.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}