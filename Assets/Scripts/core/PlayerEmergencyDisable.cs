// Assets/Scripts/core/PlayerEmergencyDisable.cs
using UnityEngine;

/// <summary>
/// EMERGENCY ISOLATOR — Tắt tạm thời các script can thiệp player
/// để chẩn đoán xem ai gây jitter.
///
/// Gắn lên XR Origin Hands (XR Rig). Bật/tắt từng cờ ở Inspector
/// rồi Play để cô lập nguyên nhân.
///
/// Chạy ở DefaultExecutionOrder(-1000) → trước mọi script khác.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class PlayerEmergencyDisable : MonoBehaviour
{
    [Header("═══ Tắt từng script để test ═══")]
    [Tooltip("Tắt PlayerSafetyClamp → không reset khi rơi")]
    public bool disableSafetyClamp = false;

    [Tooltip("Tắt CharacterController → bỏ va chạm vật lý")]
    public bool disableCharacterController = false;

    [Tooltip("Tắt XRBodyTransformer → bỏ tracking-driven movement")]
    public bool disableBodyTransformer = false;

    [Tooltip("Pin XR Origin tại spawn — KHÔNG cho mọi script di chuyển nó")]
    public bool pinAtSpawn = false;
    public Vector3 pinPosition = new Vector3(0, 0.05f, 0);

    [Header("═══ Log ═══")]
    public bool logActions = true;

    void Start()
    {
        if (disableSafetyClamp)
        {
            var clamp = GetComponent<PlayerSafetyClamp>();
            if (clamp != null) { clamp.enabled = false; Log("Disabled PlayerSafetyClamp"); }
        }

        if (disableCharacterController)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null) { cc.enabled = false; Log("Disabled CharacterController"); }
        }

        if (disableBodyTransformer)
        {
            // XRBodyTransformer ở namespace UnityEngine.XR.Interaction.Toolkit
            var comps = GetComponents<MonoBehaviour>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                if (c.GetType().Name == "XRBodyTransformer")
                {
                    c.enabled = false;
                    Log($"Disabled {c.GetType().Name}");
                }
            }
        }
    }

    void LateUpdate()
    {
        if (pinAtSpawn)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null && cc.enabled)
            {
                cc.enabled = false;
                transform.position = pinPosition;
                cc.enabled = true;
            }
            else
            {
                transform.position = pinPosition;
            }
        }
    }

    void Log(string msg)
    {
        if (logActions) Debug.Log($"[EmergencyDisable] {msg}");
    }
}
