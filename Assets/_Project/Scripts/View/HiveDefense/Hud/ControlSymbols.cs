using System.Collections.Generic;
using DigitalStory.Logic;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Writes the HUD's key prompts for the device in use: the keyboard's words and letters, or, for a controller, its names
    /// (RB, Start) and a small diamond of four dots for each note's face button, with the pressed position filled. The diamond is
    /// built from elements and the style sheet, so it needs no font glyph and no image, and it names no brand of pad.
    /// </summary>
    /// <remarks>Call it when a prompt or the device changes, never every frame: a controller prompt makes new elements.</remarks>
    public static class ControlSymbols
    {
        private const string PromptClass = "pad-prompt";
        private const string PartClass = "pad-part";
        private const string DiamondClass = "pad-diamond";
        private const string CellDiamondClass = "pad-diamond--cell";
        private const string DotClass = "pad-dot";
        private const string DotOnClass = "pad-dot--on";

        // The dots in the holes' order: hole 0 is the West button, 1 South, 2 East and 3 North.
        private static readonly string[] DotPlaces = { "pad-dot--west", "pad-dot--south", "pad-dot--east", "pad-dot--north" };

        // The note colours of the HUD's style sheet, which tint the filled dot of a diamond in a prompt.
        private static readonly string[] HoleClasses = { "hole-0", "hole-1", "hole-2", "hole-3" };

        /// <summary>
        /// Shows <paramref name="keyboardText"/> on the label: as it is for the keyboard, or for a controller with its names for
        /// the keys and a diamond, in the note's colour, for each note key. The label keeps its own classes, so it shows and hides
        /// as before.
        /// </summary>
        public static void ShowPrompt(Label label, string keyboardText, ControlDevice device)
        {
            string text = keyboardText ?? string.Empty;
            if (label.childCount > 0)
            {
                label.Clear();
            }

            label.RemoveFromClassList(PromptClass);
            if (device != ControlDevice.Gamepad || text.Length == 0)
            {
                label.text = text;
                return;
            }

            var parts = new List<PromptPart>();
            ControlPrompt.Split(text, HiveDefenseInput.HoleKeyNames, HiveDefenseInput.PadNames, parts);
            if (!HasHole(parts))
            {
                label.text = parts.Count > 0 ? parts[0].Text : string.Empty;
                return;
            }

            // The parts sit in a row inside the label, which lays its children out once it holds any.
            label.text = string.Empty;
            label.AddToClassList(PromptClass);
            for (int index = 0; index < parts.Count; index++)
            {
                PromptPart part = parts[index];
                label.Add(part.IsHole ? MakeDiamond(part.Hole, true) : MakeWords(part.Text));
            }
        }

        /// <summary>
        /// Shows a note on a cell that already has the note's colour (a recent note, a songbook pip): the key's letter, or for a
        /// controller a dark diamond in the middle. A hole below 0 shows nothing. The diamond is made once and kept.
        /// </summary>
        public static void ShowHole(Label cell, int hole, ControlDevice device)
        {
            VisualElement diamond = cell.childCount > 0 ? cell[0] : null;
            if (device == ControlDevice.Gamepad && hole >= 0)
            {
                if (diamond == null)
                {
                    diamond = MakeDiamond(hole, false);
                    diamond.AddToClassList(CellDiamondClass);
                    cell.Add(diamond);
                }
                else
                {
                    SetHole(diamond, hole, false);
                }

                diamond.style.display = DisplayStyle.Flex;
                cell.text = string.Empty;
                return;
            }

            if (diamond != null)
            {
                diamond.style.display = DisplayStyle.None;
            }

            cell.text = hole >= 0 ? HiveDefenseInput.HoleName(hole) : string.Empty;
        }

        private static bool HasHole(List<PromptPart> parts)
        {
            for (int index = 0; index < parts.Count; index++)
            {
                if (parts[index].IsHole)
                {
                    return true;
                }
            }

            return false;
        }

        private static Label MakeWords(string text)
        {
            var words = new Label(text) { pickingMode = PickingMode.Ignore };
            words.AddToClassList(PartClass);
            return words;
        }

        private static VisualElement MakeDiamond(int hole, bool isTinted)
        {
            var diamond = new VisualElement { pickingMode = PickingMode.Ignore };
            diamond.AddToClassList(DiamondClass);
            for (int place = 0; place < DotPlaces.Length; place++)
            {
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList(DotClass);
                dot.AddToClassList(DotPlaces[place]);
                diamond.Add(dot);
            }

            SetHole(diamond, hole, isTinted);
            return diamond;
        }

        private static void SetHole(VisualElement diamond, int hole, bool isTinted)
        {
            for (int place = 0; place < diamond.childCount; place++)
            {
                VisualElement dot = diamond[place];
                bool isOn = place == hole;
                dot.EnableInClassList(DotOnClass, isOn);
                dot.EnableInClassList(HoleClasses[place], isTinted && isOn);
            }
        }
    }
}
