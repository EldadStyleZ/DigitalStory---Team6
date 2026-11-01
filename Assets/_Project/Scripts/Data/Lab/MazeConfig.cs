using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How the lab's maze is generated.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Maze Config", fileName = "MazeConfig")]
    public sealed class MazeConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("The same seed always builds the same maze.")]
        public int Seed { get; private set; } = 4242;

        [field: SerializeField, Tooltip("The maze's size.")]
        public MazeShape Shape { get; private set; } = new MazeShape(60, 60, 3, 1, 0.015f);
    }
}
