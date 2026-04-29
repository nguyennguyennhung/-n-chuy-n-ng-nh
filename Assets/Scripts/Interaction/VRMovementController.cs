using UnityEngine;

/// <summary>
/// ĐIỀU KHIỂN DI CHUYỂN TRONG VR (Dùng cho OVRCameraRig).
/// Giúp bạn đi lại quanh phòng bằng Joystick trên tay cầm.
/// </summary>
public class VRMovementController : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 2.0f; // Tốc độ đi bộ
    public float turnAngle = 45f;  // Góc xoay mỗi lần gạt cần

    private Transform cameraRigTransform;
    private Transform centerEyeTransform;

    // Biến để xử lý xoay Snap (không bị xoay liên tục gây chóng mặt)
    private bool readyToTurn = true;

    void Start()
    {
        cameraRigTransform = this.transform;
        // Tìm camera trung tâm để biết hướng người dùng đang nhìn
        centerEyeTransform = GetComponentInChildren<OVRCameraRig>().centerEyeAnchor;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        // Lấy dữ liệu từ cần gạt trái (Left Thumbstick)
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        if (input.magnitude > 0.1f)
        {
            // Tính toán hướng di chuyển dựa trên hướng mắt đang nhìn
            Vector3 forward = centerEyeTransform.forward;
            Vector3 right = centerEyeTransform.right;

            // Loại bỏ thành phần Y để không bị bay lên trời hoặc lún xuống đất
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            // Tính vector di chuyển cuối cùng
            Vector3 moveDirection = (forward * input.y + right * input.x) * moveSpeed * Time.deltaTime;

            // Di chuyển toàn bộ Rig
            cameraRigTransform.position += moveDirection;
        }
    }

    void HandleRotation()
    {
        // Lấy dữ liệu từ cần gạt phải (Right Thumbstick)
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

        // Xử lý xoay Snap (gạt một cái xoay 45 độ)
        if (readyToTurn && Mathf.Abs(input.x) > 0.7f)
        {
            float rotationAmount = Mathf.Sign(input.x) * turnAngle;
            cameraRigTransform.Rotate(0, rotationAmount, 0);
            readyToTurn = false; // Chặn xoay tiếp cho đến khi thả cần ra
        }
        else if (Mathf.Abs(input.x) < 0.2f)
        {
            readyToTurn = true;
        }
    }
}
