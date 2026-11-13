namespace DigitalStory.Logic
{
    /// <summary>One piece of a control prompt as <see cref="ControlPrompt.Split"/> cuts it: a run of words, or one flute hole.</summary>
    public readonly struct PromptPart
    {
        private PromptPart(string text, int hole)
        {
            Text = text;
            Hole = hole;
        }

        /// <summary>The words, for a text part; empty for a hole.</summary>
        public string Text { get; }

        /// <summary>The hole, counted from 0, for a hole part; -1 for text.</summary>
        public int Hole { get; }

        /// <summary>True for a hole, false for words.</summary>
        public bool IsHole => Hole >= 0;

        /// <summary>A run of words.</summary>
        public static PromptPart Words(string text) => new PromptPart(text ?? string.Empty, -1);

        /// <summary>One hole, counted from 0.</summary>
        public static PromptPart OfHole(int hole) => new PromptPart(string.Empty, hole);
    }
}
