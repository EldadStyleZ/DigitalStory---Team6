using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The night's bed of sound, made on the fly with no clips: a soft field noise (wind in the grass that swells in
    /// gusts, with far crickets) and the swarm's buzz, a hum of many detuned wings over filtered noise that grows with the
    /// swarm and rises in pitch as it moves.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DigitalStory/Ambience Voice")]
    public sealed class AmbienceVoice : MonoBehaviour
    {
        private const float TwoPi = Mathf.PI * 2f;
        private const int Wings = 9;

        [Header("Field")]
        [SerializeField, Range(0f, 1f)] private float _fieldVolume = 0f;
        [SerializeField, Range(0f, 1f)] private float _leavesVolume = 0f;
        [SerializeField, Range(0f, 1f)] private float _cricketVolume = 0.03f;

        [Header("Buzz")]
        [SerializeField, Range(0f, 1f)] private float _buzzVolume = 0f;
        [SerializeField, Range(60f, 400f)] private float _buzzPitch = 170f;
        [SerializeField, Range(0f, 1f), Tooltip("How much of the buzz stays while the swarm stands still.")] private float _buzzAtRest = 0.3f;

        [Header("Streams")]
        [SerializeField, Range(0f, 1f)] private float _streamVolume = 0.12f;

        // Written on the main thread, read on the audio thread; single floats are safe to share.
        private volatile float _swarm = 0.5f;
        private volatile float _speed;
        private volatile float _water;

        private const int Bubbles = 14;
        private readonly float[] _bubblePhase = new float[Bubbles];
        private readonly float[] _bubblePitch = new float[Bubbles];
        private readonly float[] _bubbleRise = new float[Bubbles];
        private readonly float[] _bubbleLoud = new float[Bubbles];
        private readonly float[] _bubbleDecay = new float[Bubbles];
        private float _smoothWater;

        // Smaller bees: three more layers, one, two and three octaves up, each a few voices of its own, quieter as they rise.
        private const int SmallVoices = 4;
        // Tuned to the hum's own harmonics (octave, fifth above it, two octaves), so together they ring as one warm chord.
        private static readonly float[] SmallOctave = { 2f, 3f, 4f };
        private static readonly float[] SmallLevel = { 0.4f, 0.2f, 0.1f };
        private readonly float[] _smallPhase = new float[3 * SmallVoices];
        private readonly float[] _smallDrift = new float[3 * SmallVoices];
        private readonly float[] _smallDetune = new float[3 * SmallVoices];
        private float _smallLow;
        private float _tremolo;
        private float _rushLow;
        private float _rushBand;

        private AudioSource _source;

        // A real recording beats any synth: drop a looping bee swarm sound at Resources/Sounds/BeeSwarm and it plays,
        // louder and a little higher as the swarm moves, in place of the synthesized buzz.
        private const string SwarmClipPath = "Sounds/BeeSwarm";
        [SerializeField, Range(0f, 1f)] private float _swarmClipVolume = 0.22f;
        [SerializeField, Range(0f, 1f), Tooltip("How much of the recording stays while the swarm stands still.")] private float _swarmClipAtRest = 0.3f;
        private AudioSource _swarmClip;

        // The reward when new bees join: a rising bell arpeggio with a shower of small chimes, at Resources/Sounds/Collect.
        private const string CollectClipPath = "Sounds/Collect";
        [SerializeField, Range(0f, 1f)] private float _collectVolume = 0.55f;
        private AudioClip _collectClip;

        // While a nest is being collected: a soft rising shimmer and quickening chime ticks, like a charge filling up.
        [SerializeField, Range(0f, 1f)] private float _chargeVolume = 0.09f;
        private volatile float _charge;
        private float _chargeLevel;
        private float _chargeSmooth;
        private float _chargeA;
        private float _chargeB;
        private float _chargeTrem;
        private float _tickClock;
        private float _tickPhase;
        private float _tickPitch;
        private float _tickLoud;
        private int _tickNote;
        private static readonly float[] TickNotes = { 523.3f, 587.3f, 659.3f, 784f, 880f, 1046.5f, 1174.7f, 1318.5f };
        private float _clipSpeed;

        // The buzz steps back while the flute sounds, so the songs stay clear over it.
        [SerializeField, Range(0f, 1f), Tooltip("Share of the buzz kept while a flute note sounds.")] private float _duckUnderFlute = 0.55f;
        private bool _fluteSounding;
        private float _duck = 1f;
        private WhiteNoise _noise;
        private float _rate;
        private bool _ready;

        private float _windLow;
        private float _windLower;
        private float _gustPhase;
        private float _leafLow;
        private float _leafHigh;
        private float _rustle;
        private float _rustleTarget;
        private float _cricketPhase;
        private float _cricketPulse;
        private float _bandLow;
        private float _bandBand;
        private float _smoothSwarm;
        private float _smoothSpeed;
        private float _buzzLow;
        private readonly float[] _wingPhase = new float[Wings];
        private readonly float[] _wingDetune = new float[Wings];
        private readonly float[] _wingDrift = new float[Wings];
        private readonly float[] _wingLoud = new float[Wings];
        private readonly float[] _wingLoudTarget = new float[Wings];

        /// <summary>Whether a flute note is sounding now, so the buzz steps back under it. Call every frame.</summary>
        public void SetFluteSounding(bool sounding)
        {
            _fluteSounding = sounding;
        }

        /// <summary>How far a nest's collecting has got, 0 to 1; 0 while none is being collected. Call every frame.</summary>
        public void SetCharge(float progress)
        {
            _charge = Mathf.Clamp01(progress);
        }

        /// <summary>Plays the reward chime, for bees joining the swarm.</summary>
        public void PlayCollect()
        {
            if (_collectClip != null)
            {
                _source.PlayOneShot(_collectClip, _collectVolume);
            }
        }

        /// <summary>How big the swarm is, 0 to 1, and how fast it moves, 0 to 1. Call every frame.</summary>
        public void Feed(float swarm, float speed, float water)
        {
            _water = Mathf.Clamp01(water);
            if (_swarmClip != null)
            {
                _clipSpeed = Mathf.Lerp(_clipSpeed, Mathf.Clamp01(speed), 1f - Mathf.Exp(-4f * Time.deltaTime));
                float moving = Mathf.SmoothStep(0f, 1f, _clipSpeed);
                _duck = Mathf.Lerp(_duck, _fluteSounding ? _duckUnderFlute : 1f, 1f - Mathf.Exp(-(_fluteSounding ? 12f : 3f) * Time.deltaTime));
                _swarmClip.volume = _duck * _swarmClipVolume * (0.4f + (0.6f * Mathf.Clamp01(swarm))) * (_swarmClipAtRest + ((1f - _swarmClipAtRest) * moving));
                _swarmClip.pitch = 1f + (0.12f * moving);
            }
            _swarm = Mathf.Clamp01(swarm);
            _speed = Mathf.Clamp01(speed);
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.loop = true;
            _noise = WhiteNoise.Start();
            _collectClip = Resources.Load<AudioClip>(CollectClipPath);
            var clip = Resources.Load<AudioClip>(SwarmClipPath);
            if (clip != null)
            {
                var child = new GameObject("Bee Swarm Recording");
                child.transform.SetParent(transform, false);
                _swarmClip = child.AddComponent<AudioSource>();
                _swarmClip.clip = clip;
                _swarmClip.loop = true;
                _swarmClip.spatialBlend = 0f;
                _swarmClip.volume = 0f;
                _swarmClip.Play();
            }
            for (int wing = 0; wing < Wings; wing++)
            {
                _wingPhase[wing] = Random.value;
                _wingDetune[wing] = Random.Range(0.993f, 1.007f);
            }

            for (int voice = 0; voice < _smallPhase.Length; voice++)
            {
                _smallPhase[voice] = Random.value;
                _smallDrift[voice] = Random.value;
                _smallDetune[voice] = Random.Range(0.994f, 1.006f);
            }
        }

        private void OnEnable() => AudioSettings.OnAudioConfigurationChanged += HandleAudioChanged;

        private void OnDisable() => AudioSettings.OnAudioConfigurationChanged -= HandleAudioChanged;

        private void Start() => Begin();

        private void HandleAudioChanged(bool deviceWasChanged) => Begin();

        private void Begin()
        {
            _rate = AudioSettings.outputSampleRate;
            _ready = _rate > 0f;
            if (_ready)
            {
                _source.Play();
            }
        }

        // Unity calls this on the audio thread.
        private float Charge(float dt)
        {
            float target = _charge;
            bool on = target > 0.001f;
            _chargeLevel += ((on ? 1f : 0f) - _chargeLevel) * dt * (on ? 4f : 6f);
            if (on)
            {
                _chargeSmooth += (target - _chargeSmooth) * dt * 8f;
            }

            if (_chargeLevel < 0.0005f)
            {
                _tickClock = 0f;
                _tickNote = 0;
                return 0f;
            }

            float p = _chargeSmooth;

            // The shimmer: a soft fifth that glides up an octave as the nest fills, trembling faster near the end.
            float pitch = 330f * Mathf.Pow(2f, p);
            _chargeA += dt * pitch;
            _chargeB += dt * pitch * 1.5f;
            _chargeTrem += dt * Mathf.Lerp(4f, 14f, p);
            _chargeA -= Mathf.Floor(_chargeA);
            _chargeB -= Mathf.Floor(_chargeB);
            _chargeTrem -= Mathf.Floor(_chargeTrem);
            float trem = 0.7f + (0.3f * Mathf.Sin(_chargeTrem * TwoPi));
            float shimmer = ((Mathf.Sin(_chargeA * TwoPi) * 0.6f) + (Mathf.Sin(_chargeB * TwoPi) * 0.35f)) * trem * (0.4f + (0.6f * p));

            // The ticks: small chimes climbing the scale, coming quicker as the nest fills.
            _tickClock += dt;
            float interval = Mathf.Lerp(0.45f, 0.12f, p);
            if (_tickClock >= interval)
            {
                _tickClock = 0f;
                _tickPitch = TickNotes[_tickNote % TickNotes.Length] * (_tickNote >= TickNotes.Length ? 2f : 1f);
                _tickNote = Mathf.Min(_tickNote + 1, (TickNotes.Length * 2) - 1);
                _tickLoud = 1f;
                _tickPhase = 0f;
            }

            _tickPhase += dt * _tickPitch;
            _tickPhase -= Mathf.Floor(_tickPhase);
            float tick = ((Mathf.Sin(_tickPhase * TwoPi) * 0.8f) + (Mathf.Sin(_tickPhase * TwoPi * 2f) * 0.2f)) * _tickLoud;
            _tickLoud *= 1f - (dt * 14f);

            return ((shimmer * 0.5f) + (tick * 0.9f)) * _chargeLevel * _chargeVolume;
        }

        // A babbling brook: a soft rush of water under many small bubbles, each a quick rising "plip".
        private float Stream(float white, float dt)
        {
            float f = 2f * Mathf.Sin(Mathf.PI * 900f * dt);
            _rushLow += f * _rushBand;
            float high = white - _rushLow - (1.2f * _rushBand);
            _rushBand += f * high;
            float sound = _rushBand * 0.25f;

            for (int bubble = 0; bubble < Bubbles; bubble++)
            {
                if (_bubbleLoud[bubble] < 0.002f)
                {
                    // Each idle bubble may start now; about forty a second across the brook.
                    if (Mathf.Abs(_noise.Next()) > 1f - (40f * dt / Bubbles * 2f))
                    {
                        float pick = Mathf.Abs(_noise.Next());
                        _bubblePitch[bubble] = Mathf.Lerp(450f, 1600f, pick * pick);
                        _bubbleRise[bubble] = Mathf.Lerp(2f, 9f, Mathf.Abs(_noise.Next()));
                        _bubbleLoud[bubble] = Mathf.Lerp(0.15f, 0.6f, Mathf.Abs(_noise.Next()));
                        _bubbleDecay[bubble] = Mathf.Lerp(25f, 70f, Mathf.Abs(_noise.Next()));
                        _bubblePhase[bubble] = 0f;
                    }

                    continue;
                }

                _bubblePitch[bubble] *= 1f + (_bubbleRise[bubble] * dt);
                _bubblePhase[bubble] += dt * _bubblePitch[bubble];
                if (_bubblePhase[bubble] >= 1f)
                {
                    _bubblePhase[bubble] -= 1f;
                }

                sound += Mathf.Sin(_bubblePhase[bubble] * TwoPi) * _bubbleLoud[bubble] * 0.3f;
                _bubbleLoud[bubble] *= 1f - (_bubbleDecay[bubble] * dt);
            }

            return sound;
        }

        // The first harmonics of a sawtooth, each softened a little more than 1/n, so it buzzes but never scratches.
        private static float Buzz(float angle, int harmonics)
        {
            float sum = 0f;
            for (int n = 1; n <= harmonics; n++)
            {
                sum += Mathf.Sin(angle * n) / (n * (1f + (0.08f * n)));
            }

            return sum;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_ready)
            {
                return;
            }

            float dt = 1f / _rate;
            float swarmTarget = _swarm;
            float speedTarget = _speed;
            float waterTarget = _water;
            float follow = 1f - Mathf.Exp(-dt * 3f);

            // One-pole filters for the wind: a soft low hiss.
            float windCut = 1f - Mathf.Exp(-TwoPi * 900f * dt);
            float windCutLower = 1f - Mathf.Exp(-TwoPi * 250f * dt);
            float leafCut = 1f - Mathf.Exp(-TwoPi * 3500f * dt);
            float leafHighCut = 1f - Mathf.Exp(-TwoPi * 500f * dt);

            for (int index = 0; index < data.Length; index += channels)
            {
                _smoothSwarm += (swarmTarget - _smoothSwarm) * follow;
                _smoothSpeed += (speedTarget - _smoothSpeed) * follow;
                _smoothWater += (waterTarget - _smoothWater) * follow;

                // Forest: a low wind under the trees, swelling in slow gusts, and leaves rustling above it in quick,
                // uneven flurries that ride on the gusts.
                float white = _noise.Next();
                _windLow += (white - _windLow) * windCut;
                _windLower += (_windLow - _windLower) * windCutLower;
                _gustPhase += dt * 0.06f;
                float gust = 0.55f + (0.3f * Mathf.Sin(_gustPhase * TwoPi)) + (0.15f * Mathf.Sin(_gustPhase * TwoPi * 2.71f + 1.3f));
                float wind = _windLower * 1.6f * gust;

                float leafWhite = _noise.Next();
                _leafLow += (leafWhite - _leafLow) * leafCut;
                _leafHigh += (_leafLow - _leafHigh) * leafHighCut;
                float leaves = _leafLow - _leafHigh;
                if (Mathf.Abs(leafWhite) > 0.9993f)
                {
                    _rustleTarget = Mathf.Abs(_noise.Next()) * gust;
                }

                _rustle += (_rustleTarget - _rustle) * dt * 6f;
                float field = ((wind * _fieldVolume) + (leaves * (0.15f + _rustle) * gust * _leavesVolume)) * 0.8f;

                // Crickets far off: a soft trill that wanders a little in pitch, in bursts.
                _cricketPulse += dt * 0.45f;
                if (_cricketPulse >= 1f)
                {
                    _cricketPulse -= 1f;
                }

                _cricketPhase += dt * (3900f + (300f * Mathf.Sin(_gustPhase * 17f)));
                if (_cricketPhase >= 1f)
                {
                    _cricketPhase -= 1f;
                }

                float trill = Mathf.Max(0f, Mathf.Sin(_cricketPulse * 60f * TwoPi));
                float burst = _cricketPulse < 0.4f ? trill * trill * Mathf.Sin(_cricketPulse / 0.4f * Mathf.PI) : 0f;
                float cricket = Mathf.Sin(_cricketPhase * TwoPi) * burst * _cricketVolume;

                // Buzz: a handful of bees, each a narrow pulse (the sharp snap of beating wings, rich in overtones) with its
                // own quick vibrato, slightly out of tune with the others, then shaped by a nasal formant: "bzzzz".
                float pitch = _buzzPitch * (1f + (0.25f * _smoothSpeed));
                float wings = 0f;
                for (int wing = 0; wing < Wings; wing++)
                {
                    _wingDrift[wing] += dt * (11f + (wing * 1.7f));
                    if (_wingDrift[wing] >= 1f)
                    {
                        _wingDrift[wing] -= 1f;
                    }

                    float vibrato = 1f + (0.006f * Mathf.Sin(_wingDrift[wing] * TwoPi));
                    _wingPhase[wing] += dt * pitch * _wingDetune[wing] * vibrato;
                    if (_wingPhase[wing] >= 1f)
                    {
                        _wingPhase[wing] -= 1f;
                    }

                    // A smooth sawtooth built from its first harmonics: the reedy buzz of a bee's wings, with no aliased edge.
                    float tone = Buzz(_wingPhase[wing] * TwoPi, 8);
                    wings += tone * (0.75f + (0.25f * Mathf.Sin((_wingDrift[wing] * TwoPi) + wing)));
                }

                wings /= Wings;

                // Soften the edges a little, then a formant round 1 kHz gives the nasal buzz.
                float buzzCut = 1f - Mathf.Exp(-TwoPi * 2200f * dt);
                float smallCut = 1f - Mathf.Exp(-TwoPi * 2600f * dt);
                _buzzLow += (wings - _buzzLow) * buzzCut;
                float f = 2f * Mathf.Sin(Mathf.PI * 600f * dt);
                _bandLow += f * _bandBand;
                float high = _buzzLow - _bandLow - (0.9f * _bandBand);
                _bandBand += f * high;

                float moving = Mathf.SmoothStep(0f, 1f, _smoothSpeed);
                float loud = _buzzVolume * (0.35f + (0.65f * _smoothSwarm)) * (_buzzAtRest + ((1f - _buzzAtRest) * moving));
                // The smaller bees, each layer its own few voices with a faster flutter.
                float small = 0f;
                for (int voice = 0; voice < _smallPhase.Length; voice++)
                {
                    int layer = voice / SmallVoices;
                    _smallDrift[voice] += dt * (13f + (voice * 2.3f));
                    if (_smallDrift[voice] >= 1f)
                    {
                        _smallDrift[voice] -= 1f;
                    }

                    float wobble = Mathf.Sin(_smallDrift[voice] * TwoPi);
                    _smallPhase[voice] += dt * pitch * SmallOctave[layer] * _smallDetune[voice] * (1f + (0.005f * wobble));
                    if (_smallPhase[voice] >= 1f)
                    {
                        _smallPhase[voice] -= 1f;
                    }

                    float beat = Buzz(_smallPhase[voice] * TwoPi, 4);
                    small += beat * SmallLevel[layer] * (0.6f + (0.4f * Mathf.Sin((_smallDrift[voice] * TwoPi * 0.37f) + voice)));
                }

                small /= SmallVoices;
                _smallLow += (small - _smallLow) * smallCut;

                // A gentle wing-beat tremolo makes the warm chord hum like bees without any harshness.
                _tremolo += dt * 9f;
                if (_tremolo >= 1f)
                {
                    _tremolo -= 1f;
                }

                float flutter = 0.75f + (0.25f * Mathf.Sin(_tremolo * TwoPi));
                float buzz = ((_buzzLow * 1.1f) + (_smallLow * 0.9f)) * flutter * loud;

                float charge = Charge(dt);
                float stream = _smoothWater > 0.001f ? Stream(white, dt) * _smoothWater * _streamVolume : 0f;
                // A soft limiter, so the layers never clip into a crackle when they add up.
                float sum = field + cricket + buzz + stream + charge;
                float sample = sum / (1f + Mathf.Abs(sum));
                for (int channel = 0; channel < channels; channel++)
                {
                    data[index + channel] += sample;
                }
            }
        }
    }
}
