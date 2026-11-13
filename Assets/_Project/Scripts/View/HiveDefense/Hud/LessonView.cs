using System;
using DigitalStory.Logic;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The first round's lesson: one prompt at a time, from <see cref="Tutorial.Current"/>. Nothing shows once it is
    /// <see cref="TutorialStep.Done"/>.
    /// </summary>
    public sealed class LessonView
    {
        private const string ShownClass = "line--shown";

        private readonly Tutorial _tutorial;
        private readonly Label _line;
        private readonly string[] _prompts;
        private TutorialStep _shown;
        private bool _isShown;
        private ControlDevice _device;

        /// <summary>Builds the view. The prompts are indexed by <see cref="TutorialStep"/>; the entry for Done is empty.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="ArgumentException">A step has no prompt.</exception>
        public LessonView(Tutorial tutorial, Label line, string move, string sting, string collectNest, string visitHive, string healHive, string callWave)
        {
            _tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial));
            _line = line ?? throw new ArgumentNullException(nameof(line));
            _prompts = new string[Enum.GetValues(typeof(TutorialStep)).Length];
            _prompts[(int)TutorialStep.Move] = Prompt(move, nameof(move));
            _prompts[(int)TutorialStep.PlaySting] = Prompt(sting, nameof(sting));
            _prompts[(int)TutorialStep.CollectNest] = Prompt(collectNest, nameof(collectNest));
            _prompts[(int)TutorialStep.VisitHive] = Prompt(visitHive, nameof(visitHive));
            _prompts[(int)TutorialStep.HealHive] = Prompt(healHive, nameof(healHive));
            _prompts[(int)TutorialStep.CallWave] = Prompt(callWave, nameof(callWave));
            _prompts[(int)TutorialStep.Done] = string.Empty;
        }

        /// <summary>Names the keys of the prompts for the device in use, starting with the prompt on screen.</summary>
        public void ShowControls(ControlDevice device)
        {
            if (device == _device)
            {
                return;
            }

            _device = device;
            if (_isShown)
            {
                ControlSymbols.ShowPrompt(_line, _prompts[(int)_shown], device);
            }
        }

        /// <summary>Shows the current step. Call once per frame; it changes the line only when the step changes.</summary>
        public void Tick()
        {
            TutorialStep step = _tutorial.Current;
            if (_isShown && step == _shown)
            {
                return;
            }

            _isShown = true;
            _shown = step;
            string text = _prompts[(int)step];
            ControlSymbols.ShowPrompt(_line, text, _device);
            _line.EnableInClassList(ShownClass, text.Length > 0);
        }

        private static string Prompt(string text, string name)
        {
            return string.IsNullOrWhiteSpace(text) ? throw new ArgumentException("The lesson prompt is empty.", name) : text;
        }
    }
}
