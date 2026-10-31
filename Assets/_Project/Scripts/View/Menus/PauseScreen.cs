using System;
using DigitalStory.Data;

namespace DigitalStory.View
{
    /// <summary>The screen shown while the night is paused.</summary>
    public sealed class PauseScreen : IMenuScreen
    {
        public const string ResumeButtonName = "resume";
        public const string QuitButtonName = "quit";

        private readonly MenuTextConfig _texts;
        private readonly Action _resume;
        private readonly Action _quit;

        public PauseScreen(MenuTextConfig texts, Action resume, Action quit)
        {
            _texts = texts;
            _resume = resume;
            _quit = quit;
        }

        public void Fill(MenuCard card)
        {
            card.AddHeading(_texts.PauseTitle);
            card.AddParagraph(_texts.PauseMessage);
            card.AddMainButton(ResumeButtonName, _texts.ResumeButton, _resume);
            card.AddSecondButton(QuitButtonName, _texts.QuitButton, _quit);
        }
    }
}
