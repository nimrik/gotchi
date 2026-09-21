namespace Gotchi.Data
{
    public enum CurrencyType { Soft, Premium }

    public enum SpeciesType
    {
        Bunny, Cat, Panda, RedPanda, Seal, Raccoon, Penguin,
        Fennec, Fox, Pig, Otter, Hedgehog, Dog
    }

    public enum SkillBranch
    {
        Sport, Social, PvP, Hunter, Science, Nature, ExplorerAdventure
    }

    // The game is built around battles: the Battle Club (ranked) and the Wild (campaign) both feed the PvP branch,
    // the only one left. The Sport, Social, Science, Nature, Explorer and Hunter games were removed on 2026-09-21;
    // their enum values stay so old saves still load. Everything the player sees iterates Active, never the enum.
    public static class SkillBranches
    {
        public static readonly SkillBranch[] Active = { SkillBranch.PvP };

        public static bool IsActive(SkillBranch branch)
        {
            foreach (var active in Active) if (active == branch) return true;
            return false;
        }
    }

    // Battle Club. A fighting style is the cat's battle type: CLAW beats TRICK, TRICK beats FLUFF, FLUFF beats CLAW.
    public enum BattleStyle { Normal, Claw, Fluff, Trick }

    public enum BattleStat { Hp, Attack, Defense, Speed }

    public enum EmotionCategory
    {
        Joy, Sadness, Anger, Fear, Disgust, Surprise,
        GuiltAndShame, ConnectionAndCare, Vulnerability, InterestAndAwe
    }

    public enum EmotionType
    {
        Joy, Gladness, Relief, Love, Pride, Satisfaction,
        Grief, Sorrow, Loneliness, Despair, Depression,
        Rage, Fury, Irritation, Annoyance, Resentment,
        Terror, Panic, Anxiety, Worry, Nervousness,
        Dislike, Revulsion, Contempt, Aversion,
        Astonishment, Amazement, Shock,
        Remorse, Regret, Embarrassment, Humiliation,
        Compassion, Empathy, Gratitude, Affection, Warmth,
        Helplessness, Powerlessness, Inadequacy, Overwhelmed,
        Curiosity, Wonder, Inspiration, Excitement
    }
}
