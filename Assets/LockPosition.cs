using UnityEngine;

public class FixedSeatCamera : MonoBehaviour
{
    [Header("Cấu hình đối tượng")]
    public Transform CenterEyeAnchor;

    [Header("Chỉnh độ cao")]
    public float eyeHeight = 0.6f; // Bạn chỉnh số này trong Inspector để cao lên

    private Vector3 fixedPosition;

    void Start()
    {
        // Khóa vị trí dựa trên vị trí của cái Ghế (Object gắn script này)
        // Cộng thêm chiều cao tầm mắt mong muốn
        fixedPosition = transform.position + Vector3.up * eyeHeight;

        // Vô hiệu hóa bộ điều khiển di chuyển để phím W,A,S,D không có tác dụng
        CharacterController cc = GetComponentInParent<CharacterController>();
        if (cc != null) cc.enabled = false;

        MonoBehaviour playerController = GetComponentInParent<OVRPlayerController>();
        if (playerController != null) playerController.enabled = false;
    }

    void LateUpdate()
    {
        if (CenterEyeAnchor == null) return;

        // 1. ÉP VỊ TRÍ: Bất chấp phím bấm, vị trí luôn đứng im tại ghế
        CenterEyeAnchor.position = fixedPosition;

        // 2. KHÓA TRỤC XOAY: Chỉ cho phép xoay Y (trái/phải)
        // Triệt tiêu X (ngửa lên/xuống) và Z (nghiêng) nếu cần fix cứng 1 hướng
        Vector3 currentRot = CenterEyeAnchor.eulerAngles;
        CenterEyeAnchor.eulerAngles = new Vector3(0, currentRot.y, 0);
    }
}