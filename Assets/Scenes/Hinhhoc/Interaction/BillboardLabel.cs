using UnityEngine;

/// <summary>
/// Gắn vào TextMesh 3D để chữ luôn quay mặt về phía camera (Billboard effect).
/// Dùng cho các nhãn đỉnh (A, B, C, S, O) và ký hiệu cạnh (h, R, D, l) của khối hình.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}
