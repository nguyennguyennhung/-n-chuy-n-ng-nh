using UnityEngine;

/// <summary>
/// Tự động dọn dẹp component cũ/lỗi khi scene bắt đầu.
/// GẮN VÀO: [BuildingBlock] Camera Rig
/// </summary>
[DefaultExecutionOrder(-1000)]
public class CleanupOldComponents : MonoBehaviour
{
    void Awake()
    {
        // 1. Xóa MRPassThroughMaterialChanger (gây lỗi Renderer missing)
        var allComponents = GetComponents<MonoBehaviour>();
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            string typeName = comp.GetType().Name;

            if (typeName == "MRPassThroughMaterialChanger" ||
                typeName == "CharacterPhysicsHandler")
            {
                Debug.Log($"[Cleanup] Xóa component cũ: {typeName}");
                Destroy(comp);
            }
        }

        // 2. Xóa Rigidbody cũ (xung đột CharacterController)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("[Cleanup] Xóa Rigidbody cũ trên Camera Rig");
            Destroy(rb);
        }

        // 3. Xóa CapsuleCollider cũ
        CapsuleCollider cc = GetComponent<CapsuleCollider>();
        if (cc != null)
        {
            Debug.Log("[Cleanup] Xóa CapsuleCollider cũ trên Camera Rig");
            Destroy(cc);
        }

        Debug.Log("[Cleanup] Đã dọn dẹp xong Camera Rig");
    }
}
