using System.Collections;
using Gotchi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Standard press animation for every tappable element: shrink on touch, soft overshoot on release.
    public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float PressedScale = 0.94f;
        public const float OvershootScale = 1.03f;

        public System.Action<bool> OnPressedChanged;

        private Vector3 _base = Vector3.one;
        private bool _down;
        private Coroutine _routine;
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;
            if (_routine == null) _base = transform.localScale;
            _down = true;
            OnPressedChanged?.Invoke(true);
            Animate(_base * PressedScale, 0.07f, false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_down) return;
            _down = false;
            OnPressedChanged?.Invoke(false);
            Animate(_base * OvershootScale, 0.08f, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_down) return;
            _down = false;
            OnPressedChanged?.Invoke(false);
            Animate(_base, 0.1f, false);
        }

        private void Animate(Vector3 target, float duration, bool settle)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ScaleTo(target, duration, settle));
        }

        private IEnumerator ScaleTo(Vector3 target, float duration, bool settle)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, target, SimpleTween.EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            transform.localScale = target;
            if (settle)
            {
                start = target; elapsed = 0f;
                while (elapsed < 0.1f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    transform.localScale = Vector3.Lerp(start, _base, SimpleTween.EaseOutCubic(Mathf.Clamp01(elapsed / 0.1f)));
                    yield return null;
                }
                transform.localScale = _base;
            }
            _routine = null;
        }
    }
}
