// === WireframeController.cs ===
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace AntiGravity.Interaction
{
    public class WireframeController : MonoBehaviour
    {
        public Material lineMaterial;
        public GameObject labelPrefab;

        private List<LineRenderer> _activeLines = new List<LineRenderer>();
        private List<GameObject> _activeLabels = new List<GameObject>();
        private GeometryObject _currentGeo;

        public void ShowWireframe(GeometryObject geo)
        {
            Clear();
            _currentGeo = geo;
            if (geo == null) return;
            geo.Select();
            geo.SetTransparency(true);
            DrawGeometryDetails(geo);
        }

        public void HideWireframe()
        {
            if (_currentGeo != null)
            {
                _currentGeo.SetTransparency(false);
                _currentGeo.Deselect();
            }
            Clear();
        }

        private void DrawGeometryDetails(GeometryObject geo)
        {
            Vector3 center = geo.transform.position;
            float r = geo.radius;
            float h = geo.height;
            Vector3 top = center + Vector3.up * h;

            CreateLabel("O", center);

            switch (geo.shapeType)
            {
                case GeometryShapeType.Pyramid:
                    DrawPyramid(center, r, h);
                    break;
                case GeometryShapeType.Cone:
                    DrawCone(center, r, h);
                    break;
                case GeometryShapeType.Cube:
                    DrawCube(center, r, h); // FIX: Vẽ Cube với nhãn a/b/c và đường chéo (Lỗi 2b)
                    break;
                case GeometryShapeType.Sphere:
                    DrawSphere(center, r); // FIX: Vẽ Sphere với 3 vòng tròn và nhãn r (Lỗi 2b)
                    break;
                case GeometryShapeType.Cylinder:
                    DrawCylinder(center, r, h); // FIX: Vẽ Cylinder với 2 đáy, 4 trụ và nhãn r/h (Lỗi 2b)
                    break;
            }
        }

        private void DrawPyramid(Vector3 c, float r, float h)
        {
            Vector3 S = c + Vector3.up * h;
            Vector3 A = c + new Vector3(-r, 0, -r);
            Vector3 B = c + new Vector3(r, 0, -r);
            Vector3 C = c + new Vector3(r, 0, r);
            Vector3 D = c + new Vector3(-r, 0, r);
            CreateLine(new Vector3[] { A, B, C, D, A }, Color.white);
            CreateLine(new Vector3[] { S, A }, Color.white); CreateLine(new Vector3[] { S, B }, Color.white);
            CreateLine(new Vector3[] { S, C }, Color.white); CreateLine(new Vector3[] { S, D }, Color.white);
            CreateLine(new Vector3[] { S, c }, Color.red);
            CreateLabel("S", S); CreateLabel("h", (S + c) / 2); CreateLabel("a", (A + B) / 2);
        }

        private void DrawCube(Vector3 c, float r, float h)
        {
            float hh = h * 0.5f; // FIX-2: Tính toán lại để bao quanh center đúng cách
            Vector3 A = c + new Vector3(-r, -hh, -r);
            Vector3 B = c + new Vector3( r, -hh, -r);
            Vector3 C = c + new Vector3( r, -hh,  r);
            Vector3 D = c + new Vector3(-r, -hh,  r);
            Vector3 E = c + new Vector3(-r,  hh, -r);
            Vector3 F = c + new Vector3( r,  hh, -r);
            Vector3 G = c + new Vector3( r,  hh,  r);
            Vector3 H = c + new Vector3(-r,  hh,  r);

            // 4 cạnh đáy dưới
            CreateLine(new Vector3[] { A, B, C, D, A }, Color.white);
            // 4 cạnh đáy trên
            CreateLine(new Vector3[] { E, F, G, H, E }, Color.white);
            // 4 cạnh dọc
            CreateLine(new Vector3[] { A, E }, Color.white);
            CreateLine(new Vector3[] { B, F }, Color.white);
            CreateLine(new Vector3[] { C, G }, Color.white);
            CreateLine(new Vector3[] { D, H }, Color.white);
            // Đường chéo đáy
            CreateLine(new Vector3[] { A, C }, Color.yellow);
            // Nhãn
            CreateLabel("a", (A + B) / 2);
            CreateLabel("b", (B + C) / 2);
            CreateLabel("c", (A + E) / 2);
        }

        private void DrawSphere(Vector3 c, float r)
        {
            // 3 vòng tròn vuông góc
            DrawCircle(c, r, Vector3.up);
            DrawCircle(c, r, Vector3.right);
            DrawCircle(c, r, Vector3.forward);
            // FIX: Đường bán kính và nhãn r (Lỗi 2b)
            CreateLine(new Vector3[] { c, c + Vector3.right * r }, Color.blue);
            CreateLabel("r", c + Vector3.right * r / 2);
        }

        private void DrawCylinder(Vector3 c, float r, float h)
        {
            Vector3 top = c + Vector3.up * h;
            // 2 vòng tròn đáy
            DrawCircle(c, r, Vector3.up);
            DrawCircle(top, r, Vector3.up);
            // 4 đường trụ đứng
            for (int i = 0; i < 4; i++) {
                float ang = i * 90 * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * r;
                CreateLine(new Vector3[] { c + dir, top + dir }, Color.white);
            }
            // FIX: Nhãn r và h (Lỗi 2b)
            CreateLine(new Vector3[] { top, c }, Color.red);
            CreateLabel("r", c + Vector3.right * r / 2); 
            CreateLabel("h", (top + c) / 2);
        }

        private void DrawCone(Vector3 c, float r, float h)
        {
            Vector3 S = c + Vector3.up * h;
            DrawCircle(c, r, Vector3.up);
            CreateLine(new Vector3[] { S, c }, Color.red);
            CreateLine(new Vector3[] { c, c + Vector3.right * r }, Color.blue);
            CreateLabel("S", S); CreateLabel("h", (S + c) / 2); CreateLabel("r", c + Vector3.right * r / 2);
        }

        private void DrawCircle(Vector3 center, float radius, Vector3 normal)
        {
            int segments = 32;
            Vector3[] points = new Vector3[segments + 1];
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
            for (int i = 0; i <= segments; i++)
            {
                float ang = i * (360f / segments) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(ang) * radius, 0, Mathf.Sin(ang) * radius);
                points[i] = center + rot * p;
            }
            CreateLine(points, Color.white);
        }

        private void CreateLine(Vector3[] p, Color clr)
        {
            GameObject obj = new GameObject("Line"); obj.transform.SetParent(transform);
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startWidth = lr.endWidth = 0.005f;
            lr.positionCount = p.Length; lr.SetPositions(p);
            lr.startColor = lr.endColor = clr; lr.useWorldSpace = true;
            _activeLines.Add(lr);
        }

        private void CreateLabel(string t, Vector3 p)
        {
            if (labelPrefab == null) return;
            GameObject l = Instantiate(labelPrefab, p, Quaternion.identity, transform);
            l.GetComponent<TextMeshPro>().text = t;
            l.AddComponent<BillboardLabel>();
            _activeLabels.Add(l);
        }

        private void Clear()
        {
            foreach (var l in _activeLines) if (l != null) Destroy(l.gameObject);
            foreach (var lb in _activeLabels) if (lb != null) Destroy(lb.gameObject);
            _activeLines.Clear(); _activeLabels.Clear();
        }
    }

    // BillboardLabel đã có sẵn trong Math/BillboardLabel.cs — không duplicate ở đây
}
