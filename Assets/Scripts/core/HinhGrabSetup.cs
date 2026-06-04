// Assets/Scripts/core/HinhGrabSetup.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Gắn lên Hinh 1 (hoặc prefab hình học) → tự setup grab interaction XRI.
///
/// Auto-add:
///   - Rigidbody (kinematic mặc định, không rơi)
///   - XRGrabInteractable (XRI 3.x)
///   - Collider (nếu chưa có)
///
/// Tránh Meta ISDK (GrabFreeTransformer, HandGrabInteractable...) — chuyển sang stack XRI thuần.
/// </summary>
[DisallowMultipleComponent]
public class HinhGrabSetup : MonoBehaviour
{
    [Header("═══ Rigidbody ═══")]
    public bool useGravity = false;
    public bool isKinematic = true;
    [Tooltip("Mass (kg). Kinematic thì không quan trọng.")]
    public float mass = 1f;

    [Header("═══ XR Grab ═══")]
    public XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.Instantaneous;
    public bool trackPosition = true;
    public bool trackRotation = true;
    public bool throwOnDetach = false;

    [Header("═══ Layer ═══")]
    [Tooltip("Tên layer dành cho hình tương tác")]
    public string shapesLayerName = "Shapes";

    [Header("═══ Auto run ═══")]
    public bool runOnAwake = true;

    void Awake()
    {
        if (runOnAwake) Apply();
    }

    [ContextMenu("Apply Setup")]
    public void Apply()
    {
        // 1) Rigidbody
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = useGravity;
        rb.isKinematic = isKinematic;
        rb.mass = mass;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 2) Collider (đảm bảo có ít nhất 1)
        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider>();
            // Auto-size theo bounds renderer nếu có
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                col.center = transform.InverseTransformPoint(rend.bounds.center);
                col.size = rend.bounds.size;
            }
        }

        // 3) XRGrabInteractable
        var grab = GetComponent<XRGrabInteractable>();
        if (grab == null) grab = gameObject.AddComponent<XRGrabInteractable>();
        grab.movementType = movementType;
        grab.trackPosition = trackPosition;
        grab.trackRotation = trackRotation;
        grab.throwOnDetach = throwOnDetach;

        // 4) Layer
        int layer = LayerMask.NameToLayer(shapesLayerName);
        if (layer >= 0) gameObject.layer = layer;
    }
}
