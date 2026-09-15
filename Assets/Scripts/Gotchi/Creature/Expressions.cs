using Gotchi.Data;

namespace Gotchi.Creature
{
    public enum LoopClip { Idle, Walk, Sleep, Sad, Happy, Alert }
    public enum OneShot { Hop, Wiggle, Pat, WaveL, WaveR, TailFlick, Attack, Hurt, Faint, Eat, Celebrate, Dance, Nod, Shiver, Stretch, Yawn, Shake, EarTwitch, LookAround, Sniff, Groom }
    public enum Stance { Stand, Sit, Lie, Sleep }

    // A face as continuous parameters. Every field is a spring target inside the creature, so moving between
    // any two expressions is a smooth morph — there are no discrete eye or mouth sprites any more.
    public struct FaceTarget
    {
        public float EyeOpen;      // 0 shut … 1 open (top lid)
        public float Squint;       // 0 … 1 happy arch from below
        public float EyeScale;     // 1 normal · 1.25 wide · 0.9 narrow
        public float Pupil;        // highlight size multiplier
        public float LidTilt;      // degrees, inner corner of the top lid pulled down (angry)
        public float BrowShow, BrowTilt, BrowRaise;
        public float MouthCurve;   // -1 frown … +1 smile
        public float MouthOpen;    // 0 … 1
        public float MouthWidth;   // multiplier
        public float Blush;        // 0 … 1
        public float Tear, Sweat, Heart, Sparkle, Anger;
        public LoopClip Loop;
        public OneShot? Enter;

        public static FaceTarget Neutral => new FaceTarget
        {
            EyeOpen = 1f, Squint = 0f, EyeScale = 1f, Pupil = 1f, LidTilt = 0f, BrowShow = 0f, BrowTilt = 0f, BrowRaise = 0f,
            MouthCurve = 0.8f, MouthOpen = 0f, MouthWidth = 1f, Blush = 0.55f, Loop = LoopClip.Idle,
        };
    }

    public static class Expressions
    {
        public static FaceTarget For(EmotionType emotion)
        {
            var e = FaceTarget.Neutral;
            switch (EmotionCatalog.GetCategory(emotion))
            {
                case EmotionCategory.Joy: e.Squint = 1f; e.Blush = 0.75f; e.MouthCurve = 1f; e.Loop = LoopClip.Happy; e.Enter = OneShot.Hop; break;
                case EmotionCategory.Sadness: e.MouthCurve = -0.8f; e.BrowShow = 1f; e.BrowTilt = -14f; e.Tear = 1f; e.EyeOpen = 0.85f; e.Loop = LoopClip.Sad; break;
                case EmotionCategory.Anger: e.EyeOpen = 0.78f; e.LidTilt = 18f; e.MouthCurve = -0.2f; e.MouthWidth = 0.9f; e.BrowShow = 1f; e.BrowTilt = 22f; e.Blush = 0.8f; e.Anger = 1f; e.Loop = LoopClip.Alert; e.Enter = OneShot.Wiggle; break;
                case EmotionCategory.Fear: e.EyeScale = 1.25f; e.Pupil = 0.7f; e.MouthCurve = -0.3f; e.MouthWidth = 0.6f; e.Sweat = 1f; e.Loop = LoopClip.Alert; e.Enter = OneShot.Shiver; break;
                case EmotionCategory.Disgust: e.EyeOpen = 0.55f; e.MouthCurve = -0.4f; e.MouthWidth = 0.8f; e.BrowShow = 1f; e.BrowTilt = 6f; break;
                case EmotionCategory.Surprise: e.EyeScale = 1.28f; e.Pupil = 1.15f; e.MouthOpen = 1f; e.MouthCurve = 0.3f; e.Loop = LoopClip.Alert; e.Enter = OneShot.Hop; break;
                case EmotionCategory.GuiltAndShame: e.EyeOpen = 0.6f; e.MouthCurve = -0.2f; e.MouthWidth = 0.6f; e.Blush = 1f; e.Loop = LoopClip.Sad; break;
                case EmotionCategory.ConnectionAndCare: e.Squint = 1f; e.Heart = 1f; e.Blush = 0.95f; e.MouthCurve = 1f; e.Loop = LoopClip.Happy; e.Enter = OneShot.Nod; break;
                case EmotionCategory.Vulnerability: e.MouthCurve = -0.2f; e.MouthWidth = 0.6f; e.BrowShow = 1f; e.BrowTilt = -12f; e.EyeScale = 1.08f; e.Loop = LoopClip.Sad; break;
                case EmotionCategory.InterestAndAwe: e.MouthOpen = 0.7f; e.MouthCurve = 0.4f; e.Sparkle = 1f; e.EyeScale = 1.12f; e.Pupil = 1.2f; e.Loop = LoopClip.Alert; e.Enter = OneShot.Nod; break;
            }
            switch (emotion)
            {
                case EmotionType.Relief: case EmotionType.Satisfaction: case EmotionType.Gladness:
                    e.Squint = 1f; e.MouthCurve = 0.9f; e.Blush = 0.7f; e.Loop = LoopClip.Idle; e.Enter = null; break;
                case EmotionType.Pride: e.EyeOpen = 0f; e.Squint = 0f; e.MouthCurve = 0.9f; e.Loop = LoopClip.Alert; e.Enter = OneShot.Nod; break;
                case EmotionType.Love: case EmotionType.Affection: case EmotionType.Warmth: e.Heart = 1f; break;
                case EmotionType.Excitement: case EmotionType.Amazement: case EmotionType.Inspiration:
                    e.Squint = 0f; e.EyeScale = 1.25f; e.Pupil = 1.3f; e.Sparkle = 1f; e.MouthCurve = 1f; e.MouthOpen = 0.5f; e.Loop = LoopClip.Happy; e.Enter = OneShot.Hop; break;
                case EmotionType.Terror: case EmotionType.Panic: e.EyeScale = 1.3f; e.Pupil = 0.6f; e.MouthOpen = 1f; e.MouthCurve = -0.4f; e.Sweat = 1f; break;
                case EmotionType.Shock: case EmotionType.Astonishment: e.EyeScale = 1.3f; e.Pupil = 0.7f; e.MouthOpen = 1f; break;
                case EmotionType.Nervousness: case EmotionType.Anxiety: case EmotionType.Worry:
                    e.EyeScale = 1.05f; e.MouthCurve = -0.3f; e.MouthWidth = 0.6f; e.MouthOpen = 0f; e.Sweat = 1f; e.BrowShow = 1f; e.BrowTilt = -10f; e.Enter = null; break;
                case EmotionType.Irritation: case EmotionType.Annoyance: e.Anger = 0f; e.Enter = null; break;
                case EmotionType.Depression: case EmotionType.Despair: e.EyeOpen = 0.5f; e.Tear = 0f; break;
                case EmotionType.Grief: e.EyeOpen = 0f; break;
                case EmotionType.Curiosity: e.MouthOpen = 0f; e.MouthCurve = 0.3f; e.MouthWidth = 0.6f; e.Sparkle = 0f; e.BrowShow = 1f; e.BrowTilt = 0f; e.BrowRaise = 2.5f; e.EyeScale = 1.08f; break;
                case EmotionType.Wonder: e.EyeScale = 1.25f; break;
                case EmotionType.Overwhelmed: e.EyeScale = 1.25f; e.MouthCurve = 0f; e.MouthOpen = 0f; e.Sweat = 1f; break;
                case EmotionType.Embarrassment: case EmotionType.Humiliation: e.Blush = 1f; break;
                case EmotionType.Remorse: case EmotionType.Regret: e.EyeOpen = 0f; e.MouthCurve = -0.8f; break;
                case EmotionType.Contempt: e.EyeOpen = 0.55f; e.MouthCurve = -0.1f; e.MouthWidth = 0.6f; break;
            }
            return e;
        }
    }
}
