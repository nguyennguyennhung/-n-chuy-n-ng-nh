using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Khi tay (1 trong 2) ở gần hình, hình mờ dần để học sinh nhìn được cấu trúc bên trong.
/// Khoảng cách tính theo Vector3.Distance từ tay đến renderer.bounds.center.
///
/// Không dựa vào pinch/state controller — chỉ dùng PROXIMITY.
/// Nếu cần phân biệt "hover vs select", kết hợp với HandPinchManipulator.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ShapeTouchTransparency : MonoBehaviour
{
    public Transform leftHand;
    public Transform rightHand;

    [Header("Khoảng cách kích hoạt (mét, world)")]
    public float touchDistance = 0.25f;
    public float fadeDistance  = 0.6f;  // ngoài tầm này = opaque hoàn toàn

    [Header("Alpha mục tiêu")]
    [Range(0f, 1f)] public float opaqueAlpha = 1.0f;
    [Range(0f, 1f)] public float touchAlpha  = 0.25f;

    [Header("Tốc độ chuyển (giây)")]
    public float lerpDuration = 0.2f;

    private Renderer rend;
    private Material runtimeMat;
    private float currentAlpha;
    private float vel;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int SurfaceId   = Shader.PropertyToID("_Surface");

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        runtimeMat = rend.material;
        EnsureTransparentMode(runtimeMat);
        currentAlpha = opaqueAlpha;
        ApplyAlpha(currentAlpha);
    }

    private void Update()
    {
        float dL = leftHand  ? Vector3.Distance(leftHand.position,  rend.bounds.center) : float.PositiveInfinity;
        float dR = rightHand ? Vector3.Distance(rightHand.position, rend.bounds.center) : float.PositiveInfinity;
        float d  = Mathf.Min(dL, dR);

        // Linear remap: d <= touchDistance => touchAlpha; d >= fadeDistance => opaqueAlpha
        float t = Mathf.InverseLerp(touchDistance, fadeDistance, d);
        float target = Mathf.Lerp(touchAlpha, opaqueAlpha, t);

        currentAlpha = Mathf.SmoothDamp(currentAlpha, target, ref vel, lerpDuration);
        ApplyAlpha(currentAlpha);
    }

    private void ApplyAlpha(float a)
    {
        if (runtimeMat.HasProperty(BaseColorId))
        {
            Color c = runtimeMat.GetColor(BaseColorId);
            c.a = a;
            runtimeMat.SetColor(BaseColorId, c);
        }
    }

    private static void EnsureTransparentMode(Material mat)
    {
        if (mat == null || !mat.HasProperty(SurfaceId)) return;
        mat.SetFloat(SurfaceId, 1);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void OnDestroy()
    {
        // ★ FIX: rend.material clone runtime — phải Destroy thủ công để không leak GPU asset.
        if (runtimeMat != null) Destroy(runtimeMat);
    }
}
