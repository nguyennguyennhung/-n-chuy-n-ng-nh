using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerBodyFollowHead : MonoBehaviour
{
    [Tooltip("CenterEyeAnchor (đầu người chơi)")]
    public Transform head;

    [Tooltip("Tốc độ rơi do trọng lực")]
    public float gravity = -9.81f;

    private CharacterController cc;
    private float verticalVelocity;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (head == null) return;

        // 1) Cập nhật chiều cao capsule theo độ cao đầu
        float headHeight = Mathf.Clamp(head.localPosition.y, 1f, 2f);
        cc.height = headHeight;
        Vector3 newCenter = cc.center;
        newCenter.y = headHeight / 2f + cc.skinWidth;
        // Đẩy center theo XZ của đầu để capsule "ôm" quanh đầu
        newCenter.x = head.localPosition.x;
        newCenter.z = head.localPosition.z;
        cc.center = newCenter;

        // 2) Áp dụng trọng lực để người chơi luôn dính sàn
        if (cc.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        cc.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }
}