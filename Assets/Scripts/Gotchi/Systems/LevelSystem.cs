using System;
using Gotchi.Data;

namespace Gotchi.Systems
{
    // Character level from accumulated level XP (care, cuddles, mini-games). Each level unlocks a story chapter.
    public class LevelSystem
    {
        public const int MaxLevel = 12;

        private readonly PetSaveData _data;

        public event Action<int> OnLevelUp;
        public event Action<int> OnXpChanged;

        public LevelSystem(PetSaveData data)
        {
            _data = data;
        }

        public static int XpRequiredForLevel(int level) => level <= 1 ? 0 : 60 * (level - 1) * (level - 1) + 40 * (level - 1);

        public int Xp => _data.levelXp;

        public int Level
        {
            get
            {
                int level = 1;
                while (level < MaxLevel && _data.levelXp >= XpRequiredForLevel(level + 1)) level++;
                return level;
            }
        }

        public float ProgressToNext
        {
            get
            {
                int level = Level;
                if (level >= MaxLevel) return 1f;
                int start = XpRequiredForLevel(level), end = XpRequiredForLevel(level + 1);
                return (float)(_data.levelXp - start) / (end - start);
            }
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            int before = Level;
            _data.levelXp += amount;
            OnXpChanged?.Invoke(_data.levelXp);
            int after = Level;
            if (after > before) OnLevelUp?.Invoke(after);
        }
    }

    public static class StoryBook
    {
        private static readonly string[] Chapters =
        {
            "You found {0} on your doorstep one cold night, wrapped in a blanket. Two blinks later, you were family.",
            "First steps. {0} discovered the window and now checks the weather every morning.",
            "{0} learned that snacks taste better after a bubble bath. Science, apparently.",
            "A rainy day. {0} napped on the rug and dreamed about bubbles the size of the moon.",
            "{0} found the plant in the corner and named it. The name is a secret.",
            "Big day: {0} tried a mini-game and came back glowing. Talent spotted.",
            "{0} started leaving little gifts by the door. Mostly leaves. One coin.",
            "The window cloud got a name too. {0} waves at it when it drifts by.",
            "{0} is braver now — chases dust motes like they owe rent.",
            "A quiet evening. {0} sat close and hummed something that sounded like your name.",
            "{0} built a fort from cushions and declared it a kingdom. Population: two.",
            "{0} looks at you like the story is only getting started. It is.",
        };

        public static string Chapter(int level, string petName) =>
            string.Format(Chapters[Math.Clamp(level, 1, Chapters.Length) - 1], petName);

        public static int ChapterCount => Chapters.Length;
    }
}
