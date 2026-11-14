using System;
using System.Globalization;
using System.Text;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The nests in the HUD: the swarm's level beside the wasp count, "Nests 2/8", a short "Level 3  +400 wasps" flash when a nest
    /// is collected, and the line "A nest hums: J K ..." when a nest hums a song. The combo to play at a nest is
    /// <see cref="NestComboView"/>'s. It listens to the match and unsubscribes in <see cref="Dispose"/>.
    /// </summary>
    /// <remarks>Timers are floats advanced by <see cref="Tick"/>. Text and widths change only when a value changes.</remarks>
    public sealed class NestHudView : IDisposable
    {
        private const string ShownClass = "line--shown";

        private readonly HiveDefenseSession _session;
        private readonly NestConfig _config;
        private readonly Label _level;
        private readonly Label _count;
        private readonly Label _hum;
        private readonly Label _levelLine;
        private readonly float _humSeconds;
        private readonly float _levelSeconds;
        private readonly StringBuilder _text = new StringBuilder(48);
        private float _humLeft;
        private float _levelLeft;
        private int _shownLevel = -1;
        private int _shownCollected = -1;
        private int _shownCount = -1;
        private string _humText;
        private ControlDevice _device;

        /// <summary>Finds "level-label", "nest-count", "hum-line" and "level-line" in the root and starts listening.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="InvalidOperationException">The UXML lacks an element.</exception>
        public NestHudView(HiveDefenseSession session, NestConfig config, VisualElement root, float humSeconds, float levelSeconds)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _level = Require<Label>(root, "level-label");
            _count = Require<Label>(root, "nest-count");
            _hum = Require<Label>(root, "hum-line");
            _levelLine = Require<Label>(root, "level-line");
            _humSeconds = humSeconds;
            _levelSeconds = levelSeconds;
            _hum.RemoveFromClassList(ShownClass);
            _levelLine.RemoveFromClassList(ShownClass);

            session.Nests.NestCollected += OnNestCollected;
            session.Discovery.Hummed += OnHummed;
        }

        /// <summary>Stops listening to the match.</summary>
        public void Dispose()
        {
            _session.Nests.NestCollected -= OnNestCollected;
            _session.Discovery.Hummed -= OnHummed;
        }

        /// <summary>Names the hummed notes for the device in use: the keys' letters, or a controller's button symbols.</summary>
        public void ShowControls(ControlDevice device)
        {
            if (device == _device)
            {
                return;
            }

            _device = device;
            if (_humText != null)
            {
                ControlSymbols.ShowPrompt(_hum, _humText, device);
            }
        }

        /// <summary>Updates the level, the counter and the timers. Call once per frame.</summary>
        public void Tick(float deltaTime)
        {
            int level = _session.Level;
            if (level != _shownLevel)
            {
                _shownLevel = level;
                _level.text = "Lv " + Number(level);
            }

            NestField nests = _session.Nests;
            int collected = nests.CollectedCount;
            if (collected != _shownCollected || nests.Count != _shownCount)
            {
                _shownCollected = collected;
                _shownCount = nests.Count;
                _count.text = "Nests " + Number(collected) + "/" + Number(nests.Count);
            }

            _humLeft = Countdown(_humLeft, deltaTime, _hum);
            _levelLeft = Countdown(_levelLeft, deltaTime, _levelLine);
        }

        private static float Countdown(float left, float deltaTime, VisualElement line)
        {
            if (left <= 0f)
            {
                return 0f;
            }

            left -= deltaTime;
            if (left <= 0f)
            {
                line.RemoveFromClassList(ShownClass);
                return 0f;
            }

            return left;
        }

        private void OnNestCollected(int index)
        {
            _levelLine.text = "Level " + Number(_session.Level) + "  +" + Number(_config.WaspsPerNest) + " wasps";
            _levelLeft = _levelSeconds;
            _levelLine.AddToClassList(ShownClass);
        }

        private void OnHummed(int song)
        {
            SongDefinition definition = _session.Caster[song].Song;
            int notes = Mathf.Min(_config.HummedNotes, definition.Keys.Count);
            _text.Clear();
            _text.Append("A nest hums: ");
            for (int key = 0; key < notes; key++)
            {
                _text.Append(HiveDefenseInput.HoleName(definition.Keys[key])).Append(' ');
            }

            _text.Append("...");
            _humText = _text.ToString();
            ControlSymbols.ShowPrompt(_hum, _humText, _device);
            _humLeft = _humSeconds;
            _hum.AddToClassList(ShownClass);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException("The HUD UXML has no element named " + name + ".");
        }
    }
}
