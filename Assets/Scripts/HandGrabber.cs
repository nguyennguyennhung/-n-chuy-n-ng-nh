using System.Collections;
using UnityEngine;

/// <summary>
/// Phát hiện pinch (chụm ngón cái + trỏ) — hoạt động cho cả Quest thật lẫn Simulator.
/// Khi pinch: tìm Grabbable gần nhất quanh "pinch point" → cầm.
/// Khi mở ngón: thả.
/// </summary>
[RequireComponent(typeof(OVRSkeleton))]
public class HandGrabber : MonoBehaviour
{
    [Tooltip("OVRHand component của tay này (thường cùng GameObject)")]
    public OVRHand hand;

    [Tooltip("Bán kính tìm vật quanh pinch point (mét)")]
    public float grabRadius = 0.1f;

    [Tooltip("Ngưỡng pinch strength để bắt đầu cầm (0-1)")]
    [Range(0.5f, 1f)] public float pinchThreshold = 0.7f;

    OVRSkeleton skeleton;
    Transform thumbTip;
    Transform indexTip;
    Transform grabAnchor;

    Grabbable heldObject;
    bool wasPinching = false;
    Vector3 lastAnchorPos;
    Quaternion lastAnchorRot;

    IEnumerator Start()
    {
        skeleton = GetComponent<OVRSkeleton>();

        while (skeleton == null || !skeleton.IsInitialized || skeleton.Bones.Count == 0)
            yield return null;

        foreach (var bone in skeleton.Bones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip) indexTip = bone.Transform;
            if (bone.Id == OVRSkeleton.BoneId.Hand_ThumbTip) thumbTip = bone.Transform;
        }

        // GameObject "GrabAnchor" ẩn ở giữa thumb-tip và index-tip
        var anchorObj = new GameObject(name + "_GrabAnchor");
        grabAnchor = anchorObj.transform;
        grabAnchor.SetParent(transform, false);

        lastAnchorPos = grabAnchor.position;
        lastAnchorRot = grabAnchor.rotation;
    }

    void Update()
    {
        if (indexTip == null || thumbTip == null || hand == null) return;
        if (!hand.IsTracked) return;

        // Pinch point = giữa đầu ngón cái và đầu ngón trỏ
        grabAnchor.position = (thumbTip.position + indexTip.position) * 0.5f;
        grabAnchor.rotation = indexTip.rotation;

        float pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        bool isPinching = pinchStrength >= pinchThreshold;

        if (isPinching && !wasPinching)
            TryGrab();
        else if (!isPinching && wasPinching && heldObject != null)
            ReleaseGrab();

        wasPinching = isPinching;
        lastAnchorPos = grabAnchor.position;
        lastAnchorRot = grabAnchor.rotation;
    }

    void TryGrab()
    {
        Collider[] hits = Physics.OverlapSphere(grabAnchor.position, grabRadius);
        Grabbable closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var g = hit.GetComponentInParent<Grabbable>();
            if (g != null && !g.isHeld)
            {
                float dist = Vector3.Distance(g.transform.position, grabAnchor.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = g;
                }
            }
        }

        if (closest != null)
        {
            heldObject = closest;
            heldObject.Grab(grabAnchor);
        }
    }

    void ReleaseGrab()
    {
        // Tính vận tốc lúc thả từ delta vị trí frame trước
        Vector3 velocity = (grabAnchor.position - lastAnchorPos) / Time.deltaTime;

        // Tính vận tốc góc từ delta rotation
        Quaternion deltaRot = grabAnchor.rotation * Quaternion.Inverse(lastAnchorRot);
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        Vector3 angularVelocity = axis * (angle * Mathf.Deg2Rad / Time.deltaTime);

        heldObject.Release(velocity, angularVelocity);
        heldObject = null;
    }
}