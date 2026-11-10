using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Where each player's camera goes on screen, in viewport space (0 to 1, from the bottom left). One player fills
    /// the screen; two split it left and right; three or four take quadrants, and with three the empty quadrant
    /// shows an overview of the whole map.
    /// </summary>
    public readonly struct SplitLayout
    {
        private static readonly Rect Full = new Rect(0f, 0f, 1f, 1f);
        private static readonly Rect Left = new Rect(0f, 0f, 0.5f, 1f);
        private static readonly Rect Right = new Rect(0.5f, 0f, 0.5f, 1f);
        private static readonly Rect TopLeft = new Rect(0f, 0.5f, 0.5f, 0.5f);
        private static readonly Rect TopRight = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly Rect BottomLeft = new Rect(0f, 0f, 0.5f, 0.5f);
        private static readonly Rect BottomRight = new Rect(0.5f, 0f, 0.5f, 0.5f);

        private SplitLayout(Rect[] players, bool hasOverview, Rect overview)
        {
            Players = players;
            HasOverview = hasOverview;
            Overview = overview;
        }

        /// <summary>Each player's viewport, P1 first.</summary>
        public Rect[] Players { get; }

        /// <summary>Whether a quadrant is left over for the overview camera.</summary>
        public bool HasOverview { get; }

        /// <summary>The overview's viewport, when there is one.</summary>
        public Rect Overview { get; }

        /// <summary>Whether a line runs down the middle of the screen.</summary>
        public bool HasVerticalDivider => Players.Length >= 2;

        /// <summary>Whether a line runs across the middle of the screen.</summary>
        public bool HasHorizontalDivider => Players.Length >= 3;

        /// <summary>The layout for 1 to 4 players.</summary>
        public static SplitLayout For(int players)
        {
            switch (players)
            {
                case 1:
                    return new SplitLayout(new[] { Full }, false, default);
                case 2:
                    return new SplitLayout(new[] { Left, Right }, false, default);
                case 3:
                    return new SplitLayout(new[] { TopLeft, TopRight, BottomLeft }, true, BottomRight);
                case 4:
                    return new SplitLayout(new[] { TopLeft, TopRight, BottomLeft, BottomRight }, false, default);
                default:
                    throw new ArgumentOutOfRangeException(nameof(players), players, "Split screen is for 1 to 4 players.");
            }
        }
    }

    /// <summary>Camera maths: keeping a view on the map and fitting the whole map in a view.</summary>
    public static class CameraFraming
    {
        /// <summary>
        /// The nearest camera centre to the one wanted whose view stays inside the bounds. On an axis where the view
        /// is bigger than the bounds, the view is centred on them instead.
        /// </summary>
        public static Vector2 Clamp(Vector2 desired, Vector2 halfExtents, Rect bounds) => new Vector2(
            ClampAxis(desired.x, halfExtents.x, bounds.xMin, bounds.xMax),
            ClampAxis(desired.y, halfExtents.y, bounds.yMin, bounds.yMax));

        /// <summary>Half the width and height an orthographic camera sees.</summary>
        public static Vector2 HalfExtents(float orthographicSize, float aspect) =>
            new Vector2(orthographicSize * aspect, orthographicSize);

        /// <summary>The smallest orthographic size that shows all of the bounds at this aspect.</summary>
        public static float SizeToFit(Rect bounds, float aspect) =>
            Mathf.Max(bounds.height / 2f, bounds.width / 2f / aspect);

        private static float ClampAxis(float value, float half, float min, float max)
        {
            if (half * 2f >= max - min)
            {
                return (min + max) / 2f;
            }

            return Mathf.Clamp(value, min + half, max - half);
        }
    }
}
