using System;
using System.Collections;
using Gotchi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Sapphire-style text box: typewriter text, a bouncing ▼ when there is more, and an optional
    // YES / NO style choice box that pops up above the right corner.
    public class DialogBoxView
    {
        private const float CharsPerSecond = 45f;

        private readonly MonoBehaviour _host;
        private readonly Text _text;
        private readonly Image _arrow;
        private readonly RectTransform _choice;
        private Coroutine _typing;
        private Coroutine _bounce;
        private Coroutine _auto;

        // When > 0, a line with a continuation auto-advances after this many seconds (tap still skips ahead).
        public float AutoAdvanceSeconds;
        private string _full = "";
        private Action _onDone;
        private string[] _pendingOptions;
        private Action<int> _pendingPick;
        private bool _choosing;

        public readonly RectTransform Root;
        public bool IsOpen => Root.gameObject.activeSelf;

        public DialogBoxView(Transform parent, MonoBehaviour host, int fontSize = 30)
        {
            _host = host;
            var frame = UIFactory.CreateFrame("Dialog", parent, Color.white);
            Root = frame.rectTransform;
            _text = UIFactory.CreatePixelText("Text", Root, "", fontSize, UIFactory.MenuInk, TextAnchor.UpperLeft);
            UIFactory.Fill(_text.rectTransform, 34f, 60f, 24f, 20f);
            _arrow = UIFactory.CreateCursor(Root, 22f, UIFactory.MenuInk);
            _arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            UIFactory.Place(_arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-46f, 18f), new Vector2(-24f, 40f));
            _arrow.enabled = false;
            var tap = Root.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(Advance);

            _choice = UIFactory.CreateRect("Choice", Root);
            _choice.gameObject.SetActive(false);
            Root.gameObject.SetActive(false);
        }

        public void Say(string text, Action onDone = null)
        {
            _pendingOptions = null;
            _pendingPick = null;
            Show(text, onDone);
        }

        public void Ask(string text, string[] options, Action<int> onPick)
        {
            _pendingOptions = options;
            _pendingPick = onPick;
            Show(text, null);
        }

        // Static text with no arrow and no continuation (e.g. "What will MOCHI do?").
        public void Prompt(string text)
        {
            if (_typing != null) { _host.StopCoroutine(_typing); _typing = null; }
            if (_bounce != null) { _host.StopCoroutine(_bounce); _bounce = null; }
            if (_auto != null) { _host.StopCoroutine(_auto); _auto = null; }
            _pendingOptions = null;
            _pendingPick = null;
            _onDone = null;
            _choosing = false;
            _choice.gameObject.SetActive(false);
            _arrow.enabled = false;
            _full = text;
            _text.text = text;
            Root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_auto != null) { _host.StopCoroutine(_auto); _auto = null; }
            if (_typing != null) { _host.StopCoroutine(_typing); _typing = null; }
            if (_bounce != null) { _host.StopCoroutine(_bounce); _bounce = null; }
            _choice.gameObject.SetActive(false);
            _choosing = false;
            Root.gameObject.SetActive(false);
        }

        private void Show(string text, Action onDone)
        {
            _full = text.Replace("…", "...").Replace("—", "-").Replace("·", "-");
            _onDone = onDone;
            _choosing = false;
            _choice.gameObject.SetActive(false);
            _arrow.enabled = false;
            if (_bounce != null) { _host.StopCoroutine(_bounce); _bounce = null; }
            if (_auto != null) { _host.StopCoroutine(_auto); _auto = null; }
            bool wasOpen = Root.gameObject.activeSelf;
            Root.gameObject.SetActive(true);
            if (!wasOpen) _host.StartCoroutine(SimpleTween.PopIn(Root, 0.18f));
            if (_typing != null) _host.StopCoroutine(_typing);
            _typing = _host.StartCoroutine(Type());
        }

        private IEnumerator Type()
        {
            _text.text = "";
            float shown = 0f;
            while (shown < _full.Length)
            {
                shown += Time.deltaTime * CharsPerSecond;
                _text.text = _full.Substring(0, Mathf.Min(_full.Length, Mathf.FloorToInt(shown)));
                yield return null;
            }
            _text.text = _full;
            _typing = null;
            Finished();
        }

        private void Finished()
        {
            if (_pendingOptions != null) ShowChoices();
            else
            {
                _arrow.enabled = true;
                _bounce = _host.StartCoroutine(Bounce());
                if (AutoAdvanceSeconds > 0f && _onDone != null) _auto = _host.StartCoroutine(AutoAdvance());
            }
        }

        private IEnumerator AutoAdvance()
        {
            yield return new WaitForSeconds(AutoAdvanceSeconds);
            _auto = null;
            Advance();
        }

        private IEnumerator Bounce()
        {
            float time = 0f;
            var rect = _arrow.rectTransform;
            Vector2 rest = rect.anchoredPosition;
            while (true)
            {
                time += Time.deltaTime;
                rect.anchoredPosition = rest + new Vector2(0f, Mathf.Abs(Mathf.Sin(time * 5f)) * 6f);
                yield return null;
            }
        }

        private void ShowChoices()
        {
            for (int i = _choice.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_choice.GetChild(i).gameObject);
            var options = _pendingOptions;
            var pick = _pendingPick;
            _pendingOptions = null;
            _pendingPick = null;
            _choosing = true;

            const float rowHeight = 60f, width = 250f;
            float height = options.Length * rowHeight + 32f;
            UIFactory.Place(_choice, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-width, 10f), new Vector2(0f, 10f + height));
            var box = UIFactory.CreateFrame("Box", _choice, Color.white);
            UIFactory.Fill(box.rectTransform);
            var cursors = new Image[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                var row = UIFactory.CreatePanel("Row" + i, box.transform, Color.clear);
                UIFactory.Place(row.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -16f - (i + 1) * rowHeight), new Vector2(-12f, -16f - i * rowHeight));
                var label = UIFactory.CreatePixelText("Label", row.transform, options[i].ToUpperInvariant(), 30, UIFactory.MenuInk, TextAnchor.MiddleLeft);
                UIFactory.Fill(label.rectTransform, 48f, 8f, 0f, 0f);
                cursors[i] = UIFactory.CreateCursor(row.transform, 20f, UIFactory.MenuInk);
                UIFactory.Place(cursors[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, -10f), new Vector2(36f, 10f));
                cursors[i].enabled = i == 0;
                var button = row.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() =>
                {
                    if (!_choosing) return;
                    _choosing = false;
                    for (int c = 0; c < cursors.Length; c++) cursors[c].enabled = c == index;
                    _host.StartCoroutine(ConfirmChoice(index, pick));
                });
            }
            _choice.gameObject.SetActive(true);
            _host.StartCoroutine(SimpleTween.PopIn(_choice, 0.15f));
        }

        private IEnumerator ConfirmChoice(int index, Action<int> pick)
        {
            yield return new WaitForSeconds(0.14f);
            Hide();
            pick?.Invoke(index);
        }

        // Tap: finish typing, or continue.
        private void Advance()
        {
            if (_auto != null) { _host.StopCoroutine(_auto); _auto = null; }
            if (_typing != null)
            {
                _host.StopCoroutine(_typing);
                _typing = null;
                _text.text = _full;
                Finished();
                return;
            }
            if (_choosing || _pendingOptions != null) return;
            var done = _onDone;
            _onDone = null;
            if (done != null) done();
            else Hide();
        }
    }
}
