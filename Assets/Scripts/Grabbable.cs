using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    [Tooltip("Sau khi thả, vật có rơi theo trọng lực không")]
    public bool dropPhysicsOnRelease = true;

    [Tooltip("Giới hạn vận tốc khi thả (tránh văng vô lý)")]
    public float maxReleaseSpeed = 3f;
    public float maxReleaseAngular = 10f;

    [HideInInspector] public bool isHeld = false;

    Rigidbody rb;
    Transform grabAnchor;

    // Lưu các collider của vật + collider người chơi để toggle ignore
    Collider[] myColliders;
    static CharacterController playerCC;
    Vector3 originPosition;
    Quaternion originRotation;
    [HideInInspector] public Vector3 originalScale;   // ← thêm dòng này

    void Awake()
{
    rb = GetComponent<Rigidbody>();
    myColliders = GetComponentsInChildren<Collider>();
    if (playerCC == null) playerCC = FindObjectOfType<CharacterController>();

    originPosition = transform.position;
    originRotation = transform.rotation;
    originalScale = transform.localScale;   // ← thêm dòng này
}


    public void Grab(Transform anchor)
{
    grabAnchor = anchor;
    isHeld = true;
    rb.isKinematic = true;
    rb.useGravity = false;
    SetIgnorePlayer(true);
}

public void Release(Vector3 velocity, Vector3 angularVelocity)
{
    isHeld = false;
    SetIgnorePlayer(false);

    if (dropPhysicsOnRelease)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.ClampMagnitude(velocity, maxReleaseSpeed);
        rb.angularVelocity = Vector3.ClampMagnitude(angularVelocity, maxReleaseAngular);
    }

    grabAnchor = null;
}

    void SetIgnorePlayer(bool ignore)
    {
        if (playerCC == null) return;
        foreach (var col in myColliders)
        {
            if (col != null)
                Physics.IgnoreCollision(col, playerCC, ignore);
        }
    }

    void LateUpdate()
    {
        if (isHeld && grabAnchor != null)
        {
            transform.position = grabAnchor.position;
            transform.rotation = grabAnchor.rotation;
        }
    }
    public void ResetToOrigin()
{
    if (isHeld)
    {
        isHeld = false;
        SetIgnorePlayer(false);
    }

    // Clear velocity TRƯỚC khi set kinematic
    rb.velocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;

    // Sau đó mới set kinematic
    rb.isKinematic = true;
    rb.useGravity = false;

    transform.position = originPosition;
    transform.rotation = originRotation;
    transform.localScale = originalScale;
}
}