using UnityEngine;

namespace Gotchi.Creature
{
    // Jelly silhouette: N points around the body, each a damped spring on its radius, coupled to its neighbours
    // so a poke dents locally and the dent ripples around the body and wobbles back. Angles are radians from +x
    // around the body centre; displacements are creature units (positive = outward).
    public sealed class SoftBody
    {
        public readonly int N;
        public readonly float[] Disp, Vel;
        private readonly float[] _force;
        private float _accumulator;

        public float Stiffness = 240f;    // pull back to the rest radius
        public float Damping = 7f;        // low = wobblier
        public float Coupling = 1100f;    // neighbour spring (wave propagation)
        public float MaxDisp = 10f;

        public SoftBody(int n)
        {
            N = n;
            Disp = new float[n];
            Vel = new float[n];
            _force = new float[n];
        }

        public void Step(float dt)
        {
            const float h = 1f / 240f;
            _accumulator += Mathf.Min(dt, 0.05f);
            while (_accumulator >= h)
            {
                _accumulator -= h;
                for (int i = 0; i < N; i++)
                {
                    float lap = Disp[(i + 1) % N] + Disp[(i - 1 + N) % N] - 2f * Disp[i];
                    float a = -Stiffness * Disp[i] - Damping * Vel[i] + Coupling * lap + _force[i];
                    Vel[i] += a * h;
                }
                for (int i = 0; i < N; i++)
                {
                    Disp[i] += Vel[i] * h;
                    if (Disp[i] > MaxDisp) { Disp[i] = MaxDisp; if (Vel[i] > 0f) Vel[i] = 0f; }
                    else if (Disp[i] < -MaxDisp) { Disp[i] = -MaxDisp; if (Vel[i] < 0f) Vel[i] = 0f; }
                }
            }
            for (int i = 0; i < N; i++) _force[i] = 0f;
        }

        public float At(float angle)
        {
            float f = angle / (Mathf.PI * 2f) * N;
            f = ((f % N) + N) % N;
            int i = (int)f;
            float t = f - i;
            return Mathf.Lerp(Disp[i % N], Disp[(i + 1) % N], t);
        }

        private float Weight(int i, float angle, float width)
        {
            float d = Mathf.DeltaAngle(i / (float)N * 360f, angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            return Mathf.Exp(-(d * d) / (width * width));
        }

        // Instant velocity change around an angle (a tap, a landing, a flick). Width in radians.
        public void Impulse(float angle, float amount, float width = 0.55f)
        {
            for (int i = 0; i < N; i++) Vel[i] += amount * Weight(i, angle, width);
        }

        // Force pulling the local surface toward a target displacement while a finger rests on it.
        public void Hold(float angle, float target, float width = 0.6f, float strength = 900f)
        {
            for (int i = 0; i < N; i++)
            {
                float w = Weight(i, angle, width);
                if (w < 0.01f) continue;
                _force[i] += strength * (target * w - Disp[i]) * w;
            }
        }

        public void KickAll(float amount)
        {
            for (int i = 0; i < N; i++) Vel[i] += amount;
        }

        // Alternating outward/inward kick: a shake that runs around the body.
        public void Ripple(float amount, int lobes = 2, float phase = 0f)
        {
            for (int i = 0; i < N; i++) Vel[i] += amount * Mathf.Sin(i / (float)N * Mathf.PI * 2f * lobes + phase);
        }
    }
}
