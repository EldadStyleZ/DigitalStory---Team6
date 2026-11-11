using System;
using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The quiet songbook in the HUD's bottom-left corner: one chip per song. An unknown song is an empty slot; a known one
    /// shows its category mark, name, notes and a thin cooldown bar. It listens to the caster and unsubscribes in
    /// <see cref="Dispose"/>.
    /// </summary>
    /// <remarks>Timers are floats advanced by <see cref="Tick"/>. Classes and sizes change only when a value changes.</remarks>
    public sealed class SongbookView : IDisposable
    {
        private const string KnownClass = "chip--known";
        private const string BrightClass = "chip--bright";
        private const string NudgeClass = "chip--nudge";
        private const string RestingClass = "chip--resting";
        private const string HoleClassPrefix = "hole-";
        private const int HoleColours = 6;
        private const float BarScale = 1000f;
        private const float BrightSeconds = 0.35f;
        private const float NudgeSeconds = 0.4f;
        private const float NudgeStepSeconds = 0.06f;

        private readonly HiveDefenseSession _session;
        private readonly SwarmConfig _config;
        private readonly Label _header;
        private readonly VisualElement[] _chips;
        private readonly VisualElement[] _cooldowns;
        private readonly bool[] _known;
        private readonly bool[] _resting;
        private readonly int[] _barShown;
        private readonly float[] _brightLeft;
        private readonly float[] _nudgeLeft;
        private readonly bool[] _nudgeOn;
        private int _knownShown = -1;

        /// <summary>Builds the chips into the container, which holds a "songbook-title" label and a "songbook-chips" column.</summary>
        public SongbookView(HiveDefenseSession session, SwarmConfig config, VisualElement songbook)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (songbook == null)
            {
                throw new ArgumentNullException(nameof(songbook));
            }

            _header = songbook.Q<Label>("songbook-title")
                ?? throw new InvalidOperationException("The HUD UXML has no element named songbook-title.");
            VisualElement column = songbook.Q<VisualElement>("songbook-chips")
                ?? throw new InvalidOperationException("The HUD UXML has no element named songbook-chips.");

            SwarmSongCaster caster = session.Caster;
            int count = caster.Count;
            _chips = new VisualElement[count];
            _cooldowns = new VisualElement[count];
            _known = new bool[count];
            _resting = new bool[count];
            _barShown = new int[count];
            _brightLeft = new float[count];
            _nudgeLeft = new float[count];
            _nudgeOn = new bool[count];

            for (int index = 0; index < count; index++)
            {
                _barShown[index] = -1;
                BuildChip(column, index);
            }

            caster.SongCast += OnSongCast;
            caster.SongNotReady += OnSongNotReady;
        }

        /// <summary>Stops listening to the caster.</summary>
        public void Dispose()
        {
            _session.Caster.SongCast -= OnSongCast;
            _session.Caster.SongNotReady -= OnSongNotReady;
        }

        /// <summary>Updates the chips. Call once per frame.</summary>
        public void Tick(float deltaTime)
        {
            SongDiscovery discovery = _session.Discovery;
            if (discovery.KnownCount != _knownShown)
            {
                _knownShown = discovery.KnownCount;
                _header.text = "Songs " + _knownShown.ToString(CultureInfo.InvariantCulture)
                    + "/" + discovery.Count.ToString(CultureInfo.InvariantCulture);
            }

            for (int song = 0; song < _chips.Length; song++)
            {
                ShowKnown(song, discovery.IsKnown(song));
                ShowCooldown(song);
                ShowTimers(song, deltaTime);
            }
        }

        private void BuildChip(VisualElement column, int index)
        {
            SwarmSongEffect effect = _session.Caster[index];
            SongDefinition song = effect.Song;
            Color accent = _config.ColorOf(effect.Category);

            var chip = new VisualElement();
            chip.AddToClassList("chip");
            chip.pickingMode = PickingMode.Ignore;

            var mark = new VisualElement();
            mark.AddToClassList("chip-mark");
            mark.AddToClassList("chip-mark--" + effect.Category.ToString().ToLowerInvariant());
            mark.style.backgroundColor = accent;
            mark.pickingMode = PickingMode.Ignore;
            chip.Add(mark);

            var title = new Label(song.Title);
            title.AddToClassList("chip-title");
            title.style.color = accent;
            title.pickingMode = PickingMode.Ignore;
            chip.Add(title);

            var pips = new VisualElement();
            pips.AddToClassList("chip-pips");
            pips.pickingMode = PickingMode.Ignore;
            for (int key = 0; key < song.Keys.Count; key++)
            {
                var pip = new Label(HiveDefenseInput.HoleName(song.Keys[key]));
                pip.AddToClassList("pip");
                pip.AddToClassList(HoleClassPrefix + (((song.Keys[key] % HoleColours) + HoleColours) % HoleColours));
                pip.pickingMode = PickingMode.Ignore;
                pips.Add(pip);
            }

            chip.Add(pips);

            var cooldown = new VisualElement();
            cooldown.AddToClassList("chip-cooldown");
            cooldown.style.backgroundColor = accent;
            cooldown.pickingMode = PickingMode.Ignore;
            chip.Add(cooldown);

            column.Add(chip);
            _chips[index] = chip;
            _cooldowns[index] = cooldown;
        }

        private void ShowKnown(int song, bool isKnown)
        {
            if (isKnown == _known[song])
            {
                return;
            }

            _known[song] = isKnown;
            _chips[song].EnableInClassList(KnownClass, isKnown);
            if (isKnown)
            {
                _brightLeft[song] = BrightSeconds;
            }
        }

        private void ShowCooldown(int song)
        {
            float fraction = _session.Caster.CooldownFraction(song);
            int bar = (int)(Mathf.Clamp01(fraction) * BarScale);
            if (bar != _barShown[song])
            {
                _barShown[song] = bar;
                _cooldowns[song].style.width = Length.Percent(bar / BarScale * 100f);
            }

            bool isResting = fraction > 0f;
            if (isResting == _resting[song])
            {
                return;
            }

            _resting[song] = isResting;
            _chips[song].EnableInClassList(RestingClass, isResting);
            if (!isResting && _known[song])
            {
                _brightLeft[song] = BrightSeconds; // The song is ready again.
            }
        }

        private void ShowTimers(int song, float deltaTime)
        {
            if (_brightLeft[song] > 0f)
            {
                bool wasOff = _brightLeft[song] == BrightSeconds;
                _brightLeft[song] = Mathf.Max(0f, _brightLeft[song] - deltaTime);
                if (wasOff)
                {
                    _chips[song].AddToClassList(BrightClass);
                }
                else if (_brightLeft[song] <= 0f)
                {
                    _chips[song].RemoveFromClassList(BrightClass);
                }
            }

            if (_nudgeLeft[song] > 0f)
            {
                _nudgeLeft[song] = Mathf.Max(0f, _nudgeLeft[song] - deltaTime);
                bool on = _nudgeLeft[song] > 0f && ((int)(_nudgeLeft[song] / NudgeStepSeconds) % 2 == 0);
                if (on != _nudgeOn[song])
                {
                    _nudgeOn[song] = on;
                    _chips[song].EnableInClassList(NudgeClass, on);
                }
            }
        }

        private void OnSongCast(int song)
        {
            _brightLeft[song] = BrightSeconds;
            _chips[song].RemoveFromClassList(BrightClass); // So the next Tick lights it again, even in the middle of a flash.
        }

        private void OnSongNotReady(int song)
        {
            if (_known[song])
            {
                _nudgeLeft[song] = NudgeSeconds;
            }
        }
    }
}
