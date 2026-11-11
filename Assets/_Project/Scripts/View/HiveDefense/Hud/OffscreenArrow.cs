using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Places an arrow label at the screen edge, pointing at a world point that is out of sight. The arrow stays on the line
    /// from the screen's centre toward the point, and slides in along it until it is clear of every HUD block it is given, so
    /// it never covers a panel, the phase line, the lesson or the songbook.
    /// </summary>
    public static class OffscreenArrow
    {
        private const float Tiny = 0.0001f;

        /// <summary>
        /// Returns false when the point is on screen. Otherwise moves the arrow to the edge, <paramref name="margin"/> panel pixels
        /// in, pointing at the point, then in toward the centre until it is <paramref name="clearance"/> panel pixels clear of
        /// each of <paramref name="keepOut"/>, and returns true. The layer's size, and the blocks' world bounds, are in panel
        /// pixels, so the layer must cover the whole panel from its top-left corner. Nothing is allocated.
        /// </summary>
        public static bool TryPlace(
            Camera camera,
            Label arrow,
            Vector3 world,
            float width,
            float height,
            float margin,
            IReadOnlyList<VisualElement> keepOut,
            float clearance)
        {
            Vector3 view = camera.WorldToViewportPoint(world);
            bool isInFront = view.z > 0f;
            if (isInFront && view.x >= 0f && view.x <= 1f && view.y >= 0f && view.y <= 1f)
            {
                return false;
            }

            // Panel pixels from the centre, y down. A point behind the camera points the opposite way.
            float sign = isInFront ? 1f : -1f;
            float dx = (view.x - 0.5f) * width * sign;
            float dy = -(view.y - 0.5f) * height * sign;
            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
            {
                dy = 1f;
            }

            float halfWidth = Mathf.Max(1f, width * 0.5f - margin);
            float halfHeight = Mathf.Max(1f, height * 0.5f - margin);
            float scale = Mathf.Min(
                Mathf.Abs(dx) > Tiny ? halfWidth / Mathf.Abs(dx) : float.MaxValue,
                Mathf.Abs(dy) > Tiny ? halfHeight / Mathf.Abs(dy) : float.MaxValue);
            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            scale = ClearOf(keepOut, clearance, centerX, centerY, dx, dy, scale);

            arrow.style.left = centerX + dx * scale;
            arrow.style.top = centerY + dy * scale;
            arrow.style.rotate = new Rotate(new Angle(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, AngleUnit.Degree));
            return true;
        }

        // Moving in along the ray can only leave a block or enter one nearer the centre, so one pass per block settles it.
        // A block that holds the centre itself cannot be cleared by moving in, and is passed over.
        private static float ClearOf(
            IReadOnlyList<VisualElement> keepOut, float clearance, float centerX, float centerY, float dx, float dy, float scale)
        {
            if (keepOut == null)
            {
                return scale;
            }

            for (int pass = 0; pass < keepOut.Count; pass++)
            {
                bool hasMoved = false;
                for (int index = 0; index < keepOut.Count; index++)
                {
                    Rect block = keepOut[index].worldBound;
                    if (!(block.width > 0f) || !(block.height > 0f))
                    {
                        continue; // Hidden, or not laid out yet.
                    }

                    float left = block.xMin - clearance;
                    float right = block.xMax + clearance;
                    float top = block.yMin - clearance;
                    float bottom = block.yMax + clearance;
                    float x = centerX + dx * scale;
                    float y = centerY + dy * scale;
                    if (x <= left || x >= right || y <= top || y >= bottom)
                    {
                        continue;
                    }

                    // Where the ray from the centre first enters the block: the later of its entries on the two axes.
                    float enter = Mathf.Max(Entry(centerX, dx, left, right), Entry(centerY, dy, top, bottom));
                    if (enter > 0f && enter < scale)
                    {
                        scale = enter;
                        hasMoved = true;
                    }
                }

                if (!hasMoved)
                {
                    break;
                }
            }

            return scale;
        }

        // The ray parameter at which a line from start, moving by step per unit, enters the slab from low to high.
        private static float Entry(float start, float step, float low, float high)
        {
            if (Mathf.Abs(step) < Tiny)
            {
                return start > low && start < high ? float.NegativeInfinity : float.PositiveInfinity;
            }

            return Mathf.Min((low - start) / step, (high - start) / step);
        }
    }
}
