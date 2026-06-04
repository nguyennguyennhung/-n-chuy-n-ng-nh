using System.Collections;
using UnityEngine;

[RequireComponent(typeof(OVRSkeleton))]
public class FingerTipPoker : MonoBehaviour
{
    public float touchRadius = 0.012f;

    private OVRSkeleton skeleton;

    IEnumerator Start()
    {
        skeleton = GetComponent<OVRSkeleton>();
        while (skeleton == null || !skeleton.IsInitialized || skeleton.Bones.Count == 0)
            yield return null;

        Transform indexTip = null;
        foreach (var bone in skeleton.Bones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
            {
                indexTip = bone.Transform;
                break;
            }
        }
        if (indexTip == null) yield break;

        var tip = new GameObject("FingerTipTrigger");
        tip.tag = "FingerTip";
        tip.transform.SetParent(indexTip, false);

        var col = tip.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = touchRadius;

        var rb = tip.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}