using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The words on the title and pause screens, and the shared words on the result screen.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Menu Text Config", fileName = "MenuTextConfig")]
    public sealed class MenuTextConfig : ScriptableObject
    {
        [field: Header("Title screen")]
        [field: SerializeField, Tooltip("The name of the game.")]
        public string GameTitle { get; private set; } = "The Piper";

        [field: SerializeField, TextArea, Tooltip("What the game is about.")]
        public string Pitch { get; private set; } =
            "Hamelin is crawling with rats, and you have one toy: a flute with six holes. "
            + "Every note moves the swarm differently. Gather the rats behind you and lead them to the river before dawn.";

        [field: SerializeField, Tooltip("The goal. {0} is the rats to drown and {1} is all the rats.")]
        public string GoalFormat { get; private set; } = "Drown {0} of {1} rats";

        [field: SerializeField, TextArea, Tooltip("A hint shown under the goal.")]
        public string Tip { get; private set; } =
            "A cat roams the streets and eats rats. Holes 5 and 6 drive it off. "
            + "Try playing hole 3, then 2, then 1: a lullaby that puts cats to sleep.";

        [field: SerializeField, Tooltip("The button that starts the night.")]
        public string StartButton { get; private set; } = "Start playing";

        [SerializeField, Tooltip("How to play, one line each.")]
        private HowToLine[] _howTo =
        {
            new HowToLine("Move", "Arrows or WASD, or hold the mouse button"),
            new HowToLine("Flute", "Six holes: keys 1 to 6, or click the flute and slide between holes"),
            new HowToLine("Call", "Holes 1 and 2: the rats come after you", 0),
            new HowToLine("March", "Holes 3 and 4: charmed rats run ahead, the way you are facing", 2),
            new HowToLine("Scare", "Holes 5 and 6: scatters rats and drives cats off", 4),
            new HowToLine("Songs", "The right run of notes casts a spell. The songs are listed above the flute"),
            new HowToLine("Pause", "P or Esc"),
        };

        [field: Header("Pause screen")]
        [field: SerializeField, Tooltip("The heading while paused.")]
        public string PauseTitle { get; private set; } = "Pause";

        [field: SerializeField, TextArea, Tooltip("The line under the heading.")]
        public string PauseMessage { get; private set; } = "The rats are waiting patiently. More or less.";

        [field: SerializeField, Tooltip("The button that carries on.")]
        public string ResumeButton { get; private set; } = "Resume";

        [field: SerializeField, Tooltip("The button that gives up and goes to the title.")]
        public string QuitButton { get; private set; } = "Back to the title";

        [field: Header("Result screen")]
        [field: SerializeField, Tooltip("The score after winning. {0} is the rats drowned.")]
        public string WonStatFormat { get; private set; } = "{0} rats in the river";

        [field: SerializeField, Tooltip("The score after losing. {0} is the rats drowned and {1} is the goal.")]
        public string LostStatFormat { get; private set; } = "{0}/{1} drowned";

        [field: SerializeField, Tooltip("The button after winning.")]
        public string PlayAgainButton { get; private set; } = "Play again";

        [field: SerializeField, Tooltip("The button after losing.")]
        public string RetryButton { get; private set; } = "Try again";

        [field: Header("HUD")]
        [field: SerializeField, Tooltip("The rats drowned so far. {0} is drowned and {1} is the goal.")]
        public string GoalCountFormat { get; private set; } = "{0}/{1}";

        [field: SerializeField, Tooltip("The caption under the goal.")]
        public string DrownedCaption { get; private set; } = "drowned";

        [field: SerializeField, Tooltip("The caption over the time.")]
        public string UntilDawnCaption { get; private set; } = "Until dawn";

        [field: SerializeField, Tooltip("The time left. {0} is the seconds.")]
        public string SecondsFormat { get; private set; } = "{0}s";

        [field: SerializeField, Tooltip("The rats still alive. {0} is the rats.")]
        public string RatsLeftFormat { get; private set; } = "{0} rats left";

        [field: SerializeField, Tooltip("The rats alive once cats have eaten some. {0} is alive and {1} is eaten.")]
        public string RatsLeftAndEatenFormat { get; private set; } = "{0} rats left · {1} eaten";

        [field: Header("Flute")]
        [field: SerializeField, Tooltip("The word over holes 1 and 2.")]
        public string CallLabel { get; private set; } = "Call";

        [field: SerializeField, Tooltip("The word over holes 3 and 4.")]
        public string MarchLabel { get; private set; } = "March";

        [field: SerializeField, Tooltip("The word over holes 5 and 6.")]
        public string ScareLabel { get; private set; } = "Scare";

        public IReadOnlyList<HowToLine> HowTo => _howTo;
    }
}
