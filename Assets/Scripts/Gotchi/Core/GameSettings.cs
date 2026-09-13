using UnityEngine;

namespace Gotchi.Core
{
    // Device-local preferences (not part of the pet save).
    public static class GameSettings
    {
        private const string SfxKey = "gotchi.sfx";
        private const string MusicKey = "gotchi.music";
        private const string NotificationsKey = "gotchi.notifications";

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
            set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); Apply(); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.6f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); Apply(); }
        }

        public static bool NotificationsEnabled
        {
            get => PlayerPrefs.GetInt(NotificationsKey, 1) == 1;
            set => PlayerPrefs.SetInt(NotificationsKey, value ? 1 : 0);
        }

        public static void Apply()
        {
            AudioListener.volume = SfxVolume;
            PlayerPrefs.Save();
        }
    }
}
