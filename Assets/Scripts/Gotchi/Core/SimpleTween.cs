using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.Core
{
    // Coroutine-based tweens. Start them from any MonoBehaviour: StartCoroutine(SimpleTween.PunchScale(t)).
    public static class SimpleTween
    {
        // Rest poses and cancellation tokens so repeated tweens never compound (each new tween on a
        // transform starts from its rest pose and cancels the previous one).
        private static readonly Dictionary<Transform, int> Tokens = new Dictionary<Transform, int>();
        private static readonly Dictionary<Transform, Vector3> RestScales = new Dictionary<Transform, Vector3>();
        private static readonly Dictionary<Transform, Vector2> RestPositions = new Dictionary<Transform, Vector2>();

        private static int Begin(Transform t)
        {
            Tokens.TryGetValue(t, out int token);
            Tokens[t] = ++token;
            if (Tokens.Count > 512) Prune();
            return token;
        }

        private static bool Active(Transform t, int token) => t != null && Tokens.TryGetValue(t, out int current) && current == token;

        public static Vector3 RestScale(Transform t)
        {
            if (!RestScales.TryGetValue(t, out Vector3 scale)) { scale = t.localScale; RestScales[t] = scale; }
            return scale;
        }

        public static void SetRestScale(Transform t, Vector3 scale) => RestScales[t] = scale;

        // Drop entries for transforms that have been destroyed (Unity's fake-null) so the registry stays small.
        private static void Prune()
        {
            var dead = new List<Transform>();
            foreach (var key in Tokens.Keys) if (key == null) dead.Add(key);
            foreach (var key in dead) { Tokens.Remove(key); RestScales.Remove(key); RestPositions.Remove(key); }
        }

        private static Vector2 RestPosition(RectTransform t)
        {
            if (!RestPositions.TryGetValue(t, out Vector2 position)) { position = t.anchoredPosition; RestPositions[t] = position; }
            return position;
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        public static float EaseOutCubic(float t)
        {
            float x = 1f - t;
            return 1f - x * x * x;
        }

        public static IEnumerator PunchScale(Transform target, float amount = 0.15f, float duration = 0.22f)
        {
            if (target == null) yield break;
            int token = Begin(target);
            Vector3 baseScale = RestScale(target);
            float elapsed = 0f;
            while (elapsed < duration && Active(target, token))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = baseScale * (1f + Mathf.Sin(t * Mathf.PI) * amount);
                yield return null;
            }
            if (Active(target, token)) target.localScale = baseScale;
        }

        public static IEnumerator PopIn(Transform target, float duration = 0.25f)
        {
            if (target == null) yield break;
            int token = Begin(target);
            Vector3 endScale = RestScale(target);
            if (endScale == Vector3.zero) { endScale = Vector3.one; SetRestScale(target, endScale); }
            target.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < duration && Active(target, token))
            {
                elapsed += Time.deltaTime;
                target.localScale = endScale * EaseOutBack(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (Active(target, token)) target.localScale = endScale;
        }

        // A squashy hop from the rest position; safe to trigger repeatedly.
        public static IEnumerator Hop(RectTransform target, float height = 46f, float duration = 0.45f)
        {
            if (target == null) yield break;
            int token = Begin(target);
            Vector2 start = RestPosition(target);
            Vector3 baseScale = RestScale(target);
            float elapsed = 0f;
            while (elapsed < duration && Active(target, token))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float arc = Mathf.Sin(t * Mathf.PI);
                float squash = t < 0.15f ? 0.85f + t : 1f;
                target.anchoredPosition = start + new Vector2(0f, arc * height);
                target.localScale = new Vector3(baseScale.x * (2f - squash) * (1f - arc * 0.06f), baseScale.y * squash * (1f + arc * 0.08f), baseScale.z);
                yield return null;
            }
            if (Active(target, token)) { target.anchoredPosition = start; target.localScale = baseScale; }
        }

        public static IEnumerator ShrinkAndDestroy(GameObject target, float duration = 0.18f)
        {
            if (target == null) yield break;
            Transform t = target.transform;
            Vector3 startScale = t.localScale;
            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                t.localScale = startScale * (1f - EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            if (target != null) Object.Destroy(target);
        }

        public static IEnumerator FillTo(Image image, float target, float duration = 0.35f)
        {
            if (image == null) yield break;
            float start = image.fillAmount;
            float elapsed = 0f;
            while (elapsed < duration && image != null)
            {
                elapsed += Time.deltaTime;
                image.fillAmount = Mathf.Lerp(start, target, EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            if (image != null) image.fillAmount = target;
        }

        public static IEnumerator AnchorMaxXTo(RectTransform rect, float target, float duration = 0.35f)
        {
            if (rect == null) yield break;
            float start = rect.anchorMax.x;
            float elapsed = 0f;
            while (elapsed < duration && rect != null)
            {
                elapsed += Time.deltaTime;
                rect.anchorMax = new Vector2(Mathf.Lerp(start, target, EaseOutCubic(Mathf.Clamp01(elapsed / duration))), 1f);
                yield return null;
            }
            if (rect != null) rect.anchorMax = new Vector2(target, 1f);
        }

        public static IEnumerator ColorTo(Graphic graphic, Color target, float duration = 0.3f)
        {
            if (graphic == null) yield break;
            Color start = graphic.color;
            float elapsed = 0f;
            while (elapsed < duration && graphic != null)
            {
                elapsed += Time.deltaTime;
                graphic.color = Color.Lerp(start, target, EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            if (graphic != null) graphic.color = target;
        }

        public static IEnumerator Flash(Graphic graphic, Color flashColor, float duration = 0.25f)
        {
            if (graphic == null) yield break;
            Color original = graphic.color;
            graphic.color = flashColor;
            yield return ColorTo(graphic, original, duration);
        }

        public static IEnumerator CountUp(Text label, int from, int to, float duration = 0.5f, string suffix = "", string prefix = "")
        {
            if (label == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration && label != null)
            {
                elapsed += Time.deltaTime;
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, EaseOutCubic(Mathf.Clamp01(elapsed / duration))));
                label.text = prefix + value + suffix;
                yield return null;
            }
            if (label != null) label.text = prefix + to + suffix;
        }

        // Runs until the target is destroyed.
        public static IEnumerator Breathe(Transform target, float amplitude = 0.03f, float period = 2.4f)
        {
            if (target == null) yield break;
            Vector3 baseScale = RestScale(target);
            float time = 0f;
            while (target != null)
            {
                time += Time.deltaTime;
                float s = 1f + Mathf.Sin(time / period * Mathf.PI * 2f) * amplitude;
                target.localScale = new Vector3(baseScale.x * s, baseScale.y * (2f - s), baseScale.z);
                yield return null;
            }
        }
    }
}
