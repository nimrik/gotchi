using System;
using System.Collections.Generic;
using Gotchi.Data;
using Gotchi.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gotchi.Creature
{
    // A real little animal: a procedural skeleton (spine, neck + head that pitches and turns, four two-bone legs
    // solved by inverse kinematics, a tail chain, ears) drawn as one flat vector mesh in the reference style —
    // flat colour, one shadow tone, a thin dark outline, a shadow disc. Every joint is a spring, so poses
    // (stand / sit / lie / sleep), gait cycles and reactions blend into each other. The whole animal is a rigid
    // body under gravity that can be dragged, picked up and thrown. Units: body ≈ 64 wide; `Unit` → canvas px.
    public class CreatureBody : MaskableGraphic, ICanvasRaycastFilter, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public SpeciesType Species { get; private set; }
        public CreatureLook Look;
        public float Unit { get; private set; }
        public float Size { get; private set; }
        public Vector2 Origin { get; private set; }
        public float Facing = 1f;
        public CreatureBrain Brain { get; private set; }
        public SoftBody Soft { get; private set; }

        public FaceTarget Face = FaceTarget.Neutral;
        public bool Sleeping, Tired, Dirty, Hungry;
        public string Accessory = "";
        public Color Mood = new Color(1f, 0.92f, 0.6f, 1f);
        private Color _moodNow = new Color(1f, 0.92f, 0.6f, 1f);
        public float Clock => _time;

        // ---- rigid body (units; ground at y = 0) ----
        public Vector2 Pos, Vel;
        public bool Grounded = true;
        public float Gravity = 1500f;
        public float HalfWidth = 40f;
        public float WalkSpeed;
        public bool Lifted => _pressed && _dragging && !_petting;

        // ---- skeleton springs ----
        public Spring Rot, Crouch, Sway;
        public Spring HipY, ShoulderY, HeadDX, HeadDY, HeadPitch, HeadYaw, HindFold, FrontFold, PawLift;
        public Spring TailA, TailB, TailC, EarL, EarR;
        public readonly Spring[] FootX = new Spring[4], FootY = new Spring[4];   // 0 hind far, 1 front far, 2 hind near, 3 front near
        public float GaitPhase;
        public const float LegSeg = 10f;
        public const float Stride = 10f;

        // ---- face ----
        public Spring EyeOpenL, EyeOpenR, Squint, EyeScale, Pupil, LidTilt, BrowShow, BrowTilt, BrowRaise;
        public Spring MouthCurve, MouthOpen, MouthWidth, Blush, GazeX, GazeY;
        public Spring Tear, Sweat, Heart, Sparkle, Anger, Zzz, Bags, Dirt, Drool;

        public float BlinkClose, Content;
        public Vector2? LookAt;

        public event Action<PetPart> Tapped;
        public event Action<PetPart> Held;
        public event Action Petted;
        public bool Touching => _pressed;

        private readonly VectorMesh _vm = new VectorMesh();
        private readonly List<(List<Vector2> poly, PetPart part)> _hits = new List<(List<Vector2>, PetPart)>();
        private readonly List<List<Vector2>> _hitPool = new List<List<Vector2>>();
        private int _hitPoolIndex;
        private bool _ready;
        private float _time;
        private float _cos = 1f, _sin;
        private Vector2 _hip, _shoulder, _head;   // body space, current frame

        private static readonly Color BlushColor = UIFactory.Hex("FF9FB8");
        private static readonly Color PinkNose = UIFactory.Hex("E58AA5");
        private static readonly Color MouthInside = UIFactory.Hex("D85F86");
        private static readonly Color Orange = UIFactory.Hex("F5A25B");
        private static readonly Color OrangeDark = UIFactory.Hex("D98336");
        private static readonly Color Gold = UIFactory.Hex("F5B942");

        public static CreatureBody Create(Transform parent, SpeciesType species, float size)
        {
            var go = new GameObject("Creature", typeof(RectTransform), typeof(CanvasRenderer), typeof(CreatureBody));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size * 1.6f, size * 1.6f);
            var body = go.GetComponent<CreatureBody>();
            body.Setup(species, size);
            return body;
        }

        private static Spring S(float v, float k, float d) => new Spring(v, k, d);

        private void Setup(SpeciesType species, float size)
        {
            Species = species;
            Look = CreatureLook.For(species);
            Size = size;
            Unit = size / 96f;
            Origin = new Vector2(0f, -size * 0.62f);
            raycastTarget = false;
            _time = UnityEngine.Random.Range(0f, 30f);
            Soft = new SoftBody(24) { Stiffness = 300f, Damping = 9f, Coupling = 900f, MaxDisp = 5f };

            Rot = S(0f, 160f, 0.55f); Crouch = S(0f, 260f, 0.5f); Sway = S(0f, 60f, 0.8f);
            HipY = S(19f, 120f, 0.75f); ShoulderY = S(20f, 120f, 0.75f);
            HeadDX = S(6f, 90f, 0.7f); HeadDY = S(19f, 90f, 0.7f); HeadPitch = S(0f, 120f, 0.7f); HeadYaw = S(0f, 100f, 0.75f);
            HindFold = S(0f, 60f, 0.9f); FrontFold = S(0f, 60f, 0.9f); PawLift = S(0f, 150f, 0.6f);
            TailA = S(0f, 70f, 0.4f); TailB = S(0f, 55f, 0.35f); TailC = S(0f, 45f, 0.3f);
            EarL = S(0f, 220f, 0.3f); EarR = S(0f, 220f, 0.3f);
            for (int i = 0; i < 4; i++) { FootX[i] = S(RestFootX(i), 400f, 0.85f); FootY[i] = S(0f, 400f, 0.85f); }
            EyeOpenL = S(1f, 1500f, 0.9f); EyeOpenR = S(1f, 1500f, 0.9f);
            Squint = S(0f, 420f, 0.9f); EyeScale = S(1f, 300f, 0.8f); Pupil = S(1f, 300f, 0.8f);
            LidTilt = S(0f, 300f, 0.9f); BrowShow = S(0f, 300f, 0.9f); BrowTilt = S(0f, 300f, 0.85f); BrowRaise = S(0f, 300f, 0.85f);
            MouthCurve = S(0.8f, 260f, 0.85f); MouthOpen = S(0f, 420f, 0.8f); MouthWidth = S(1f, 260f, 0.85f);
            Blush = S(0.55f, 60f, 1f); GazeX = S(0f, 540f, 0.72f); GazeY = S(0f, 540f, 0.72f);
            Tear = S(0f, 80f, 1f); Sweat = S(0f, 80f, 1f); Heart = S(0f, 80f, 1f); Sparkle = S(0f, 80f, 1f);
            Anger = S(0f, 80f, 1f); Zzz = S(0f, 40f, 1f); Bags = S(0f, 60f, 1f); Dirt = S(0f, 60f, 1f); Drool = S(0f, 60f, 1f);

            Brain = new CreatureBrain(this);
            RefreshPose();
            _ready = true;
        }

        public void SnapMood() => _moodNow = Mood;

        // ---- skeleton geometry (body space: +x = facing direction, y up, ground = 0) ----

        public float W => Look.BodyW;
        public float H => Look.BodyH;
        public Vector2 Center => (_hip + _shoulder) * 0.5f + new Vector2(0f, 2f);
        public Vector2 FaceCenter => _head;
        public Vector2 HeadCenter => _head;
        public bool Quadruped => Look.Plan == BodyPlan.Quadruped;

        public static float RestFootX(int leg) => leg switch { 0 => -15f, 1 => 7f, 2 => -11f, _ => 12f };
        private Vector2 LegAttach(int leg) => (leg % 2 == 0) ? _hip + new Vector2(leg == 0 ? -2f : 1f, 0f) : _shoulder + new Vector2(leg == 1 ? -2f : 1f, 0f);
        public bool IsHind(int leg) => leg % 2 == 0;
        public bool IsNear(int leg) => leg >= 2;

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float c = Mathf.Cos(deg * Mathf.Deg2Rad), s = Mathf.Sin(deg * Mathf.Deg2Rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
        private static Vector2 Dir(float rad) => new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        // Body space → local rect space.
        public Vector2 Map(Vector2 p)
        {
            p = new Vector2(p.x * Facing, p.y);
            p = new Vector2(p.x * _cos - p.y * _sin, p.x * _sin + p.y * _cos);
            p += Pos + new Vector2(Sway.Value, 0f);
            return Origin + p * Unit;
        }
        public Vector2 WorldFromLocal(Vector2 local) => (local - Origin) / Unit;
        public Vector2 BodyFromLocal(Vector2 local)
        {
            Vector2 q = WorldFromLocal(local) - Pos - new Vector2(Sway.Value, 0f);
            q = new Vector2(q.x * _cos + q.y * _sin, -q.x * _sin + q.y * _cos);
            return new Vector2(q.x / Facing, q.y);
        }
        private void MapOnly(List<Vector2> poly) { for (int i = 0; i < poly.Count; i++) poly[i] = Map(poly[i]); }

        // ---- per-frame update ----

        private void Update()
        {
            if (!_ready) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _time += dt;
            ApplyBaseTargets();
            Brain.Tick(dt);
            Gait(dt);
            TouchTick(dt);
            ApplyOverrides();
            StepPhysics(dt);
            SetVerticesDirty();
        }

        private void ApplyBaseTargets()
        {
            var f = Face;
            EyeOpenL.Target = EyeOpenR.Target = Sleeping ? 0f : f.EyeOpen;
            Squint.Target = Sleeping ? 0f : f.Squint;
            EyeScale.Target = f.EyeScale; Pupil.Target = f.Pupil; LidTilt.Target = f.LidTilt;
            BrowShow.Target = Sleeping ? 0f : f.BrowShow; BrowTilt.Target = f.BrowTilt; BrowRaise.Target = f.BrowRaise;
            MouthCurve.Target = Sleeping ? 0.35f : f.MouthCurve; MouthOpen.Target = Sleeping ? 0f : f.MouthOpen; MouthWidth.Target = f.MouthWidth;
            Blush.Target = f.Blush;
            Tear.Target = f.Tear; Sweat.Target = f.Sweat; Heart.Target = f.Heart; Sparkle.Target = f.Sparkle; Anger.Target = Sleeping ? 0f : f.Anger;
            Zzz.Target = Sleeping ? 1f : 0f; Bags.Target = Tired ? 1f : 0f; Dirt.Target = Dirty ? 1f : 0f; Drool.Target = Hungry ? 1f : 0f;

            Rot.Target = 0f; Sway.Target = 0f; Crouch.Target = 0f;
            HipY.Target = 19f; ShoulderY.Target = 20f; HeadDX.Target = 6f; HeadDY.Target = 19f; HeadPitch.Target = 0f; HeadYaw.Target = 0f;
            HindFold.Target = 0f; FrontFold.Target = 0f; PawLift.Target = 0f;
            TailA.Target = 0f; TailB.Target = 0f; TailC.Target = 0f; EarL.Target = EarR.Target = 0f;
            for (int i = 0; i < 4; i++) { FootX[i].Target = RestFootX(i); FootY[i].Target = 0f; FootX[i].Set(400f, 0.85f); FootY[i].Set(400f, 0.85f); }
            GazeX.Target = 0f; GazeY.Target = 0f;
            BlinkClose = 0f; Content = 0f; LookAt = null;
        }

        // Called by the brain to put the skeleton into a stance (targets only; springs do the blending).
        public void SetStance(Stance stance, float weight = 1f)
        {
            float w = Mathf.Clamp01(weight);
            switch (stance)
            {
                case Stance.Sit:
                    HipY.Target = Mathf.Lerp(HipY.Target, 9f, w); ShoulderY.Target = Mathf.Lerp(ShoulderY.Target, 21f, w);
                    HeadDX.Target = Mathf.Lerp(HeadDX.Target, 4f, w); HeadDY.Target = Mathf.Lerp(HeadDY.Target, 20f, w);
                    HindFold.Target = w; FootX[0].Target = Mathf.Lerp(FootX[0].Target, -8f, w); FootX[2].Target = Mathf.Lerp(FootX[2].Target, -3f, w);
                    FootX[1].Target = Mathf.Lerp(FootX[1].Target, 6f, w); FootX[3].Target = Mathf.Lerp(FootX[3].Target, 10f, w);
                    TailA.Target += -70f * w; TailB.Target += -20f * w; TailC.Target += 30f * w;
                    break;
                case Stance.Lie:
                    HipY.Target = Mathf.Lerp(HipY.Target, 8.5f, w); ShoulderY.Target = Mathf.Lerp(ShoulderY.Target, 10f, w);
                    HeadDX.Target = Mathf.Lerp(HeadDX.Target, 8f, w); HeadDY.Target = Mathf.Lerp(HeadDY.Target, 14f, w);
                    HindFold.Target = w; FrontFold.Target = w;
                    FootX[0].Target = Mathf.Lerp(FootX[0].Target, -9f, w); FootX[2].Target = Mathf.Lerp(FootX[2].Target, -4f, w);
                    FootX[1].Target = Mathf.Lerp(FootX[1].Target, 9f, w); FootX[3].Target = Mathf.Lerp(FootX[3].Target, 13f, w);
                    TailA.Target += -85f * w; TailB.Target += -30f * w; TailC.Target += 20f * w;
                    break;
                case Stance.Sleep:
                    HipY.Target = Mathf.Lerp(HipY.Target, 7.5f, w); ShoulderY.Target = Mathf.Lerp(ShoulderY.Target, 8f, w);
                    HeadDX.Target = Mathf.Lerp(HeadDX.Target, 4f, w); HeadDY.Target = Mathf.Lerp(HeadDY.Target, 10f, w); HeadPitch.Target += -22f * w;
                    HindFold.Target = w; FrontFold.Target = w;
                    FootX[0].Target = Mathf.Lerp(FootX[0].Target, -9f, w); FootX[2].Target = Mathf.Lerp(FootX[2].Target, -4f, w);
                    FootX[1].Target = Mathf.Lerp(FootX[1].Target, 8f, w); FootX[3].Target = Mathf.Lerp(FootX[3].Target, 12f, w);
                    TailA.Target += -95f * w; TailB.Target += -40f * w; TailC.Target += 10f * w;
                    EarL.Target += 10f * w; EarR.Target += 10f * w;
                    break;
            }
        }

        private void ApplyOverrides()
        {
            if (LookAt.HasValue)
            {
                Vector2 d = LookAt.Value - _head;
                GazeX.Target = Mathf.Clamp(d.x / 22f, -1f, 1f);
                GazeY.Target = Mathf.Clamp(d.y / 22f, -1f, 1f);
                HeadYaw.Target += Mathf.Clamp(-d.x / 40f, -0.6f, 0.6f);
                HeadPitch.Target += Mathf.Clamp(d.y * 0.8f, -18f, 18f);
            }
            if (Content > 0f)
            {
                float c = Mathf.Clamp01(Content);
                Squint.Target = Mathf.Lerp(Squint.Target, 1f, c);
                MouthCurve.Target = Mathf.Lerp(MouthCurve.Target, 1f, c);
                Blush.Target = Mathf.Max(Blush.Target, Mathf.Lerp(Blush.Target, 0.9f, c));
                BrowShow.Target = Mathf.Lerp(BrowShow.Target, 0f, c);
                Anger.Target = Mathf.Lerp(Anger.Target, 0f, c);
            }
            if (BlinkClose > 0f)
            {
                EyeOpenL.Target = Mathf.Min(EyeOpenL.Target, 1f - BlinkClose);
                EyeOpenR.Target = Mathf.Min(EyeOpenR.Target, 1f - BlinkClose);
            }
        }

        // ---- gait: feet placed by a lateral-sequence walk whenever the brain is walking ----

        private static readonly float[] PhaseOffset = { 0.5f, 0.75f, 0f, 0.25f };

        private void Gait(float dt)
        {
            bool airborne = !Grounded || Lifted;
            if (airborne)
            {
                // Legs dangle from their attachment points and sway with the motion.
                for (int i = 0; i < 4; i++)
                {
                    Vector2 a = LegAttach(i);
                    FootX[i].Target = a.x + Mathf.Sin(_time * 5f + i) * 1.5f - Vel.x * 0.01f;
                    FootY[i].Target = a.y - LegSeg * 1.7f;
                    FootX[i].Set(160f, 0.5f); FootY[i].Set(160f, 0.5f);
                }
                HindFold.Target = 0f; FrontFold.Target = 0f;
                TailA.Target += -Vel.y * 0.08f;
                return;
            }
            float speed = Mathf.Abs(WalkSpeed) > 0.01f && Grounded ? Mathf.Abs(Vel.x) : 0f;
            if (speed < 1.5f) return;
            GaitPhase = (GaitPhase + dt * speed / Stride) % 1f;
            const float duty = 0.62f;
            float stride = Mathf.Min(Stride, 4f + speed * 0.25f), stepH = 3.2f;
            for (int i = 0; i < 4; i++)
            {
                float t = (GaitPhase + PhaseOffset[i]) % 1f;
                float x, y;
                if (t < duty) { x = stride * (0.5f - t / duty); y = 0f; }
                else { float s = (t - duty) / (1f - duty); x = stride * (-0.5f + s); y = stepH * Mathf.Sin(s * Mathf.PI); }
                FootX[i].Target = RestFootX(i) + x; FootY[i].Target = y;
                FootX[i].Set(1400f, 0.9f); FootY[i].Set(1400f, 0.9f);
            }
            HindFold.Target = 0f; FrontFold.Target = 0f;
            ShoulderY.Target += 0.7f * Mathf.Sin(GaitPhase * Mathf.PI * 4f);
            HipY.Target += 0.7f * Mathf.Sin(GaitPhase * Mathf.PI * 4f + Mathf.PI);
            HeadPitch.Target += Mathf.Sin(GaitPhase * Mathf.PI * 4f) * 2f;
            TailA.Target += Mathf.Sin(GaitPhase * Mathf.PI * 2f) * 8f;
        }

        // ---- physics ----

        public void Jump(float speed)
        {
            if (!Grounded || Lifted) return;
            Grounded = false;
            Vel.y = speed;
            Crouch.Kick(-speed * 0.03f);
            EarL.Kick(speed * 0.35f); EarR.Kick(speed * 0.35f);
            TailA.Kick(-speed * 0.3f);
        }

        public void Push(Vector2 velocity)
        {
            Vel += velocity;
            if (Vel.y > 0f) Grounded = false;
        }

        private void Land(float impact)
        {
            Crouch.Kick(impact * 0.05f);
            EarL.Kick(impact * 0.5f); EarR.Kick(impact * 0.5f);
            TailA.Kick(impact * 0.4f);
            Soft.Ripple(impact * 0.1f, 2, Mathf.PI * 0.5f);
            Rot.Kick(-Vel.x * 0.4f);
        }

        private Vector2 _tether;

        private void StepPhysics(float dt)
        {
            Vector2 prevVel = Vel;
            if (Lifted)
            {
                const float k = 200f, c = 26f;
                Vector2 spring = k * (_tether - Pos) - c * Vel;
                Vel += (spring + new Vector2(0f, -Gravity)) * dt;
                Pos += Vel * dt;
                if (Pos.y <= 0f)
                {
                    Pos.y = 0f;
                    if (Vel.y < 0f) { if (!Grounded) Land(-Vel.y); Vel.y = 0f; }
                    Grounded = true;
                    Vel.x *= Mathf.Exp(-dt * 4f);
                }
                else if (Pos.y > 0.4f) Grounded = false;
            }
            else if (Grounded)
            {
                if (Mathf.Abs(WalkSpeed) > 0.01f) Vel.x = Mathf.Lerp(Vel.x, WalkSpeed, 1f - Mathf.Exp(-dt * 10f));
                else Vel.x *= Mathf.Exp(-dt * 7f);
                Vel.y = 0f; Pos.y = 0f;
                Pos.x += Vel.x * dt;
            }
            else
            {
                Vel.y -= Gravity * dt;
                Vel *= Mathf.Exp(-dt * 0.2f);
                Pos += Vel * dt;
                if (Pos.y <= 0f)
                {
                    Pos.y = 0f;
                    float impact = -Vel.y;
                    if (impact > 150f) Vel.y = impact * 0.25f;
                    else { Vel.y = 0f; Grounded = true; }
                    Land(impact);
                }
            }
            if (Pos.x > HalfWidth) { Pos.x = HalfWidth; if (Vel.x > 0f) { Vel.x = -Vel.x * 0.45f; Rot.Kick(Vel.x * 0.6f); } }
            if (Pos.x < -HalfWidth) { Pos.x = -HalfWidth; if (Vel.x < 0f) { Vel.x = -Vel.x * 0.45f; Rot.Kick(Vel.x * 0.6f); } }

            Vector2 accel = Vector2.ClampMagnitude((Vel - prevVel) / Mathf.Max(dt, 0.001f), 4000f);
            if (accel.sqrMagnitude > 2500f)
            {
                EarL.Kick(accel.y * 0.012f + accel.x * 0.008f); EarR.Kick(accel.y * 0.012f - accel.x * 0.008f);
                TailA.Kick(accel.y * 0.02f + accel.x * 0.02f * Facing);
                HeadPitch.Kick(-accel.x * 0.03f * Facing);
                if (!Grounded) Rot.Kick(-accel.x * 0.004f);
            }
            if (Lifted) Rot.Set(55f, 0.35f); else if (Grounded) Rot.Set(160f, 0.55f); else Rot.Set(30f, 0.3f);

            Soft.Step(dt);
            Rot.Step(dt); Crouch.Step(dt); Sway.Step(dt);
            HipY.Step(dt); ShoulderY.Step(dt); HeadDX.Step(dt); HeadDY.Step(dt); HeadPitch.Step(dt); HeadYaw.Step(dt);
            HindFold.Step(dt); FrontFold.Step(dt); PawLift.Step(dt);
            TailA.Step(dt); TailB.Step(dt); TailC.Step(dt); EarL.Step(dt); EarR.Step(dt);
            for (int i = 0; i < 4; i++) { FootX[i].Step(dt); FootY[i].Step(dt); }
            EyeOpenL.Step(dt); EyeOpenR.Step(dt); Squint.Step(dt); EyeScale.Step(dt); Pupil.Step(dt); LidTilt.Step(dt);
            BrowShow.Step(dt); BrowTilt.Step(dt); BrowRaise.Step(dt); MouthCurve.Step(dt); MouthOpen.Step(dt); MouthWidth.Step(dt);
            Blush.Step(dt); GazeX.Step(dt); GazeY.Step(dt);
            Tear.Step(dt); Sweat.Step(dt); Heart.Step(dt); Sparkle.Step(dt); Anger.Step(dt); Zzz.Step(dt); Bags.Step(dt); Dirt.Step(dt); Drool.Step(dt);
            RefreshPose();
        }

        private void RefreshPose()
        {
            Rot.Value = Mathf.Clamp(Rot.Value, -110f, 110f);
            _cos = Mathf.Cos(Rot.Value * Mathf.Deg2Rad);
            _sin = Mathf.Sin(Rot.Value * Mathf.Deg2Rad);
            float crouch = Mathf.Clamp(Crouch.Value, -6f, 8f);
            switch (Look.Plan)
            {
                case BodyPlan.Upright:
                    _hip = new Vector2(-6f, 10f - crouch * 0.4f); _shoulder = new Vector2(2f, 26f - crouch * 0.6f);
                    _head = new Vector2(3f + HeadYaw.Value * 2f, 34f - crouch * 0.6f);
                    break;
                case BodyPlan.Flat:
                    _hip = new Vector2(-14f, 8f); _shoulder = new Vector2(8f, 10f - crouch * 0.3f);
                    _head = new Vector2(15f, 17f + HeadDY.Value * 0.15f - crouch * 0.4f);
                    break;
                default:
                    _hip = new Vector2(-12f, Mathf.Max(6f, HipY.Value - crouch));
                    _shoulder = new Vector2(9f, Mathf.Max(7f, ShoulderY.Value - crouch));
                    _head = _shoulder + new Vector2(HeadDX.Value, HeadDY.Value) + new Vector2(0f, -crouch * 0.3f);
                    break;
            }
        }

        // ---- touch (unchanged model: tap / hold / drag along the floor / lift / throw / rub) ----

        private bool _pressed, _dragging, _petting, _heldFired;
        private float _pressTime, _lastDirX, _lastReversal, _lastPet, _purr, _flinchUntil, _contactAngle;
        private int _reversals;
        private Vector2 _pressLocal, _curLocal, _prevLocal, _fingerVel, _contactBody, _grab;
        private PetPart _part;
        public Vector2 LastContact { get; private set; }

        private Vector2 LocalPoint(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out Vector2 local);
            return local;
        }
        public void OnPointerDown(PointerEventData e) => Press(LocalPoint(e));
        public void OnDrag(PointerEventData e) => Move(LocalPoint(e));
        public void OnPointerUp(PointerEventData e) => Release();
        public void SimulatePress(Vector2 bodyPoint) => Press(Map(bodyPoint));
        public void SimulateMove(Vector2 bodyPoint) => Move(Map(bodyPoint));
        public void SimulateMoveWorld(Vector2 world) => Move(Origin + world * Unit);
        public void SimulateRelease() => Release();

        private void Press(Vector2 local)
        {
            if (!_ready) return;
            if (!HitTest(local, out _part)) _part = PetPart.Body;
            _pressed = true; _dragging = false; _petting = false; _heldFired = false;
            _pressTime = _time; _reversals = 0; _lastDirX = 0f; _purr = 0f;
            _pressLocal = _curLocal = _prevLocal = local;
            _fingerVel = Vector2.zero;
            _contactBody = BodyFromLocal(local);
            _grab = _contactBody - Center;
            Vector2 q = _contactBody - Center;
            _contactAngle = Mathf.Atan2(q.y, q.x);
            Soft.Impulse(_contactAngle, -120f, 0.6f);
            if (_part == PetPart.Head) { EarL.Kick(70f); EarR.Kick(70f); HeadPitch.Kick(_contactBody.y > _head.y ? -120f : 80f); }
            else if (_part == PetPart.Tail) { TailA.Kick(260f); TailB.Kick(-200f); }
            else if (_part == PetPart.Paws) PawLift.Kick(60f);
            else Crouch.Kick(2.5f);
            _flinchUntil = _time + 0.1f;
            LastContact = _contactBody;
            _tether = Pos;
        }

        private void Move(Vector2 local)
        {
            if (!_pressed) return;
            _prevLocal = _curLocal;
            _curLocal = local;
            Vector2 drag = _curLocal - _pressLocal;
            if (!_dragging && drag.magnitude > 12f) _dragging = true;
            Vector2 delta = _curLocal - _prevLocal;
            if (Mathf.Abs(delta.x) > 1.2f)
            {
                float dir = Mathf.Sign(delta.x);
                if (_lastDirX != 0f && dir != _lastDirX)
                {
                    _reversals++;
                    _lastReversal = _time;
                    if (_reversals >= 2 && Grounded && (_part == PetPart.Head || _part == PetPart.Body) && Mathf.Abs(drag.y) < 30f) _petting = true;
                }
                _lastDirX = dir;
            }
        }

        private void Release()
        {
            if (!_pressed) return;
            bool wasLifted = Lifted;
            _pressed = false;
            float held = _time - _pressTime;
            if (wasLifted)
            {
                Vel = Vector2.ClampMagnitude(_fingerVel / Unit, 750f);
                if (Pos.y > 0.01f || Vel.y > 0f) Grounded = false;
                Rot.Kick(Vel.x * 0.35f);
            }
            if (!_dragging && held < 0.35f) Tapped?.Invoke(_part);
        }

        private Vector2 GrabOffsetWorld()
        {
            Vector2 g = new Vector2(_grab.x * Facing, _grab.y);
            return new Vector2(g.x * _cos - g.y * _sin, g.x * _sin + g.y * _cos);
        }

        private void TouchTick(float dt)
        {
            if (!_pressed) return;
            if (_time < _flinchUntil) BlinkClose = 1f;
            float held = _time - _pressTime;
            Vector2 delta = _curLocal - _prevLocal;
            _fingerVel = Vector2.Lerp(_fingerVel, delta / Mathf.Max(dt, 0.001f), 0.5f);
            _prevLocal = _curLocal;
            Vector2 fingerBody = BodyFromLocal(_curLocal);
            LookAt = fingerBody;
            if (_time - _lastReversal > 1.5f) _reversals = 0;

            if (_petting)
            {
                Content = 1f;
                float dx = (_curLocal.x - _pressLocal.x) / Unit;
                Sway.Target += dx * 0.06f;
                HeadYaw.Target += dx * 0.02f;
                HeadPitch.Target += 10f;
                EarL.Target += 12f; EarR.Target += 12f;
                if (_time - _lastPet > 0.7f) { _lastPet = _time; Petted?.Invoke(); }
                return;
            }
            if (_dragging)
            {
                Vector2 fingerWorld = WorldFromLocal(_curLocal);
                Vector2 cm = new Vector2(Center.x * Facing, Center.y);
                Vector2 cmR = new Vector2(cm.x * _cos - cm.y * _sin, cm.x * _sin + cm.y * _cos);
                _tether = fingerWorld - GrabOffsetWorld() - cmR;   // body position that puts the grab point under the finger
                if (_tether.y < 0f) _tether.y = 0f;
                float lift = Mathf.Clamp01(Pos.y / 6f);
                Vector2 g = new Vector2(_grab.x * Facing, _grab.y);
                float hang = Mathf.DeltaAngle(0f, 90f - Mathf.Atan2(g.y, g.x) * Mathf.Rad2Deg);
                if (g.magnitude > 4f) Rot.Target += Mathf.Clamp(hang, -60f, 60f) * lift;
                bool dangling = lift > 0.3f;
                if (dangling)
                {
                    EyeScale.Target += 0.15f * lift; MouthOpen.Target += 0.3f * lift; MouthCurve.Target -= 0.5f * lift;
                    EarL.Target += 20f * lift; EarR.Target += 20f * lift;
                }
                else EyeScale.Target += 0.08f;
                return;
            }
            Soft.Hold(_contactAngle, -3f, 0.7f, 700f);
            if (held > 0.3f)
            {
                float c = Mathf.Clamp01((held - 0.3f) / 0.5f);
                Content = c;
                if (_part == PetPart.Head) { HeadPitch.Target += 12f * c; EarL.Target += 12f * c; EarR.Target += 12f * c; }
                else Crouch.Target += 1.5f * c;
                _purr += dt;
                Sway.Target += Mathf.Sin(_purr * 140f) * 0.2f * c;
                if (held > 0.9f && !_heldFired) { _heldFired = true; Held?.Invoke(_part); }
                if (held > 0.9f && _time - _lastPet > 1.4f) { _lastPet = _time; Petted?.Invoke(); }
            }
        }

        // ---- hit testing ----

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!_ready) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local);
            return HitTest(local, out _);
        }

        public bool HitTest(Vector2 local, out PetPart part)
        {
            for (int i = 0; i < _hits.Count; i++)
                if (VectorMesh.Contains(_hits[i].poly, local)) { part = _hits[i].part; return true; }
            part = PetPart.Body;
            return false;
        }

        private List<Vector2> HitPoly(PetPart part)
        {
            if (_hitPoolIndex == _hitPool.Count) _hitPool.Add(new List<Vector2>(48));
            var list = _hitPool[_hitPoolIndex++];
            list.Clear();
            _hits.Add((list, part));
            return list;
        }

        // ---- drawing ----

        private Color Ink => CreatureLook.Ink;
        private Color Outline { get { var c = Color.Lerp(Look.OutlineColor, CreatureLook.Ink, 0.35f); c.a = 0.9f; return c; } }
        private float OutlineW => 1.05f * Unit;
        private Color Limb => Look.Has(Marks.OrangeFeet) ? Orange : Look.Has(Marks.EyePatches) && Species == SpeciesType.Panda ? Look.Ear : Look.Body;
        private Color LimbShade => Look.Has(Marks.OrangeFeet) ? OrangeDark : Look.Has(Marks.EyePatches) && Species == SpeciesType.Panda ? Color.Lerp(Look.Ear, Color.black, 0.25f) : Look.Shade;

        // Flat part with one hard shadow tone on its lower part (clipped), plus the thin outline.
        private void Part(List<Vector2> poly, Color fill, Color shade, float shadeFraction = 0.38f, bool outline = true)
        {
            if (outline) { var ex = _vm.Poly(); _vm.Expand(poly, OutlineW, ex); _vm.Fill(ex, Outline); }
            _vm.Fill(poly, fill);
            if (shadeFraction > 0f && shade.a > 0f)
            {
                float minY = float.MaxValue, maxY = float.MinValue;
                for (int i = 0; i < poly.Count; i++) { minY = Mathf.Min(minY, poly[i].y); maxY = Mathf.Max(maxY, poly[i].y); }
                float cut = minY + (maxY - minY) * shadeFraction;
                var plane = _vm.Poly();
                VectorMesh.HalfPlaneAbove(new Vector2(0f, cut), 180f, 4000f, plane);   // region below the cut line
                var band = _vm.Poly();
                _vm.Clip(poly, plane, band);
                _vm.Fill(band, shade);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!_ready) return;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            _vm.Feather = 1.3f / Mathf.Max(0.3f, scale);
            _vm.Begin(vh);
            _hits.Clear();
            _hitPoolIndex = 0;

            DrawMoodGlow();
            DrawShadow();
            DrawTail();
            if (Quadruped) { if (HindFold.Value <= 0.6f) DrawLeg(0); if (FrontFold.Value <= 0.6f) DrawLeg(1); else DrawPaw(1); }
            if (Look.Ears == EarShape.Floppy) DrawEar(false);
            else DrawEar(false);
            DrawBody();
            if (Quadruped) { if (HindFold.Value > 0.35f) DrawHaunch(); if (HindFold.Value <= 0.6f) DrawLeg(2); else DrawPaw(2); if (FrontFold.Value <= 0.6f) DrawLeg(3); else DrawPaw(3); }
            if (Look.Plan != BodyPlan.Quadruped) DrawFlippers();
            DrawScarf();
            DrawHead();
            DrawEar(true);
            DrawFace();
            DrawConditions();
            DrawExtras();
            DrawHeadwear();
        }

        private void DrawMoodGlow()
        {
            _moodNow = Color.Lerp(_moodNow, Mood, 1f - Mathf.Exp(-Time.deltaTime * 4f));
            var c = _moodNow; c.a = 0.5f;
            float r = Size * 0.62f;
            _vm.SoftDisc(Map(Center + new Vector2(0f, 4f)), r, r * 0.9f, c, 40);
        }

        private void DrawShadow()
        {
            float k = 1f / (1f + Pos.y * 0.03f);
            Vector2 c = Origin + new Vector2(Pos.x + Sway.Value, 1f) * Unit;
            var disc = _vm.Poly();
            VectorMesh.Ellipse(c, 34f * Unit * k, 6.5f * Unit * k, 32, disc);
            _vm.Fill(disc, new Color(0.2f, 0.12f, 0.2f, 0.32f * k));
        }

        private void DrawTail()
        {
            if (Look.Tail == TailShape.None) return;
            Vector2 root = _hip + new Vector2(-7f, 2f);
            if (Look.Tail == TailShape.Stub)
            {
                var stub = _vm.Poly();
                VectorMesh.Ellipse(root + new Vector2(-2f, 2f + TailA.Value * 0.03f), 5f, 4.5f, 18, stub);
                MapOnly(stub);
                Part(stub, Look.Has(Marks.Cotton) ? Color.white : Look.Body, Look.Has(Marks.Cotton) ? new Color(0.9f, 0.86f, 0.9f) : Look.Shade);
                HitPoly(PetPart.Tail).AddRange(stub);
                return;
            }
            float a1 = 125f + TailA.Value, a2 = a1 - 40f + TailB.Value, a3 = a2 - 35f + TailC.Value;
            float len = Look.Tail == TailShape.ThinCurl ? 5f : 9f;
            Vector2 p1 = root + Dir(a1 * Mathf.Deg2Rad) * len, p2 = p1 + Dir(a2 * Mathf.Deg2Rad) * (len * 0.9f), p3 = p2 + Dir(a3 * Mathf.Deg2Rad) * (len * 0.8f);
            var path = _vm.Poly();
            VectorMesh.Bezier(root, p1, p2, p3, 14, path);
            float width = Look.Tail == TailShape.Bushy ? 9f : Look.Tail == TailShape.ThinCurl ? 2.6f : 5.5f;
            var hit = HitPoly(PetPart.Tail);
            Ribbon(path, width * 0.5f + 2f, hit); MapOnly(hit);
            MapOnly(path);
            _vm.Stroke(path, width * Unit + OutlineW * 2f, Outline);
            _vm.Stroke(path, width * Unit, Look.Body);
            if (Look.Has(Marks.TailTip)) { var tip = _vm.Poly(); VectorMesh.Ellipse(Map(p3), width * 0.5f * Unit, width * 0.5f * Unit, 16, tip); _vm.Fill(tip, Look.TailTip); }
            if (Look.Has(Marks.TailRings))
                foreach (float t in new[] { 0.35f, 0.65f })
                {
                    Vector2 pt = Bez(root, p1, p2, p3, t), tan = (Bez(root, p1, p2, p3, t + 0.02f) - pt).normalized, n = new Vector2(-tan.y, tan.x) * (width * 0.5f - 0.3f);
                    var ring = _vm.Poly(); ring.Add(Map(pt - n)); ring.Add(Map(pt + n));
                    _vm.Stroke(ring, 3f * Unit, Look.Ear, false);
                }
        }

        private static Vector2 Bez(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) { float u = 1f - t; return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3; }

        private static void Ribbon(List<Vector2> path, float half, List<Vector2> result)
        {
            result.Clear();
            int n = path.Count;
            for (int i = 0; i < n; i++) { Vector2 t = (i < n - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized; result.Add(path[i] + new Vector2(-t.y, t.x) * half); }
            for (int i = n - 1; i >= 0; i--) { Vector2 t = (i < n - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized; result.Add(path[i] - new Vector2(-t.y, t.x) * half); }
        }

        // Two-bone IK for a leg: attach → knee → foot. Front legs bend backward, hind legs forward.
        private void DrawLeg(int leg)
        {
            Vector2 a = LegAttach(leg);
            Vector2 f = new Vector2(FootX[leg].Value, FootY[leg].Value);
            if (leg == 3 && PawLift.Value > 0.1f) f += new Vector2(2f, PawLift.Value);
            Vector2 d = f - a;
            float dist = Mathf.Clamp(d.magnitude, 1.5f, LegSeg * 2f - 0.4f);
            d = d.normalized * dist;
            float cosT = Mathf.Clamp(dist / (2f * LegSeg), -1f, 1f);
            float theta = Mathf.Acos(cosT);
            float sign = IsHind(leg) ? 1f : -1f;
            Vector2 knee = a + Rotate(d.normalized, sign * theta * Mathf.Rad2Deg) * LegSeg;
            bool near = IsNear(leg);
            Color fill = near ? Limb : LimbShade, shade = near ? LimbShade : Color.Lerp(LimbShade, Color.black, 0.12f);
            var upper = _vm.Poly(); VectorMesh.Capsule(a, knee, 3.7f, 14, upper); MapOnly(upper);
            var lower = _vm.Poly(); VectorMesh.Capsule(knee, f + new Vector2(0f, 1.5f), 3.3f, 14, lower); MapOnly(lower);
            Part(upper, fill, shade, 0.5f);
            Part(lower, fill, shade, 0.35f);
            var paw = _vm.Poly(); VectorMesh.Ellipse(f + new Vector2(0.6f, 1.2f), 4.2f, 2.5f, 16, paw); MapOnly(paw);
            Part(paw, near ? Limb : LimbShade, shade, 0f);
            HitPoly(PetPart.Paws).AddRange(upper);
            HitPoly(PetPart.Paws).AddRange(lower);
        }

        private void DrawPaw(int leg)
        {
            Vector2 f = new Vector2(FootX[leg].Value, Mathf.Max(0f, FootY[leg].Value));
            bool near = IsNear(leg);
            var paw = _vm.Poly(); VectorMesh.Ellipse(f + new Vector2(0.6f, 1.4f), 4.4f, 2.6f, 16, paw); MapOnly(paw);
            Part(paw, near ? Limb : LimbShade, near ? LimbShade : Color.Lerp(LimbShade, Color.black, 0.12f), 0f);
            HitPoly(PetPart.Paws).AddRange(paw);
        }

        private void DrawHaunch()
        {
            var h = _vm.Poly();
            VectorMesh.Ellipse(_hip + new Vector2(-1f, -1.5f), 9.5f, 8.5f, 24, h);
            MapOnly(h);
            Part(h, Limb, LimbShade, 0.4f);
        }

        private void DrawBody()
        {
            var body = _vm.Poly();
            switch (Look.Plan)
            {
                case BodyPlan.Upright:
                    for (int i = 0; i < 40; i++)
                    {
                        float t = i / 40f * Mathf.PI * 2f;
                        float rx = 15f * (1f - 0.35f * Mathf.Max(0f, Mathf.Sin(t)));
                        body.Add(new Vector2(Mathf.Cos(t) * rx, 20f + Mathf.Sin(t) * 20f));
                    }
                    break;
                case BodyPlan.Flat:
                    VectorMesh.Ellipse(new Vector2(-4f, 9f), 24f, 9.5f, 40, body);
                    break;
                default:
                {
                    Vector2 c = Center;
                    VectorMesh.Capsule(_hip, _shoulder, 10.5f, 40, body);
                    for (int i = 0; i < body.Count; i++)
                    {
                        Vector2 q = body[i] - c; float ang = Mathf.Atan2(q.y, q.x);
                        float sag = q.y < 0f ? 1.5f * Mathf.Cos(Mathf.Clamp((body[i].x - c.x) / 14f, -1f, 1f) * Mathf.PI * 0.5f) : 0f;
                        body[i] += q.normalized * Soft.At(ang) + new Vector2(0f, -sag);
                    }
                    break;
                }
            }
            MapOnly(body);
            HitPoly(PetPart.Body).AddRange(body);
            Part(body, Look.Body, Look.Shade, 0.32f);
            // belly / chest
            var belly = _vm.Poly();
            if (Look.Plan == BodyPlan.Upright) VectorMesh.Ellipse(new Vector2(2f, 17f), 10f, 13f, 24, belly);
            else if (Look.Plan == BodyPlan.Flat) VectorMesh.Ellipse(new Vector2(0f, 6f), 16f, 5f, 24, belly);
            else VectorMesh.Ellipse(Center + new Vector2(2f, -4.5f), 11f, 5f, 24, belly);
            MapOnly(belly);
            if (!(Look.Has(Marks.EyePatches) && Species == SpeciesType.Panda)) _vm.Fill(belly, Look.Belly);
            if (Species == SpeciesType.Panda) { var band = _vm.Poly(); VectorMesh.Ellipse(_shoulder + new Vector2(-1f, 2f), 6f, 11f, 20, band); MapOnly(band); _vm.Fill(band, Look.Ear); }
            if (Look.Has(Marks.Stripes))
            {
                Color stripe = Color.Lerp(Look.Body, Outline, 0.35f);
                foreach (float x in new[] { -8f, -2f, 4f })
                {
                    var line = _vm.Poly(); Vector2 top = Center + new Vector2(x, 9f);
                    line.Add(Map(top)); line.Add(Map(top + new Vector2(1f, -4.5f)));
                    _vm.Stroke(line, 1.8f * Unit, stripe);
                }
            }
            if (Look.Has(Marks.Spikes))
            {
                Color spike = Color.Lerp(Look.Ear, Look.OutlineColor, 0.25f);
                for (int k = 0; k < 6; k++)
                {
                    float x = -14f + k * 5f;
                    var p = _vm.Poly(); Vector2 b = Center + new Vector2(x, 8f);
                    p.Add(b + new Vector2(-3f, 0f)); p.Add(b + new Vector2(3f, 0f)); p.Add(b + new Vector2(-1f + Mathf.Sin(_time + k) * 0.3f, 8f));
                    MapOnly(p); Part(p, spike, Color.Lerp(spike, Color.black, 0.15f), 0.3f);
                }
            }
        }

        private void DrawFlippers()
        {
            bool upright = Look.Plan == BodyPlan.Upright;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector2 a = upright ? new Vector2(s * 12f, 24f) : new Vector2(6f + s * 5f, 6f);
                float ang = upright ? (s > 0 ? -70f - PawLift.Value * 6f : -110f) : (s > 0 ? -35f : -150f);
                Vector2 tip = a + Dir(ang * Mathf.Deg2Rad) * (upright ? 12f : 9f);
                var fl = _vm.Poly(); VectorMesh.Capsule(a, tip, upright ? 3.5f : 3.2f, 14, fl); MapOnly(fl);
                Part(fl, s > 0 ? Look.Body : Look.Shade, Look.Shade, 0.35f);
                HitPoly(PetPart.Paws).AddRange(fl);
            }
            if (upright)
                foreach (float x in new[] { -5f, 6f })
                {
                    var ft = _vm.Poly(); VectorMesh.Ellipse(new Vector2(x, 1.2f), 5f, 2.4f, 14, ft); MapOnly(ft);
                    Part(ft, Orange, OrangeDark, 0f);
                }
            else { var tf = _vm.Poly(); VectorMesh.Ellipse(new Vector2(-27f, 5f + TailA.Value * 0.05f), 5f, 3f, 14, tf); MapOnly(tf); Part(tf, Look.Body, Look.Shade, 0.4f); }
        }

        private float HeadRx => Look.Plan == BodyPlan.Upright ? 0f : Look.Plan == BodyPlan.Flat ? 11f : 18.5f;
        private float HeadRy => Look.Plan == BodyPlan.Flat ? 10f : 17f;

        private void DrawHead()
        {
            if (Look.Plan == BodyPlan.Upright) return;
            var head = _vm.Poly();
            VectorMesh.Ellipse(_head, HeadRx, HeadRy, 36, head);
            MapOnly(head);
            HitPoly(PetPart.Head).AddRange(head);
            Part(head, Look.Body, Look.Shade, 0.16f);
            Vector2 F = FacePoint();
            if (Look.Has(Marks.Muzzle)) { var m = _vm.Poly(); VectorMesh.Ellipse(F + new Vector2(0.5f, -4.5f), 8f, 5f, 20, m); MapOnly(m); _vm.Fill(m, Look.Belly); }
            if (Look.Has(Marks.EyePatches))
                for (int s = -1; s <= 1; s += 2) { var p = _vm.Poly(); VectorMesh.Ellipse(F + new Vector2(s > 0 ? 6.5f : -6.5f, 2f), 5.5f, 6.5f, 20, p, s * 20f); MapOnly(p); _vm.Fill(p, Look.Ear); }
            if (Look.Has(Marks.DogPatch)) { var p = _vm.Poly(); VectorMesh.Ellipse(F + new Vector2(6.5f, 2.5f), 5.5f, 6f, 20, p); MapOnly(p); _vm.Fill(p, Look.Ear); }
            if (Look.Has(Marks.BrowSpots))
                for (int s = -1; s <= 1; s += 2) { var p = _vm.Poly(); VectorMesh.Ellipse(F + new Vector2(s > 0 ? 6.5f : -6.5f, 8f), 2.6f, 1.8f, 12, p); MapOnly(p); _vm.Fill(p, Look.Belly); }
            if (Look.Has(Marks.Stripes))
            {
                Color stripe = Color.Lerp(Look.Body, Outline, 0.35f);
                foreach (var (x, rot) in new[] { (-4f, 12f), (1f, 0f), (6f, -12f) })
                {
                    var line = _vm.Poly(); Vector2 c = _head + new Vector2(x + 1f, HeadRy * 0.62f);
                    line.Add(Map(c + Rotate(new Vector2(0f, -3f), rot))); line.Add(Map(c + Rotate(new Vector2(0f, 3f), rot)));
                    _vm.Stroke(line, 1.8f * Unit, stripe);
                }
            }
        }

        private Vector2 FacePoint() => Look.Plan == BodyPlan.Upright ? _head + new Vector2(2f + HeadYaw.Value * 3f, 0f) : _head + new Vector2(2f + HeadYaw.Value * 5f, -1f + HeadPitch.Value * 0.1f);

        private void DrawEar(bool near)
        {
            if (Look.Ears == EarShape.None) return;
            int s = near ? 1 : -1;
            float droop = near ? EarR.Value : EarL.Value;
            float rx, ry, pinch; bool inner = true;
            switch (Look.Ears)
            {
                case EarShape.Pointy: rx = 6.5f; ry = 9f; pinch = 0.95f; break;
                case EarShape.Tall: rx = 4.5f; ry = 14f; pinch = 0.3f; break;
                case EarShape.Round: rx = 6f; ry = 6f; pinch = 0f; break;
                case EarShape.Floppy: rx = 4.5f; ry = 9f; pinch = 0.3f; inner = false; break;
                default: rx = 3.6f; ry = 3.6f; pinch = 0f; inner = false; break;
            }
            Vector2 baseP; float dirDeg;
            if (Look.Plan == BodyPlan.Upright) return;
            if (Look.Ears == EarShape.Floppy) { baseP = _head + new Vector2(near ? 8f : -9f, 6f); dirDeg = 180f + s * 12f + droop * 0.5f; }
            else { baseP = _head + new Vector2(near ? 8.5f : -8.5f, HeadRy * 0.78f); dirDeg = s * -20f - s * droop; }
            var ear = _vm.Poly(); VectorMesh.Leaf(rx, ry, pinch, 20, ear);
            for (int i = 0; i < ear.Count; i++) ear[i] = baseP + Rotate(ear[i] + new Vector2(0f, -ry * 0.35f), dirDeg);
            MapOnly(ear);
            HitPoly(PetPart.Head).AddRange(ear);
            Part(ear, near ? Look.Ear : Color.Lerp(Look.Ear, Look.Shade, 0.5f), Look.Shade, near ? 0.25f : 0f);
            if (inner && near)
            {
                var ins = _vm.Poly(); VectorMesh.Leaf(rx * 0.55f, ry * 0.62f, pinch, 16, ins);
                for (int i = 0; i < ins.Count; i++) ins[i] = baseP + Rotate(ins[i] + new Vector2(0f, ry * 0.08f), dirDeg);
                MapOnly(ins); _vm.Fill(ins, Look.EarInner);
            }
        }

        private void DrawFace()
        {
            Vector2 F = FacePoint();
            Vector2 gaze = new Vector2(GazeX.Value, GazeY.Value);
            float scale = EyeScale.Value * Look.EyeSize;
            float squint = Mathf.Clamp01(Squint.Value);
            float spread = Look.Plan == BodyPlan.Upright ? 5f : 6.5f;
            // blush
            float bl = Mathf.Clamp01(Blush.Value);
            if (bl > 0.02f)
                for (int s = -1; s <= 1; s += 2)
                {
                    var b = _vm.Poly(); VectorMesh.Ellipse(F + new Vector2(s > 0 ? 11.5f : -11.5f, -3f), 3.6f, 2.2f, 16, b); MapOnly(b);
                    var c = BlushColor; c.a = 0.75f * bl; _vm.Fill(b, c);
                }
            for (int s = -1; s <= 1; s += 2)
            {
                float open = Mathf.Clamp01(s < 0 ? EyeOpenL.Value : EyeOpenR.Value);
                Vector2 e = F + new Vector2(s > 0 ? spread : -spread, 2f);
                float rx = 3.4f * scale, ry = 3.8f * scale * (1f - 0.3f * squint);
                Color skin = Look.Has(Marks.EyePatches) || (Look.Has(Marks.DogPatch) && s > 0) ? Look.Ear : Look.Body;
                var eye = _vm.Poly(); VectorMesh.Ellipse(e, rx, ry, 22, eye); MapOnly(eye);
                if (open > 0.02f)
                {
                    _vm.Fill(eye, Ink);
                    var hi = _vm.Poly(); VectorMesh.Ellipse(e + new Vector2(-rx * 0.3f, ry * 0.3f) + gaze * 0.5f, rx * 0.36f, rx * 0.36f, 10, hi); MapOnly(hi);
                    var hic = _vm.Poly(); _vm.Clip(hi, eye, hic); _vm.Fill(hic, Color.white);
                    if (squint > 0.02f)
                    {
                        float rd = ry * 2.4f; Vector2 d = new Vector2(e.x, e.y - ry - rd + squint * ry * 1.8f);
                        var disc = _vm.Poly(); VectorMesh.Ellipse(d, rd, rd, 30, disc); MapOnly(disc);
                        var cover = _vm.Poly(); _vm.Clip(eye, disc, cover); _vm.Fill(cover, skin);
                        var arch = _vm.Poly(); VectorMesh.Arc(d, rd, rd, 62f, 118f, 10, arch); MapOnly(arch);
                        _vm.Stroke(arch, 1.6f * scale * Unit, Color.Lerp(skin, Ink, squint * squint));
                    }
                    if (open < 0.98f)
                    {
                        float lidY = e.y + ry - (1f - open) * 2f * ry; float tilt = LidTilt.Value * s;
                        var plane = _vm.Poly(); VectorMesh.HalfPlaneAbove(new Vector2(e.x, lidY), tilt, 30f, plane); MapOnly(plane);
                        var cover = _vm.Poly(); _vm.Clip(eye, plane, cover); _vm.Fill(cover, skin);
                    }
                }
                if (open < 0.5f)
                {
                    var arc = _vm.Poly(); VectorMesh.Arc(new Vector2(e.x, e.y + ry * 0.5f), rx * 0.95f, ry * 0.8f, 205f, 335f, 10, arc); MapOnly(arc);
                    _vm.Stroke(arc, 1.5f * scale * Unit, Color.Lerp(skin, Ink, Mathf.Clamp01((0.5f - open) * 2.5f)));
                }
                if (Bags.Value > 0.02f) _vm.SoftDisc(Map(e + new Vector2(0f, -ry * 1.2f)), rx * 1.5f * Unit, ry * 0.6f * Unit, new Color(0.35f, 0.25f, 0.45f, 0.3f * Bags.Value));
            }
            // brows
            float show = Mathf.Clamp01(BrowShow.Value);
            if (show > 0.02f)
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 b = F + new Vector2(s > 0 ? spread : -spread, 6.5f + BrowRaise.Value); float tilt = BrowTilt.Value * s;
                    var line = _vm.Poly(); line.Add(Map(b + Rotate(new Vector2(-2.6f, 0f), tilt))); line.Add(Map(b + Rotate(new Vector2(2.6f, 0f), tilt)));
                    _vm.Stroke(line, 1.5f * Unit, Color.Lerp(Look.Body, Ink, show));
                }
            // nose + mouth (on the muzzle, forward of the eyes)
            Vector2 n = F + new Vector2(0.5f, -3.2f);
            float mouthY = n.y - 3f;
            switch (Look.Nose)
            {
                case NoseKind.PinkTriangle: { var t = _vm.Poly(); t.Add(Map(n + new Vector2(-2f, 1f))); t.Add(Map(n + new Vector2(2f, 1f))); t.Add(Map(n + new Vector2(0f, -1.5f))); _vm.Fill(t, PinkNose); break; }
                case NoseKind.DarkDot: { var d = _vm.Poly(); VectorMesh.Ellipse(n, 2.1f, 1.5f, 12, d); MapOnly(d); _vm.Fill(d, Ink); break; }
                case NoseKind.Snout: { var sn = _vm.Poly(); VectorMesh.Ellipse(n + new Vector2(1f, -0.5f), 5f, 3.6f, 18, sn); MapOnly(sn); Part(sn, UIFactory.Hex("FF9CB0"), UIFactory.Hex("E884A0"), 0.35f); for (int s = -1; s <= 1; s += 2) { var h = _vm.Poly(); VectorMesh.Ellipse(n + new Vector2(1f + s * 1.8f, -0.5f), 0.8f, 1.2f, 8, h); MapOnly(h); _vm.Fill(h, Color.Lerp(UIFactory.Hex("FF9CB0"), Ink, 0.5f)); } mouthY = n.y - 5.5f; break; }
                case NoseKind.Beak:
                {
                    float opn = Mathf.Clamp01(MouthOpen.Value);
                    var bk = _vm.Poly(); bk.Add(Map(n + new Vector2(-2f, 2f))); bk.Add(Map(n + new Vector2(-2f, -1.5f))); bk.Add(Map(n + new Vector2(7f, 0f))); Part(bk, Orange, OrangeDark, 0.5f);
                    if (opn > 0.05f) { var lw = _vm.Poly(); lw.Add(Map(n + new Vector2(-2f, -1.5f))); lw.Add(Map(n + new Vector2(-1f, -2.5f - opn * 3f))); lw.Add(Map(n + new Vector2(6f, -1f - opn * 2f))); _vm.Fill(lw, OrangeDark); }
                    DrawWhiskersAndTongue(F); return;
                }
            }
            float curve = MouthCurve.Value, opn2 = Mathf.Clamp01(MouthOpen.Value), w = 5.5f * MouthWidth.Value;
            Vector2 m = new Vector2(n.x, mouthY);
            Color mouthSkin = Look.Has(Marks.Muzzle) ? Look.Belly : Look.Body;
            float lineAlpha = Mathf.Clamp01(1f - opn2 * 1.6f);
            if (lineAlpha > 0.03f)
            {
                var path = _vm.Poly();
                if (Look.Mouth == MouthKind.Cat)
                {
                    VectorMesh.Quadratic(m + new Vector2(-w * 0.5f, curve * 1.1f), m + new Vector2(-w * 0.25f, -curve * 2.2f), m + new Vector2(0f, curve * 1.1f), 6, path);
                    VectorMesh.Quadratic(m + new Vector2(0f, curve * 1.1f), m + new Vector2(w * 0.25f, -curve * 2.2f), m + new Vector2(w * 0.5f, curve * 1.1f), 6, path, true);
                }
                else VectorMesh.Quadratic(m + new Vector2(-w * 0.5f, curve * 1.4f), m + new Vector2(0f, -curve * 2.4f), m + new Vector2(w * 0.5f, curve * 1.4f), 8, path);
                MapOnly(path);
                _vm.Stroke(path, 1.2f * Unit, Color.Lerp(mouthSkin, Ink, lineAlpha));
            }
            if (opn2 > 0.05f)
            {
                float rx = 1.5f + 2.2f * opn2, ry = 0.8f + 2.6f * opn2;
                var mo = _vm.Poly(); VectorMesh.Ellipse(m + new Vector2(0f, -opn2), rx, ry, 16, mo); MapOnly(mo); _vm.Fill(mo, Ink);
                var ins = _vm.Poly(); VectorMesh.Ellipse(m + new Vector2(0f, -opn2 - ry * 0.25f), rx * 0.6f, ry * 0.5f, 12, ins); MapOnly(ins); _vm.Fill(ins, MouthInside);
            }
            DrawWhiskersAndTongue(F);
        }

        private void DrawWhiskersAndTongue(Vector2 F)
        {
            if (Look.Has(Marks.Tongue) && MouthOpen.Value < 0.5f) { var t = _vm.Poly(); VectorMesh.Ellipse(F + new Vector2(1.5f, -7.5f), 1.8f, 2.4f, 12, t); MapOnly(t); _vm.Fill(t, UIFactory.Hex("F27FA5")); }
            if (Look.Whiskers == 0 || Look.Plan == BodyPlan.Quadruped) return;
            Color c = Color.Lerp(Look.Body, Ink, 0.45f);
            for (int i = 0; i < Mathf.Min(2, Look.Whiskers); i++)
            {
                float y = -2f - i * 2.5f;
                var a = _vm.Poly(); a.Add(Map(F + new Vector2(11f, y))); a.Add(Map(F + new Vector2(18f, y - 1f + i * 2f))); _vm.Stroke(a, 0.8f * Unit, c);
                var b = _vm.Poly(); b.Add(Map(F + new Vector2(-8f, y))); b.Add(Map(F + new Vector2(-15f, y - 1f + i * 2f))); _vm.Stroke(b, 0.8f * Unit, c);
            }
        }

        private void DrawConditions()
        {
            if (Dirt.Value > 0.02f)
                foreach (var (x, y) in new[] { (-8f, -2f), (6f, 1f), (-1f, -5f) })
                    _vm.SoftDisc(Map(Center + new Vector2(x, y)), 6f * Unit, 4f * Unit, new Color(0.45f, 0.3f, 0.2f, 0.32f * Dirt.Value));
            if (Drool.Value > 0.02f)
            {
                float drip = (_time * 0.6f) % 1f; var drop = _vm.Poly();
                VectorMesh.Ellipse(FacePoint() + new Vector2(3f, -6f - drip * 2.5f), 1.2f, 2.2f + drip, 12, drop); MapOnly(drop);
                var c = UIFactory.Sky; c.a = Drool.Value; _vm.Fill(drop, c);
            }
        }

        private void DrawExtras()
        {
            Vector2 hd = _head;
            if (Tear.Value > 0.02f)
            {
                float fall = (_time * 1.1f) % 1f; var drop = _vm.Poly();
                VectorMesh.Ellipse(FacePoint() + new Vector2(-8f, -3f - fall * 7f), 1.5f, 2.5f, 12, drop); MapOnly(drop);
                var c = UIFactory.Sky; c.a = Tear.Value * (1f - fall * 0.6f); _vm.Fill(drop, c);
            }
            if (Sweat.Value > 0.02f)
            {
                float slide = (_time * 0.45f) % 1f; var drop = _vm.Poly();
                VectorMesh.Ellipse(hd + new Vector2(14f, 10f - slide * 6f), 2f, 3.2f, 12, drop); MapOnly(drop);
                var c = UIFactory.Sky; c.a = Sweat.Value; _vm.Fill(drop, c);
            }
            if (Heart.Value > 0.02f)
            {
                var heart = _vm.Poly(); float beat = 1f + Mathf.Max(0f, Mathf.Sin(_time * 5f)) * 0.15f;
                VectorMesh.Heart(hd + new Vector2(16f, 16f + Mathf.Sin(_time * 2.5f) * 1.5f), 5.5f * beat, 24, heart); MapOnly(heart);
                var c = UIFactory.Pink; c.a = Heart.Value; _vm.Fill(heart, c);
            }
            if (Sparkle.Value > 0.02f)
            {
                float tw = 0.8f + 0.25f * Mathf.Sin(_time * 7f); var c = UIFactory.Butter; c.a = Sparkle.Value;
                var star = _vm.Poly(); VectorMesh.Star(hd + new Vector2(-17f, 13f), 4.5f * tw, 1.7f * tw, 4, star, _time * 20f); MapOnly(star); _vm.Fill(star, c);
                var star2 = _vm.Poly(); VectorMesh.Star(hd + new Vector2(-11f, 19f), 2.5f, 1f, 4, star2, -_time * 30f); MapOnly(star2); _vm.Fill(star2, c);
            }
            if (Anger.Value > 0.02f)
            {
                var c = UIFactory.PinkDark; c.a = Anger.Value; Vector2 o = hd + new Vector2(12f, 17f); float pulse = 1f + Mathf.Sin(_time * 12f) * 0.08f;
                foreach (var (dx, dy) in new[] { (1f, 1f), (1f, -1f) }) { var line = _vm.Poly(); line.Add(Map(o + new Vector2(-dx, -dy) * 3f * pulse)); line.Add(Map(o + new Vector2(dx, dy) * 3f * pulse)); _vm.Stroke(line, 1.8f * Unit, c, false); }
            }
            if (Zzz.Value > 0.02f)
                for (int k = 0; k < 3; k++)
                {
                    float phase = (_time * 0.35f + k * 0.33f) % 1f; float size = 3.5f + k * 1.8f + phase * 2f;
                    Vector2 o = hd + new Vector2(14f + k * 6f + phase * 7f, 12f + k * 7f + phase * 12f);
                    var z = _vm.Poly(); z.Add(Map(o + new Vector2(-size * 0.5f, size * 0.5f))); z.Add(Map(o + new Vector2(size * 0.5f, size * 0.5f))); z.Add(Map(o + new Vector2(-size * 0.5f, -size * 0.5f))); z.Add(Map(o + new Vector2(size * 0.5f, -size * 0.5f)));
                    var c = UIFactory.Lavender; c.a = Zzz.Value * Mathf.Min(1f, phase * 4f) * (1f - phase); _vm.Stroke(z, 1.4f * Unit, c, false);
                }
        }

        private void DrawScarf()
        {
            if (Accessory != "scarf_star") return;
            Vector2 neck = Look.Plan == BodyPlan.Quadruped ? _shoulder + new Vector2(4f, 6f) : _shoulder + new Vector2(0f, 2f);
            var band = _vm.Poly(); VectorMesh.Ellipse(neck, 10f, 4.5f, 22, band, -12f); MapOnly(band); Part(band, Gold, UIFactory.Hex("D99A2B"), 0.4f);
            var tail = _vm.Poly(); tail.Add(Map(neck + new Vector2(6f, -2f))); tail.Add(Map(neck + new Vector2(9f, -9f))); _vm.Stroke(tail, 4.5f * Unit + OutlineW * 2f, Outline); _vm.Stroke(tail, 4.5f * Unit, Gold);
        }

        private void DrawHeadwear()
        {
            Vector2 top = _head + new Vector2(0f, HeadRy * 0.92f);
            if (Look.Plan == BodyPlan.Upright) top = new Vector2(2f, 40f);
            switch (Accessory)
            {
                case "hat_beanie":
                {
                    var dome = _vm.Poly(); VectorMesh.Ellipse(top + new Vector2(0f, -1f), 14f, 9f, 28, dome); MapOnly(dome);
                    var plane = _vm.Poly(); VectorMesh.HalfPlaneAbove(top + new Vector2(0f, -1.5f), 0f, 60f, plane); MapOnly(plane);
                    var cap = _vm.Poly(); _vm.Clip(dome, plane, cap); Part(cap, UIFactory.Primary, Color.Lerp(UIFactory.Primary, Color.black, 0.18f), 0.35f);
                    var band = _vm.Poly(); VectorMesh.Capsule(top + new Vector2(-13f, -1f), top + new Vector2(13f, -1f), 2.2f, 12, band); MapOnly(band); Part(band, Color.white, new Color(0.9f, 0.88f, 0.92f), 0.4f);
                    var pom = _vm.Poly(); VectorMesh.Ellipse(top + new Vector2(0f, 8.5f), 3.2f, 3.2f, 14, pom); MapOnly(pom); Part(pom, Color.white, new Color(0.9f, 0.88f, 0.92f), 0.4f);
                    break;
                }
                case "crown_tiny":
                {
                    var band = _vm.Poly(); VectorMesh.Ellipse(top + new Vector2(0f, 1f), 8f, 3f, 18, band); MapOnly(band); Part(band, Gold, UIFactory.Hex("D99A2B"), 0.4f);
                    foreach (var (x, y, col) in new[] { (-5f, 5f, UIFactory.Pink), (0f, 6f, UIFactory.Sky), (5f, 5f, UIFactory.Pink) })
                    { var j = _vm.Poly(); VectorMesh.Ellipse(top + new Vector2(x, y), 2f, 2f, 12, j); MapOnly(j); Part(j, col, Color.Lerp(col, Color.black, 0.2f), 0.4f); }
                    break;
                }
                case "bow_cherry":
                {
                    Vector2 p = _head + new Vector2(-9f, HeadRy * 0.7f);
                    for (int s = -1; s <= 1; s += 2) { var l = _vm.Poly(); VectorMesh.Ellipse(p + new Vector2(s * 3.5f, 0f), 3.8f, 2.8f, 14, l, s * 15f); MapOnly(l); Part(l, UIFactory.Coral, Color.Lerp(UIFactory.Coral, Color.black, 0.2f), 0.4f); }
                    var k = _vm.Poly(); VectorMesh.Ellipse(p, 2f, 2f, 10, k); MapOnly(k); _vm.Fill(k, UIFactory.PinkDark);
                    break;
                }
            }
        }
    }
}
