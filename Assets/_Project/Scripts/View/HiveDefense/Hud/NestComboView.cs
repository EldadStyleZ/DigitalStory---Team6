using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// A nest's combo in the HUD (plan section U): while the core stands in an open nest's ring, a large row of slots near the
    /// top centre shows the notes to play, as the song book shows notes: each in its colour with its key's letter, or the
    /// controller's four-dot diamond once the pad was used last. A right note fills its slot with a quick pop, a wrong one
    /// shakes the row and empties it, and the last bursts the row as the nest is collected. It listens only to the nest
    /// field's events (<see cref="NestField.ComboShown"/>, <see cref="NestField.ComboHidden"/>, <see cref="NestField.ComboNote"/>),
    /// which a later effect can listen to as well, and unsubscribes in <see cref="Dispose"/>.
    /// </summary>
    /// <remarks>The slots are made once. Timers are floats advanced by <see cref="Tick"/>, and styles change only while one runs.</remarks>
    public sealed class NestComboView : IDisposable
    {
        private const string ShownClass = "combo--shown";
        private const string SlotClass = "combo-slot";
        private const string FilledClass = "combo-slot--filled";
        private const string HiddenClass = "combo-slot--hidden";

        // The note colours of the HUD's style sheet, in the holes' order.
        private static readonly string[] HoleClasses = { "hole-0", "hole-1", "hole-2", "hole-3", "hole-4", "hole-5" };

        private readonly NestField _nests;
        private readonly NestConfig _config;
        private readonly VisualElement _box;
        private readonly VisualElement _row;
        private readonly Label[] _slots;
        private readonly int[] _holes;
        private readonly float[] _popLeft;
        private int _nest = NestField.NoNest;
        private int _length;
        private float _shakeLeft;
        private float _burstLeft;
        private bool _isShown;
        private ControlDevice _device;

        /// <summary>Finds "combo-box" and "combo-slots" in the root, makes a slot for each note of the longest combo and starts listening.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="InvalidOperationException">The UXML lacks an element.</exception>
        public NestComboView(NestField nests, NestConfig config, VisualElement root)
        {
            _nests = nests ?? throw new ArgumentNullException(nameof(nests));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _box = root.Q<VisualElement>("combo-box") ?? throw new InvalidOperationException("The HUD UXML has no element named combo-box.");
            _row = root.Q<VisualElement>("combo-slots") ?? throw new InvalidOperationException("The HUD UXML has no element named combo-slots.");
            int most = Mathf.Max(1, config.ComboLongest);
            _slots = new Label[most];
            _holes = new int[most];
            _popLeft = new float[most];
            _row.Clear();
            for (int index = 0; index < most; index++)
            {
                var slot = new Label { pickingMode = PickingMode.Ignore };
                slot.AddToClassList(SlotClass);
                _row.Add(slot);
                _slots[index] = slot;
            }

            _box.RemoveFromClassList(ShownClass);
            nests.ComboShown += OnComboShown;
            nests.ComboHidden += OnComboHidden;
            nests.ComboNote += OnComboNote;
        }

        /// <summary>True while the prompt is on screen, bursting included. For tests.</summary>
        public bool IsShown => _isShown;

        /// <summary>How many slots are filled now. For tests.</summary>
        public int FilledCount
        {
            get
            {
                int filled = 0;
                for (int index = 0; index < _length; index++)
                {
                    filled += _slots[index].ClassListContains(FilledClass) ? 1 : 0;
                }

                return filled;
            }
        }

        /// <summary>True while the row shakes after a wrong note. For tests.</summary>
        public bool IsShaking => _shakeLeft > 0f;

        /// <summary>Stops listening to the match.</summary>
        public void Dispose()
        {
            _nests.ComboShown -= OnComboShown;
            _nests.ComboHidden -= OnComboHidden;
            _nests.ComboNote -= OnComboNote;
        }

        /// <summary>Names the notes for the device in use: the keys' letters, or a controller's diamonds.</summary>
        public void ShowControls(ControlDevice device)
        {
            if (device == _device)
            {
                return;
            }

            _device = device;
            for (int index = 0; index < _length; index++)
            {
                ControlSymbols.ShowHole(_slots[index], _holes[index], device);
            }
        }

        /// <summary>Plays the pops, the shake and the burst. Call once per frame with real time, so they stay crisp in slow motion.</summary>
        public void Tick(float deltaTime)
        {
            if (!_isShown)
            {
                return;
            }

            for (int index = 0; index < _length; index++)
            {
                if (_popLeft[index] <= 0f)
                {
                    continue;
                }

                _popLeft[index] = Mathf.Max(0f, _popLeft[index] - deltaTime);
                float done = 1f - (_popLeft[index] / _config.PromptPopSeconds);
                SetScale(_slots[index], 1f + (_config.PromptPopScale * Mathf.Sin(done * Mathf.PI)));
            }

            if (_shakeLeft > 0f)
            {
                _shakeLeft = Mathf.Max(0f, _shakeLeft - deltaTime);
                float share = _shakeLeft / _config.PromptShakeSeconds;
                float elapsed = _config.PromptShakeSeconds - _shakeLeft;
                float swing = _config.PromptShakePixels * share * Mathf.Sin(elapsed * _config.PromptShakeHertz * 2f * Mathf.PI);
                _row.style.translate = new Translate(new Length(swing, LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
            }

            if (_burstLeft > 0f)
            {
                _burstLeft = Mathf.Max(0f, _burstLeft - deltaTime);
                float done = 1f - (_burstLeft / _config.PromptBurstSeconds);
                float eased = 1f - ((1f - done) * (1f - done));
                SetScale(_box, 1f + (_config.PromptBurstScale * eased));
                _box.style.opacity = 1f - done;
                if (_burstLeft <= 0f)
                {
                    Hide();
                }
            }
        }

        private void OnComboShown(int nest)
        {
            Nest shown = _nests[nest];
            _nest = nest;
            _length = Mathf.Min(shown.Combo.Count, _slots.Length);
            ResetLook();
            for (int index = 0; index < _slots.Length; index++)
            {
                Label slot = _slots[index];
                bool isUsed = index < _length;
                slot.EnableInClassList(HiddenClass, !isUsed);
                slot.RemoveFromClassList(FilledClass);
                for (int hole = 0; hole < HoleClasses.Length; hole++)
                {
                    slot.RemoveFromClassList(HoleClasses[hole]);
                }

                if (isUsed)
                {
                    int note = shown.Combo[index];
                    _holes[index] = note;
                    if (note >= 0 && note < HoleClasses.Length)
                    {
                        slot.AddToClassList(HoleClasses[note]);
                    }

                    ControlSymbols.ShowHole(slot, note, _device);
                }
            }

            _isShown = true;
            _box.AddToClassList(ShownClass);
        }

        private void OnComboHidden(int nest)
        {
            if (nest == _nest && _burstLeft <= 0f)
            {
                Hide();
            }
        }

        private void OnComboNote(int nest, NestComboStep step)
        {
            if (nest != _nest)
            {
                return;
            }

            switch (step)
            {
                case NestComboStep.Advanced:
                    Fill(_nests[nest].NotesPlayed - 1);
                    break;
                case NestComboStep.Broken:
                    Empty();
                    _shakeLeft = _config.PromptShakeSeconds;
                    break;
                case NestComboStep.Restarted:
                    Empty();
                    _shakeLeft = _config.PromptShakeSeconds;
                    Fill(0);
                    break;
                case NestComboStep.Completed:
                    Fill(_length - 1);
                    _burstLeft = _config.PromptBurstSeconds;
                    break;
            }
        }

        private void Fill(int index)
        {
            if (index < 0 || index >= _length)
            {
                return;
            }

            _slots[index].AddToClassList(FilledClass);
            _popLeft[index] = _config.PromptPopSeconds;
        }

        private void Empty()
        {
            for (int index = 0; index < _length; index++)
            {
                _slots[index].RemoveFromClassList(FilledClass);
                _popLeft[index] = 0f;
                SetScale(_slots[index], 1f);
            }
        }

        private void Hide()
        {
            _isShown = false;
            _nest = NestField.NoNest;
            _box.RemoveFromClassList(ShownClass);
            ResetLook();
        }

        // Back to rest: no pop, no shake, no burst.
        private void ResetLook()
        {
            _shakeLeft = 0f;
            _burstLeft = 0f;
            for (int index = 0; index < _slots.Length; index++)
            {
                _popLeft[index] = 0f;
                SetScale(_slots[index], 1f);
            }

            _row.style.translate = new Translate(new Length(0f, LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
            SetScale(_box, 1f);
            _box.style.opacity = 1f;
        }

        private static void SetScale(VisualElement element, float scale)
        {
            element.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }
    }
}
