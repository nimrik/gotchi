using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Creature;
using Gotchi.Creature3D;
using Gotchi.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public enum PetPart { Head, Body, Paws, Tail }

    // The pet as seen everywhere (room, shop previews, onboarding, story, mini-games). The Cat is the 3D model
    // (Cat3DView: Blender FBX on an off-screen stage shown through a RawImage); every other species is the
    // procedural 2D CreatureBody. Both sit behind the same API: mood glow, off-body care overlays (rain cloud,
    // thought bubble), touch wiring and idle wandering.
    public class PetPortraitView
    {
        private static readonly Dictionary<EmotionCategory, Color> Moods = new Dictionary<EmotionCategory, Color>
        {
            { EmotionCategory.Joy, UIFactory.Butter },
            { EmotionCategory.Sadness, UIFactory.Sky },
            { EmotionCategory.Anger, UIFactory.Coral },
            { EmotionCategory.Fear, UIFactory.Lavender },
            { EmotionCategory.Disgust, UIFactory.Mint },
            { EmotionCategory.Surprise, UIFactory.Hex("FFC9A3") },
            { EmotionCategory.GuiltAndShame, UIFactory.Hex("F5C6D0") },
            { EmotionCategory.ConnectionAndCare, UIFactory.Pink },
            { EmotionCategory.Vulnerability, UIFactory.Hex("D8DCE8") },
            { EmotionCategory.InterestAndAwe, UIFactory.Hex("B5EAF2") },
        };

        public readonly float Size;
        public readonly CreatureBody Body;      // null when the pet is the 3D cat
        public readonly Cat3DView Cat3D;        // null for the 2D species
        public bool Is3D => Cat3D != null;
        public RectTransform Root => Is3D ? Cat3D.Root : Body.rectTransform;
        public CreatureBrain Animator => Body != null ? Body.Brain : null;

        private bool _allowWander;
        public bool AllowWander
        {
            get => _allowWander;
            set { _allowWander = value; if (Cat3D != null) Cat3D.AllowWander = value; }
        }

        private readonly MonoBehaviour _host;
        private readonly RectTransform _holder;
        private RectTransform _rainCloud, _thought, _thoughtIcon;
        private UIFactory.IconKind? _thoughtKind;
        private bool _tired, _wandering;
        private LoopClip _emotionLoop = LoopClip.Idle;

        public static Color MoodColor(EmotionCategory category) => Moods[category];

        public PetPortraitView(Transform parent, SpeciesType species, MonoBehaviour host, float size)
        {
            _host = host;
            Size = size;
            _holder = UIFactory.CreateRect("PetHolder", parent);
            if (species == SpeciesType.Cat)
            {
                try { Cat3D = new Cat3DView(_holder, host, size); }
                catch (Exception e) { Debug.LogError("[PetPortraitView] 3D cat failed, falling back to 2D: " + e); }
            }
            if (Cat3D == null) Body = CreatureBody.Create(_holder, species, size);
            BuildOverlays();
            SetEmotion(EmotionType.Joy, false);
            if (!Is3D) host.StartCoroutine(IdleLife());
        }

        // ---- emotion ----

        public void SetEmotion(EmotionType emotion, bool animate)
        {
            var category = EmotionCatalog.GetCategory(emotion);
            var face = Expressions.For(emotion);
            _emotionLoop = face.Loop;
            if (Is3D)
            {
                Cat3D.SetFace(face);
                ApplyLoop();
                Cat3D.Mood = Moods[category];
                if (!animate) { Cat3D.SnapMood(); return; }
                if (face.Enter.HasValue) Cat3D.Play(face.Enter.Value);
                return;
            }
            Body.Face = face;
            ApplyLoop();
            Body.Mood = Moods[category];
            if (!animate) { Body.SnapMood(); return; }
            if (face.Enter.HasValue) Body.Brain.Play(face.Enter.Value);
        }

        private void ApplyLoop()
        {
            var loop = _tired ? LoopClip.Sleep : _emotionLoop;
            if (Is3D) Cat3D.SetLoop(loop); else Body.Brain.SetLoop(loop);
        }

        public void Play(OneShot clip, float direction = 1f) { if (Is3D) Cat3D.Play(clip, direction); else Body.Brain.Play(clip, direction); }
        public void SetLoop(LoopClip loop) { _emotionLoop = loop; ApplyLoop(); }
        public void SetFacing(float direction) { if (Is3D) Cat3D.Facing = direction; else Body.Facing = direction; }
        public void Celebrate() => Play(OneShot.Celebrate);

        // Body tap: a jelly wiggle (no hop — the creature stays calm unless the game asks for a jump).
        public void Boop() => Play(OneShot.Wiggle);

        public void React(PetPart part)
        {
            switch (part)
            {
                case PetPart.Head: Play(OneShot.Pat); break;
                case PetPart.Paws:
                    if (Is3D) Cat3D.WaveLastPaw();
                    else Body.Brain.Play(Body.LastContact.x < 0f ? OneShot.WaveL : OneShot.WaveR);
                    break;
                case PetPart.Tail:
                    if (Is3D || Body.Look.Tail != TailShape.None) Play(OneShot.TailFlick); else Boop();
                    break;
                default: Boop(); break;
            }
        }

        // ---- idle behaviour (2D; the 3D cat wanders on its own when AllowWander is set) ----

        private IEnumerator IdleLife()
        {
            while (Root != null)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(20f, 45f));
                if (Root == null || Body.Brain.Fainted || Body.Touching || !Body.Grounded) continue;
                if (_allowWander && !_wandering && !_tired && UnityEngine.Random.value < 0.5f) _host.StartCoroutine(Wander());
            }
        }

        private IEnumerator Wander()
        {
            _wandering = true;
            float target = Mathf.Clamp(Body.Pos.x + UnityEngine.Random.Range(14f, 30f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f), -Body.HalfWidth * 0.8f, Body.HalfWidth * 0.8f);
            bool arrived = false;
            Body.Brain.WalkTo(target, UnityEngine.Random.Range(16f, 24f), () => arrived = true);
            float guard = 0f;
            while (!arrived && Root != null && (guard += Time.deltaTime) < 6f) yield return null;
            yield return new WaitForSeconds(UnityEngine.Random.Range(4f, 9f));
            if (Root == null) yield break;
            if (Mathf.Abs(Body.Pos.x) > 6f && Body.Grounded && !Body.Touching)
            {
                arrived = false; guard = 0f;
                Body.Brain.WalkTo(0f, UnityEngine.Random.Range(16f, 24f), () => arrived = true);
                while (!arrived && Root != null && (guard += Time.deltaTime) < 6f) yield return null;
            }
            if (Body != null && !Body.Touching) Body.Facing = 1f;
            _wandering = false;
        }

        // ---- touch ----

        // Taps and long presses report the part; petting (rubbing) spawns hearts by itself.
        public void EnableTouch(Action<PetPart> onTap)
        {
            if (Is3D)
            {
                Cat3D.EnableTouch();
                Cat3D.Tapped += part => onTap?.Invoke(part);
                Cat3D.Held += part => onTap?.Invoke(part);
                Cat3D.Petted += () => _host.StartCoroutine(HeartPuff());
                return;
            }
            Body.raycastTarget = true;
            Body.Tapped += part => onTap?.Invoke(part);
            Body.Held += part => onTap?.Invoke(part);
            Body.Petted += () => _host.StartCoroutine(HeartPuff());
        }

        private IEnumerator HeartPuff()
        {
            var heart = UIFactory.CreateIcon(UIFactory.IconKind.Heart, _holder, UnityEngine.Random.Range(30f, 44f), Color.clear);
            heart.anchoredPosition = new Vector2(UnityEngine.Random.Range(-Size * 0.3f, Size * 0.3f), Size * 0.1f);
            foreach (var g in heart.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            var graphics = heart.GetComponentsInChildren<Graphic>();
            Vector2 start = heart.anchoredPosition;
            float elapsed = 0f, duration = 1f;
            while (elapsed < duration && heart != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                heart.anchoredPosition = start + new Vector2(Mathf.Sin(t * 5f) * 12f, t * 140f);
                float alpha = 1f - SimpleTween.EaseOutCubic(t);
                foreach (var g in graphics) { var c = g.color; c.a = alpha; g.color = c; }
                yield return null;
            }
            if (heart != null) UnityEngine.Object.Destroy(heart.gameObject);
        }

        // ---- care conditions ----

        private void BuildOverlays()
        {
            _rainCloud = UIFactory.CreateRect("RainCloud", Root);
            _rainCloud.anchoredPosition = new Vector2(-Size * 0.42f, Size * 0.5f);
            foreach (var (x, y, d) in new[] { (0f, 6f, 64f), (-30f, -4f, 48f), (30f, -2f, 50f) })
            {
                var puff = UIFactory.CreateCircle("Puff", _rainCloud, UIFactory.Hex("C9C4D2"), d);
                puff.rectTransform.anchoredPosition = new Vector2(x, y);
                puff.raycastTarget = false;
            }
            foreach (float x in new[] { -22f, 0f, 22f })
            {
                var drop = UIFactory.CreateRounded("Drop", _rainCloud, UIFactory.Sky, 0.3f);
                drop.rectTransform.sizeDelta = new Vector2(8f, 22f);
                drop.rectTransform.anchoredPosition = new Vector2(x, -40f);
                drop.raycastTarget = false;
            }

            _thought = UIFactory.CreateRect("Thought", Root);
            _thought.anchoredPosition = new Vector2(Size * 0.42f, Size * 0.56f);
            foreach (var (x, y, d) in new[] { (-34f, -52f, 14f), (-20f, -34f, 22f), (0f, 0f, 96f) })
            {
                var puff = UIFactory.CreateCircle("Puff", _thought, Color.white, d);
                puff.rectTransform.anchoredPosition = new Vector2(x, y);
                puff.raycastTarget = false;
            }
            SetConditions(100f, 100f, 100f, 100f);
        }

        public void SetConditions(float hunger, float hygiene, float energy, float happiness)
        {
            const float low = 35f;
            bool tired = energy < low, dirty = hygiene < low, sad = happiness < low, hungry = hunger < low;
            if (Is3D) Cat3D.SetConditions(dirty, hungry);
            else { Body.Tired = tired; Body.Dirty = dirty; Body.Hungry = hungry; }
            _rainCloud.gameObject.SetActive(sad);
            if (tired != _tired)
            {
                _tired = tired;
                if (Is3D) Cat3D.SetSleeping(tired); else Body.Sleeping = tired;
                ApplyLoop();
            }

            UIFactory.IconKind? craving = null;
            float lowest = low;
            if (hunger < lowest) { lowest = hunger; craving = UIFactory.IconKind.Cookie; }
            if (hygiene < lowest) { lowest = hygiene; craving = UIFactory.IconKind.Shower; }
            if (energy < lowest) { lowest = energy; craving = UIFactory.IconKind.Moon; }
            if (happiness < lowest) craving = UIFactory.IconKind.Heart;
            _thought.gameObject.SetActive(craving.HasValue);
            if (craving == _thoughtKind) return;
            _thoughtKind = craving;
            if (_thoughtIcon != null) UnityEngine.Object.Destroy(_thoughtIcon.gameObject);
            if (!craving.HasValue) return;
            _thoughtIcon = UIFactory.CreateIcon(craving.Value, _thought, 54f, Color.white);
            _thoughtIcon.anchoredPosition = Vector2.zero;
        }

        // ---- cosmetics (drawn by the body so they deform with it) ----

        public void SetAccessory(string id) { if (Is3D) Cat3D.SetAccessory(id); else Body.Accessory = id ?? ""; }
    }
}
