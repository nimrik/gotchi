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

    // The pet as seen everywhere (room, shop previews, onboarding, story, battles). The Cat is the 3D model
    // (Cat3DView: Blender FBX on an off-screen stage shown through a RawImage); every other species is the
    // procedural 2D CreatureBody. Both sit behind the same API: a face, clips, touch wiring and idle wandering.
    //
    // There is no emotion system any more (2026-09-21). What is left of it is SetFace: the named faces
    // (EmotionType + Expressions) are the renderer's vocabulary of expressions, used by animation code that wants
    // the cat to look hurt, proud or annoyed for a moment. Nothing computes a mood, nothing shows one, and the
    // care overlays that hung off the old needs (rain cloud, thought bubble, dirt, drool, sleepiness) are gone.
    public class PetPortraitView
    {
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
        private bool _wandering, _wornOut;
        private LoopClip _emotionLoop = LoopClip.Idle;

        // `coat` re-colours the 3D cat (other players' cats); null is the player's own cocoa cat.
        public PetPortraitView(Transform parent, SpeciesType species, MonoBehaviour host, float size, CatCoat coat = null)
        {
            _host = host;
            Size = size;
            _holder = UIFactory.CreateRect("PetHolder", parent);
            if (species == SpeciesType.Cat)
            {
                try { Cat3D = new Cat3DView(_holder, host, size, coat); }
                catch (Exception e) { Debug.LogError("[PetPortraitView] 3D cat failed, falling back to 2D: " + e); }
            }
            if (Cat3D == null) Body = CreatureBody.Create(_holder, species, size);
            SetFace(EmotionType.Satisfaction, false);
            if (!Is3D) host.StartCoroutine(IdleLife());
        }

        // ---- face ----

        // Puts one of the named faces on the pet (and the idle loop that goes with it). Presentation only.
        public void SetFace(EmotionType face, bool animate)
        {
            var target = Expressions.For(face);
            _emotionLoop = target.Loop;
            if (Is3D)
            {
                Cat3D.SetFace(target);
                ApplyLoop();
                if (animate && target.Enter.HasValue) Cat3D.Play(target.Enter.Value);
                return;
            }
            Body.Face = target;
            ApplyLoop();
            if (animate && target.Enter.HasValue) Body.Brain.Play(target.Enter.Value);
        }

        private void ApplyLoop()
        {
            if (Is3D) Cat3D.SetLoop(_emotionLoop); else Body.Brain.SetLoop(_emotionLoop);
        }

        // Worn out (under a tenth of its health): the cat lies down until it has rested enough to fight again.
        public void SetWornOut(bool wornOut)
        {
            if (wornOut == _wornOut || !Is3D) return;
            _wornOut = wornOut;
            if (wornOut) Cat3D.Play(OneShot.Faint); else Cat3D.Revive();
        }

        public void Play(OneShot clip, float direction = 1f) { if (Is3D) Cat3D.Play(clip, direction); else Body.Brain.Play(clip, direction); }
        public void SetLoop(LoopClip loop) { _emotionLoop = loop; ApplyLoop(); }
        public void SetFacing(float direction) { if (Is3D) Cat3D.Facing = direction; else Body.Facing = direction; }

        // Battle staging: the player's cat is seen from behind, looking up the field at its rival, mouth shut.
        public void StageForBattle(float facing, bool backView, bool keepMouthClosed)
        {
            if (Is3D) Cat3D.Stage(facing, backView, keepMouthClosed); else Body.Facing = facing;
        }
        public void Celebrate() => Play(OneShot.Celebrate);

        // Walk loop on the spot while the owner slides the pet across the screen (leaving in a huff, coming back).
        public void March(float facing, float animSpeed = 1f) { if (Is3D) Cat3D.StartWalkInPlace(facing, animSpeed); else Body.Facing = facing; }
        public void StopMarch() { if (Is3D) Cat3D.StopWalkInPlace(); else Body.Facing = 1f; }

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
                if (_allowWander && !_wandering && UnityEngine.Random.value < 0.5f) _host.StartCoroutine(Wander());
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

        // ---- cosmetics (drawn by the body so they deform with it) ----

        public void SetAccessory(string id) { if (Is3D) Cat3D.SetAccessory(id); else Body.Accessory = id ?? ""; }
    }
}
