using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// DEBUG OVERLAY — chẩn đoán HandPhysics
///
/// Hiển thị trên màn hình:
///   - OVRHand / OVRSkeleton có init xong chưa
///   - Số collider đã được gắn vào tay
///   - Layer của tay & layer của hình đang chạm
///   - Vật đang chạm (nếu có)
///
/// CÁCH DÙNG:
///   - Gắn vào GameObject "InteractionManager"
///   - Kéo 2 OVRHandPrefab vào field
///   - Chạy Play → đọc text trên góc trái màn hình
/// </summary>
public class HandPhysicsDebug : MonoBehaviour
{
    public HandPhysics leftHand;
    public HandPhysics rightHand;

    [Tooltip("In ra Console mỗi 1 giây")]
    public bool logToConsole = true;

    [Tooltip("Hiển thị overlay trên màn hình")]
    public bool showOverlay = true;

    private float _logTimer;
    private GUIStyle _style;

    void Update()
    {
        if (!logToConsole) return;
        _logTimer += Time.deltaTime;
        if (_logTimer >= 1f)
        {
            _logTimer = 0f;
            Debug.Log(BuildReport());
        }
    }

    void OnGUI()
    {
        if (!showOverlay) return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = 18;
            _style.normal.textColor = Color.yellow;
        }

        GUI.Box(new Rect(10, 10, 520, 280), "");
        GUI.Label(new Rect(20, 20, 500, 260), BuildReport(), _style);
    }

    string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== HAND PHYSICS DEBUG ===");

        ReportHand(sb, "LEFT ", leftHand);
        sb.AppendLine();
        ReportHand(sb, "RIGHT", rightHand);

        // Tìm các GeometryObject trong scene & layer của chúng
        sb.AppendLine();
        sb.Append("Shapes in scene: ");
        var shapes = FindObjectsOfType<GeometryObject>();
        if (shapes.Length == 0)
        {
            sb.AppendLine("NONE FOUND!");
        }
        else
        {
            sb.AppendLine($"{shapes.Length}");
            foreach (var s in shapes)
            {
                var col = s.GetComponent<Collider>();
                string colInfo = col == null ? "NO COLLIDER!"
                    : $"{col.GetType().Name} trigger={col.isTrigger}";
                sb.AppendLine($"  - {s.name} layer={LayerMask.LayerToName(s.gameObject.layer)}({s.gameObject.layer}) {colInfo}");
            }
        }

        return sb.ToString();
    }

    void ReportHand(StringBuilder sb, string tag, HandPhysics hp)
    {
        if (hp == null) { sb.AppendLine($"{tag}: NOT ASSIGNED"); return; }

        var skeleton = hp.GetComponent<OVRSkeleton>();
        var ovrHand  = hp.GetComponent<OVRHand>();
        bool skInit = skeleton != null && skeleton.IsInitialized;
        bool tracked = ovrHand != null && ovrHand.IsTracked;

        sb.Append($"{tag}: ");
        sb.Append($"OVRHand={(ovrHand != null ? "OK" : "MISSING")} ");
        sb.Append($"OVRSkeleton={(skeleton != null ? "OK" : "MISSING")} ");
        sb.AppendLine($"layer={LayerMask.LayerToName(hp.gameObject.layer)}({hp.gameObject.layer})");
        sb.AppendLine($"        SkeletonInit={skInit} HandTracked={tracked}");
        sb.AppendLine($"        TouchableLayer mask={hp.touchableLayer.value}");
        sb.AppendLine($"        isTouchingAny={hp.isTouchingAny} obj={(hp.touchedObject ? hp.touchedObject.name : "null")}");
    }
}
