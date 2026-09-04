using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    public enum GameSound
    {
        TriangleShot,
        ArcShot,
        PierceShot,
        BlastShot,
        EnemyKill,
        OreCollected,
        Build,
        Upgrade,
        PathUnlock,
        TowerUnlock,
        Denied,
        CoreHit,
        WaveStart,
        WaveClear,
        Ability,
        Repair,
        Select
    }

    public sealed class GameAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const int MusicSampleRate = 22050;
        private const int SourceCount = 8;
        private const float MasterVolume = .58f;
        private const float MusicVolume = .16f;
        private const float MusicDuration = 20f;
        private readonly Dictionary<GameSound, AudioClip> clips = new();
        private readonly float[] lastPlayed = new float[Enum.GetValues(typeof(GameSound)).Length];
        private AudioSource[] sources;
        private Camera gameCamera;
        private int nextSource;
        private float lastKill = -10f;
        private AudioSource musicSource;
        private AudioClip musicClip;
        private GameController game;

        private void Awake()
        {
            gameCamera = Camera.main;
            game = GetComponent<GameController>();
            sources = new AudioSource[SourceCount];
            for (var index = 0; index < sources.Length; index++)
            {
                var voice = new GameObject($"SFX Voice {index + 1}");
                voice.transform.SetParent(transform, false);
                var source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                source.ignoreListenerPause = true;
                sources[index] = source;
            }
            BuildClips();
            BuildMusic();
            for (var index = 0; index < lastPlayed.Length; index++) lastPlayed[index] = -10f;
        }

        private void Update()
        {
            if (musicSource == null) return;
            var target = MusicVolume * (game != null && game.IsBossWave ? 1.22f : 1f);
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, Time.unscaledDeltaTime * .06f);
        }

        private void OnDestroy()
        {
            foreach (var clip in clips.Values) if (clip != null) Destroy(clip);
            if (musicClip != null) Destroy(musicClip);
        }

        private void BuildMusic()
        {
            var musicObject = new GameObject("Procedural Background Music");
            musicObject.transform.SetParent(transform, false);
            musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.dopplerLevel = 0f;
            musicSource.ignoreListenerPause = true;
            musicSource.volume = MusicVolume;
            musicSource.priority = 32;
            musicClip = MakeBackgroundMusic();
            musicSource.clip = musicClip;
            musicSource.Play();
        }

        private static AudioClip MakeBackgroundMusic()
        {
            var frameCount = Mathf.CeilToInt(MusicDuration * MusicSampleRate);
            var samples = new float[frameCount * 2];
            const float beat = .625f; // 96 BPM: relaxed retro arcade groove.
            var roots = new[] { 45, 41, 48, 43 }; // A minor, F major, C major, G major.
            var thirds = new[] { 3, 4, 4, 4 };
            var arp = new[] { 0, 7, 12, 7, 3, 7, 15, 12, 0, 7, 12, 15, 12, 7, 3, 7 };
            var melody = new[] { 12, 15, 19, 15, 12, 10, 7, 10, 12, 15, 19, 22, 19, 15, 12, 10 };

            for (var frame = 0; frame < frameCount; frame++)
            {
                var time = frame / (float)MusicSampleRate;
                var chordIndex = Mathf.Min(3, Mathf.FloorToInt(time / 5f));
                var chordTime = time - chordIndex * 5f;
                var root = roots[chordIndex];
                var padEnvelope = Mathf.Pow(Mathf.Sin(Mathf.PI * chordTime / 5f), .24f);

                var rootFrequency = MidiFrequency(root);
                var thirdFrequency = MidiFrequency(root + thirds[chordIndex]);
                var fifthFrequency = MidiFrequency(root + 7);
                var leftPad = (Triangle(time, rootFrequency) * .46f + Triangle(time, thirdFrequency) * .24f +
                    Triangle(time, fifthFrequency) * .2f) * padEnvelope;
                var rightPad = (Triangle(time + .004f, rootFrequency) * .44f +
                    Triangle(time + .006f, fifthFrequency) * .24f + Triangle(time, thirdFrequency) * .22f) * padEnvelope;

                var stepLength = beat * .5f;
                var stepIndex = Mathf.FloorToInt(time / stepLength);
                var stepTime = time - stepIndex * stepLength;
                var stepEnvelope = Mathf.Clamp01(stepTime / .012f) * Mathf.Exp(-stepTime * 5.2f);
                var arpFrequency = MidiFrequency(root + 12 + arp[stepIndex % arp.Length]);
                var arpTone = (Triangle(time, arpFrequency) * .7f +
                    Pulse(time, arpFrequency * .5f, .25f) * .1f) * stepEnvelope;
                var pan = Mathf.Sin(stepIndex * 1.7f) * .35f;

                var beatTime = time - Mathf.Floor(time / beat) * beat;
                var bassEnvelope = Mathf.Clamp01(beatTime / .018f) * Mathf.Exp(-beatTime * 3.2f);
                var bass = (Triangle(time, rootFrequency * .5f) * .7f +
                    Pulse(time, rootFrequency * .5f, .5f) * .14f) * bassEnvelope;

                var melodyLength = beat * 2f;
                var melodyStep = Mathf.FloorToInt(time / melodyLength);
                var melodyTime = time - melodyStep * melodyLength;
                var melodyEnvelope = Mathf.Clamp01(melodyTime / .035f) * Mathf.Exp(-melodyTime * 1.9f);
                var melodyFrequency = MidiFrequency(root + melody[melodyStep % melody.Length]);
                var lead = (Triangle(time, melodyFrequency) * .82f +
                    Pulse(time, melodyFrequency, .25f) * .08f) * melodyEnvelope;

                var measureTime = time - Mathf.Floor(time / (beat * 4f)) * beat * 4f;
                var pulseEnvelope = Mathf.Clamp01(measureTime / .015f) * Mathf.Exp(-measureTime * 8f);
                var pulse = Mathf.Sin(Mathf.PI * 2f * (68f * measureTime - 14f * measureTime * measureTime)) *
                    pulseEnvelope;
                var offBeatTime = time - (Mathf.Floor((time + beat * .5f) / beat) * beat - beat * .5f);
                var hatEnvelope = Mathf.Exp(-Mathf.Max(0, offBeatTime) * 30f);
                var noise = Mathf.Repeat(Mathf.Sin(frame * 12.9898f) * 43758.5453f, 2f) - 1f;
                var hat = noise * hatEnvelope;

                var loopFade = Mathf.Clamp01(time / .08f) * Mathf.Clamp01((MusicDuration - time) / .08f);
                var left = (leftPad * .2f + arpTone * .07f * (1f - pan) + bass * .12f + lead * .065f +
                    pulse * .065f + hat * .01f) * loopFade;
                var right = (rightPad * .2f + arpTone * .07f * (1f + pan) + bass * .12f + lead * .065f +
                    pulse * .065f + hat * .01f) * loopFade;
                samples[frame * 2] = Mathf.Clamp(left, -.82f, .82f);
                samples[frame * 2 + 1] = Mathf.Clamp(right, -.82f, .82f);
            }

            var clip = AudioClip.Create("Shape Guard - Neon Drift", frameCount, 2, MusicSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float MidiFrequency(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        private static float Triangle(float time, float frequency) =>
            1f - 4f * Mathf.Abs(Mathf.Repeat(time * frequency, 1f) - .5f);

        private static float Pulse(float time, float frequency, float duty) =>
            Mathf.Repeat(time * frequency, 1f) < duty ? 1f : -1f;

        public void Play(GameSound sound, float volume = 1f, Vector3? worldPosition = null)
        {
            var soundIndex = (int)sound;
            var now = Time.unscaledTime;
            var isAttack = IsAttackSound(sound);
            if (!isAttack && now - lastPlayed[soundIndex] < Cooldown(sound)) return;
            if (sound == GameSound.EnemyKill && !PassesCombatLimit(sound, now)) return;
            lastPlayed[soundIndex] = now;
            if (!clips.TryGetValue(sound, out var clip) || clip == null) return;

            var source = NextVoice();
            var gameplaySound = worldPosition.HasValue;
            var spatialGain = worldPosition.HasValue ? WorldGain(worldPosition.Value) : 1f;
            if (spatialGain <= 0f) return;
            var speedPitch = gameplaySound ? Mathf.Lerp(1f, .96f,
                Mathf.InverseLerp(1f, 3f, Time.timeScale)) : 1f;
            source.pitch = speedPitch * PitchVariation(sound);
            source.panStereo = worldPosition.HasValue ? StereoPan(worldPosition.Value) : 0f;
            source.volume = Mathf.Clamp01(volume * BaseVolume(sound) * spatialGain * MasterVolume);
            // PlayOneShot layers simultaneous tower attacks instead of cutting off
            // an earlier shot when every voice in the pool is already occupied.
            source.PlayOneShot(clip);
        }

        private static bool IsAttackSound(GameSound sound) => sound is
            GameSound.TriangleShot or GameSound.ArcShot or GameSound.PierceShot or GameSound.BlastShot;

        private AudioSource NextVoice()
        {
            for (var offset = 0; offset < sources.Length; offset++)
            {
                var index = (nextSource + offset) % sources.Length;
                if (sources[index].isPlaying) continue;
                nextSource = (index + 1) % sources.Length;
                return sources[index];
            }
            var voice = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;
            return voice;
        }

        private float StereoPan(Vector3 position)
        {
            if (gameCamera == null) gameCamera = Camera.main;
            if (gameCamera == null) return 0f;
            var halfWidth = gameCamera.orthographicSize * gameCamera.aspect;
            return Mathf.Clamp((position.x - gameCamera.transform.position.x) / Mathf.Max(1f, halfWidth), -.72f, .72f);
        }

        private bool PassesCombatLimit(GameSound sound, float now)
        {
            switch (sound)
            {
                case GameSound.EnemyKill:
                    if (now - lastKill < .35f) return false;
                    lastKill = now;
                    return true;
                default:
                    return true;
            }
        }

        private float WorldGain(Vector3 position)
        {
            if (gameCamera == null) gameCamera = Camera.main;
            if (gameCamera == null) return 1f;
            var viewport = gameCamera.WorldToViewportPoint(position);
            if (viewport.x < -.06f || viewport.x > 1.06f || viewport.y < -.06f || viewport.y > 1.06f) return 0f;
            return Mathf.Lerp(1f, .48f,
                Mathf.InverseLerp(38f, GameBalance.CameraMaximumZoom, gameCamera.orthographicSize));
        }

        private static float Cooldown(GameSound sound) => sound switch
        {
            GameSound.EnemyKill => .2f,
            GameSound.OreCollected => .45f,
            GameSound.CoreHit => .28f,
            _ => .02f
        };

        private static float BaseVolume(GameSound sound) => sound switch
        {
            GameSound.TriangleShot => .12f,
            GameSound.ArcShot => .12f,
            GameSound.PierceShot => .12f,
            GameSound.BlastShot => .12f,
            GameSound.EnemyKill => .1f,
            GameSound.OreCollected => .07f,
            GameSound.CoreHit => .22f,
            GameSound.Denied => .24f,
            _ => .34f
        };

        private static float PitchVariation(GameSound sound)
        {
            var variation = sound is GameSound.TriangleShot or GameSound.ArcShot or GameSound.EnemyKill
                ? .015f : .01f;
            return 1f + UnityEngine.Random.Range(-variation, variation);
        }

        private void BuildClips()
        {
            clips[GameSound.TriangleShot] = MakeClip("Triangle Pulse", .115f, 11, (t, d, noise) =>
                Chirp(t, 430f, 190f, d) * Envelope(t, d, .005f, 2.5f) * .56f + noise * FastDecay(t, 62f) * .06f);
            clips[GameSound.ArcShot] = MakeClip("Arc Crackle", .16f, 23, (t, d, noise) =>
                (Chirp(t, 610f, 260f, d) * .34f + Chirp(t, 340f, 500f, d) * .12f +
                 noise * (.1f + Mathf.Abs(Mathf.Sin(t * 110f)) * .14f)) * Envelope(t, d, .004f, 1.7f));
            clips[GameSound.PierceShot] = MakeClip("Muted Rail Pulse", .17f, 37, (t, d, noise) =>
                (Chirp(t, 260f, 115f, d) * .42f + Sine(t, 92f) * .32f +
                 noise * FastDecay(t, 55f) * .035f) * Envelope(t, d, .006f, 2.35f));
            clips[GameSound.BlastShot] = MakeClip("Blast Thump", .34f, 41, (t, d, noise) =>
                (Chirp(t, 145f, 48f, d) * .78f + noise * FastDecay(t, 18f) * .44f) * Envelope(t, d, .002f, 1.55f));
            clips[GameSound.EnemyKill] = MakeClip("Enemy Shatter", .15f, 53, (t, d, noise) =>
                (Chirp(t, 160f, 340f, d) * .28f + noise * .28f) * Envelope(t, d, .003f, 2.7f));
            clips[GameSound.OreCollected] = MakeClip("Ore Ping", .2f, 61, (t, d, noise) =>
                (Sine(t, 470f) * .3f + Sine(t, 705f) * .11f) * Envelope(t, d, .005f, 2.5f));
            clips[GameSound.Build] = MakeClip("Build Confirm", .28f, 71, (t, d, noise) =>
                (Sine(t, t < .09f ? 190f : t < .18f ? 285f : 430f) * .62f + noise * FastDecay(t, 35f) * .14f) *
                Envelope(t, d, .004f, 1.1f));
            clips[GameSound.Upgrade] = MakeClip("Upgrade Rise", .38f, 83, (t, d, noise) =>
                (Sine(t, t < .12f ? 330f : t < .24f ? 495f : 660f) * .58f + Sine(t, 990f) * .12f) *
                Envelope(t, d, .006f, .8f));
            clips[GameSound.PathUnlock] = MakeClip("Path Unlock", .52f, 97, (t, d, noise) =>
                (Sine(t, 260f) * .3f + Sine(t, 390f) * .28f + Sine(t, 520f) * .24f) * Envelope(t, d, .012f, .7f));
            clips[GameSound.TowerUnlock] = MakeClip("Tower Blueprint", .72f, 101, (t, d, noise) =>
                (Sine(t, 220f) * .2f + Sine(t, 330f) * .22f + Sine(t, 440f) * .22f +
                 Sine(t, 660f) * .18f + Sine(t, 880f) * .1f) * Envelope(t, d, .015f, .58f));
            clips[GameSound.Denied] = MakeClip("Denied", .18f, 107, (t, d, noise) =>
                (Sine(t, t < .09f ? 175f : 130f) * .55f + noise * .08f) * Envelope(t, d, .002f, 1.2f));
            clips[GameSound.CoreHit] = MakeClip("Core Impact", .38f, 127, (t, d, noise) =>
                (Chirp(t, 105f, 42f, d) * .72f + noise * FastDecay(t, 15f) * .42f) * Envelope(t, d, .002f, 1.2f));
            clips[GameSound.WaveStart] = MakeClip("Wave Start", .42f, 131, (t, d, noise) =>
                (Chirp(t, 170f, 510f, d) * .48f + Sine(t, 340f) * .16f) * Envelope(t, d, .008f, .75f));
            clips[GameSound.WaveClear] = MakeClip("Wave Clear", .62f, 149, (t, d, noise) =>
                (Sine(t, 330f) * .28f + Sine(t, 495f) * .25f + Sine(t, 660f) * .21f) * Envelope(t, d, .015f, .62f));
            clips[GameSound.Ability] = MakeClip("Arc Ability", .46f, 157, (t, d, noise) =>
                (Chirp(t, 420f, 1500f, d) * .4f + noise * FastDecay(t, 7f) * .25f) * Envelope(t, d, .004f, .9f));
            clips[GameSound.Repair] = MakeClip("Core Repair", .56f, 163, (t, d, noise) =>
                (Sine(t, 420f) * .28f + Chirp(t, 520f, 940f, d) * .3f + Sine(t, 1260f) * .1f) *
                Envelope(t, d, .012f, .7f));
            clips[GameSound.Select] = MakeClip("Select", .08f, 173, (t, d, noise) =>
                Chirp(t, 620f, 840f, d) * Envelope(t, d, .002f, 2f) * .48f);
        }

        private static AudioClip MakeClip(string name, float duration, int seed,
            Func<float, float, float, float> synth)
        {
            var sampleCount = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[sampleCount];
            var random = new System.Random(seed);
            for (var index = 0; index < sampleCount; index++)
            {
                var time = index / (float)SampleRate;
                var noise = (float)random.NextDouble() * 2f - 1f;
                samples[index] = Mathf.Clamp(synth(time, duration, noise), -.92f, .92f);
            }
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Sine(float time, float frequency) => Mathf.Sin(Mathf.PI * 2f * frequency * time);

        private static float Chirp(float time, float startFrequency, float endFrequency, float duration)
        {
            var sweep = (endFrequency - startFrequency) / Mathf.Max(.001f, duration);
            return Mathf.Sin(Mathf.PI * 2f * (startFrequency * time + .5f * sweep * time * time));
        }

        private static float Envelope(float time, float duration, float attack, float decayPower)
        {
            var onset = Mathf.Clamp01(time / Mathf.Max(.001f, attack));
            return onset * Mathf.Pow(Mathf.Clamp01(1f - time / duration), decayPower);
        }

        private static float FastDecay(float time, float speed) => Mathf.Exp(-time * speed);
    }
}
