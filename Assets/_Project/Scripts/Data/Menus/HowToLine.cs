using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>One line of the how-to list on the title screen.</summary>
    [Serializable]
    public struct HowToLine
    {
        public const int NoHole = -1;

        [SerializeField, Tooltip("The short word on the left.")]
        private string _label;

        [SerializeField, TextArea, Tooltip("The explanation on the right.")]
        private string _text;

        [SerializeField, Tooltip("The flute hole whose color the label takes, counting from 0, or -1 for none.")]
        private int _colorHole;

        public HowToLine(string label, string text, int colorHole = NoHole)
        {
            _label = label;
            _text = text;
            _colorHole = colorHole;
        }

        public string Label => _label;
        public string Text => _text;
        public int ColorHole => _colorHole;
    }
}
