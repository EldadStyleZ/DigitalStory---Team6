using System.Collections;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class MuteViewTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;

        private GameRoot _root;
        private MuteView _mute;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            PlayerPrefs.DeleteKey(MuteView.MutedKey);
            yield return LoadTheScene();
            _root.Flow.Begin();
            yield return null;
        }

        [TearDown]
        public void ForgetTheChoice()
        {
            PlayerPrefs.DeleteKey(MuteView.MutedKey);
            AudioListener.volume = 1f;
        }

        [UnityTest]
        public IEnumerator Mute_ByDefault_SoundIsOn()
        {
            yield return null;

            Assert.That(_mute.IsMuted, Is.False);
            Assert.That(AudioListener.volume, Is.GreaterThan(0f));
            Assert.That(_mute.Button.text, Is.EqualTo("Mute"));
        }

        [UnityTest]
        public IEnumerator Mute_PressingTheButton_SilencesEverything()
        {
            yield return null;

            PressTheButton();

            Assert.That(_mute.IsMuted, Is.True);
            Assert.That(AudioListener.volume, Is.EqualTo(0f));
            Assert.That(_mute.Button.text, Is.EqualTo("Unmute"));
        }

        [UnityTest]
        public IEnumerator Mute_PressingItAgain_BringsTheSoundBack()
        {
            yield return null;
            PressTheButton();

            PressTheButton();

            Assert.That(_mute.IsMuted, Is.False);
            Assert.That(AudioListener.volume, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator Mute_AfterARetry_IsRemembered()
        {
            yield return null;
            PressTheButton();

            yield return LoadTheScene();

            Assert.That(_mute.IsMuted, Is.True);
            Assert.That(AudioListener.volume, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator Mute_OnScreen_DoesNotCoverTheMinimap()
        {
            yield return null;
            yield return null;
            Rect button = _mute.Button.worldBound;
            Rect map = Object.FindAnyObjectByType<MinimapView>().Element.worldBound;

            Assert.That(button.width, Is.GreaterThan(0f));
            Assert.That(button.Overlaps(map), Is.False);
        }

        [UnityTest]
        public IEnumerator Mute_PressingIt_DoesNotWalkThePiper()
        {
            yield return null;
            yield return null;
            HudLayout hud = Object.FindAnyObjectByType<HudLayout>();
            float scale = _mute.Button.panel.scaledPixelsPerPoint;
            Vector2 middle = _mute.Button.worldBound.center * scale;

            Assert.That(hud.IsOverControl(new Vector2(middle.x, Screen.height - middle.y)), Is.True);
        }

        private IEnumerator LoadTheScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<GameRoot>();
            _mute = Object.FindAnyObjectByType<MuteView>();
        }

        private void PressTheButton()
        {
            using (NavigationSubmitEvent press = NavigationSubmitEvent.GetPooled())
            {
                press.target = _mute.Button;
                _mute.Button.SendEvent(press);
            }
        }
    }
}
