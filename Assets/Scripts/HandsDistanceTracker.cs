using UnityEngine;

/// <summary>
/// Theo dõi khoảng cách 2 cổ tay và tốc độ thay đổi.
/// Các script khác đọc qua static Instance.
/// </summary>
public class HandsDistanceTracker : MonoBehaviour
{
    public static HandsDistanceTracker Instance { get; private set; }

    [Header("Tay")]
    public OVRSkeleton leftSkeleton;
    public OVRSkeleton rightSkeleton;

    [Tooltip("Ngưỡng tốc độ thay đổi khoảng cách để coi là 'đang kéo' (m/s)")]
    public float movingThreshold = 0.1f;

    public Transform LeftWrist { get; private set; }
    public Transform RightWrist { get; private set; }
    public float CurrentDistance { get; private set; }
    public float DistanceVelocity { get; private set; }  // mét/giây
    public bool IsHandsMoving => Mathf.Abs(DistanceVelocity) >= movingThreshold;

    float lastDistance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(InitBones());
    }

    System.Collections.IEnumerator InitBones()
    {
        while ((leftSkeleton == null || !leftSkeleton.IsInitialized) ||
               (rightSkeleton == null || !rightSkeleton.IsInitialized))
            yield return null;

        LeftWrist = FindBone(leftSkeleton, OVRSkeleton.BoneId.Hand_WristRoot);
        RightWrist = FindBone(rightSkeleton, OVRSkeleton.BoneId.Hand_WristRoot);

        if (LeftWrist != null && RightWrist != null)
            lastDistance = Vector3.Distance(LeftWrist.position, RightWrist.position);
    }

    Transform FindBone(OVRSkeleton skel, OVRSkeleton.BoneId id)
    {
        foreach (var b in skel.Bones) if (b.Id == id) return b.Transform;
        return null;
    }

    void Update()
    {
        if (LeftWrist == null || RightWrist == null) return;

        CurrentDistance = Vector3.Distance(LeftWrist.position, RightWrist.position);
        DistanceVelocity = (CurrentDistance - lastDistance) / Time.deltaTime;
        lastDistance = CurrentDistance;
    }
}