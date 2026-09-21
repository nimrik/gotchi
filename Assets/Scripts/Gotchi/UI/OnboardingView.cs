using System;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using System.Collections;
using Gotchi.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class OnboardingResult
    {
        public string Email = "";
        public string SessionToken = "";
        public string DisplayName = "";
        public SpeciesType Species = SpeciesType.Cat;
        public string PetName = "Mochi";
        public string AgeBand = "";
        public int PreferredPlayHour = 15;
    }

    // First-run flow: account → name your friend → the door → two quick questions.
    public class OnboardingView
    {
        private readonly MonoBehaviour _host;
        private readonly IAuthService _auth;
        private readonly Action<OnboardingResult> _onDone;
        private readonly OnboardingResult _result = new OnboardingResult();
        private readonly RectTransform _safe;
        private readonly Text _error;
        private RectTransform _current;
        private int _step;

        private readonly Dictionary<string, List<Button>> _optionGroups = new Dictionary<string, List<Button>>();
        private readonly Dictionary<string, int> _answers = new Dictionary<string, int>();

        public OnboardingView(Transform parent, IAuthService auth, Action<OnboardingResult> onDone, MonoBehaviour host)
        {
            _host = host;
            _auth = auth;
            _onDone = onDone;

            var background = UIFactory.CreatePanel("OnboardingBackground", parent, UIFactory.Cream);
            UIFactory.Fill(background.rectTransform);
            var glow = UIFactory.CreateGradient("Glow", background.transform, UIFactory.Peach);
            UIFactory.Place(glow.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _safe = UIFactory.CreateRect("Safe", background.transform);
            UIFactory.ApplySafeArea(_safe);

            _error = UIFactory.CreateText("Error", _safe, "", 24, UIFactory.PinkDark, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_error.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 40f), new Vector2(-24f, 100f));

            ShowStep(0);
        }

        public void ShowStep(int step)
        {
            _step = step;
            if (_current != null) UnityEngine.Object.Destroy(_current.gameObject);
            _error.text = "";
            _current = UIFactory.CreateRect("Step" + step, _safe);
            UIFactory.Fill(_current, 28f, 28f, 60f, 120f);
            switch (step)
            {
                case 0: BuildAccount(); break;
                case 1: BuildCharacter(); break;
                case 2: BuildHatch(); break;
                default: BuildQuestions(); break;
            }
            _host.StartCoroutine(SimpleTween.PopIn(_current, 0.3f));
        }

        private Image Card(string name, float top, float bottom)
        {
            var card = UIFactory.CreateCard(name, _current, UIFactory.Card, 1f);
            UIFactory.Place((RectTransform)card.transform.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, bottom), new Vector2(0f, -top));
            return card;
        }

        private static InputField Field(Transform parent, string placeholder, bool password, float top)
        {
            var field = UIFactory.CreateInputField("Field", parent, placeholder, password ? InputField.ContentType.Password : InputField.ContentType.EmailAddress);
            UIFactory.Place(field.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -top - 84f), new Vector2(-32f, -top));
            return field;
        }

        private void Title(string title, string subtitle)
        {
            var t = UIFactory.CreateText("Title", _current, title, 52, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(0f, -20f));
            var s = UIFactory.CreateText("Subtitle", _current, subtitle, 26, UIFactory.Muted, TextAnchor.MiddleCenter);
            UIFactory.Place(s.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -170f), new Vector2(-20f, -110f));
        }

        private void BuildAccount()
        {
            Title("Welcome to Gotchi", "Make an account so your friend can follow you between devices.");
            var card = Card("AccountCard", 200f, 520f);
            var displayName = Field(card.transform, "Your name (shown on leaderboards)", false, 40f);
            displayName.contentType = InputField.ContentType.Name;
            displayName.characterLimit = 16;
            var email = Field(card.transform, "Email", false, 150f);
            var password = Field(card.transform, "Password (6+ characters)", true, 260f);

            var create = UIFactory.CreateButton("Create", card.transform, "Create account", UIFactory.Pink, () =>
            {
                _auth.SignUp(email.text, password.text, result =>
                {
                    if (!result.Success) { _error.text = result.Message; return; }
                    _result.Email = email.text.Trim();
                    _result.SessionToken = result.SessionToken;
                    _result.DisplayName = displayName.text.Trim();
                    ShowStep(1);
                });
            }, 30, _host);
            UIFactory.Place((RectTransform)create.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-240f, 110f), new Vector2(240f, 186f));

            var guest = UIFactory.CreateButton("Guest", card.transform, "Play as guest", UIFactory.Card, () => { _result.Email = ""; _result.DisplayName = displayName.text.Trim(); ShowStep(1); }, 28, _host);
            UIFactory.Place((RectTransform)guest.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 94f));
        }

        // The first release is cats only, so there is nothing to choose here: the player names the friend who is
        // about to turn up, and the door scene reveals who it is. (A 13-species picker stood here until 2026-09-21.)
        private void BuildCharacter()
        {
            Title("Someone is coming", "You will meet them in a moment. What will you call your new friend?");
            var card = Card("CharacterCard", 200f, 900f);

            var mystery = UIFactory.CreateCircle("Mystery", card.transform, UIFactory.Lavender, 220f);
            UIFactory.Place(mystery.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, -270f), new Vector2(110f, -50f));
            mystery.raycastTarget = false;
            var mark = UIFactory.CreatePixelText("Mark", mystery.transform, "?", 120, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Fill(mark.rectTransform);
            _host.StartCoroutine(SimpleTween.Breathe(mystery.transform, 0.04f, 2.2f));

            var nameField = Field(card.transform, "Name your friend", false, 0f);
            UIFactory.Place(nameField.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(32f, 126f), new Vector2(-32f, 210f));
            nameField.contentType = InputField.ContentType.Name;
            nameField.characterLimit = 14;
            nameField.text = "Mochi";

            var next = UIFactory.CreateButton("Next", card.transform, "Open the door", UIFactory.Pink, () =>
            {
                string name = nameField.text.Trim();
                if (name.Length == 0) { _error.text = "Give your friend a name first."; return; }
                _result.PetName = name;
                _result.Species = SpeciesType.Cat;
                ShowStep(2);
            }, 30, _host);
            UIFactory.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 100f));
        }

        private int _unwrapTaps;
        private RectTransform _bundle;
        private RectTransform _stage;
        private readonly List<RectTransform> _folds = new List<RectTransform>();
        private PetPortraitView _foundFriend;
        private Image _warmGlow;
        private DialogBoxView _doorDialog;

        // A knock at the door: a bundle on the doorstep, three taps peel the blanket back.
        private void BuildHatch()
        {
            Title("A knock at the door", "Someone left a bundle on your doorstep.");
            var card = Card("DoorCard", 200f, 120f);
            var night = UIFactory.CreateRounded("Night", card.transform, UIFactory.Hex("3F3B63"), 1f);
            UIFactory.Fill(night.rectTransform, 16f, 16f, 16f, 220f);
            night.raycastTarget = false;
            var moon = UIFactory.CreateCircle("Moon", night.transform, UIFactory.Butter, 90f);
            UIFactory.Place(moon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -140f), new Vector2(-60f, -50f));
            moon.raycastTarget = false;
            var moonMask = UIFactory.CreateCircle("Mask", moon.transform, UIFactory.Hex("3F3B63"), 84f);
            moonMask.rectTransform.anchoredPosition = new Vector2(26f, 16f);
            moonMask.raycastTarget = false;
            foreach (var (x, y) in new[] { (0.12f, 0.85f), (0.3f, 0.72f), (0.55f, 0.9f), (0.8f, 0.6f), (0.2f, 0.5f) })
            {
                var star = UIFactory.CreateCircle("Star", night.transform, new Color(1f, 1f, 1f, 0.8f), 8f);
                UIFactory.Place(star.rectTransform, new Vector2(x, y), new Vector2(x, y), new Vector2(-4f, -4f), new Vector2(4f, 4f));
                star.raycastTarget = false;
            }
            var door = UIFactory.CreateRounded("Door", night.transform, UIFactory.Hex("8C5A3C"), 0.8f);
            UIFactory.Place(door.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 160f), new Vector2(170f, 720f));
            door.raycastTarget = false;
            var knob = UIFactory.CreateCircle("Knob", door.transform, UIFactory.Butter, 22f);
            UIFactory.Place(knob.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, -11f), new Vector2(-38f, 11f));
            knob.raycastTarget = false;
            var step = UIFactory.CreateRounded("Step", night.transform, UIFactory.Hex("5B5480"), 0.6f);
            UIFactory.Place(step.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(40f, 60f), new Vector2(-40f, 170f));
            step.raycastTarget = false;

            _warmGlow = UIFactory.CreateCircle("Warm", night.transform, new Color(1f, 0.85f, 0.55f, 0f), 900f);
            UIFactory.Place(_warmGlow.rectTransform, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(-450f, -450f), new Vector2(450f, 450f));
            _warmGlow.raycastTarget = false;

            _stage = UIFactory.CreateRect("Stage", night.transform);
            UIFactory.Place(_stage, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(0f, 250f));

            _bundle = UIFactory.CreateRect("Bundle", _stage);
            _bundle.anchoredPosition = Vector2.zero;
            var basket = UIFactory.CreateRounded("Basket", _bundle, UIFactory.Hex("C9955C"), 1f);
            basket.rectTransform.sizeDelta = new Vector2(360f, 150f);
            basket.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            basket.raycastTarget = false;
            var blanket = UIFactory.CreateCircle("Blanket", _bundle, UIFactory.Lavender, 300f);
            blanket.rectTransform.localScale = new Vector3(1.05f, 0.72f, 1f);
            blanket.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            blanket.raycastTarget = false;
            _folds.Clear();
            foreach (var (x, y, rot, c) in new[] { (-70f, 40f, 30f, "DCCBFF"), (70f, 30f, -30f, "D2C0FF"), (0f, -10f, 0f, "E6DAFF") })
            {
                var fold = UIFactory.CreateRounded("Fold", _bundle, UIFactory.Hex(c), 1.4f);
                fold.rectTransform.sizeDelta = new Vector2(190f, 120f);
                fold.rectTransform.anchoredPosition = new Vector2(x, y);
                fold.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
                fold.raycastTarget = false;
                _folds.Add(fold.rectTransform);
            }
            var tap = UIFactory.CreatePanel("Tap", _bundle, Color.clear);
            tap.rectTransform.sizeDelta = new Vector2(380f, 300f);
            tap.gameObject.AddComponent<Button>().onClick.AddListener(OnBundleTap);

            _doorDialog = new DialogBoxView(card.transform, _host, 30);
            UIFactory.Place(_doorDialog.Root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 206f));
            _doorDialog.Say("Someone left a bundle on your doorstep... Tap the blanket.");
            _unwrapTaps = 0;
        }

        private void OnBundleTap()
        {
            if (_bundle == null || _unwrapTaps >= 3) return;
            _unwrapTaps++;
            _host.StartCoroutine(Wobble(_bundle, 0.35f, 6f));
            var fold = _folds[_unwrapTaps - 1];
            _host.StartCoroutine(PeelAway(fold, _unwrapTaps == 1 ? -1f : 1f));
            if (_unwrapTaps < 3) _doorDialog.Say(_unwrapTaps == 1 ? "Something moved..." : "Tiny ears!");
            if (_unwrapTaps >= 3) _host.StartCoroutine(Reveal());
        }

        public void DebugHatch()
        {
            for (int i = 0; i < 3; i++) OnBundleTap();
        }

        private IEnumerator Reveal()
        {
            yield return new WaitForSeconds(0.35f);
            var anchor = UIFactory.CreateRect("Friend", _stage);
            anchor.anchoredPosition = new Vector2(0f, 40f);
            _foundFriend = new PetPortraitView(anchor, _result.Species, _host, 300f);
            _foundFriend.SetFace(EmotionType.Nervousness, false);
            _host.StartCoroutine(SimpleTween.PopIn(anchor, 0.4f));
            _host.StartCoroutine(Shiver(anchor));
            yield return new WaitForSeconds(0.3f);
            _doorDialog.Ask($"{_result.PetName} is shivering... Take {_result.PetName} home?", new[] { "Yes", "No" }, choice =>
            {
                if (choice == 0) TakeHome();
                else ShowStep(1);
            });
        }

        private void TakeHome()
        {
            _host.StartCoroutine(SimpleTween.ColorTo(_warmGlow, new Color(1f, 0.85f, 0.55f, 0.45f), 0.8f));
            _foundFriend?.SetFace(EmotionType.Gratitude, true);
            _doorDialog.Say($"{_result.PetName} is warm now. Welcome home!", () => ShowStep(3));
        }

        private static IEnumerator Shiver(RectTransform target)
        {
            float time = 0f;
            while (target != null && time < 6f)
            {
                time += Time.deltaTime;
                target.anchoredPosition = new Vector2(Mathf.Sin(time * 40f) * 3f, 40f);
                yield return null;
            }
            if (target != null) target.anchoredPosition = new Vector2(0f, 40f);
        }

        private static IEnumerator PeelAway(RectTransform fold, float direction)
        {
            var graphic = fold.GetComponent<Graphic>();
            Vector2 start = fold.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < 0.5f && fold != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.5f;
                fold.anchoredPosition = start + new Vector2(direction * 220f * t, 60f * t);
                fold.localRotation = Quaternion.Euler(0f, 0f, direction * 60f * t);
                var c = graphic.color; c.a = 1f - t; graphic.color = c;
                yield return null;
            }
            if (fold != null) UnityEngine.Object.Destroy(fold.gameObject);
        }

        private static IEnumerator Wobble(RectTransform target, float duration, float degrees)
        {
            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * degrees * (1f - t));
                yield return null;
            }
            if (target != null) target.localRotation = Quaternion.identity;
        }

        private void BuildQuestions()
        {
            Title("Two quick questions", "This helps Gotchi fit around you.");
            var card = Card("QuestionsCard", 200f, 710f);
            float y = 24f;
            y = Question(card.transform, y, "age", "How old are you?", new[] { "Under 13", "13 to 17", "18 and up" });
            Question(card.transform, y, "time", "When do you usually play?", new[] { "Morning", "Afternoon", "Evening" });

            var start = UIFactory.CreateButton("Start", card.transform, "Start playing", UIFactory.Pink, Finish, 30, _host);
            UIFactory.Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-220f, 20f), new Vector2(220f, 96f));
        }

        private float Question(Transform parent, float top, string key, string prompt, string[] options)
        {
            var label = UIFactory.CreateText(key, parent, prompt, 28, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -top - 44f), new Vector2(-28f, -top));
            var row = UIFactory.CreateRect(key + "Row", parent);
            UIFactory.Place(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -top - 130f), new Vector2(-24f, -top - 52f));
            UIFactory.AddHorizontalLayout(row.gameObject, 10f, new RectOffset(0, 0, 0, 0), true);
            var buttons = new List<Button>();
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                var button = UIFactory.CreateButton(options[i], row, options[i], UIFactory.MenuGrey, () => Choose(key, index), 22, _host, UIFactory.MenuInk);
                buttons.Add(button);
            }
            _optionGroups[key] = buttons;
            if (!_answers.ContainsKey(key)) _answers[key] = -1;
            return top + 150f;
        }

        private void Choose(string key, int index)
        {
            _answers[key] = index;
            var buttons = _optionGroups[key];
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].image.color = i == index ? UIFactory.Card : UIFactory.MenuGrey;
            }
        }

        private void Finish()
        {
            foreach (var pair in _answers)
                if (pair.Value < 0) { _error.text = "Answer both questions to continue."; return; }

            _result.AgeBand = new[] { "under13", "13-17", "18+" }[_answers["age"]];
            _result.PreferredPlayHour = new[] { 9, 15, 20 }[_answers["time"]];
            _onDone?.Invoke(_result);
        }
    }
}
