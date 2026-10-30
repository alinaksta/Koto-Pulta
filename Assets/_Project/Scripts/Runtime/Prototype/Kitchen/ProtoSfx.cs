using UnityEngine;
using UnityEngine.Audio;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Sound effects of the prototype kitchen (clips are assigned by <see cref="ProtoKitchen"/>).
    /// Everything goes to the mixer's SFX group, so the SFX volume slider works.
    /// </summary>
    public static class ProtoSfx
    {
        public static AudioClip[] FolderSlams = new AudioClip[0];
        public static AudioClip ShredderStart;
        public static AudioClip ShredderLoop;
        public static AudioClip ShredderFinish;
        public static AudioClip Stapler;
        public static AudioClip TakeOrder;
        public static AudioClip CatChewing;
        public static AudioMixerGroup Output;

        private static int s_lastFolder = -1;
        private static readonly System.Collections.Generic.Dictionary<AudioClip, float> s_starts = new();
        private const float SilenceThreshold = 0.02f;

        /// <summary>
        /// Where the sound actually begins: several clips start with up to 0.4 s of silence, which felt like lag.
        /// Measured once per clip.
        /// </summary>
        public static float SoundStart(AudioClip clip)
        {
            if (clip == null)
                return 0f;
            if (s_starts.TryGetValue(clip, out float start))
                return start;

            start = 0f;
            if (clip.loadState != AudioDataLoadState.Loaded)
                clip.LoadAudioData();

            int channels = Mathf.Max(1, clip.channels);
            var samples = new float[clip.samples * channels];
            if (clip.loadState == AudioDataLoadState.Loaded && clip.GetData(samples, 0))
            {
                // Relative to the clip's peak, so quiet clips aren't cut.
                float peak = 0f;
                foreach (float sample in samples)
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                float threshold = Mathf.Min(SilenceThreshold, peak * 0.1f);

                for (int i = 0; i < samples.Length; i++)
                {
                    if (Mathf.Abs(samples[i]) > threshold)
                    {
                        // A few milliseconds earlier, so the attack isn't cut.
                        start = Mathf.Max(0f, (float)(i / channels) / clip.frequency - 0.005f);
                        break;
                    }
                }
            }

            s_starts[clip] = start;
            return start;
        }

        /// <summary>Loads the clips' data now, so the first play doesn't wait for it.</summary>
        public static void Preload()
        {
            if (FolderSlams != null)
                foreach (AudioClip clip in FolderSlams)
                    SoundStart(clip);
            SoundStart(ShredderStart);
            SoundStart(ShredderLoop);
            SoundStart(ShredderFinish);
            SoundStart(Stapler);
            SoundStart(TakeOrder);
            SoundStart(CatChewing);
        }

        /// <summary>A random folder slam, never the same one twice in a row.</summary>
        public static AudioClip RandomFolderSlam()
        {
            if (FolderSlams == null || FolderSlams.Length == 0)
                return null;

            int index = Random.Range(0, FolderSlams.Length);
            if (FolderSlams.Length > 1 && index == s_lastFolder)
                index = (index + 1 + Random.Range(0, FolderSlams.Length - 1)) % FolderSlams.Length;
            s_lastFolder = index;
            return FolderSlams[index];
        }

        /// <summary>A source on the object, routed to SFX.</summary>
        public static AudioSource CreateSource(GameObject host, float spatial, float minDistance = 1f, float maxDistance = 15f)
        {
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = Output;
            source.spatialBlend = spatial;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
            return source;
        }

        /// <summary>Plays once at a point in the world (3D).</summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float minDistance = 1f, float maxDistance = 15f)
            => PlayOnce(clip, null, position, volume, 1f, minDistance, maxDistance);

        /// <summary>Plays once, following the object (3D).</summary>
        public static void PlayAttached(AudioClip clip, Transform parent, float volume = 1f, float minDistance = 1f, float maxDistance = 15f)
            => PlayOnce(clip, parent, parent != null ? parent.position : Vector3.zero, volume, 1f, minDistance, maxDistance);

        /// <summary>Loops, following the object (3D). Destroy the source's GameObject to stop it.</summary>
        public static AudioSource PlayLoopAttached(AudioClip clip, Transform parent, float volume = 1f, float minDistance = 1f, float maxDistance = 15f)
        {
            if (clip == null || parent == null)
                return null;

            var go = new GameObject($"Sfx_{clip.name}");
            go.transform.SetParent(parent, false);
            AudioSource source = CreateSource(go, 1f, minDistance, maxDistance);
            source.clip = clip;
            source.volume = volume;
            source.loop = true;
            source.time = SoundStart(clip);
            source.Play();
            return source;
        }

        /// <summary>Plays once without position (UI-like).</summary>
        public static void Play2D(AudioClip clip, float volume = 1f)
            => PlayOnce(clip, null, Vector3.zero, volume, 0f, 1f, 15f);

        private static void PlayOnce(AudioClip clip, Transform parent, Vector3 position, float volume, float spatial, float minDistance, float maxDistance)
        {
            if (clip == null)
                return;

            var go = new GameObject($"Sfx_{clip.name}");
            if (parent != null)
                go.transform.SetParent(parent, false);
            else
                go.transform.position = position;

            AudioSource source = CreateSource(go, spatial, minDistance, maxDistance);
            source.clip = clip;
            source.volume = volume;
            float start = SoundStart(clip);
            source.time = start;
            source.Play();
            Object.Destroy(go, clip.length - start + 0.1f);
        }
    }
}
