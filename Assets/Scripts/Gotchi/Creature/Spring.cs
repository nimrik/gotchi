using UnityEngine;

namespace Gotchi.Creature
{
    // Damped harmonic oscillator: every animated value in the creature is one of these, so nothing ever
    // snaps — targets can change at any moment and the value glides (or wobbles) toward them. Semi-implicit
    // Euler with sub-steps keeps even stiff springs stable at low frame rates.
    public struct Spring
    {
        public float Value, Velocity, Target, Stiffness, Damping;

        public Spring(float value, float stiffness, float dampingRatio = 1f)
        {
            Value = Target = value;
            Velocity = 0f;
            Stiffness = stiffness;
            Damping = dampingRatio * 2f * Mathf.Sqrt(stiffness);
        }

        public void Step(float dt)
        {
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.006f), 1, 12);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                Velocity += (Stiffness * (Target - Value) - Damping * Velocity) * h;
                Value += Velocity * h;
            }
        }

        public void Kick(float impulse) => Velocity += impulse;
        public void Snap(float value) { Value = Target = value; Velocity = 0f; }
        public void Set(float stiffness, float dampingRatio) { Stiffness = stiffness; Damping = dampingRatio * 2f * Mathf.Sqrt(stiffness); }
    }

    public static class Motion
    {
        // Signed 1-D noise in -1..1, smooth in t.
        public static float Noise(float t, float seed) => Mathf.PerlinNoise(t, seed * 17.31f) * 2f - 1f;

        public static float Pulse(float t, float at, float width) => Mathf.Exp(-((t - at) * (t - at)) / (width * width));

        // Smooth 0→1→0 hump over 0..1.
        public static float Hump(float t) => Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
    }
}
