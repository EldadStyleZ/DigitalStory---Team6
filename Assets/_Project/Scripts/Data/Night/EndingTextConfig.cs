using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The words shown when the night ends.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Ending Text Config", fileName = "EndingTextConfig")]
    public sealed class EndingTextConfig : ScriptableObject
    {
        [field: Header("Won")]
        [field: SerializeField, Tooltip("Title after reaching the quota.")]
        public string WonTitle { get; private set; } = "Hamelin is saved";

        [field: SerializeField, TextArea, Tooltip("Message after reaching the quota.")]
        public string WonMessage { get; private set; } = "The town is quiet. Now you only have to collect the payment the mayor promised you. What could possibly go wrong?";

        [field: Header("Quota out of reach")]
        [field: SerializeField, Tooltip("Title when too few rats remain.")]
        public string QuotaOutOfReachTitle { get; private set; } = "The rats won tonight";

        [field: SerializeField, TextArea, Tooltip("Message when too few rats remain.")]
        public string QuotaOutOfReachMessage { get; private set; } = "The cats ate too many rats. There are not enough left to reach the goal.";

        [field: Header("Dawn")]
        [field: SerializeField, Tooltip("Title when the sun rises first.")]
        public string DawnTitle { get; private set; } = "The sun rose. You died.";

        [field: SerializeField, TextArea, Tooltip("Message when the sun rises first.")]
        public string DawnMessage { get; private set; } = "The Piper is a creature of the night. All that is left is a colorful hat and a flute. The rats ate the hat.";
    }
}
