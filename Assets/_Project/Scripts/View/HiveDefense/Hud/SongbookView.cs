using System;
using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The quiet songbook in the HUD's bottom-right corner: one chip per song. Every slot shows its notes as letter pips for the
    /// notes found so far and dots for the rest, so the length is visible, and the first note takes the category colour even
    /// before the song is known. A known song also shows its category mark, name, cost and a thin cooldown bar. It listens to
    /// the caster and unsubscribes in <see cref="Dispose"/>.
    /// </summary>
    /// <remarks>Timers are floats advanced by <see cref="Tick"/>. Classes and sizes change only when a value changes.</remarks>
    public sealed class SongbookView : IDisposable
    {
        private const string KnownClass = "chip--known";
        private const string BrightClass = "chip--bright";
        private const string NudgeClass = "chip--nudge";
        private const string RestingClass = "chip--resting";
        private const string HoleClassPrefix = "hole-";
        private const string UnknownPipClass = "pip--unknown";
        private const string FreeText = "free";
        private const int CostFree = -1;
        private const int CostUnset = -2;
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
        private readonly VisualElement[][] _pips;
        private readonly Label[] _costs;
        private readonly string[] _costTexts;
        private readonly int[] _foundShown;
        private readonly int[] _costShown;
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
            _pips = new VisualElement[count][];
            _costs = new Label[count];
            _costTexts = new string[count];
            _foundShown = new int[count];
            _costShown = new int[count];
            _known = new bool[count];
            _resting = new bool[count];
            _barShown = new int[count];
            _brightLeft = new float[count];
            _nudgeLeft = new float[count];
            _nudgeOn = new bool[count];

            for (int index = 0; index < count; index++)
            {
                _barShown[index] = -1;
                _foundShown[index] = -1;
                _costShown[index] = CostUnset;
                BuildChip(column, index);
            }

            caster.SongCast += OnSongCast;
            caster.SongNotReady += OnSongRefused;
            session.Discovery.Hummed += OnHummed;
        }

        /// <summary>Stops listening to the caster.</summary>
        public void Dispose()
        {
            _session.Caster.SongCast -= OnSongCast;
            _session.Caster.SongNotReady -= OnSongRefused;
            _session.Discovery.Hummed -= OnHummed;
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
                ShowPips(song, discovery);
                ShowCost(song);
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
            var pipCells = new VisualElement[song.Keys.Count];
            for (int key = 0; key < pipCells.Length; key++)
            {
                var pip = new Label(string.Empty);
                pip.AddToClassList("pip");
                pip.AddToClassList(UnknownPipClass);
                if (key == 0)
                {
                    pip.style.backgroundColor = accent; // The first law: the first note is the colour.
                }

                pip.pickingMode = PickingMode.Ignore;
                pips.Add(pip);
                pipCells[key] = pip;
            }

            chip.Add(pips);

            var cost = new Label(string.Empty);
            cost.AddToClassList("chip-cost");
            cost.pickingMode = PickingMode.Ignore;
            chip.Add(cost);
            _costs[index] = cost;
            _pips[index] = pipCells;
            _costTexts[index] = effect.WaspCost.ToString(CultureInfo.InvariantCulture);

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

        // A found note is a letter on its hole colour. An unfound one is a dot, and the first one keeps the category colour.
        private void ShowPips(int song, SongDiscovery discovery)
        {
            int found = discovery.KnownNotes(song);
            if (found == _foundShown[song])
            {
                return;
            }

            _foundShown[song] = found;
            SongDefinition definition = _session.Caster[song].Song;
            Color accent = _config.ColorOf(_session.Caster[song].Category);
            VisualElement[] cells = _pips[song];
            for (int key = 0; key < cells.Length; key++)
            {
                var pip = (Label)cells[key];
                int hole = definition.Keys[key];
                string holeClass = HoleClassPrefix + (((hole % HoleColours) + HoleColours) % HoleColours);
                bool isFound = key < found;
                pip.EnableInClassList(UnknownPipClass, !isFound);
                pip.EnableInClassList(holeClass, isFound);
                pip.text = isFound ? HiveDefenseInput.HoleName(hole) : string.Empty;
                if (isFound)
                {
                    pip.style.backgroundColor = StyleKeyword.Null;
                }
                else if (key == 0)
                {
                    pip.style.backgroundColor = accent;
                }
            }
        }

        // Free while the song's next cast costs nothing (its first cast, or a farm phase unless it is a tower), the wasp price
        // otherwise (HiveDefenseSession.SongPrice). A song that never costs anything shows nothing.
        private void ShowCost(int song)
        {
            int price = _session.Caster[song].WaspCost;
            int cost = price == 0 ? 0 : (_session.SongPrice(song) == 0 ? CostFree : price);
            if (cost == _costShown[song])
            {
                return;
            }

            _costShown[song] = cost;
            Label label = _costs[song];
            label.text = cost == CostFree ? FreeText : (cost == 0 ? string.Empty : _costTexts[song]);
            label.EnableInClassList("chip-cost--free", cost == CostFree);
            label.EnableInClassList("chip-cost--none", cost == 0);
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

        // A nest hums a song: its slot lights once, like a cast, so the player sees which one to look at.
        private void OnHummed(int song)
        {
            _brightLeft[song] = BrightSeconds;
            _chips[song].RemoveFromClassList(BrightClass);
        }

        private void OnSongRefused(int song, SongRefusal refusal)
        {
            if (_known[song])
            {
                _nudgeLeft[song] = NudgeSeconds;
            }
        }
    }
}
