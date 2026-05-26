using UnityEngine;

/// <summary>
/// CẦU NỐI: HandPhysics → InteractionCore
///
/// QUY TẮC:
///   - 1 hoặc 2 tay chạm vào hình  → SelectObject (wireframe + transparency)
///   - Cả 2 tay rời khỏi hình > holdTime giây → ClearAll
///
/// CÁCH SETUP:
///   - Gắn script này vào GameObject "InteractionManager" (cùng chỗ InteractionCore)
///   - Kéo OVRHandPrefab_Left và OVRHandPrefab_Right vào 2 field
/// </summary>
public class HandTouchSelector : MonoBehaviour
{
    [Header("Tham chiếu tay")]
    [Tooltip("HandPhysics gắn trên OVRHandPrefab tay trái")]
    public HandPhysics leftHand;

    [Tooltip("HandPhysics gắn trên OVRHandPrefab tay phải")]
    public HandPhysics rightHand;

    [Header("Tham chiếu Core")]
    public InteractionCore core;

    [Header("Cài đặt")]
    [Tooltip("Thời gian giữ chạm tối thiểu trước khi chọn (s) — chống nháy")]
    public float touchHoldTime = 0.15f;

    [Tooltip("Thời gian rời tay trước khi bỏ chọn (s)")]
    public float releaseHoldTime = 0.5f;

    private GeometryObject _lastTouched;
    private float _touchTimer;
    private float _releaseTimer;

    void Awake()
    {
        if (!core) core = FindObjectOfType<InteractionCore>();
    }

    void Update()
    {
        if (core == null) return;

        // Lấy hình đang chạm từ cả 2 tay (ưu tiên tay phải)
        GeometryObject touchedRight = rightHand ? rightHand.GetTouchedGeometry() : null;
        GeometryObject touchedLeft  = leftHand  ? leftHand.GetTouchedGeometry()  : null;
        GeometryObject current = touchedRight != null ? touchedRight : touchedLeft;

        if (current != null)
        {
            // Đang chạm hình nào đó
            _releaseTimer = 0f;

            if (current == _lastTouched)
            {
                _touchTimer += Time.deltaTime;
            }
            else
            {
                _lastTouched = current;
                _touchTimer = 0f;
            }

            // Đủ thời gian giữ → chọn
            if (_touchTimer >= touchHoldTime && core.Selected != current)
            {
                core.SelectObject(current);
            }
        }
        else
        {
            // Không chạm hình nào
            _touchTimer = 0f;
            _lastTouched = null;

            if (core.Selected != null)
            {
                _releaseTimer += Time.deltaTime;
                if (_releaseTimer >= releaseHoldTime)
                {
                    core.ClearAll();
                    _releaseTimer = 0f;
                }
            }
        }
    }
}
