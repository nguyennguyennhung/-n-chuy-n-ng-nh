using UnityEngine;

/// <summary>
/// ĐIỀU KHIỂN CAMERA BẰNG CHUỘT + BÀN PHÍM (dành cho test trên PC).
/// 
/// Cách dùng:
/// - WASD: Di chuyển camera (trước/sau/trái/phải)
/// - Giữ chuột phải + kéo: Xoay camera nhìn xung quanh
/// - Space: Bay lên trên
/// - Left Ctrl: Bay xuống dưới
/// - Left Shift: Tăng tốc di chuyển (x3)
/// 
/// CÁCH GẮN:
/// 1. Click chọn "Main Camera" trong Hierarchy
/// 2. Kéo thả script này vào Inspector của Main Camera
/// </summary>
public class FreeCameraController : MonoBehaviour
{
    [Header("Tốc độ di chuyển")]
    [Tooltip("Tốc độ di chuyển bình thường (mét/giây)")]
    public float moveSpeed = 5f;

    [Tooltip("Tốc độ khi giữ Shift (mét/giây)")]
    public float fastSpeed = 15f;

    [Header("Tốc độ xoay camera")]
    [Tooltip("Độ nhạy chuột khi xoay camera")]
    public float lookSpeed = 2f;

    // Biến lưu góc xoay hiện tại của camera
    private float rotationX = 0f; // Xoay lên/xuống
    private float rotationY = 0f; // Xoay trái/phải

    void Start()
    {
        // Lấy góc xoay ban đầu của camera
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;
    }

    void Update()
    {
        // ===== XOAY CAMERA (giữ chuột phải + kéo) =====
        // Input.GetMouseButton(1) = giữ chuột PHẢI
        if (Input.GetMouseButton(1))
        {
            // Input.GetAxis("Mouse X") = chuột kéo sang trái/phải
            // Input.GetAxis("Mouse Y") = chuột kéo lên/xuống
            rotationY += Input.GetAxis("Mouse X") * lookSpeed;
            rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;

            // Giới hạn góc nhìn lên/xuống (không cho lật ngược camera)
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            // Áp dụng góc xoay vào camera
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }

        // ===== DI CHUYỂN CAMERA (WASD + Space + Ctrl) =====
        // Chọn tốc độ: nếu giữ Shift → nhanh, không giữ → bình thường
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : moveSpeed;

        // Tính hướng di chuyển
        Vector3 moveDirection = Vector3.zero;

        // Lấy hướng nhìn ngang (loại bỏ thành phần Y để không bị bay lên trời khi nhấn W)
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // W = tiến, S = lùi (theo hướng ngang)
        if (Input.GetKey(KeyCode.W)) moveDirection += forward;
        if (Input.GetKey(KeyCode.S)) moveDirection -= forward;

        // A = sang trái, D = sang phải
        if (Input.GetKey(KeyCode.A)) moveDirection -= right;
        if (Input.GetKey(KeyCode.D)) moveDirection += right;

        // Space = bay lên, Ctrl = hạ xuống (vẫn giữ để linh hoạt)
        if (Input.GetKey(KeyCode.Space))       moveDirection += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) moveDirection -= Vector3.up;

        // Di chuyển camera
        if (moveDirection != Vector3.zero)
        {
            transform.position += moveDirection.normalized * speed * Time.deltaTime;
        }
    }
}
