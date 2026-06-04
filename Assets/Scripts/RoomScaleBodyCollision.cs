using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RoomScaleBodyCollision : MonoBehaviour
{
    [Tooltip("CenterEyeAnchor (đầu người chơi)")]
    public Transform head;

    private CharacterController cc;
    private Vector3 lastHeadXZ;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (head != null)
            lastHeadXZ = new Vector3(head.position.x, 0, head.position.z);
    }

    void LateUpdate()
    {
        if (head == null) return;

        // 1) Co giãn capsule theo chiều cao đầu (cúi/đứng)
        float headLocalY = Mathf.Clamp(head.localPosition.y, 1f, 2f);
        cc.height = headLocalY;
        Vector3 center = cc.center;
        center.y = headLocalY / 2f + cc.skinWidth;
        center.x = head.localPosition.x;
        center.z = head.localPosition.z;
        cc.center = center;

        // 2) Tính bước đi thật (dịch chuyển XZ của đầu giữa các frame)
        Vector3 currentHeadXZ = new Vector3(head.position.x, 0, head.position.z);
        Vector3 delta = currentHeadXZ - lastHeadXZ;

        // 3) Dùng CharacterController.Move để xử lý va chạm với tường
        // Nếu tường chặn, cc.Move sẽ chỉ cho phép phần delta hợp lệ
        Vector3 before = transform.position;
        cc.Move(delta + Vector3.down * 0.01f); // thêm down nhỏ giữ dính sàn
        Vector3 actualMove = transform.position - before;

        // 4) Nếu CharacterController bị chặn (di chuyển ít hơn delta),
        //    đẩy ngược TrackingSpace để "huỷ" phần đầu đã thò qua tường
        Vector3 blocked = delta - new Vector3(actualMove.x, 0, actualMove.z);
        if (blocked.sqrMagnitude > 0.0001f)
        {
            // Đẩy ngược cả rig để đầu lùi lại đúng vị trí bị chặn
            transform.position -= blocked;
        }

        lastHeadXZ = new Vector3(head.position.x, 0, head.position.z);
    }
}