using UnityEngine;

namespace Gotchi.UI
{
    // Small software rasteriser for illustrated backdrops: anti-aliased signed-distance shapes with
    // vertical gradients, soft shadows and an optional clip rect. Coordinates are canvas units (y up);
    // the texture is rendered at `Scale` of that and drawn bilinear, so edges stay smooth, not pixelated.
    public sealed class ScenePainter
    {
        public readonly int Width, Height;
        public readonly float Scale;
        private readonly Color[] _px;
        private Rect? _clip;

        public ScenePainter(float canvasWidth, float canvasHeight, float scale, Color background)
        {
            Scale = scale;
            Width = Mathf.RoundToInt(canvasWidth * scale);
            Height = Mathf.RoundToInt(canvasHeight * scale);
            _px = new Color[Width * Height];
            for (int i = 0; i < _px.Length; i++) _px[i] = background;
        }

        public void SetClip(float x, float y, float w, float h) => _clip = new Rect(x * Scale, y * Scale, w * Scale, h * Scale);
        public void ClearClip() => _clip = null;

        // ---- shapes (all in canvas units) ----

        public void Rect(float x, float y, float w, float h, Color color) => RoundedRect(x, y, w, h, 0f, color, color);
        public void RoundedRect(float x, float y, float w, float h, float r, Color color) => RoundedRect(x, y, w, h, r, color, color);

        public void RoundedRect(float x, float y, float w, float h, float r, Color top, Color bottom, float soft = 1f)
        {
            float s = Scale;
            float cx = (x + w * 0.5f) * s, cy = (y + h * 0.5f) * s, hx = w * 0.5f * s, hy = h * 0.5f * s, rr = Mathf.Min(r * s, Mathf.Min(hx, hy));
            float pad = soft * s + 2f;
            Paint(cx - hx - pad, cy - hy - pad, cx + hx + pad, cy + hy + pad, soft * s, (px, py) =>
            {
                float qx = Mathf.Abs(px - cx) - (hx - rr), qy = Mathf.Abs(py - cy) - (hy - rr);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - rr;
            }, py => Color.Lerp(bottom, top, Mathf.Clamp01((py - (cy - hy)) / Mathf.Max(1f, 2f * hy))));
        }

        public void Circle(float cx, float cy, float r, Color color, float soft = 1f) => Ellipse(cx, cy, r, r, 0f, color, color, soft);
        public void Ellipse(float cx, float cy, float rx, float ry, float rotation, Color color, float soft = 1f) => Ellipse(cx, cy, rx, ry, rotation, color, color, soft);

        public void Ellipse(float cx, float cy, float rx, float ry, float rotation, Color top, Color bottom, float soft = 1f)
        {
            float s = Scale;
            float ccx = cx * s, ccy = cy * s, rrx = rx * s, rry = ry * s, m = Mathf.Min(rrx, rry);
            float cos = Mathf.Cos(rotation * Mathf.Deg2Rad), sin = Mathf.Sin(rotation * Mathf.Deg2Rad);
            float reach = Mathf.Max(rrx, rry) + soft * s + 2f;
            Paint(ccx - reach, ccy - reach, ccx + reach, ccy + reach, soft * s, (px, py) =>
            {
                float dx = px - ccx, dy = py - ccy;
                float lx = dx * cos + dy * sin, ly = -dx * sin + dy * cos;
                float k = Mathf.Sqrt(lx * lx / (rrx * rrx) + ly * ly / (rry * rry));
                return (k - 1f) * m;
            }, py => Color.Lerp(bottom, top, Mathf.Clamp01((py - (ccy - rry)) / Mathf.Max(1f, 2f * rry))));
        }

        public void Line(float x0, float y0, float x1, float y1, float width, Color color, float soft = 1f)
        {
            float s = Scale;
            float ax = x0 * s, ay = y0 * s, bx = x1 * s, by = y1 * s, hw = width * 0.5f * s;
            float pad = hw + soft * s + 2f;
            Paint(Mathf.Min(ax, bx) - pad, Mathf.Min(ay, by) - pad, Mathf.Max(ax, bx) + pad, Mathf.Max(ay, by) + pad, soft * s, (px, py) =>
            {
                float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
                float h = Mathf.Clamp01((pax * bax + pay * bay) / Mathf.Max(0.0001f, bax * bax + bay * bay));
                float dx = pax - bax * h, dy = pay - bay * h;
                return Mathf.Sqrt(dx * dx + dy * dy) - hw;
            }, _ => color);
        }

        // Soft drop shadow: a rounded rect blurred by `blur` canvas units.
        public void Shadow(float x, float y, float w, float h, float r, float blur, float alpha) =>
            RoundedRect(x, y, w, h, r, new Color(0.35f, 0.2f, 0.25f, alpha), new Color(0.35f, 0.2f, 0.25f, alpha), blur);

        public void ShadowEllipse(float cx, float cy, float rx, float ry, float blur, float alpha) =>
            Ellipse(cx, cy, rx, ry, 0f, new Color(0.35f, 0.2f, 0.25f, alpha), blur);

        // ---- core ----

        private void Paint(float minX, float minY, float maxX, float maxY, float soft, System.Func<float, float, float> sdf, System.Func<float, Color> colorAt)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX)), y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(maxX)), y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(maxY));
            if (_clip.HasValue)
            {
                var c = _clip.Value;
                x0 = Mathf.Max(x0, Mathf.FloorToInt(c.xMin)); y0 = Mathf.Max(y0, Mathf.FloorToInt(c.yMin));
                x1 = Mathf.Min(x1, Mathf.CeilToInt(c.xMax) - 1); y1 = Mathf.Min(y1, Mathf.CeilToInt(c.yMax) - 1);
            }
            float edge = Mathf.Max(0.75f, soft);
            for (int y = y0; y <= y1; y++)
            {
                float py = y + 0.5f;
                Color col = colorAt(py);
                for (int x = x0; x <= x1; x++)
                {
                    float d = sdf(x + 0.5f, py);
                    float cover = 1f - Mathf.Clamp01((d + edge * 0.5f) / edge);
                    if (cover <= 0f) continue;
                    float a = cover * col.a;
                    int i = y * Width + x;
                    var dst = _px[i];
                    _px[i] = new Color(col.r * a + dst.r * (1f - a), col.g * a + dst.g * (1f - a), col.b * a + dst.b * (1f - a), a + dst.a * (1f - a));
                }
            }
        }

        public Sprite ToSprite()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(_px);
            texture.Apply(false, true);
            return Sprite.Create(texture, new UnityEngine.Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
