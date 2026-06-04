// Assets/Scripts/core/PlayerYDiagnostic.cs
using UnityEngine;
using System.Text;

/// <summary>
/// Overlay debug Y position để chẩn đoán jitter.
///
/// Gắn lên XR Origin → hiển thị:
///   - transform.y, isGrounded, vận tốc Y
///   - Collider gần nhất dưới CC (sàn hay không)
///   - Log mỗi khi Y giảm đột ngột (> 0.1m/frame)
///
/// Mục đích: biết CHÍNH XÁC tại sao cam rơi.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerYDiagnostic : MonoBehaviour
{
    public bool showOverlay = true;
    public int fontSize = 12;
    public Vector2 panelPos = new Vector2(10, 220);
    [Tooltip("Log khi Y giảm > N mét trong 1 frame")]
    public float jumpThreshold = 0.05f;
    [Tooltip("Max khoảng cách check sàn dưới CC")]
    public float groundProbeDist = 5f;

    CharacterController _cc;
    float _prevY;
    float _velY;
    bool _grounded;
    string _floorInfo = "(?)";

    void Start()
    {
        _cc = GetComponent<CharacterController>();
        _prevY = transform.position.y;
    }

    Vector3 _prevPos;
    int _teleportCount;

    void Update()
    {
        Vector3 pos = transform.position;
        float dy = pos.y - _prevY;
        Vector3 delta = pos - _prevPos;
        _velY = dy / Mathf.Max(Time.deltaTime, 0.001f);

        if (Mathf.Abs(dy) > jumpThreshold)
        {
            _teleportCount++;
            string who = "?";
            // Heuristic: nếu Y nhảy về spawn anchor area → PlayerSafetyClamp reset
            if (Mathf.Abs(pos.y - 0.05f) < 0.1f) who = "→ về spawn (SafetyClamp?)";
            else if (dy < 0) who = "→ rơi (gravity?)";
            Debug.Log($"[YDiag] ⚠ Frame {Time.frameCount}: pos {_prevPos:F2} → {pos:F2} | Δ={delta:F2} (v_y={_velY:F1}m/s) {who}");
        }
        _prevY = pos.y;
        _prevPos = pos;

        _grounded = _cc != null && _cc.isGrounded;
        ProbeFloor();
    }

    void ProbeFloor()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, groundProbeDist, ~0, QueryTriggerInteraction.Ignore))
        {
            string layer = LayerMask.LayerToName(hit.collider.gameObject.layer);
            _floorInfo = $"{hit.collider.name} (layer={layer}, Δy={hit.point.y - transform.position.y:F2})";
        }
        else _floorInfo = "(không có collider dưới)";
    }

    void OnGUI()
    {
        if (!showOverlay) return;
        Vector3 p = transform.position;
        var sb = new StringBuilder();
        sb.AppendLine("<b>═ PLAYER Y DIAG ═</b>");
        sb.AppendLine($"pos = <color=#80ff80>({p.x:F2}, {p.y:F2}, {p.z:F2})</color>");
        sb.AppendLine($"velocity Y  = <color={(Mathf.Abs(_velY) < 0.05f ? "#80ff80" : "#ffd060")}>{_velY:F2} m/s</color>");
        sb.AppendLine($"isGrounded  = <color={(_grounded ? "#80ff80" : "#ff6060")}>{_grounded}</color>");
        sb.AppendLine($"Teleports   = <color={(_teleportCount > 5 ? "#ff6060" : "#a0a0a0")}>{_teleportCount}</color>");
        sb.AppendLine($"Floor below:");
        sb.AppendLine($"  <color=#a0d8ff>{_floorInfo}</color>");

        GUIStyle s = new GUIStyle(GUI.skin.box)
        {
            fontSize = fontSize, alignment = TextAnchor.UpperLeft,
            richText = true, padding = new RectOffset(8, 8, 6, 6)
        };
        s.normal.textColor = Color.white;
        GUI.Box(new Rect(panelPos.x, panelPos.y, 400, 170), sb.ToString(), s);
    }
}
