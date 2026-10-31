using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>One effect to start: a sprite that moves, grows and fades.</summary>
    public readonly struct EffectSpawn
    {
        /// <param name="sprite">What to draw. It should be 1 scene unit wide.</param>
        /// <param name="order">Where it sits among the other sprites.</param>
        /// <param name="position">Where it starts, in town units.</param>
        /// <param name="velocity">How fast it moves, in town units per second.</param>
        /// <param name="color">Its color at full strength.</param>
        /// <param name="size">How wide it starts, in town units.</param>
        /// <param name="growth">How fast it widens, in town units per second.</param>
        /// <param name="flatness">Its height as a share of its width.</param>
        /// <param name="seconds">How long it lasts.</param>
        /// <param name="fadeSeconds">How long before the end it starts to fade.</param>
        public EffectSpawn(
            Sprite sprite,
            int order,
            Vector2 position,
            Vector2 velocity,
            Color color,
            float size,
            float growth,
            float flatness,
            float seconds,
            float fadeSeconds)
        {
            Sprite = sprite;
            Order = order;
            Position = position;
            Velocity = velocity;
            Color = color;
            Size = size;
            Growth = growth;
            Flatness = flatness;
            Seconds = seconds;
            FadeSeconds = fadeSeconds;
        }

        public Sprite Sprite { get; }
        public int Order { get; }
        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public Color Color { get; }
        public float Size { get; }
        public float Growth { get; }
        public float Flatness { get; }
        public float Seconds { get; }
        public float FadeSeconds { get; }
    }
}
