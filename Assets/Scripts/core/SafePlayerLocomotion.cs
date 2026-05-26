using UnityEngine;

/// <summary>
/// Gắn lên XR Origin (VR). CHỈ chịu trách nhiệm:
///  1. Đảm bảo có CharacterController (collision với tường).
///  2. Clamp XR Origin vào trong PlayBoundary (chỉ khi VƯỢT bounds).
///  3. Reset về spawnAnchor khi rơi y < fallThreshold.
///
/// KHÔNG ghi đè cc.center / cc.height — để XRIT 3's "Character Controller Driver"
/// (có sẵn trong Locomotion System) tự lo.
///
/// KHÔNG tự di chuyển — phải có Continuous Move Provider hoặc Teleportation Provider
/// gắn cùng XR Origin / Locomotion System.
/// </summary>
[DisallowMultipleComponent]
public class SafePlayerLocomotion : MonoBehaviour
{
    [Header("Camera (Main Camera của XR Origin)")]
    public Transform headTransform;

    [Header("Anchor spawn (Empty GameObject ở vị trí khởi đầu)")]
    public Transform spawnAnchor;

    [Header("Tham chiếu PlayBoundary trong scene")]
    public PlayBoundary boundary;

    [Header("Mặc định nếu CharacterController chưa có")]
    public float defaultRadius = 0.3f;
    public float defaultHeight = 1.6f;

    [Header("Khoá rơi (giây)")]
    public float fallResetDelay = 0.4f;

    private CharacterController cc;
    private float outsideTimer;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = gameObject.AddComponent<CharacterController>();
            cc.radius = defaultRadius;
            cc.height = defaultHeight;
            cc.center = new Vector3(0, defaultHeight * 0.5f, 0);
            cc.slopeLimit = 60f;
            cc.stepOffset = 0.2f;
        }
    }

    private void LateUpdate()
    {
        if (cc == null || !cc.enabled) return;

        // 1) Clamp vào bounds — CHỈ khi đã vượt
        if (boundary != null)
        {
            Vector3 cur = transform.position;
            Vector3 clamped = boundary.Clamp(cur);

            // Chỉ "đẩy nhẹ" khi đã ra ngoài, không ghi đè ép buộc.
            if ((clamped - cur).sqrMagnitude > 0.0001f)
            {
                cc.Move(clamped - cur);
            }

            // 2) Fall guard
            if (boundary.IsFallen(transform.position))
            {
                outsideTimer += Time.deltaTime;
                if (outsideTimer >= fallResetDelay) ResetToSpawn();
            }
            else
            {
                outsideTimer = 0f;
            }
        }
    }

    public void ResetToSpawn()
    {
        if (spawnAnchor == null) return;
        bool wasEnabled = cc.enabled;
        cc.enabled = false;
        transform.SetPositionAndRotation(spawnAnchor.position, spawnAnchor.rotation);
        cc.enabled = wasEnabled;
        outsideTimer = 0f;
    }
}
