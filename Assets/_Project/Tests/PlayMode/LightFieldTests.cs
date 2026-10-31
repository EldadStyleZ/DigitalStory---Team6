using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class LightFieldTests
    {
        private static readonly Rect View = new Rect(0f, 0f, 10f, 6f);

        [Test]
        public void Add_InView_StoresTheLight()
        {
            var field = new LightField();
            field.Begin(View);

            field.Add(new Vector2(5f, 3f), 1.5f, 0.85f);

            Assert.That(field.Count, Is.EqualTo(1));
            Assert.That(field.Lights[0], Is.EqualTo(new Vector4(5f, 3f, 1.5f, 0.85f)));
        }

        [Test]
        public void Add_FarOutOfView_LeavesItOut()
        {
            var field = new LightField();
            field.Begin(View);

            field.Add(new Vector2(20f, 3f), 1.5f, 0.85f);

            Assert.That(field.Count, Is.EqualTo(0));
        }

        [Test]
        public void Add_JustOutOfViewButReachingIn_KeepsIt()
        {
            var field = new LightField();
            field.Begin(View);

            field.Add(new Vector2(11f, 3f), 1.5f, 0.85f);

            Assert.That(field.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_GlowOnly_KeepsItWhenTheGlowReachesTheView()
        {
            var field = new LightField();
            field.Begin(View);

            field.Add(new Vector2(11f, 3f), 0.1f, 0f, Color.red, 1.5f);

            Assert.That(field.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_WithGlow_ScalesItsColorByStrength()
        {
            var field = new LightField();
            field.Begin(View);

            field.Add(new Vector2(5f, 3f), 1.5f, 0.85f, new Color(1f, 0.5f, 0f, 0.2f), 1.2f);

            Assert.That(field.Glows[0].x, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(field.Glows[0].y, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(field.Glows[0].w, Is.EqualTo(1.2f).Within(0.001f));
        }

        [Test]
        public void Add_PastTheLimit_KeepsTheFirstOnes()
        {
            var field = new LightField();
            field.Begin(View);

            for (int index = 0; index < LightField.MostLights + 5; index++)
            {
                field.Add(new Vector2(index % 10, 3f), 1.5f, index == 0 ? 0.92f : 0.85f);
            }

            Assert.That(field.Count, Is.EqualTo(LightField.MostLights));
            Assert.That(field.Lights[0].w, Is.EqualTo(0.92f).Within(0.001f));
        }

        [Test]
        public void Begin_NewFrame_ForgetsTheOldLights()
        {
            var field = new LightField();
            field.Begin(View);
            field.Add(new Vector2(5f, 3f), 1.5f, 0.85f);

            field.Begin(View);

            Assert.That(field.Count, Is.EqualTo(0));
        }
    }
}
