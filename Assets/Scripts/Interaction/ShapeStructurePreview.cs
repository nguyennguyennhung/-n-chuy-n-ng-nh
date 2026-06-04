using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HOVER PREVIEW "X-RAY" — Khi tay lại gần hình:
///   1. Vỏ ngoài mờ dần (alpha minAlpha)
///   2. Các CẠNH (edges) sáng lên màu xanh nhạt
///   3. Các ĐỈNH (vertices) phồng lên thành chấm vàng
///   4. Tâm hình (center) hiện chấm đỏ nhịp đập (pulse)
/// Khi tay rời ra: tự fade ngược về opaque, ẩn cấu trúc.
///
/// CÁCH GẮN:
///   - Gắn lên mỗi GameObject có MeshFilter + Renderer (hình Cube/Sphere/Cylinder...).
///   - KHÔNG cần component nào khác. Tự auto-tìm OVRSkeleton để lấy đầu ngón trỏ.
///   - NÊN gỡ ShapeTouchTransparency trước khi gắn cái này (tránh 2 script
///     cùng ghi _BaseColor alpha gây nháy).
///
/// HIỆU NĂNG:
///   - Edges/vertices/center build 1 LẦN trong Start.
///   - Update chỉ scale + alpha, không Instantiate/Destroy.
///   - Dùng MaterialPropertyBlock cho dots → không tạo material instance.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Renderer))]
[DisallowMultipleComponent]
public class ShapeStructurePreview : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════

    [Header("Tay (nguồn distance trigger)")]
    [Tooltip("Kéo bone IndexTip của tay trái. Bỏ trống = tự tìm OVRSkeleton.")]
    public Transform leftHandTip;
    [Tooltip("Kéo bone IndexTip của tay phải. Bỏ trống = tự tìm OVRSkeleton.")]
    public Transform rightHandTip;

    [Header("Khoảng cách kích hoạt (mét, world)")]
    [Tooltip("Tay vào gần hơn ngưỡng này = preview đầy đủ (intensity = 1).")]
    public float showDistance = 0.15f;
    [Tooltip("Tay xa hơn ngưỡng này = preview tắt hoàn toàn (intensity = 0).")]
    public float hideDistance = 0.35f;

    [Header("Bật/tắt từng phần")]
    public bool enableTransparency = true;
    public bool enableEdges        = true;
    public bool enableVertices     = true;
    public bool enableCenter       = true;

    [Header("Diện mạo — Vỏ hình")]
    [Range(0f, 1f)] public float minAlpha = 0.25f;   // alpha khi tay sát
    [Range(0f, 1f)] public float maxAlpha = 1.0f;    // alpha khi tay xa

    [Header("Diện mạo — Cạnh")]
    public Color edgeColor = new Color(0.7f, 1f, 0.9f, 1f);
    public float edgeWidth = 0.004f;

    [Header("Diện mạo — Đỉnh")]
    public Color vertexColor = new Color(1f, 0.85f, 0.2f, 1f);
    public float vertexSize = 0.014f;

    [Header("Diện mạo — Tâm")]
    public Color centerColor = new Color(1f, 0.3f, 0.3f, 1f);
    public float centerSize = 0.022f;
    [Tooltip("Tốc độ nhịp đập (lần/giây)")]
    public float pulseSpeed = 2f;
    [Range(0f, 0.5f)] public float pulseAmplitude = 0.2f;

    [Header("Smooth")]
    [Tooltip("Thời gian fade IN/OUT (giây)")]
    public float fadeDuration = 0.18f;

    // ═══════════════════════════════════════════════════════
    //  INTERNAL
    // ═══════════════════════════════════════════════════════

    private Renderer _rend;
    private Material _mat;
    private float _origAlpha = 1f;
    private bool _hasBaseColor;
    private bool _structureBuilt;

    private readonly List<LineRenderer> _edges    = new List<LineRenderer>();
    private readonly List<Transform>    _vertices = new List<Transform>();
    private Transform _center;
    private MeshRenderer _centerRend;

    private float _intensity;          // 0 → 1
    private MaterialPropertyBlock _mpb;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");
    private static readonly int SurfaceId   = Shader.PropertyToID("_Surface");

    // Cache static lookups
    private static Material s_dotMaterial;
    private static Material s_lineMaterial;

    // ═══════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════

    private void Awake()
    {
        _rend = GetComponent<Renderer>();
        if (_rend != null)
        {
            // KHÔNG truy cập .material trong Awake nếu chưa Play — tránh leak ở editor.
            _mat = _rend.material; // Unity tự clone — đây là instance.
            _hasBaseColor = _mat.HasProperty(BaseColorId);
            _origAlpha = _hasBaseColor ? _mat.GetColor(BaseColorId).a : 1f;
            EnsureTransparentShader(_mat);
        }
        _mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        TryAutoFindHands();
        BuildStructure();
        ApplyIntensity(0f);
    }

    private void Update()
    {
        // Re-find tay nếu bị null (OVRSkeleton init muộn)
        if (leftHandTip == null || rightHandTip == null) TryAutoFindHands();

        float d = MinDistanceToHands();
        float target = Mathf.InverseLerp(hideDistance, showDistance, d);
        // target = 1 nếu d <= showDistance; = 0 nếu d >= hideDistance.

        float step = Time.deltaTime / Mathf.Max(0.01f, fadeDuration);
        _intensity = Mathf.MoveTowards(_intensity, target, step);

        ApplyIntensity(_intensity);
    }

    private void OnDestroy()
    {
        // Material instance đã clone từ rend.material → leak nếu không Destroy.
        if (_mat != null) Destroy(_mat);
    }

    // ═══════════════════════════════════════════════════════
    //  AUTO-FIND TAY
    // ═══════════════════════════════════════════════════════

    private void TryAutoFindHands()
    {
        if (leftHandTip != null && rightHandTip != null) return;

        // Tìm tất cả OVRSkeleton trong scene (cả inactive)
        var skeletons = FindObjectsOfType<OVRSkeleton>(true);
        foreach (var skel in skeletons)
        {
            if (skel == null || !skel.IsInitialized) continue;
            Transform tip = FindBone(skel, OVRSkeleton.BoneId.Hand_IndexTip);
            if (tip == null) continue;

            // Phân biệt trái/phải qua SkeletonType
            if (skel.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft && leftHandTip == null)
                leftHandTip = tip;
            else if (skel.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight && rightHandTip == null)
                rightHandTip = tip;
        }
    }

    private static Transform FindBone(OVRSkeleton skel, OVRSkeleton.BoneId id)
    {
        if (skel == null || !skel.IsInitialized || skel.Bones == null) return null;
        foreach (var b in skel.Bones)
        {
            if (b == null) continue;
            if (b.Id == id) return b.Transform;
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════
    //  BUILD STRUCTURE — 1 lần trong Start
    // ═══════════════════════════════════════════════════════

    private void BuildStructure()
    {
        if (_structureBuilt) return;
        _structureBuilt = true;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Mesh mesh = mf.sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        // === EDGES ===
        if (enableEdges) BuildEdges(verts, tris);

        // === VERTICES ===
        if (enableVertices) BuildVertices(verts);

        // === CENTER ===
        if (enableCenter) BuildCenter(mesh.bounds.center);
    }

    private void BuildEdges(Vector3[] verts, int[] tris)
    {
        // Dedup edges theo cặp đỉnh (sorted)
        var seen = new HashSet<long>();
        for (int i = 0; i < tris.Length; i += 3)
        {
            AddEdgeIfNew(tris[i], tris[i + 1], verts, seen);
            AddEdgeIfNew(tris[i + 1], tris[i + 2], verts, seen);
            AddEdgeIfNew(tris[i + 2], tris[i], verts, seen);
        }
    }

    private void AddEdgeIfNew(int a, int b, Vector3[] verts, HashSet<long> seen)
    {
        // Key dựa trên POSITION (không phải index) để gom cạnh trùng do mesh weld
        Vector3 pa = verts[a], pb = verts[b];
        long key = HashPosPair(pa, pb);
        if (!seen.Add(key)) return;

        GameObject go = new GameObject("Edge");
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, pa);
        lr.SetPosition(1, pb);
        lr.startWidth = lr.endWidth = edgeWidth;
        lr.material = GetSharedLineMaterial();
        lr.startColor = lr.endColor = edgeColor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        _edges.Add(lr);
    }

    private void BuildVertices(Vector3[] verts)
    {
        // Dedup vertex theo position (mesh có thể trùng vertex do UV seam)
        var seen = new HashSet<long>();
        for (int i = 0; i < verts.Length; i++)
        {
            long key = HashPos(verts[i]);
            if (!seen.Add(key)) continue;

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = $"Vertex_{i}";
            dot.transform.SetParent(transform, false);
            dot.transform.localPosition = verts[i];
            dot.transform.localScale = Vector3.zero; // bắt đầu ẩn

            // Bỏ collider — chỉ là marker visual
            Destroy(dot.GetComponent<Collider>());

            // Material chung + propBlock để đổi màu rẻ
            var rend = dot.GetComponent<MeshRenderer>();
            rend.sharedMaterial = GetSharedDotMaterial();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, vertexColor);
            mpb.SetColor(ColorId, vertexColor);
            rend.SetPropertyBlock(mpb);

            _vertices.Add(dot.transform);
        }
    }

    private void BuildCenter(Vector3 localCenter)
    {
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        c.name = "CenterMarker";
        c.transform.SetParent(transform, false);
        c.transform.localPosition = localCenter;
        c.transform.localScale = Vector3.zero;
        Destroy(c.GetComponent<Collider>());

        _centerRend = c.GetComponent<MeshRenderer>();
        _centerRend.sharedMaterial = GetSharedDotMaterial();
        _centerRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _centerRend.receiveShadows = false;

        var mpb = new MaterialPropertyBlock();
        mpb.SetColor(BaseColorId, centerColor);
        mpb.SetColor(ColorId, centerColor);
        _centerRend.SetPropertyBlock(mpb);

        _center = c.transform;
    }

    // ═══════════════════════════════════════════════════════
    //  APPLY INTENSITY (gọi mỗi frame — KHÔNG cấp phát)
    // ═══════════════════════════════════════════════════════

    private void ApplyIntensity(float t)
    {
        t = Mathf.Clamp01(t);

        // 1. Alpha vỏ hình
        if (enableTransparency && _mat != null && _hasBaseColor)
        {
            float a = Mathf.Lerp(maxAlpha, minAlpha, t) * _origAlpha;
            Color c = _mat.GetColor(BaseColorId);
            c.a = a;
            _mat.SetColor(BaseColorId, c);
        }

        // 2. Cạnh — alpha fade
        if (enableEdges)
        {
            Color ec = edgeColor;
            ec.a = t;
            for (int i = 0; i < _edges.Count; i++)
            {
                var lr = _edges[i];
                if (lr == null) continue;
                lr.startColor = ec;
                lr.endColor = ec;
                lr.startWidth = lr.endWidth = edgeWidth * Mathf.Lerp(0.4f, 1f, t);
            }
        }

        // 3. Đỉnh — scale-in
        if (enableVertices)
        {
            float scale = vertexSize * t;
            for (int i = 0; i < _vertices.Count; i++)
            {
                if (_vertices[i] == null) continue;
                _vertices[i].localScale = new Vector3(scale, scale, scale);
            }
        }

        // 4. Tâm — scale + pulse
        if (enableCenter && _center != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmplitude;
            float s = centerSize * t * pulse;
            _center.localScale = new Vector3(s, s, s);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  UTILS
    // ═══════════════════════════════════════════════════════

    private float MinDistanceToHands()
    {
        Vector3 c = transform.TransformPoint(GetComponent<MeshFilter>().sharedMesh.bounds.center);
        float dL = leftHandTip  != null ? Vector3.Distance(leftHandTip.position,  c) : float.PositiveInfinity;
        float dR = rightHandTip != null ? Vector3.Distance(rightHandTip.position, c) : float.PositiveInfinity;
        return Mathf.Min(dL, dR);
    }

    private static void EnsureTransparentShader(Material mat)
    {
        if (mat == null) return;
        // URP Lit
        if (mat.HasProperty(SurfaceId))
        {
            mat.SetFloat(SurfaceId, 1); // 1 = Transparent
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    private static Material GetSharedLineMaterial()
    {
        if (s_lineMaterial != null) return s_lineMaterial;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        s_lineMaterial = new Material(sh) { color = Color.white, name = "ShapeStructurePreview_LineShared" };
        return s_lineMaterial;
    }

    private static Material GetSharedDotMaterial()
    {
        if (s_dotMaterial != null) return s_dotMaterial;
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        s_dotMaterial = new Material(sh) { name = "ShapeStructurePreview_DotShared" };
        return s_dotMaterial;
    }

    // Hash position thành long key (làm tròn 4 decimals để gom các đỉnh trùng do float)
    private static long HashPos(Vector3 p)
    {
        const int M = 10000;
        long x = (long)Mathf.RoundToInt(p.x * M);
        long y = (long)Mathf.RoundToInt(p.y * M);
        long z = (long)Mathf.RoundToInt(p.z * M);
        return (x * 73856093L) ^ (y * 19349663L) ^ (z * 83492791L);
    }
    private static long HashPosPair(Vector3 a, Vector3 b)
    {
        long ha = HashPos(a), hb = HashPos(b);
        return ha < hb ? (ha * 1000003L) ^ hb : (hb * 1000003L) ^ ha; // symmetric
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Vẽ 2 vòng tròn show/hide để debug bán kính
        Vector3 c = Application.isPlaying && GetComponent<MeshFilter>() != null
            ? transform.TransformPoint(GetComponent<MeshFilter>().sharedMesh.bounds.center)
            : transform.position;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(c, showDistance);

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(c, hideDistance);
    }
#endif
}
