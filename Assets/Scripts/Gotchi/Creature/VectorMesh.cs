using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.Creature
{
    // Emits anti-aliased vector shapes straight into a uGUI VertexHelper. Every edge gets a thin feather strip
    // whose alpha fades to zero, so shapes stay smooth at any size without MSAA or textures. Coordinates are
    // canvas units; polygons are lists of points (any winding — it is fixed on the way in).
    public sealed class VectorMesh
    {
        private VertexHelper _vh;
        public float Feather = 1.5f;

        private readonly List<Vector2> _normals = new List<Vector2>(256);
        private readonly List<Vector2> _scratch = new List<Vector2>(256);

        // Pooled point lists so a full creature rebuild allocates nothing after the first frame.
        private readonly List<List<Vector2>> _pool = new List<List<Vector2>>();
        private int _poolIndex;

        public void Begin(VertexHelper vh)
        {
            _vh = vh;
            _poolIndex = 0;
        }

        public List<Vector2> Poly()
        {
            if (_poolIndex == _pool.Count) _pool.Add(new List<Vector2>(64));
            var list = _pool[_poolIndex++];
            list.Clear();
            return list;
        }

        private int Vert(Vector2 p, Color c)
        {
            _vh.AddVert(new Vector3(p.x, p.y, 0f), c, Vector4.zero);
            return _vh.currentVertCount - 1;
        }

        // ---- fills ----

        // Star-shaped polygon (every vertex visible from its centroid) filled as a fan with a feathered edge.
        public void Fill(List<Vector2> poly, Color color)
        {
            int n = poly.Count;
            if (n < 3 || color.a <= 0.002f) return;
            EnsureCcw(poly);
            Vector2 c = Centroid(poly);
            float minR = float.MaxValue;
            for (int i = 0; i < n; i++) minR = Mathf.Min(minR, (poly[i] - c).sqrMagnitude);
            minR = Mathf.Sqrt(minR);
            float f = Mathf.Min(Feather, minR * 0.8f);
            if (f < 0.01f) f = 0.01f;
            ComputeNormals(poly, _normals);
            Color clear = color; clear.a = 0f;

            int ci = Vert(c, color);
            int inner = _vh.currentVertCount;
            for (int i = 0; i < n; i++) Vert(poly[i] - _normals[i] * (f * 0.5f), color);
            int outer = _vh.currentVertCount;
            for (int i = 0; i < n; i++) Vert(poly[i] + _normals[i] * (f * 0.5f), clear);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                _vh.AddTriangle(ci, inner + i, inner + j);
                _vh.AddTriangle(inner + i, outer + i, outer + j);
                _vh.AddTriangle(inner + i, outer + j, inner + j);
            }
        }

        // Fill with an outline band of the given width outside the polygon edge.
        public void Outlined(List<Vector2> poly, Color fill, Color outline, float width)
        {
            if (width > 0.01f && outline.a > 0.002f)
            {
                var expanded = Poly();
                Expand(poly, width, expanded);
                Fill(expanded, outline);
            }
            Fill(poly, fill);
        }

        // Radial gradient disc: solid at the centre fading to nothing at the rim (shadows, glows, airbrush blush).
        public void SoftDisc(Vector2 center, float rx, float ry, Color color, int segments = 28)
        {
            if (color.a <= 0.002f) return;
            Color clear = color; clear.a = 0f;
            int ci = Vert(center, color);
            int ring = _vh.currentVertCount;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vert(center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), clear);
            }
            for (int i = 0; i < segments; i++) _vh.AddTriangle(ci, ring + i, ring + (i + 1) % segments);
        }

        // ---- strokes ----

        // Polyline with round caps and a feathered edge. Colours should be opaque (caps overlap the body).
        public void Stroke(List<Vector2> path, float width, Color color, bool roundCaps = true)
        {
            int n = path.Count;
            if (n < 2 || color.a <= 0.002f || width <= 0.01f) return;
            float half = width * 0.5f;
            float f = Mathf.Min(Feather, half * 0.9f);
            Color clear = color; clear.a = 0f;

            _scratch.Clear();
            for (int i = 0; i < n; i++)
            {
                Vector2 t;
                if (i == 0) t = path[1] - path[0];
                else if (i == n - 1) t = path[n - 1] - path[n - 2];
                else t = (path[i] - path[i - 1]).normalized + (path[i + 1] - path[i]).normalized;
                if (t.sqrMagnitude < 1e-8f) t = Vector2.right;
                t.Normalize();
                _scratch.Add(new Vector2(-t.y, t.x));
            }

            int first = _vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                Vector2 nrm = _scratch[i];
                Vert(path[i] + nrm * (half + f * 0.5f), clear);
                Vert(path[i] + nrm * (half - f * 0.5f), color);
                Vert(path[i] - nrm * (half - f * 0.5f), color);
                Vert(path[i] - nrm * (half + f * 0.5f), clear);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int a = first + i * 4, b = first + (i + 1) * 4;
                Quad(a, a + 1, b + 1, b);
                Quad(a + 1, a + 2, b + 2, b + 1);
                Quad(a + 2, a + 3, b + 3, b + 2);
            }
            if (roundCaps)
            {
                var cap = Poly();
                Ellipse(path[0], half, half, 14, cap);
                Fill(cap, color);
                var cap2 = Poly();
                Ellipse(path[n - 1], half, half, 14, cap2);
                Fill(cap2, color);
            }
        }

        public void OutlinedStroke(List<Vector2> path, float width, Color fill, Color outline, float outlineWidth)
        {
            Stroke(path, width + outlineWidth * 2f, outline);
            Stroke(path, width, fill);
        }


        // ---- shaded fills (per-vertex colour from a callback, concentric rings for smooth gradients) ----

        public delegate Color ColorAt(Vector2 p);
        public delegate Color SideColorAt(Vector2 p, Vector2 side);
        private static readonly float[] Rings = { 0.3f, 0.6f, 0.85f };

        public void FillShaded(List<Vector2> poly, ColorAt colorAt, float feather = -1f)
        {
            int n = poly.Count;
            if (n < 3) return;
            EnsureCcw(poly);
            Vector2 c = Centroid(poly);
            float minR = float.MaxValue;
            for (int i = 0; i < n; i++) minR = Mathf.Min(minR, (poly[i] - c).sqrMagnitude);
            minR = Mathf.Sqrt(minR);
            float f = Mathf.Min(feather > 0f ? feather : Feather, minR * 0.8f);
            if (f < 0.01f) f = 0.01f;
            ComputeNormals(poly, _normals);

            int ci = Vert(c, colorAt(c));
            int prev = -1;
            for (int r = 0; r < Rings.Length; r++)
            {
                int ring = _vh.currentVertCount;
                for (int i = 0; i < n; i++) { Vector2 v = c + (poly[i] - c) * Rings[r]; Vert(v, colorAt(v)); }
                if (r == 0) for (int i = 0; i < n; i++) _vh.AddTriangle(ci, ring + i, ring + (i + 1) % n);
                else Quads(prev, ring, n);
                prev = ring;
            }
            int inner = _vh.currentVertCount;
            for (int i = 0; i < n; i++) Vert(poly[i] - _normals[i] * (f * 0.5f), colorAt(poly[i]));
            Quads(prev, inner, n);
            int outer = _vh.currentVertCount;
            for (int i = 0; i < n; i++) { Color cc = colorAt(poly[i]); cc.a = 0f; Vert(poly[i] + _normals[i] * (f * 0.5f), cc); }
            Quads(inner, outer, n);
        }

        public void OutlinedShaded(List<Vector2> poly, ColorAt colorAt, Color outline, float width, float feather = -1f)
        {
            if (width > 0.01f && outline.a > 0.002f)
            {
                var expanded = Poly();
                Expand(poly, width, expanded);
                Fill(expanded, outline);
            }
            FillShaded(poly, colorAt, feather);
        }

        private void Quads(int a, int b, int n)
        {
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                _vh.AddTriangle(a + i, b + i, b + j);
                _vh.AddTriangle(a + i, b + j, a + j);
            }
        }

        // Stroke whose colour depends on which side of the centre line a vertex lies (cylindrical shading).
        public void StrokeShaded(List<Vector2> path, float width, SideColorAt colorAt, bool roundCaps = true)
        {
            int n = path.Count;
            if (n < 2 || width <= 0.01f) return;
            float half = width * 0.5f;
            float f = Mathf.Min(Feather, half * 0.9f);
            _scratch.Clear();
            for (int i = 0; i < n; i++)
            {
                Vector2 t;
                if (i == 0) t = path[1] - path[0];
                else if (i == n - 1) t = path[n - 1] - path[n - 2];
                else t = (path[i] - path[i - 1]).normalized + (path[i + 1] - path[i]).normalized;
                if (t.sqrMagnitude < 1e-8f) t = Vector2.right;
                t.Normalize();
                _scratch.Add(new Vector2(-t.y, t.x));
            }
            int first = _vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                Vector2 nrm = _scratch[i];
                Color l = colorAt(path[i], nrm), r = colorAt(path[i], -nrm);
                Color lc = l; lc.a = 0f; Color rc = r; rc.a = 0f;
                Vert(path[i] + nrm * (half + f * 0.5f), lc);
                Vert(path[i] + nrm * (half - f * 0.5f), l);
                Vert(path[i] - nrm * (half - f * 0.5f), r);
                Vert(path[i] - nrm * (half + f * 0.5f), rc);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int a = first + i * 4, b = first + (i + 1) * 4;
                Quad(a, a + 1, b + 1, b);
                Quad(a + 1, a + 2, b + 2, b + 1);
                Quad(a + 2, a + 3, b + 3, b + 2);
            }
            if (roundCaps)
            {
                var cap = Poly();
                Ellipse(path[0], half, half, 14, cap);
                Fill(cap, colorAt(path[0], Vector2.zero));
                var cap2 = Poly();
                Ellipse(path[n - 1], half, half, 14, cap2);
                Fill(cap2, colorAt(path[n - 1], Vector2.zero));
            }
        }

        private void Quad(int a, int b, int c, int d)
        {
            _vh.AddTriangle(a, b, c);
            _vh.AddTriangle(a, c, d);
        }

        // ---- geometry helpers ----

        public static Vector2 Centroid(List<Vector2> poly)
        {
            Vector2 c = Vector2.zero;
            for (int i = 0; i < poly.Count; i++) c += poly[i];
            return c / Mathf.Max(1, poly.Count);
        }

        public static float SignedArea(List<Vector2> poly)
        {
            float a = 0f;
            for (int i = 0, n = poly.Count; i < n; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % n];
                a += p.x * q.y - q.x * p.y;
            }
            return a * 0.5f;
        }

        public static void EnsureCcw(List<Vector2> poly)
        {
            if (SignedArea(poly) < 0f) poly.Reverse();
        }

        // Outward unit vertex normals with a clamped miter so corners keep their shape.
        private static void ComputeNormals(List<Vector2> poly, List<Vector2> normals)
        {
            normals.Clear();
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = poly[(i - 1 + n) % n], cur = poly[i], next = poly[(i + 1) % n];
                Vector2 e0 = cur - prev, e1 = next - cur;
                Vector2 n0 = new Vector2(e0.y, -e0.x).normalized, n1 = new Vector2(e1.y, -e1.x).normalized;
                Vector2 v = (n0 + n1);
                if (v.sqrMagnitude < 1e-6f) v = n1;
                v.Normalize();
                float cos = Mathf.Max(0.5f, Vector2.Dot(v, n1));
                normals.Add(v / cos);
            }
        }

        public void Expand(List<Vector2> poly, float amount, List<Vector2> result)
        {
            EnsureCcw(poly);
            ComputeNormals(poly, _normals);
            result.Clear();
            for (int i = 0; i < poly.Count; i++) result.Add(poly[i] + _normals[i] * amount);
        }

        public static void Ellipse(Vector2 c, float rx, float ry, int segments, List<Vector2> result, float rotationDeg = 0f)
        {
            result.Clear();
            float cr = Mathf.Cos(rotationDeg * Mathf.Deg2Rad), sr = Mathf.Sin(rotationDeg * Mathf.Deg2Rad);
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * rx, y = Mathf.Sin(a) * ry;
                result.Add(c + new Vector2(x * cr - y * sr, x * sr + y * cr));
            }
        }

        // Ellipse pinched toward a point at the top: pinch 0 = ellipse, 1 = teardrop. Base is at y=0, tip at 2*ry.
        public static void Leaf(float rx, float ry, float pinch, int segments, List<Vector2> result)
        {
            result.Clear();
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float s = Mathf.Sin(a);
                float x = Mathf.Cos(a) * rx * (1f - pinch * Mathf.Max(0f, s) * Mathf.Max(0f, s));
                result.Add(new Vector2(x, ry * (s + 1f)));
            }
        }

        public static void Capsule(Vector2 a, Vector2 b, float radius, int segments, List<Vector2> result)
        {
            result.Clear();
            Vector2 d = (b - a);
            float ang = Mathf.Atan2(d.y, d.x);
            int half = Mathf.Max(3, segments / 2);
            for (int i = 0; i <= half; i++)
            {
                float t = ang - Mathf.PI * 0.5f + Mathf.PI * i / half;
                result.Add(b + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * radius);
            }
            for (int i = 0; i <= half; i++)
            {
                float t = ang + Mathf.PI * 0.5f + Mathf.PI * i / half;
                result.Add(a + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * radius);
            }
        }

        public static void Arc(Vector2 c, float rx, float ry, float fromDeg, float toDeg, int segments, List<Vector2> result)
        {
            result.Clear();
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)segments) * Mathf.Deg2Rad;
                result.Add(c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry));
            }
        }

        public static void Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int segments, List<Vector2> result, bool append = false)
        {
            if (!append) result.Clear();
            for (int i = append ? 1 : 0; i <= segments; i++)
            {
                float t = i / (float)segments, u = 1f - t;
                result.Add(u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3);
            }
        }

        public static void Quadratic(Vector2 p0, Vector2 p1, Vector2 p2, int segments, List<Vector2> result, bool append = false)
        {
            if (!append) result.Clear();
            for (int i = append ? 1 : 0; i <= segments; i++)
            {
                float t = i / (float)segments, u = 1f - t;
                result.Add(u * u * p0 + 2f * u * t * p1 + t * t * p2);
            }
        }

        public static void Heart(Vector2 c, float size, int segments, List<Vector2> result)
        {
            result.Clear();
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments * Mathf.PI * 2f;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                result.Add(c + new Vector2(x, y) * (size / 16f));
            }
        }

        public static void Star(Vector2 c, float outer, float inner, int points, List<Vector2> result, float rotationDeg = 0f)
        {
            result.Clear();
            for (int i = 0; i < points * 2; i++)
            {
                float a = (rotationDeg + 90f) * Mathf.Deg2Rad + i * Mathf.PI / points;
                float r = (i % 2 == 0) ? outer : inner;
                result.Add(c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
        }

        // Sutherland–Hodgman: subject clipped by a convex polygon. Result is convex if the subject is.
        public void Clip(List<Vector2> subject, List<Vector2> convexClip, List<Vector2> result)
        {
            EnsureCcw(convexClip);
            result.Clear();
            result.AddRange(subject);
            var input = Poly();
            int m = convexClip.Count;
            for (int e = 0; e < m && result.Count > 0; e++)
            {
                Vector2 a = convexClip[e], b = convexClip[(e + 1) % m];
                input.Clear();
                input.AddRange(result);
                result.Clear();
                int n = input.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector2 cur = input[i], prev = input[(i - 1 + n) % n];
                    bool curIn = Side(a, b, cur) >= 0f, prevIn = Side(a, b, prev) >= 0f;
                    if (curIn)
                    {
                        if (!prevIn) result.Add(Intersect(prev, cur, a, b));
                        result.Add(cur);
                    }
                    else if (prevIn) result.Add(Intersect(prev, cur, a, b));
                }
            }
        }

        private static float Side(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

        private static Vector2 Intersect(Vector2 p, Vector2 q, Vector2 a, Vector2 b)
        {
            Vector2 r = q - p, s = b - a;
            float denom = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(denom) < 1e-8f) return p;
            float t = ((a.x - p.x) * s.y - (a.y - p.y) * s.x) / denom;
            return p + r * Mathf.Clamp01(t);
        }

        public static bool Contains(List<Vector2> poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Vector2 a = poly[i], b = poly[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        public static void HalfPlaneAbove(Vector2 point, float tiltDeg, float extent, List<Vector2> result)
        {
            // A big rectangle whose bottom edge is the line through `point` rotated by tilt: clip against it to keep
            // everything above the line.
            result.Clear();
            Vector2 dir = new Vector2(Mathf.Cos(tiltDeg * Mathf.Deg2Rad), Mathf.Sin(tiltDeg * Mathf.Deg2Rad));
            Vector2 up = new Vector2(-dir.y, dir.x);
            result.Add(point - dir * extent);
            result.Add(point + dir * extent);
            result.Add(point + dir * extent + up * extent);
            result.Add(point - dir * extent + up * extent);
        }
    }
}
