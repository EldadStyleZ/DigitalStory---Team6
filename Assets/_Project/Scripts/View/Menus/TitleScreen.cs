using System;
using DigitalStory.Data;
using DigitalStory.Logic;

namespace DigitalStory.View
{
    /// <summary>The first screen: what the game is, how to play and the button that starts the night.</summary>
    public sealed class TitleScreen : IMenuScreen
    {
        public const string StartButtonName = "start";

        private readonly MenuTextConfig _texts;
        private readonly TownArtConfig _art;
        private readonly HudConfig _hud;
        private readonly NightSession _session;
        private readonly Action _start;

        public TitleScreen(MenuTextConfig texts, TownArtConfig art, HudConfig hud, NightSession session, Action start)
        {
            _texts = texts;
            _art = art;
            _hud = hud;
            _session = session;
            _start = start;
        }

        public void Fill(MenuCard card)
        {
            card.AddGameTitle(_texts.GameTitle);
            card.AddParagraph(_texts.Pitch);
            card.AddStat(string.Format(_texts.GoalFormat, _session.Quota, _session.Swarm.Count));
            card.AddParagraph(_texts.Tip);

            foreach (HowToLine line in _texts.HowTo)
            {
                card.AddHowToLine(line.Label, line.Text, line.ColorHole >= 0 ? _art.HoleColor(line.ColorHole) : _hud.TextColor);
            }

            card.AddMainButton(StartButtonName, _texts.StartButton, _start);
        }
    }
}
