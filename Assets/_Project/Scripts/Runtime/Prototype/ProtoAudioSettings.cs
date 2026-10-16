using UnityEngine;
using UnityEngine.Audio;

namespace Game.Prototype
{
    /// <summary>
    /// Master / SFX / Music volume. SFX and Music use the same mixer parameters and PlayerPrefs keys
    /// as the existing <c>MixerUpdate</c> settings, so both menus stay in sync.
    /// </summary>
    public static class ProtoAudioSettings
    {
        public const float MinDb = -60f;
        public const float MaxDb = 20f;

        private const string MasterKey = "Audio.MasterVolume";
        private const string SfxParameter = "SFXVolume";
        private const string MusicParameter = "MusicVolume";
        private const string SfxKey = "Audio.SFXVolume";
        private const string MusicKey = "Audio.MusicVolume";

        /// <summary>
        /// Master volume (0..1), applied through <see cref="AudioListener.volume"/>.
        /// </summary>
        public static float Master
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set
            {
                float clamped = Mathf.Clamp01(value);
                AudioListener.volume = clamped;
                PlayerPrefs.SetFloat(MasterKey, clamped);
                PlayerPrefs.Save();
            }
        }

        public static void ApplyMaster() => AudioListener.volume = Master;

        public static float GetSfx(AudioMixer mixer) => Get(mixer, SfxParameter, SfxKey);
        public static float GetMusic(AudioMixer mixer) => Get(mixer, MusicParameter, MusicKey);
        public static void SetSfx(AudioMixer mixer, float db) => Set(mixer, SfxParameter, SfxKey, db);
        public static void SetMusic(AudioMixer mixer, float db) => Set(mixer, MusicParameter, MusicKey, db);

        /// <summary>
        /// Finds the project's audio mixer through any loaded AudioSource routed into it (e.g. the persistent SoundService).
        /// </summary>
        public static AudioMixer FindMixer()
        {
            var sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var source in sources)
            {
                if (source.outputAudioMixerGroup != null)
                    return source.outputAudioMixerGroup.audioMixer;
            }

            return null;
        }

        private static float Get(AudioMixer mixer, string parameter, string key)
        {
            if (mixer != null && mixer.GetFloat(parameter, out float value))
                return value;

            return PlayerPrefs.GetFloat(key, 0f);
        }

        private static void Set(AudioMixer mixer, string parameter, string key, float db)
        {
            if (mixer != null)
                mixer.SetFloat(parameter, db);

            PlayerPrefs.SetFloat(key, db);
            PlayerPrefs.Save();
        }
    }
}
