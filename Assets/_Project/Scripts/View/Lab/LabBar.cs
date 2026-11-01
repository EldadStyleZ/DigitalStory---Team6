using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>A bar on screen that fills from the left.</summary>
    public sealed class LabBar
    {
        private readonly VisualElement _fill;

        public LabBar(float height, Color fillColor, Color trackColor)
        {
            Root = new VisualElement();
            Root.pickingMode = PickingMode.Ignore;
            Root.style.height = height;
            Root.style.backgroundColor = trackColor;

            _fill = new VisualElement();
            _fill.pickingMode = PickingMode.Ignore;
            _fill.style.height = Length.Percent(100f);
            _fill.style.backgroundColor = fillColor;
            Root.Add(_fill);
        }

        public VisualElement Root { get; }

        /// <summary>How full the bar is, from 0 to 1, for tests and debugging.</summary>
        public float Share { get; private set; }

        public void Show(float share)
        {
            Share = Mathf.Clamp01(share);
            _fill.style.width = Length.Percent(Share * 100f);
        }
    }
}
