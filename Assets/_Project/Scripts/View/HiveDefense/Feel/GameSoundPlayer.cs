using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DigitalStory.View
{
    /// <summary>
    /// Plays the optional recorded sounds of plan section AC: every AudioSource is made when the player is made (at load) and
    /// every clip's data is loaded then, so play never adds a component or allocates. Each moment sounds at most its slot's
    /// number at once and a new one takes the oldest's voice (<see cref="ImpactFxPool"/>, the same rule as the impact
    /// effects). Without a library, or for a moment whose slot is empty, it plays nothing.
    /// </summary>
    public sealed class GameSoundPlayer
    {
        private const float LoopEaseSeconds = 0.5f;

        private readonly EventVoices[] _events;
        private readonly AudioSource _loop;
        private readonly float _master;
        private readonly float _loopVolume;
        private readonly float _loopQuietShare;
        private float _loopShare;

        /// <param name="library">The sounds; null for none.</param>
        /// <param name="parent">Holds the sources.</param>
        /// <exception cref="ArgumentNullException">The parent is missing.</exception>
        public GameSoundPlayer(GameSoundLibrary library, Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            int eventCount = Enum.GetValues(typeof(GameSoundEvent)).Length;
            _events = new EventVoices[eventCount];
            if (library == null)
            {
                return;
            }

            _master = library.MasterVolume;
            _loopQuietShare = library.SwarmLoopQuietShare;
            for (int index = 0; index < eventCount; index++)
            {
                var kind = (GameSoundEvent)index;
                GameSoundSlot slot = library.Slot(kind);
                if (!slot.IsSet)
                {
                    continue;
                }

                if (slot.MostAtOnce < 1)
                {
                    Debug.LogError($"{library.name}: the {kind} slot needs at least one voice, so it plays nothing.", library);
                    continue;
                }

                ClipCount += Preload(slot.Clips);
                if (kind == GameSoundEvent.SwarmLoop)
                {
                    _loop = MakeSource(parent, "SwarmLoop");
                    _loop.clip = slot.Clips[0];
                    _loop.loop = true;
                    _loopVolume = slot.Volume * _master;
                    _loop.volume = _loopVolume * _loopQuietShare;
                    _loop.Play();
                    SourceCount++;
                    continue;
                }

                _events[index] = new EventVoices(kind, slot, parent);
                SourceCount += slot.MostAtOnce;
            }
        }

        /// <summary>How many clips were loaded.</summary>
        public int ClipCount { get; }

        /// <summary>How many AudioSources were made.</summary>
        public int SourceCount { get; }

        /// <summary>How many one-shots have played since the player was made. For tests and the benchmark.</summary>
        public int PlayCount { get; private set; }

        /// <summary>True when the moment has a recorded sound, so the game's own sound for it stays quiet.</summary>
        public bool Has(GameSoundEvent kind) => kind == GameSoundEvent.SwarmLoop ? _loop != null : _events[(int)kind] != null;

        /// <summary>
        /// Plays the moment's sound at a pitch (1 is the clip's own), with the slot's variation on top. Returns false when the
        /// moment has no sound or its least gap refused this one.
        /// </summary>
        public bool Play(GameSoundEvent kind, float now, float pitch)
        {
            EventVoices voices = _events[(int)kind];
            if (voices == null)
            {
                return false;
            }

            int index = voices.Rule.Claim(now);
            if (index == ImpactFxPool.None)
            {
                return false;
            }

            voices.Fire(index, pitch, _master);
            PlayCount++;
            return true;
        }

        /// <summary>Eases the swarm loop's volume toward the swarm's size, a share from 0 (smallest) to 1 (largest).</summary>
        public void Tick(float swarmShare, float deltaTime)
        {
            if (_loop == null)
            {
                return;
            }

            float target = Mathf.Clamp01(swarmShare);
            _loopShare = Mathf.MoveTowards(_loopShare, target, deltaTime / LoopEaseSeconds);
            _loop.volume = _loopVolume * Mathf.Lerp(_loopQuietShare, 1f, _loopShare);
        }

        /// <summary>Destroys every source. The player plays nothing after.</summary>
        public void Dispose()
        {
            if (_loop != null)
            {
                Object.Destroy(_loop.gameObject);
            }

            for (int index = 0; index < _events.Length; index++)
            {
                _events[index]?.Destroy();
                _events[index] = null;
            }
        }

        private static AudioSource MakeSource(Transform parent, string name)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private static int Preload(AudioClip[] clips)
        {
            int count = 0;
            for (int index = 0; index < clips.Length; index++)
            {
                if (clips[index] != null)
                {
                    clips[index].LoadAudioData();
                    count++;
                }
            }

            return count;
        }

        // One moment's voices, made once, and which clip played last so the next differs when there are several.
        private sealed class EventVoices
        {
            private readonly AudioSource[] _sources;
            private readonly AudioClip[] _clips;
            private readonly float _volume;
            private readonly float _pitchJitter;
            private int _lastClip = -1;

            public EventVoices(GameSoundEvent kind, GameSoundSlot slot, Transform parent)
            {
                Rule = new ImpactFxPool(slot.MostAtOnce, slot.LeastGapSeconds);
                _clips = slot.Clips;
                _volume = slot.Volume;
                _pitchJitter = slot.PitchJitter;
                var holder = new GameObject(kind.ToString());
                holder.transform.SetParent(parent, false);
                _sources = new AudioSource[slot.MostAtOnce];
                for (int index = 0; index < _sources.Length; index++)
                {
                    AudioSource source = holder.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    _sources[index] = source;
                }
            }

            public ImpactFxPool Rule { get; }

            public void Fire(int index, float pitch, float master)
            {
                AudioSource source = _sources[index];
                source.Stop();
                source.clip = _clips[NextClip()];
                source.volume = _volume * master;
                source.pitch = pitch * (1f + UnityEngine.Random.Range(-_pitchJitter, _pitchJitter));
                source.Play();
            }

            public void Destroy()
            {
                if (_sources.Length > 0 && _sources[0] != null)
                {
                    Object.Destroy(_sources[0].gameObject);
                }
            }

            // A random clip, never the one that played last when there are several.
            private int NextClip()
            {
                if (_clips.Length < 2)
                {
                    return 0;
                }

                int next = UnityEngine.Random.Range(0, _clips.Length - 1);
                if (next >= _lastClip && _lastClip >= 0)
                {
                    next++;
                }

                _lastClip = next;
                return next;
            }
        }
    }
}
