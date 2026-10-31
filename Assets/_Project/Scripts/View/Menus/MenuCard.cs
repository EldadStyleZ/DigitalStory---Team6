using System;
using DigitalStory.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>The panel every menu screen is written on, with one method for each kind of line.</summary>
    public sealed class MenuCard
    {
        private const float Width = 640f;
        private const float Padding = 20f;
        private const float Radius = 12f;
        private const float GameTitleFontSize = 44f;
        private const float HeadingFontSize = 30f;
        private const float StatFontSize = 22f;
        private const float BodyFontSize = 14f;
        private const float HowToLabelWidth = 64f;
        private const float LineGap = 8f;
        private const float RowGap = 2f;
        private const float ButtonGap = 10f;
        private const float ButtonFontSize = 15f;
        private const float ButtonSidePadding = 18f;
        private const float ButtonEndPadding = 9f;
        private const float ButtonRadius = 10f;

        private readonly HudConfig _hud;
        private readonly VisualElement _card;

        private VisualElement _buttonRow;

        public MenuCard(VisualElement parent, HudConfig hud)
        {
            _hud = hud;
            _card = new VisualElement();
            _card.style.width = Width;
            _card.style.maxWidth = Length.Percent(96f);
            _card.style.paddingLeft = Padding;
            _card.style.paddingRight = Padding;
            _card.style.paddingTop = Padding;
            _card.style.paddingBottom = Padding;
            _card.style.borderTopLeftRadius = Radius;
            _card.style.borderTopRightRadius = Radius;
            _card.style.borderBottomLeftRadius = Radius;
            _card.style.borderBottomRightRadius = Radius;
            _card.style.backgroundColor = hud.PanelColor;
            parent.Add(_card);
        }

        /// <summary>The heading now on the card.</summary>
        public string Title { get; private set; }

        /// <summary>The card itself, so tests can measure it.</summary>
        public VisualElement Root => _card;

        public void Clear()
        {
            _card.Clear();
            _buttonRow = null;
            Title = null;
        }

        /// <summary>Adds the large name of the game as the heading.</summary>
        public void AddGameTitle(string text)
        {
            Title = text;
            _card.Add(NewLine(text, GameTitleFontSize, _hud.TextColor));
        }

        public void AddHeading(string text)
        {
            Title = text;
            _card.Add(NewLine(text, HeadingFontSize, _hud.TextColor));
        }

        public void AddStat(string text) => _card.Add(NewLine(text, StatFontSize, _hud.GlowColor));

        public void AddParagraph(string text) => _card.Add(NewLine(text, BodyFontSize, _hud.TextColor));

        public void AddHowToLine(string label, string text, Color labelColor)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = RowGap;

            Label word = NewText(label, BodyFontSize, labelColor);
            word.style.width = HowToLabelWidth;
            word.style.flexShrink = 0f;

            Label explanation = NewText(text, BodyFontSize, _hud.CaptionColor);
            explanation.style.flexGrow = 1f;
            explanation.style.flexShrink = 1f;

            row.Add(word);
            row.Add(explanation);
            _card.Add(row);
        }

        /// <summary>Adds the main button, which also takes keyboard focus.</summary>
        public void AddMainButton(string name, string text, Action pressed)
        {
            Button button = AddButton(name, text, pressed, _hud.ButtonColor, _hud.ButtonTextColor);
            button.Focus();
        }

        public void AddSecondButton(string name, string text, Action pressed)
        {
            AddButton(name, text, pressed, _hud.SecondButtonColor, _hud.TextColor);
        }

        private Button AddButton(string name, string text, Action pressed, Color background, Color textColor)
        {
            if (_buttonRow == null)
            {
                _buttonRow = new VisualElement();
                _buttonRow.style.flexDirection = FlexDirection.Row;
                _buttonRow.style.marginTop = ButtonGap;
                _card.Add(_buttonRow);
            }

            var button = new Button(pressed) { name = name, text = text };
            button.style.fontSize = ButtonFontSize;
            button.style.color = textColor;
            button.style.backgroundColor = background;
            button.style.paddingLeft = ButtonSidePadding;
            button.style.paddingRight = ButtonSidePadding;
            button.style.paddingTop = ButtonEndPadding;
            button.style.paddingBottom = ButtonEndPadding;
            button.style.marginLeft = 0f;
            button.style.marginRight = ButtonGap;
            button.style.borderTopWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderTopLeftRadius = ButtonRadius;
            button.style.borderTopRightRadius = ButtonRadius;
            button.style.borderBottomLeftRadius = ButtonRadius;
            button.style.borderBottomRightRadius = ButtonRadius;
            _buttonRow.Add(button);
            return button;
        }

        private static Label NewLine(string text, float fontSize, Color color)
        {
            Label label = NewText(text, fontSize, color);
            label.style.marginTop = LineGap;
            return label;
        }

        private static Label NewText(string text, float fontSize, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            return label;
        }
    }
}
