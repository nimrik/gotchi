using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Keeps a sliced RoundedSprite image fully round-ended: radius = half the current height.
    [ExecuteAlways]
    public class PillRadius : MonoBehaviour
    {
        private Image _image;
        private RectTransform _rect;
        private float _lastHeight = -1f;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
            Apply();
        }

        private void OnEnable() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_image == null || _rect == null) return;
            float height = _rect.rect.height;
            if (height <= 0f || Mathf.Approximately(height, _lastHeight)) return;
            _lastHeight = height;
            _image.pixelsPerUnitMultiplier = UIFactory.Radius.Base / Mathf.Max(1f, height / 2f);
        }
    }
}
