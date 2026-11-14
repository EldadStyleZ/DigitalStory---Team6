using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// A little drawn bee, in place of the word "Wasps" on the swarm's panel: an amber body with dark stripes, a dark head
    /// with antennae, a sting, and two pale wings that flutter, with a soft golden glow round it.
    /// </summary>
    public sealed class BeeIcon : VisualElement
    {
        private const float Width = 38f;
        private const float Height = 28f;
        private static readonly Color Amber = new Color(1f, 0.74f, 0.22f, 1f);
        private static readonly Color Dark = new Color(0.12f, 0.08f, 0.04f, 1f);
        private static readonly Color Wing = new Color(0.85f, 0.95f, 1f, 0.55f);

        private float _flap;

        /// <summary>Swaps the row's text title (the first label) for a bee.</summary>
        public static void ReplaceTitle(VisualElement row)
        {
            if (row == null || row.childCount == 0 || !(row[0] is Label))
            {
                return;
            }

            row[0].style.display = DisplayStyle.None;
            row.Insert(0, new BeeIcon());
        }

        public BeeIcon()
        {
            pickingMode = PickingMode.Ignore;
            style.width = Width;
            style.height = Height;
            style.flexShrink = 0;
            style.marginRight = 6f;
            generateVisualContent += Draw;
            schedule.Execute(() =>
            {
                _flap += 1f;
                MarkDirtyRepaint();
            }).Every(60);
        }

        private void Draw(MeshGenerationContext context)
        {
            Painter2D paint = context.painter2D;
            var body = new Vector2(Width * 0.52f, Height * 0.6f);
            float beat = Mathf.Sin(_flap * 1.9f);

            // A soft golden glow.
            for (int ring = 3; ring >= 1; ring--)
            {
                Ellipse(paint, body, 11f + (ring * 2.5f), 7f + (ring * 2.5f), 0f);
                paint.fillColor = new Color(Amber.r, Amber.g, Amber.b, 0.06f * (4 - ring));
                paint.Fill();
            }

            // Wings, behind the body, fluttering.
            float lift = 3f + (beat * 1.5f);
            Ellipse(paint, body + new Vector2(-2f, -6f - lift * 0.4f), 6.5f, 4f + (beat * 0.6f), -25f);
            paint.fillColor = Wing;
            paint.Fill();
            Ellipse(paint, body + new Vector2(4f, -7f - lift * 0.5f), 7f, 4.5f + (beat * 0.6f), 20f);
            paint.fillColor = Wing;
            paint.Fill();

            // The sting.
            paint.BeginPath();
            paint.MoveTo(body + new Vector2(-11f, -1.5f));
            paint.LineTo(body + new Vector2(-15f, 0.5f));
            paint.LineTo(body + new Vector2(-11f, 2f));
            paint.ClosePath();
            paint.fillColor = Dark;
            paint.Fill();

            // The body and its stripes.
            Ellipse(paint, body, 11f, 7f, 0f);
            paint.fillColor = Amber;
            paint.Fill();
            for (int stripe = 0; stripe < 2; stripe++)
            {
                float x = -4f + (stripe * 5f);
                paint.BeginPath();
                paint.MoveTo(body + new Vector2(x - 1.2f, -6.6f));
                paint.LineTo(body + new Vector2(x + 1.2f, -6.6f));
                paint.LineTo(body + new Vector2(x + 1.2f, 6.6f));
                paint.LineTo(body + new Vector2(x - 1.2f, 6.6f));
                paint.ClosePath();
                paint.fillColor = Dark;
                paint.Fill();
            }

            Ellipse(paint, body, 11f, 7f, 0f);
            paint.strokeColor = new Color(1f, 0.9f, 0.6f, 0.6f);
            paint.lineWidth = 1f;
            paint.Stroke();

            // The head, an eye's glint and the antennae.
            var head = body + new Vector2(11.5f, -1.5f);
            Ellipse(paint, head, 4.5f, 4.5f, 0f);
            paint.fillColor = Dark;
            paint.Fill();
            Ellipse(paint, head + new Vector2(1.5f, -1f), 1.1f, 1.1f, 0f);
            paint.fillColor = new Color(1f, 1f, 1f, 0.8f);
            paint.Fill();

            paint.lineWidth = 1.2f;
            paint.strokeColor = Dark;
            paint.lineCap = LineCap.Round;
            paint.BeginPath();
            paint.MoveTo(head + new Vector2(1f, -3.5f));
            paint.QuadraticCurveTo(head + new Vector2(2f, -8f), head + new Vector2(5f, -9f));
            paint.Stroke();
            paint.BeginPath();
            paint.MoveTo(head + new Vector2(2.5f, -3f));
            paint.QuadraticCurveTo(head + new Vector2(5f, -6.5f), head + new Vector2(8f, -6.5f));
            paint.Stroke();
        }

        private static void Ellipse(Painter2D paint, Vector2 centre, float rx, float ry, float degrees)
        {
            const int Points = 24;
            float turn = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(turn);
            float s = Mathf.Sin(turn);
            paint.BeginPath();
            for (int index = 0; index < Points; index++)
            {
                float a = index * Mathf.PI * 2f / Points;
                var local = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
                var point = centre + new Vector2((local.x * c) - (local.y * s), (local.x * s) + (local.y * c));
                if (index == 0)
                {
                    paint.MoveTo(point);
                }
                else
                {
                    paint.LineTo(point);
                }
            }

            paint.ClosePath();
        }
    }
}
