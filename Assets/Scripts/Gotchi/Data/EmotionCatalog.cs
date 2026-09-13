using System.Collections.Generic;

namespace Gotchi.Data
{
    // Single source of truth mirroring 09-pets-and-emotions.md. Descriptions double as
    // the visual-expression key used when generating art for each state.
    public static class EmotionCatalog
    {
        private static readonly Dictionary<EmotionCategory, EmotionType[]> ByCategory = new Dictionary<EmotionCategory, EmotionType[]>
        {
            { EmotionCategory.Joy, new[] { EmotionType.Joy, EmotionType.Gladness, EmotionType.Relief, EmotionType.Love, EmotionType.Pride, EmotionType.Satisfaction } },
            { EmotionCategory.Sadness, new[] { EmotionType.Grief, EmotionType.Sorrow, EmotionType.Loneliness, EmotionType.Despair, EmotionType.Depression } },
            { EmotionCategory.Anger, new[] { EmotionType.Rage, EmotionType.Fury, EmotionType.Irritation, EmotionType.Annoyance, EmotionType.Resentment } },
            { EmotionCategory.Fear, new[] { EmotionType.Terror, EmotionType.Panic, EmotionType.Anxiety, EmotionType.Worry, EmotionType.Nervousness } },
            { EmotionCategory.Disgust, new[] { EmotionType.Dislike, EmotionType.Revulsion, EmotionType.Contempt, EmotionType.Aversion } },
            { EmotionCategory.Surprise, new[] { EmotionType.Astonishment, EmotionType.Amazement, EmotionType.Shock } },
            { EmotionCategory.GuiltAndShame, new[] { EmotionType.Remorse, EmotionType.Regret, EmotionType.Embarrassment, EmotionType.Humiliation } },
            { EmotionCategory.ConnectionAndCare, new[] { EmotionType.Compassion, EmotionType.Empathy, EmotionType.Gratitude, EmotionType.Affection, EmotionType.Warmth } },
            { EmotionCategory.Vulnerability, new[] { EmotionType.Helplessness, EmotionType.Powerlessness, EmotionType.Inadequacy, EmotionType.Overwhelmed } },
            { EmotionCategory.InterestAndAwe, new[] { EmotionType.Curiosity, EmotionType.Wonder, EmotionType.Inspiration, EmotionType.Excitement } },
        };

        private static readonly Dictionary<EmotionType, EmotionCategory> CategoryOf = BuildReverse();

        private static readonly Dictionary<EmotionType, string> Descriptions = new Dictionary<EmotionType, string>
        {
            { EmotionType.Joy, "Wide open smile, eyes closed in a happy arc, slight forward bounce" },
            { EmotionType.Gladness, "Soft closed-eye smile, relaxed shoulders, gentle head tilt" },
            { EmotionType.Relief, "Eyes half-closed, slumped relaxed posture, small exhale mark above head" },
            { EmotionType.Love, "Heart-shaped eyes, both paws clasped near chest" },
            { EmotionType.Pride, "Chin raised, chest puffed out, confidently narrowed eyes with a small smile" },
            { EmotionType.Satisfaction, "Content closed-mouth smile, half-lidded eyes, one paw resting on belly" },
            { EmotionType.Grief, "Eyes shut tight, deep downturned frown, one large tear, hunched posture" },
            { EmotionType.Sorrow, "Droopy eyes, small tear, head tilted down" },
            { EmotionType.Loneliness, "Small hunched posture, paws wrapped around self, eyes looking down and aside" },
            { EmotionType.Despair, "Eyes wide and hollow, wavering open frown, shoulders slumped forward" },
            { EmotionType.Depression, "Flat half-lidded eyes, straight neutral-to-down mouth, body slumped low" },
            { EmotionType.Rage, "Furrowed brow, bared teeth, flushed cheeks, clenched paws, anger marks above head" },
            { EmotionType.Fury, "Sharp angled eyebrows, wide shouting open mouth, whole body leaning forward" },
            { EmotionType.Irritation, "One eyebrow raised, tight flat mouth line, slight squint" },
            { EmotionType.Annoyance, "Narrowed eyes, small flat mouth, arms crossed" },
            { EmotionType.Resentment, "Sideways glare, tight closed mouth, arms crossed, body turned slightly away" },
            { EmotionType.Terror, "Eyes wide with shrunk pupils, open scream-shaped mouth, body shrinking back, sweat drop" },
            { EmotionType.Panic, "Wide shaking eyes, open trembling mouth, paws raised near face" },
            { EmotionType.Anxiety, "Small worried eyes, subtle frown, one paw fidgeting, sweat drop" },
            { EmotionType.Worry, "Furrowed brow, small o-shaped mouth, eyes glancing sideways" },
            { EmotionType.Nervousness, "Half-closed shifting eyes, small awkward smile, one paw scratching head" },
            { EmotionType.Dislike, "One eye squinted, slight downward smirk, head tilted away" },
            { EmotionType.Revulsion, "Scrunched nose, tongue out, eyes squeezed shut, leaning back" },
            { EmotionType.Contempt, "One eyebrow raised, small smirk, eyes half-lidded looking down at viewer" },
            { EmotionType.Aversion, "Head turned away, eyes averted, mouth in a small grimace" },
            { EmotionType.Astonishment, "Round wide eyes, small round open mouth, both paws raised beside face" },
            { EmotionType.Amazement, "Sparkling wide eyes, open smiling mouth, leaning forward with interest" },
            { EmotionType.Shock, "Extremely wide eyes with tiny pupils, straight-line open gasp mouth, stiff/frozen body" },
            { EmotionType.Remorse, "Downcast eyes, small frown, one paw rubbing back of head" },
            { EmotionType.Regret, "Closed eyes, furrowed brow, head hanging low" },
            { EmotionType.Embarrassment, "Deep blush, small awkward smile, eyes looking away" },
            { EmotionType.Humiliation, "Very deep blush, eyes squeezed shut, body curled small, head down" },
            { EmotionType.Compassion, "Soft gentle eyes, warm smile, both paws reaching forward" },
            { EmotionType.Empathy, "Soft downturned eyebrows, gentle closed-mouth smile, head tilted, one paw extended" },
            { EmotionType.Gratitude, "Closed happy eyes, paws pressed together near chest, small sparkle nearby" },
            { EmotionType.Affection, "Flushed cheeks, closed content eyes, small smile, paws hugging self" },
            { EmotionType.Warmth, "Soft half-closed eyes, gentle smile, faint pink glow on cheeks" },
            { EmotionType.Helplessness, "Droopy wide eyes, small open frown, paws hanging limp at sides" },
            { EmotionType.Powerlessness, "Eyes looking down, slumped shoulders, one paw half-raised then dropped" },
            { EmotionType.Inadequacy, "Small hunched posture, eyes averted downward, tiny frown" },
            { EmotionType.Overwhelmed, "Swirling dizzy eyes, small zigzag mouth, paws pressed to head" },
            { EmotionType.Curiosity, "One eyebrow raised, head tilted, wide inquisitive eyes, one paw touching chin" },
            { EmotionType.Wonder, "Sparkling wide eyes looking upward, small open smile, paws clasped" },
            { EmotionType.Inspiration, "Bright wide eyes with a tiny sparkle above head, confident smile" },
            { EmotionType.Excitement, "Big open-mouth smile, sparkling eyes, both paws raised up, slight jump/bounce pose" },
        };

        private static Dictionary<EmotionType, EmotionCategory> BuildReverse()
        {
            var map = new Dictionary<EmotionType, EmotionCategory>();
            foreach (var pair in ByCategory)
                foreach (var emotion in pair.Value)
                    map[emotion] = pair.Key;
            return map;
        }

        public static IReadOnlyList<EmotionType> EmotionsIn(EmotionCategory category) => ByCategory[category];

        public static EmotionCategory GetCategory(EmotionType emotion) => CategoryOf[emotion];

        public static string GetDescription(EmotionType emotion) =>
            Descriptions.TryGetValue(emotion, out var text) ? text : emotion.ToString();

        public static bool IsPositive(EmotionCategory category) =>
            category == EmotionCategory.Joy ||
            category == EmotionCategory.ConnectionAndCare ||
            category == EmotionCategory.InterestAndAwe ||
            category == EmotionCategory.Surprise;
    }
}
