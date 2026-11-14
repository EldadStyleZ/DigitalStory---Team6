using System;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// A small honeycomb at the bottom of the screen: one hexagon cell for each flute key (H J K L), in the songbook's
    /// colours. A cell lights up and glows while its key is held, and flashes when it is struck, so playing reads as
    /// lighting up the hive.
    /// </summary>
    public sealed class HiveKeysView : IDisposable
    {
        private const int Keys = 4;
        private const float Radius = 22f;                 // pixels from a cell's centre to a corner
        private const float Gap = 3f;
        private const float GlowRoom = 14f;
        private const float FlashSeconds = 0.45f;

        private static readonly Color[] KeyColors =
        {
            new Color32(240, 120, 110, 255), new Color32(240, 170, 90, 255), new Color32(240, 220, 100, 255), new Color32(130, 210, 120, 255),
        };

        private static readonly Color CellDark = new Color(0.05f, 0.05f, 0.04f, 0.72f);
        private static readonly Color Wax = new Color(1f, 0.82f, 0.38f, 1f);

        private readonly HiveDefenseInput _input;
        private readonly VisualElement _comb;
        private readonly Label[] _letters = new Label[Keys];
        private readonly Vector2[] _centres = new Vector2[Keys];
        private readonly float[] _flash = new float[Keys];
        private readonly float[] _lit = new float[Keys];
        private ControlDevice _device = (ControlDevice)(-1);

        public HiveKeysView(HiveDefenseInput input, VisualElement parent)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));

            // A tight little comb of flat-topped cells: H, J and K in a zigzag, and L on top, nested between H and K above J.
            float step = Radius * 1.5f + Gap;
            float half = Radius * 0.8660254f + (Gap * 0.5f);
            int[] column = { 0, 1, 2, 1 };
            int[] row = { 2, 3, 2, 1 };
            for (int key = 0; key < Keys; key++)
            {
                _centres[key] = new Vector2(GlowRoom + Radius + (column[key] * step), GlowRoom + (row[key] * half));
            }

            _comb = new VisualElement { pickingMode = PickingMode.Ignore, name = "hive-keys" };
            _comb.style.width = (GlowRoom * 2f) + (Radius * 2f) + (step * 2f);
            _comb.style.height = (GlowRoom * 2f) + (half * 4f);
            _comb.style.marginTop = 2f;
            _comb.generateVisualContent += Draw;

            for (int key = 0; key < Keys; key++)
            {
                var letter = new Label { pickingMode = PickingMode.Ignore };
                letter.style.position = Position.Absolute;
                letter.style.left = _centres[key].x - Radius;
                letter.style.top = _centres[key].y - Radius;
                letter.style.width = Radius * 2f;
                letter.style.height = Radius * 2f;
                letter.style.unityTextAlign = TextAnchor.MiddleCenter;
                letter.style.fontSize = 15;
                letter.style.unityFontStyleAndWeight = FontStyle.Bold;
                // No default margins or padding, so the letter sits in the very middle of its cell.
                letter.style.marginLeft = 0;
                letter.style.marginRight = 0;
                letter.style.marginTop = 0;
                letter.style.marginBottom = 0;
                letter.style.paddingLeft = 0;
                letter.style.paddingRight = 0;
                letter.style.paddingTop = 0;
                letter.style.paddingBottom = 0;
                letter.style.borderLeftWidth = 0;
                letter.style.borderRightWidth = 0;
                letter.style.borderTopWidth = 0;
                letter.style.borderBottomWidth = 0;
                _comb.Add(letter);
                _letters[key] = letter;
            }

            (parent ?? throw new ArgumentNullException(nameof(parent))).Add(_comb);
            _input.HolePressed += Strike;
        }

        public void Tick(float deltaTime)
        {
            if (_input.LastDevice != _device)
            {
                _device = _input.LastDevice;
                for (int key = 0; key < Keys; key++)
                {
                    ControlSymbols.ShowHole(_letters[key], key, _device);
                }
            }

            bool changed = false;
            for (int key = 0; key < Keys; key++)
            {
                float target = _input.IsHoleHeld(key) ? 1f : 0f;
                float lit = Mathf.MoveTowards(_lit[key], target, deltaTime * (target > _lit[key] ? 14f : 5f));
                float flash = Mathf.Max(0f, _flash[key] - (deltaTime / FlashSeconds));
                changed |= !Mathf.Approximately(lit, _lit[key]) || !Mathf.Approximately(flash, _flash[key]);
                _lit[key] = lit;
                _flash[key] = flash;
                float glow = Mathf.Max(lit, flash);
                _letters[key].style.color = Color.Lerp(KeyColors[key], new Color(0.12f, 0.08f, 0.02f), glow);
            }

            if (changed)
            {
                _comb.MarkDirtyRepaint();
            }
        }

        public void Dispose()
        {
            _input.HolePressed -= Strike;
            _comb.generateVisualContent -= Draw;
            _comb.RemoveFromHierarchy();
        }

        private void Strike(int hole)
        {
            if (hole >= 0 && hole < Keys)
            {
                _flash[hole] = 1f;
                _comb.MarkDirtyRepaint();
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            Painter2D paint = context.painter2D;
            for (int key = 0; key < Keys; key++)
            {
                float glow = Mathf.Max(_lit[key], _flash[key]);
                Color color = KeyColors[key];

                // The halo of a lit cell: a few widening, fading hexagons.
                for (int ring = 3; ring >= 1 && glow > 0.01f; ring--)
                {
                    Color halo = color;
                    halo.a = glow * 0.16f * (4 - ring) * 0.5f;
                    Hexagon(paint, _centres[key], Radius + (ring * 4f) + (_flash[key] * 6f));
                    paint.fillColor = halo;
                    paint.Fill();
                }

                // The cell: dark wax, filled with honey light as it glows.
                Hexagon(paint, _centres[key], Radius);
                Color fill = Color.Lerp(CellDark, new Color(color.r, color.g, color.b, 0.95f), glow);
                paint.fillColor = fill;
                paint.Fill();

                // A thin inner bevel and the wax rim.
                Hexagon(paint, _centres[key], Radius - 4f);
                paint.strokeColor = new Color(color.r, color.g, color.b, 0.25f + (0.5f * glow));
                paint.lineWidth = 1f;
                paint.Stroke();

                Hexagon(paint, _centres[key], Radius);
                paint.strokeColor = Color.Lerp(new Color(Wax.r, Wax.g, Wax.b, 0.55f), Color.white, glow * 0.6f);
                paint.lineWidth = 2f;
                paint.Stroke();
            }
        }

        private static void Hexagon(Painter2D paint, Vector2 centre, float radius)
        {
            paint.BeginPath();
            for (int corner = 0; corner < 6; corner++)
            {
                float angle = corner * Mathf.PI / 3f;
                var point = centre + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                if (corner == 0)
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
