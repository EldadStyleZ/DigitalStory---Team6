using System;
using DigitalStory.Data;
using DigitalStory.Logic;

namespace DigitalStory.View
{
    /// <summary>The screen that tells the player how the night ended.</summary>
    public sealed class ResultScreen : IMenuScreen
    {
        public const string AgainButtonName = "again";

        private readonly NightSession _session;
        private readonly MenuTextConfig _texts;
        private readonly EndingTextConfig _endings;
        private readonly Action _again;

        public ResultScreen(NightSession session, MenuTextConfig texts, EndingTextConfig endings, Action again)
        {
            _session = session;
            _texts = texts;
            _endings = endings;
            _again = again;
        }

        public void Fill(MenuCard card)
        {
            bool isWon = _session.Outcome == NightOutcome.Won;
            int drowned = _session.Swarm.DrownedCount;

            card.AddHeading(Heading());
            card.AddStat(
                isWon
                    ? string.Format(_texts.WonStatFormat, drowned)
                    : string.Format(_texts.LostStatFormat, drowned, _session.Quota));
            card.AddParagraph(Message());
            card.AddMainButton(AgainButtonName, isWon ? _texts.PlayAgainButton : _texts.RetryButton, _again);
        }

        private string Heading() => _session.Outcome switch
        {
            NightOutcome.Won => _endings.WonTitle,
            NightOutcome.QuotaOutOfReach => _endings.QuotaOutOfReachTitle,
            _ => _endings.DawnTitle,
        };

        private string Message() => _session.Outcome switch
        {
            NightOutcome.Won => _endings.WonMessage,
            NightOutcome.QuotaOutOfReach => _endings.QuotaOutOfReachMessage,
            _ => _endings.DawnMessage,
        };
    }
}
