using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gotchi.Creature
{
    // Behaviour for the four-legged rig: it stands, sits, lies down and sleeps on its own schedule, looks
    // around, grooms, stretches, flicks its tail and shuffles to new spots — all as spring targets so every
    // change blends. Game reactions are OneShots layered on top. Calm by design: nothing hops on its own.
    public sealed class CreatureBrain
    {
        private sealed class Shot
        {
            public OneShot Clip; public float T, Duration, Dir, Amp; public bool Hold; public int Side;
            public float N => Mathf.Clamp01(T / Duration);
        }

        private readonly CreatureBody _b;
        private readonly List<Shot> _shots = new List<Shot>();
        private LoopClip _loop = LoopClip.Idle;
        private Stance _stance = Stance.Stand;
        private float _t, _seed, _breath, _stanceUntil;
        private float _nextBlink, _blinkStart = -1f, _nextFidget, _nextSaccade, _nextSniff;
        private bool _doubleBlink;
        private Vector2 _gazeTarget;
        private bool _walking;
        private float _walkTarget, _walkSpeed;
        private Action _onArrive;
        private LoopClip _returnLoop;

        public LoopClip Loop => _loop;
        public Stance CurrentStance => _stance;
        public bool Fainted { get; private set; }

        public CreatureBrain(CreatureBody body)
        {
            _b = body;
            _seed = UnityEngine.Random.Range(0f, 100f);
            _t = UnityEngine.Random.Range(0f, 10f);
            _nextBlink = _t + UnityEngine.Random.Range(1f, 4f);
            _nextFidget = _t + UnityEngine.Random.Range(5f, 12f);
            _nextSaccade = _t + UnityEngine.Random.Range(0.5f, 2f);
            _stanceUntil = _t + UnityEngine.Random.Range(6f, 14f);
        }

        public void SetLoop(LoopClip loop) => _loop = loop;
        public void SetStance(Stance stance, float holdSeconds = 20f) { _stance = stance; _stanceUntil = _t + holdSeconds; }

        public void Play(OneShot clip, float direction = 1f)
        {
            if (Fainted && clip != OneShot.Faint) return;
            float duration; bool hold = false;
            switch (clip)
            {
                case OneShot.Hop: duration = 0.5f; break;
                case OneShot.Wiggle: duration = 0.7f; break;
                case OneShot.Pat: duration = 0.9f; break;
                case OneShot.WaveL: case OneShot.WaveR: duration = 1.2f; break;
                case OneShot.TailFlick: duration = 0.8f; break;
                case OneShot.Attack: duration = 0.7f; break;
                case OneShot.Hurt: duration = 0.6f; break;
                case OneShot.Faint: duration = 0.9f; hold = true; Fainted = true; break;
                case OneShot.Eat: duration = 1.6f; break;
                case OneShot.Celebrate: duration = 1.4f; break;
                case OneShot.Dance: duration = 2f; break;
                case OneShot.Nod: duration = 0.7f; break;
                case OneShot.Shiver: duration = 1.1f; break;
                case OneShot.Stretch: duration = 2.4f; break;
                case OneShot.Yawn: duration = 1.8f; break;
                case OneShot.Shake: duration = 0.8f; break;
                case OneShot.EarTwitch: duration = 0.5f; break;
                case OneShot.LookAround: duration = 3f; break;
                case OneShot.Sniff: duration = 1.1f; break;
                case OneShot.Groom: duration = 2.6f; break;
                default: duration = 0.5f; break;
            }
            _shots.RemoveAll(s => s.Clip == clip);
            _shots.Add(new Shot { Clip = clip, Duration = duration, Dir = direction, Amp = UnityEngine.Random.Range(0.85f, 1.15f), Hold = hold, Side = UnityEngine.Random.value < 0.5f ? -1 : 1 });
            if (clip == OneShot.Stretch || clip == OneShot.Attack || clip == OneShot.Hop || clip == OneShot.Celebrate || clip == OneShot.Dance) SetStance(Stance.Stand, 8f);
            if (clip == OneShot.WaveL || clip == OneShot.WaveR || clip == OneShot.Groom) SetStance(Stance.Sit, 12f);
        }

        public void Revive()
        {
            Fainted = false;
            _shots.RemoveAll(s => s.Clip == OneShot.Faint);
            SetStance(Stance.Stand, 6f);
        }

        public void WalkTo(float targetX, float speed = 22f, Action onArrive = null)
        {
            _walking = true;
            _walkTarget = Mathf.Clamp(targetX, -_b.HalfWidth, _b.HalfWidth);
            _walkSpeed = Mathf.Max(5f, speed);
            _onArrive = onArrive;
            _returnLoop = _loop == LoopClip.Walk ? LoopClip.Idle : _loop;
            _loop = LoopClip.Walk;
            _b.Facing = _walkTarget >= _b.Pos.x ? 1f : -1f;
            SetStance(Stance.Stand, 6f);
        }

        public void StopWalking()
        {
            if (!_walking) return;
            _walking = false; _b.WalkSpeed = 0f; _loop = _returnLoop;
        }

        public void Tick(float dt)
        {
            _t += dt;
            Schedule();
            Posture();
            Breathe(dt);
            Gaze();
            Blink();
            Fidget();
            Walk(dt);
            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                var s = _shots[i];
                float prev = s.T <= 0f ? -0.001f : s.N;
                s.T += dt;
                Evaluate(s, prev, s.N);
                if (s.T >= s.Duration && !s.Hold) _shots.RemoveAt(i);
            }
        }

        private float Noise(float speed, float offset) => Motion.Noise(_t * speed, _seed + offset);

        // Rest cycle when left alone: stand a while → sit → lie → (sleep if tired) → stand again.
        private void Schedule()
        {
            if (_t < _stanceUntil || _walking || _b.Touching || Fainted) return;
            LoopClip loop = _b.Sleeping ? LoopClip.Sleep : _loop;
            switch (loop)
            {
                case LoopClip.Sleep: _stance = Stance.Sleep; _stanceUntil = _t + 30f; break;
                case LoopClip.Alert: _stance = Stance.Stand; _stanceUntil = _t + UnityEngine.Random.Range(6f, 12f); break;
                case LoopClip.Sad: _stance = UnityEngine.Random.value < 0.6f ? Stance.Lie : Stance.Sit; _stanceUntil = _t + UnityEngine.Random.Range(15f, 30f); break;
                case LoopClip.Happy: _stance = UnityEngine.Random.value < 0.7f ? Stance.Stand : Stance.Sit; _stanceUntil = _t + UnityEngine.Random.Range(8f, 16f); break;
                default:
                    _stance = _stance == Stance.Stand ? Stance.Sit : _stance == Stance.Sit ? (UnityEngine.Random.value < 0.6f ? Stance.Lie : Stance.Stand) : Stance.Stand;
                    _stanceUntil = _t + UnityEngine.Random.Range(_stance == Stance.Stand ? 6f : 14f, _stance == Stance.Stand ? 14f : 30f);
                    break;
            }
        }

        private void Posture()
        {
            var b = _b;
            Stance st = b.Sleeping ? Stance.Sleep : _walking ? Stance.Stand : _stance;
            if (st != Stance.Stand) b.SetStance(st);
            switch (b.Sleeping ? LoopClip.Sleep : _loop)
            {
                case LoopClip.Idle:
                    b.HeadYaw.Target += Noise(0.12f, 1f) * 0.25f;
                    b.HeadPitch.Target += Noise(0.15f, 2f) * 4f;
                    b.TailA.Target += Noise(0.2f, 3f) * 12f; b.TailC.Target += Noise(0.3f, 4f) * 15f;
                    break;
                case LoopClip.Happy:
                    b.EarL.Target -= 5f; b.EarR.Target -= 5f;
                    b.TailA.Target += 15f + Mathf.Sin(_t * 4f) * 18f; b.TailB.Target += Mathf.Sin(_t * 4f + 1f) * 12f;
                    b.HeadPitch.Target += 4f + Mathf.Sin(_t * 1.8f) * 2f;
                    b.HeadYaw.Target += Noise(0.2f, 1f) * 0.2f;
                    break;
                case LoopClip.Sad:
                    b.HeadPitch.Target += -16f; b.HeadDY.Target -= 3f;
                    b.EarL.Target += 22f; b.EarR.Target += 22f;
                    b.TailA.Target += -50f; b.TailB.Target += -20f;
                    b.GazeY.Target -= 0.4f;
                    if (_t >= _nextSniff) { b.Crouch.Kick(1.2f); _nextSniff = _t + UnityEngine.Random.Range(4f, 8f); }
                    break;
                case LoopClip.Sleep:
                    b.EarL.Target += 12f; b.EarR.Target += 12f;
                    break;
                case LoopClip.Alert:
                    b.HipY.Target += 1f; b.ShoulderY.Target += 1.5f; b.HeadDY.Target += 2f;
                    b.EarL.Target -= 10f; b.EarR.Target -= 10f;
                    b.TailA.Target += 25f + Mathf.Sin(_t * 2f) * 6f;
                    b.HeadYaw.Target += Noise(0.5f, 5f) * 0.4f;
                    break;
                case LoopClip.Walk:
                    b.HeadPitch.Target += -3f;
                    break;
            }
        }

        private void Breathe(float dt)
        {
            float rate, amp;
            switch (_b.Sleeping ? LoopClip.Sleep : _loop)
            {
                case LoopClip.Happy: rate = 0.45f; amp = 0.9f; break;
                case LoopClip.Sad: rate = 0.3f; amp = 0.8f; break;
                case LoopClip.Sleep: rate = 0.22f; amp = 1.3f; break;
                case LoopClip.Alert: rate = 0.55f; amp = 0.7f; break;
                default: rate = 0.33f; amp = 0.8f; break;
            }
            _breath += dt * Mathf.PI * 2f * rate * (1f + Noise(0.1f, 6f) * 0.15f);
            float s = Mathf.Sin(_breath);
            _b.ShoulderY.Target += amp * 0.5f * s; _b.HipY.Target += amp * 0.35f * s;
            _b.HeadDY.Target += amp * 0.3f * s;
            _b.EarL.Target += s * 1.2f; _b.EarR.Target += s * 1.2f;
        }

        private void Gaze()
        {
            if (_b.Sleeping) return;
            if (_t >= _nextSaccade)
            {
                _gazeTarget = UnityEngine.Random.value < 0.45f ? Vector2.zero : new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-0.5f, 0.6f));
                _nextSaccade = _t + (_loop == LoopClip.Alert ? UnityEngine.Random.Range(0.8f, 2.5f) : UnityEngine.Random.Range(2f, 6f));
            }
            _b.GazeX.Target += _gazeTarget.x + Noise(0.4f, 7f) * 0.12f;
            _b.GazeY.Target += _gazeTarget.y + Noise(0.4f, 8f) * 0.08f;
        }

        private void Blink()
        {
            if (_b.Sleeping) { _blinkStart = -1f; return; }
            if (_blinkStart < 0f && _t >= _nextBlink)
            {
                _blinkStart = _t; _doubleBlink = UnityEngine.Random.value < 0.15f;
                _nextBlink = _t + UnityEngine.Random.Range(2.5f, 7f);
            }
            if (_blinkStart >= 0f)
            {
                float e = _t - _blinkStart;
                bool closed = e < 0.09f || (_doubleBlink && e > 0.2f && e < 0.29f);
                if (closed) _b.BlinkClose = 1f;
                if (e > (_doubleBlink ? 0.29f : 0.09f)) _blinkStart = -1f;
            }
        }

        private void Fidget()
        {
            if (_t < _nextFidget) return;
            _nextFidget = _t + (_loop == LoopClip.Alert ? UnityEngine.Random.Range(4f, 9f) : UnityEngine.Random.Range(7f, 16f));
            if (Fainted || _walking || _b.Touching || _b.Sleeping || !_b.Grounded) return;
            if (_loop != LoopClip.Idle && _loop != LoopClip.Happy && _loop != LoopClip.Alert) return;
            (OneShot clip, float weight)[] pool;
            if (_stance == Stance.Sit) pool = new[] { (OneShot.Groom, 4f), (OneShot.LookAround, 3f), (OneShot.EarTwitch, 2f), (OneShot.TailFlick, 2f), (OneShot.Yawn, 1f) };
            else if (_stance == Stance.Lie) pool = new[] { (OneShot.LookAround, 3f), (OneShot.EarTwitch, 2f), (OneShot.TailFlick, 3f), (OneShot.Yawn, 2f), (OneShot.Sniff, 1f) };
            else if (_loop == LoopClip.Alert) pool = new[] { (OneShot.LookAround, 4f), (OneShot.EarTwitch, 3f), (OneShot.TailFlick, 1f), (OneShot.Sniff, 2f) };
            else pool = new[] { (OneShot.LookAround, 4f), (OneShot.EarTwitch, 3f), (OneShot.TailFlick, 2f), (OneShot.Stretch, 2f), (OneShot.Sniff, 2f), (OneShot.Shake, 1f) };
            float total = 0f; foreach (var p in pool) total += p.weight;
            float roll = UnityEngine.Random.value * total; OneShot pick = pool[0].clip;
            foreach (var p in pool) { roll -= p.weight; if (roll <= 0f) { pick = p.clip; break; } }
            if (pick == OneShot.TailFlick && _b.Look.Tail == TailShape.None) pick = OneShot.Wiggle;
            if (pick == OneShot.EarTwitch && _b.Look.Ears == EarShape.None) pick = OneShot.LookAround;
            if ((pick == OneShot.Stretch || pick == OneShot.Groom) && !_b.Quadruped) pick = OneShot.LookAround;
            Play(pick);
        }

        private void Walk(float dt)
        {
            if (!_walking) return;
            float dx = _walkTarget - _b.Pos.x;
            if (Mathf.Abs(dx) < 1f || (!_b.Grounded && _b.Pos.y > 4f))
            {
                _walking = false; _b.WalkSpeed = 0f; _loop = _returnLoop;
                var cb = _onArrive; _onArrive = null; cb?.Invoke();
                return;
            }
            _b.Facing = dx >= 0f ? 1f : -1f;
            _b.WalkSpeed = Mathf.Sign(dx) * _walkSpeed;
            if (!_b.Quadruped)
            {
                float st = Mathf.Sin(_t * 9f);
                _b.Rot.Target += st * (_b.Look.Plan == BodyPlan.Upright ? 5f : 3f);
                _b.Crouch.Target += Mathf.Abs(st) * 1.2f;
            }
        }

        private static bool Crossed(float prev, float now, float at) => prev < at && now >= at;

        private void Evaluate(Shot s, float prev, float t)
        {
            var b = _b;
            float hump = Motion.Hump(t), fade = 1f - t, amp = s.Amp, dir = s.Dir;
            switch (s.Clip)
            {
                case OneShot.Hop:
                    if (Crossed(prev, t, 0f)) b.Jump(300f * amp);
                    break;
                case OneShot.Wiggle:
                    b.Rot.Target += Mathf.Sin(t * Mathf.PI * 5f) * 5f * amp * fade;
                    b.HeadYaw.Target += Mathf.Sin(t * Mathf.PI * 5f) * 0.5f * fade;
                    if (Crossed(prev, t, 0f)) b.Soft.Ripple(30f * amp, 3, 0f);
                    break;
                case OneShot.Pat:
                    b.HeadPitch.Target += 14f * hump; b.Content = Mathf.Max(b.Content, hump);
                    b.EarL.Target += 10f * hump; b.EarR.Target += 10f * hump;
                    if (Crossed(prev, t, 0f)) b.Crouch.Kick(1.5f * amp);
                    break;
                case OneShot.WaveL: case OneShot.WaveR:
                    b.PawLift.Target += (9f + Mathf.Sin(t * Mathf.PI * 5f) * 2.5f) * hump * amp;
                    b.HeadPitch.Target += 6f * hump;
                    break;
                case OneShot.TailFlick:
                    b.TailA.Target += Mathf.Sin(t * Mathf.PI * 3f) * 30f * amp * fade;
                    b.TailC.Target += -Mathf.Sin(t * Mathf.PI * 3f + 1f) * 35f * fade;
                    break;
                case OneShot.Attack:
                    // Pounce: crouch, then spring forward with the front legs out.
                    if (t < 0.3f) { b.Crouch.Target += 6f * (t / 0.3f); b.HeadPitch.Target += -10f; }
                    if (Crossed(prev, t, 0.3f)) { b.Push(new Vector2(dir * 260f * amp, 150f)); b.Crouch.Kick(-8f); }
                    if (t > 0.3f) { b.FootX[1].Target += 8f * hump; b.FootX[3].Target += 8f * hump; b.FootY[1].Target += 4f * hump; b.FootY[3].Target += 5f * hump; }
                    b.EyeScale.Target += 0.1f * hump;
                    break;
                case OneShot.Hurt:
                    if (Crossed(prev, t, 0f)) { b.Push(new Vector2(-dir * 200f * amp, 120f)); b.HeadPitch.Kick(200f); b.Crouch.Kick(3f); }
                    b.EyeScale.Target += 0.3f * hump; b.MouthOpen.Target += 0.8f * hump; b.Squint.Target = 0f;
                    b.EarL.Target += 25f * hump; b.EarR.Target += 25f * hump;
                    break;
                case OneShot.Faint:
                {
                    float e = 1f - (1f - t) * (1f - t);
                    b.Rot.Target += -dir * 82f * e;
                    b.Sway.Target += -dir * 12f * e;
                    b.BlinkClose = Mathf.Max(b.BlinkClose, e);
                    b.HindFold.Target = 0f; b.FrontFold.Target = 0f;
                    for (int i = 0; i < 4; i++) { b.FootX[i].Target += (b.IsHind(i) ? -6f : 6f) * e; b.FootY[i].Target += 2f * e; }
                    b.EarL.Target += 30f * e; b.EarR.Target += 30f * e;
                    b.MouthOpen.Target += 0.4f * e; b.MouthCurve.Target = -0.5f;
                    break;
                }
                case OneShot.Eat:
                    b.HeadPitch.Target += -28f * hump; b.HeadDY.Target -= 6f * hump; b.HeadDX.Target += 3f * hump;
                    b.MouthOpen.Target += Mathf.Abs(Mathf.Sin(t * Mathf.PI * 5f)) * 0.8f * hump;
                    b.Content = Mathf.Max(b.Content, hump * 0.5f);
                    break;
                case OneShot.Celebrate:
                    if (Crossed(prev, t, 0.05f)) b.Jump(330f * amp);
                    if (Crossed(prev, t, 0.6f)) b.Jump(290f * amp);
                    b.EarL.Target -= 10f * hump; b.EarR.Target -= 10f * hump;
                    b.TailA.Target += Mathf.Sin(t * Mathf.PI * 8f) * 30f;
                    b.Content = Mathf.Max(b.Content, hump * 0.9f);
                    b.Sparkle.Target = Mathf.Max(b.Sparkle.Target, hump);
                    break;
                case OneShot.Dance:
                    b.Crouch.Target += Mathf.Abs(Mathf.Sin(t * Mathf.PI * 6f)) * 3f;
                    b.HeadYaw.Target += Mathf.Sin(t * Mathf.PI * 6f) * 0.6f;
                    b.HeadPitch.Target += Mathf.Sin(t * Mathf.PI * 12f) * 6f;
                    b.TailA.Target += Mathf.Sin(t * Mathf.PI * 6f) * 30f;
                    if (Crossed(prev, t, 0.25f) || Crossed(prev, t, 0.75f)) b.Jump(200f);
                    b.Content = Mathf.Max(b.Content, hump * 0.7f);
                    break;
                case OneShot.Nod:
                    b.HeadPitch.Target += -Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 14f * amp;
                    break;
                case OneShot.Shiver:
                    b.Sway.Target += Mathf.Sin(_t * 70f) * 0.6f * amp * fade;
                    b.EarL.Target += 12f * fade; b.EarR.Target += 12f * fade;
                    b.Crouch.Target += 1.5f * fade;
                    break;
                case OneShot.Stretch:
                    // The cat stretch: front legs slide forward, chest to the ground, rump up, head low, then back.
                    b.ShoulderY.Target -= 9f * hump; b.HipY.Target += 3f * hump;
                    b.HeadDY.Target -= 4f * hump; b.HeadPitch.Target += -12f * hump;
                    b.FootX[1].Target += 11f * hump; b.FootX[3].Target += 12f * hump;
                    b.TailA.Target += 30f * hump;
                    b.BlinkClose = Mathf.Max(b.BlinkClose, hump > 0.7f ? 1f : 0f);
                    b.MouthOpen.Target += 0.25f * Mathf.Max(0f, hump - 0.6f) * 2.5f;
                    break;
                case OneShot.Yawn:
                    b.MouthOpen.Target += hump * amp; b.HeadPitch.Target += 12f * hump;
                    b.BlinkClose = Mathf.Max(b.BlinkClose, Mathf.Clamp01(hump * 1.4f));
                    b.EarL.Target += 8f * hump; b.EarR.Target += 8f * hump;
                    break;
                case OneShot.Shake:
                    b.HeadYaw.Target += Mathf.Sin(t * Mathf.PI * 10f) * 0.8f * fade;
                    b.EarL.Target += Mathf.Sin(t * Mathf.PI * 10f) * 24f * fade; b.EarR.Target -= Mathf.Sin(t * Mathf.PI * 10f) * 24f * fade;
                    if (Crossed(prev, t, 0f)) b.Soft.Ripple(40f, 3, 0.5f);
                    break;
                case OneShot.EarTwitch:
                    if (Crossed(prev, t, 0f)) (s.Side < 0 ? ref b.EarL : ref b.EarR).Kick(-240f * amp);
                    break;
                case OneShot.LookAround:
                {
                    float yaw = t < 0.3f ? -1f : t < 0.6f ? 1f : 0f;
                    b.HeadYaw.Target += yaw * s.Side * 0.8f; b.GazeX.Target = yaw * s.Side * 0.6f;
                    b.HeadPitch.Target += 4f * hump;
                    b.EarL.Target -= 5f * hump; b.EarR.Target -= 5f * hump;
                    break;
                }
                case OneShot.Sniff:
                    b.HeadPitch.Target += -22f * hump; b.HeadDY.Target -= 5f * hump; b.HeadDX.Target += 4f * hump;
                    if (Crossed(prev, t, 0.3f) || Crossed(prev, t, 0.5f) || Crossed(prev, t, 0.7f)) b.HeadPitch.Kick(-80f);
                    break;
                case OneShot.Groom:
                    // Head turns down toward the raised front paw and licks in small bobs.
                    b.HeadPitch.Target += -30f * hump; b.HeadDX.Target += 1f * hump; b.HeadDY.Target -= 5f * hump;
                    b.HeadYaw.Target += 0.4f * hump;
                    b.PawLift.Target += 7f * hump;
                    b.BlinkClose = Mathf.Max(b.BlinkClose, hump > 0.5f ? 1f : 0f);
                    b.HeadPitch.Target += Mathf.Sin(t * Mathf.PI * 10f) * 4f * hump;
                    break;
            }
        }
    }
}
