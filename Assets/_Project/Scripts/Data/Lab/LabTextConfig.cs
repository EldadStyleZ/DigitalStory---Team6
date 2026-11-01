using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The words shown in the lab.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Text Config", fileName = "LabTextConfig")]
    public sealed class LabTextConfig : ScriptableObject
    {
        [SerializeField, Tooltip("The name of each flute note, in order.")]
        private string[] _noteNames = { "H", "J", "K", "L" };

        public IReadOnlyList<string> NoteNames => _noteNames;

        [field: Header("Screen")]
        [field: SerializeField, Tooltip("Before the level number.")]
        public string LevelLabel { get; private set; } = "Level";

        [field: SerializeField, Tooltip("Shown while a fight is on.")]
        public string InBattleLabel { get; private set; } = "Battle! Play a combo";

        [field: SerializeField, Tooltip("Shown while nothing has noticed the Piper.")]
        public string QuietLabel { get; private set; } = "All quiet";

        [field: SerializeField, Tooltip("Shown while the creatures hit harder, before the seconds left.")]
        public string BoostLabel { get; private set; } = "War Song";

        [field: SerializeField, Tooltip("Shown over a combo not yet unlocked, before its level.")]
        public string LockedLabel { get; private set; } = "Unlocks at level";

        [field: Header("Messages")]
        [field: SerializeField, Tooltip("When a combo is still cooling down.")]
        public string CoolingDownMessage { get; private set; } = "Still cooling down";

        [field: SerializeField, Tooltip("When there is not enough stamina.")]
        public string TooTiredMessage { get; private set; } = "Not enough stamina";

        [field: SerializeField, Tooltip("When too many creatures are alive.")]
        public string NoRoomMessage { get; private set; } = "You can't lead any more creatures";

        [field: SerializeField, Tooltip("When a combo fails.")]
        public string FailedComboMessage { get; private set; } = "Wrong combo: stamina lost";

        [field: SerializeField, Tooltip("When creatures go home after a quiet spell.")]
        public string CreatureLeftMessage { get; private set; } = "A creature went home";

        [field: SerializeField, Tooltip("When hidden enemies are about to appear.")]
        public string AmbushMessage { get; private set; } = "Ambush!";

        [field: SerializeField, Tooltip("When the Piper reaches a new level, before the level number.")]
        public string LevelUpMessage { get; private set; } = "You reached level";

        [field: Header("Pause")]
        [field: SerializeField, Tooltip("The pause screen's heading.")]
        public string PausedTitle { get; private set; } = "Paused";

        [field: SerializeField, Tooltip("The button that goes back to the maze.")]
        public string ResumeButton { get; private set; } = "Resume";

        [field: Header("Ending")]
        [field: SerializeField, Tooltip("The heading after dying.")]
        public string DeadTitle { get; private set; } = "You fell in the maze";

        [field: SerializeField, Tooltip("The choice after dying.")]
        public string DeadMessage { get; private set; } = "Continue with your level and everything you killed, or start over from level 1?";

        [field: SerializeField, Tooltip("The heading after beating the last boss.")]
        public string WonTitle { get; private set; } = "The maze is yours";

        [field: SerializeField, Tooltip("The message after beating the last boss.")]
        public string WonMessage { get; private set; } = "You beat the last boss and everything between you and it.";

        [field: SerializeField, Tooltip("The button that keeps the level.")]
        public string ContinueButton { get; private set; } = "Continue";

        [field: SerializeField, Tooltip("The button that starts over.")]
        public string NewGameButton { get; private set; } = "New game";
    }
}
