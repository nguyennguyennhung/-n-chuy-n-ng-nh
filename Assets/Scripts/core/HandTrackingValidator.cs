// Assets/Scripts/core/HandTrackingValidator.cs
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Validator runtime cho hand-tracking setup.
///
/// Gắn lên bất kỳ GameObject nào — sẽ hiện overlay tracking status
/// và log warning nếu hand subsystem không khởi tạo được.
///
/// Hữu ích để biết:
///   - Hand tracking đang BẬT/TẮT
///   - Tay trái/phải có được track không
///   - Pinch strength real-time
///   - OpenXR runtime nào đang chạy
/// </summary>
[DisallowMultipleComponent]
public class HandTrackingValidator : MonoBehaviour
{
    [Header("═══ Overlay ═══")]
    public bool showOverlay = true;
    public int fontSize = 14;
    public Vector2 panelPos = new Vector2(10, 10);

    [Header("═══ Log ═══")]
    public bool logOnStart = true;
    public bool logPinchEvents = false;

    XRHandSubsystem _hands;
    StringBuilder _sb = new StringBuilder(512);

    void OnEnable()
    {
        TryFindSubsystem();
        if (_hands != null)
        {
            _hands.updatedHands += OnHandsUpdated;
        }

        if (logOnStart) LogStartupInfo();
    }

    void OnDisable()
    {
        if (_hands != null)
            _hands.updatedHands -= OnHandsUpdated;
    }

    void TryFindSubsystem()
    {
        var list = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        if (list.Count > 0) _hands = list[0];
    }

    void LogStartupInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ HAND TRACKING VALIDATOR ═══");

        // XR Settings
        var settings = XRGeneralSettings.Instance;
        if (settings == null) { sb.AppendLine("❌ XRGeneralSettings null"); Debug.Log(sb); return; }
        sb.AppendLine($"XR Manager: {(settings.Manager != null ? "✅" : "❌")}");
        sb.AppendLine($"XR Loader: {(settings.Manager?.activeLoader?.name ?? "none")}");

        if (_hands == null) TryFindSubsystem();
        if (_hands == null)
        {
            sb.AppendLine("❌ Không tìm thấy XRHandSubsystem");
            sb.AppendLine("  → Edit > Project Settings > XR Plug-in Management > OpenXR");
            sb.AppendLine("  → Bật 'Hand Tracking Subsystem' trong Features");
        }
        else
        {
            sb.AppendLine($"✅ XRHandSubsystem found: {_hands.subsystemDescriptor.id}");
            sb.AppendLine($"  Running: {_hands.running}");
        }

        Debug.Log(sb.ToString());
    }

    void OnHandsUpdated(XRHandSubsystem subsystem,
                        XRHandSubsystem.UpdateSuccessFlags flags,
                        XRHandSubsystem.UpdateType type)
    {
        if (!logPinchEvents) return;
        // Lightweight pinch detection on right hand
        if (subsystem.rightHand.isTracked)
        {
            var thumb = subsystem.rightHand.GetJoint(XRHandJointID.ThumbTip);
            var index = subsystem.rightHand.GetJoint(XRHandJointID.IndexTip);
            if (thumb.TryGetPose(out var thumbPose) && index.TryGetPose(out var indexPose))
            {
                float dist = Vector3.Distance(thumbPose.position, indexPose.position);
                if (dist < 0.025f) Debug.Log($"[HandValidator] 🤏 Right pinch (dist={dist:F3}m)");
            }
        }
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        _sb.Clear();
        _sb.AppendLine("<b>═ HAND TRACKING ═</b>");

        if (_hands == null)
        {
            _sb.AppendLine("<color=#ff6060>❌ Subsystem null</color>");
        }
        else
        {
            _sb.AppendLine($"Running: <color={(_hands.running ? "#80ff80" : "#ff6060")}>{_hands.running}</color>");

            AppendHand("LEFT ", _hands.leftHand);
            AppendHand("RIGHT", _hands.rightHand);
        }

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = fontSize,
            alignment = TextAnchor.UpperLeft,
            richText = true,
            padding = new RectOffset(8, 8, 6, 6)
        };
        style.normal.textColor = Color.white;
        GUI.Box(new Rect(panelPos.x, panelPos.y, 300, 180), _sb.ToString(), style);
    }

    void AppendHand(string label, XRHand hand)
    {
        string color = hand.isTracked ? "#80ff80" : "#808080";
        _sb.AppendLine($"<color={color}>{label}: {(hand.isTracked ? "tracked" : "lost")}</color>");

        if (!hand.isTracked) return;

        var thumb = hand.GetJoint(XRHandJointID.ThumbTip);
        var index = hand.GetJoint(XRHandJointID.IndexTip);
        if (thumb.TryGetPose(out var t) && index.TryGetPose(out var i))
        {
            float dist = Vector3.Distance(t.position, i.position);
            string pinchColor = dist < 0.025f ? "#ffd060" : "#a0a0a0";
            _sb.AppendLine($"  pinch dist: <color={pinchColor}>{dist * 1000:F0}mm</color>");
        }
    }
}
