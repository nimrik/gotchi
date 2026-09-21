using Gotchi.Creature;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // The health and mana bar: a row of slanted blocks, ONE BLOCK PER 250 POINTS, with a 2 px gap between them.
    // The whole bar keeps its width whatever the maximum is, so as the cat grows the blocks get narrower, not the
    // bar longer. 1200 max = four full blocks and a fifth that is 4/5 as wide. A block the value reaches is filled;
    // one it does not reach is only outlined; the block the value ends in is filled part of the way. The inside of
    // an empty block is white, and a white strip runs under the whole row, so the gaps are white separators.
    // Drawn as an anti-aliased vector mesh (VectorMesh feathers every edge), so the slanted sides stay smooth.
    public class SegmentedBar : MaskableGraphic
    {
        public const float UnitsPerBlock = 250f;
        private const float Gap = 2f, Outline = 2.5f;

        public float Skew = 10f;                 // how far the top edge leans to the right of the bottom edge
        public Color FillColor = Color.red, OutlineColor = Color.black, EmptyColor = Color.white;

        private float _value = 1000f, _max = 1000f;
        private readonly VectorMesh _mesh = new VectorMesh { Feather = 1.1f };

        // The layout rule on its own, so the smoke test can check it without a canvas.
        // How many blocks a maximum needs: 1000 → 4, 1200 → 5.
        public static int BlockCount(float max) => Mathf.Max(1, Mathf.CeilToInt(max / UnitsPerBlock - 0.0001f));
        // The width of block `index` as a share of a full block: 1 for every block but the last, which holds what is left (1200 → 0.8).
        public static float BlockShare(float max, int index) => Mathf.Clamp01((max - index * UnitsPerBlock) / UnitsPerBlock);
        // How much of block `index` is filled: 1 = full, 0 = only outlined.
        public static float BlockFill(float value, float max, int index)
        {
            float capacity = Mathf.Min(UnitsPerBlock, max - index * UnitsPerBlock);
            return capacity <= 0f ? 0f : Mathf.Clamp01((value - index * UnitsPerBlock) / capacity);
        }

        public static SegmentedBar Create(string name, Transform parent, Color fill, Color outline)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var bar = go.AddComponent<SegmentedBar>();
            bar.FillColor = fill;
            bar.OutlineColor = outline;
            bar.raycastTarget = false;
            return bar;
        }

        public void Set(float value, float max)
        {
            max = Mathf.Max(1f, max);
            value = Mathf.Clamp(value, 0f, max);
            if (Mathf.Approximately(value, _value) && Mathf.Approximately(max, _max)) return;
            _value = value; _max = max;
            SetVerticesDirty();
        }

        public void SetColors(Color fill, Color outline)
        {
            if (fill == FillColor && outline == OutlineColor) return;
            FillColor = fill; OutlineColor = outline;
            SetVerticesDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (r.width <= Skew + 8f || r.height <= 4f) return;

            float units = _max / UnitsPerBlock;                              // 1200 → 4.8 blocks' worth of width
            int blocks = BlockCount(_max);
            float unitWidth = (r.width - Skew - Gap * (blocks - 1)) / units;  // the width one full block gets
            _mesh.Begin(vh);

            // A white strip under the whole row, a pixel inside the blocks' outer edge, so the 2 px gaps between the
            // blocks are white separators whatever the bar sits on.
            Slant(r, r.xMin + 1.5f, r.xMax - Skew - 1.5f, r.yMin + 1f, r.yMax - 1f, EmptyColor);

            float x = r.xMin;
            for (int i = 0; i < blocks; i++)
            {
                float width = unitWidth * BlockShare(_max, i);
                Block(r, x, width, BlockFill(_value, _max, i));
                x += width + Gap;
            }
        }

        private void Block(Rect r, float x, float width, float filled)
        {
            Slant(r, x, x + width, r.yMin, r.yMax, OutlineColor);
            float inset = Mathf.Min(Outline, r.height * 0.3f);
            float insetX = inset * 1.1f;                                     // the slanted sides are a little longer than they are thick
            float x0 = x + insetX, x1 = x + width - insetX;
            if (x1 - x0 < 1f) return;                                        // a sliver of a block: outline colour only
            Slant(r, x0, x1, r.yMin + inset, r.yMax - inset, EmptyColor);
            if (filled > 0.001f) Slant(r, x0, Mathf.Lerp(x0, x1, filled), r.yMin + inset, r.yMax - inset, FillColor);
        }

        // A parallelogram between x0 and x1 (measured along the bar's bottom edge) from yBottom to yTop, leaning right.
        private void Slant(Rect r, float x0, float x1, float yBottom, float yTop, Color color)
        {
            float lowShift = Skew * (yBottom - r.yMin) / r.height, highShift = Skew * (yTop - r.yMin) / r.height;
            var poly = _mesh.Poly();
            poly.Add(new Vector2(x0 + lowShift, yBottom));
            poly.Add(new Vector2(x1 + lowShift, yBottom));
            poly.Add(new Vector2(x1 + highShift, yTop));
            poly.Add(new Vector2(x0 + highShift, yTop));
            _mesh.Fill(poly, color);
        }
    }
}
