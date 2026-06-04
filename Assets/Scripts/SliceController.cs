using UnityEngine;
using System.Collections;
using EzySlice;

/// <summary>
/// Cắt hình bằng phím + animation lưỡi kiếm bay qua hình.
/// Phím 8: chém ngang. Phím 7: chém dọc. Phím 6: chém chéo.
/// </summary>
public class SliceController : MonoBehaviour
{
    [Header("Phím")]
    public KeyCode sliceHorizontalKey = KeyCode.Alpha8;
    public KeyCode sliceVerticalKey = KeyCode.Alpha7;
    public KeyCode sliceDiagonalKey = KeyCode.Alpha6;

    [Header("Material")]
    public Material crossSectionMaterial;
    public Material bladeMaterial;  // material lưỡi kiếm (phát sáng)

    [Header("Tham số animation")]
    public float bladeAnimationDuration = 0.6f;
    public float bladeLength = 0.5f;
    public float bladeThickness = 0.01f;
    public float bladeTravelDistance = 0.4f;

    [Header("Tham số cắt")]
    public float separationDistance = 0.15f;

    bool isAnimating = false;

    void Update()
    {
        if (isAnimating) return;

        Transform selected = ShapeSelector.Instance != null
            ? ShapeSelector.Instance.selectedShape
            : null;
        if (selected == null) return;

        // Chém ngang: lưỡi nằm theo Z, vung sang phải, mặt cắt ngang (pháp tuyến up)
        if (Input.GetKeyDown(sliceHorizontalKey))
            StartCoroutine(SlashAndSlice(selected, Vector3.up, Vector3.forward, Vector3.right));

        // Chém dọc: lưỡi nằm theo Z, vung từ trên xuống, mặt cắt đứng (pháp tuyến right)
        else if (Input.GetKeyDown(sliceVerticalKey))
            StartCoroutine(SlashAndSlice(selected, Vector3.right, Vector3.forward, Vector3.down));

        // Chém chéo: vung từ trên-trái xuống dưới-phải
        else if (Input.GetKeyDown(sliceDiagonalKey))
        {
            Vector3 swordAxis = Vector3.forward;
            Vector3 swingDir = new Vector3(1, -1, 0).normalized;
            Vector3 normal = Vector3.Cross(swordAxis, swingDir).normalized;
            StartCoroutine(SlashAndSlice(selected, normal, swordAxis, swingDir));
        }
    }

    IEnumerator SlashAndSlice(Transform shape, Vector3 planeNormal, Vector3 swordAxis, Vector3 swingDir)
    {
        isAnimating = true;

        // Tạo lưỡi kiếm: cube dẹt dài
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(blade.GetComponent<Collider>());
        blade.name = "Blade";
        blade.transform.localScale = new Vector3(bladeThickness, bladeLength, bladeThickness);
        if (bladeMaterial != null)
            blade.GetComponent<Renderer>().sharedMaterial = bladeMaterial;
        // Xoay sao cho chiều cao Y local = swordAxis
        blade.transform.rotation = Quaternion.LookRotation(swingDir.normalized, swordAxis.normalized);

        Vector3 center = shape.position;
        Vector3 startPos = center - swingDir.normalized * bladeTravelDistance;
        Vector3 endPos = center + swingDir.normalized * bladeTravelDistance;

        float t = 0f;
        bool sliced = false;
        while (t < bladeAnimationDuration)
        {
            t += Time.deltaTime;
            float p = t / bladeAnimationDuration;
            blade.transform.position = Vector3.Lerp(startPos, endPos, p);

            // Đến giữa hành trình → cắt
            if (!sliced && p >= 0.5f)
            {
                sliced = true;
                DoSlice(shape, planeNormal);
            }
            yield return null;
        }

        Destroy(blade);
        isAnimating = false;
    }

    void DoSlice(Transform shape, Vector3 planeNormal)
    {
        // Tìm MeshFilter — có thể ở chính shape hoặc trong children
        GameObject meshObj = shape.gameObject;
        var mf = meshObj.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = shape.GetComponentInChildren<MeshFilter>();
            if (mf == null)
            {
                Debug.LogWarning($"[Slice] Không tìm thấy MeshFilter trên {shape.name}.");
                return;
            }
            meshObj = mf.gameObject;
        }

        Vector3 planeWorldPos = shape.position;

        SlicedHull hull = meshObj.Slice(planeWorldPos, planeNormal, crossSectionMaterial);
        if (hull == null)
        {
            Debug.LogWarning("[Slice] Slice thất bại.");
            return;
        }

        GameObject upperHull = hull.CreateUpperHull(meshObj, crossSectionMaterial);
        GameObject lowerHull = hull.CreateLowerHull(meshObj, crossSectionMaterial);

        if (upperHull == null || lowerHull == null)
        {
            Debug.LogWarning("[Slice] Không tạo được 2 hull.");
            return;
        }

        // Đặt 2 nửa khớp với vị trí + scale + rotation của Mesh gốc
        SetupPiece(upperHull, meshObj.transform, planeNormal * separationDistance * 0.5f);
        SetupPiece(lowerHull, meshObj.transform, -planeNormal * separationDistance * 0.5f);

        // Ẩn shape gốc
        shape.gameObject.SetActive(false);

        Debug.Log($"[Slice] Cắt {shape.name} thành công.");
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