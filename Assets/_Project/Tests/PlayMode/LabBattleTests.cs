using System.Collections;
using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class LabBattleTests
    {
        private const string SceneName = "eldad-lab-scene";
        private const int H = 0;
        private const int J = 1;
        private const int K = 2;
        private const int L = 3;
        private const float GameStep = 0.05f;

        private static readonly FighterLook Plain = new FighterLook(Color.red, 1f, FighterShape.Rat);
        private static readonly FighterStats Statue = new FighterStats(5000f, 0f, 0f, 1000f, 1f);

        private LabRoot _root;
        private LabUnitsView _units;
        private LabEffectsView _effects;
        private LabHudView _hud;
        private LabScreensView _screens;
        private LabSoundView _sound;
        private ComboBalloonView _balloon;
        private LabArtConfig _art;
        private LabSoundConfig _soundConfig;
        private LabTextConfig _texts;
        private AmbushConfig _ambush;

        private LabSession Session => _root.Session;

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            _root = Object.FindAnyObjectByType<LabRoot>();
            _units = Object.FindAnyObjectByType<LabUnitsView>();
            _effects = Object.FindAnyObjectByType<LabEffectsView>();
            _hud = Object.FindAnyObjectByType<LabHudView>();
            _screens = Object.FindAnyObjectByType<LabScreensView>();
            _sound = Object.FindAnyObjectByType<LabSoundView>();
            _balloon = Object.FindAnyObjectByType<ComboBalloonView>();
            _art = Resources.FindObjectsOfTypeAll<LabArtConfig>()[0];
            _soundConfig = Resources.FindObjectsOfTypeAll<LabSoundConfig>()[0];
            _texts = Resources.FindObjectsOfTypeAll<LabTextConfig>()[0];
            _ambush = Resources.FindObjectsOfTypeAll<AmbushConfig>()[0];
        }

        private Minion StartBattle(FighterStats stats, float xp = 10f)
        {
            var type = new MinionType("Test", 12f, (int)xp, 0f, 1f, stats, Plain);
            int next = Session.Maze.Entrance;

            for (int direction = 0; direction < Maze.DirectionCount && next == Session.Maze.Entrance; direction++)
            {
                int cell = Session.Maze.NeighbourThrough(Session.Maze.Entrance, direction);
                next = cell != Maze.NoCell ? cell : next;
            }

            Minion minion = Session.Battle.AddMinion(type, Session.Maze.CenterOf(next));
            Session.Tick(Vector2.zero, 0.01f);
            Assert.That(Session.Battle.IsInBattle, Is.True, "The test needs a fight.");
            return minion;
        }

        // Hides a group right beside the Piper, so the watch sets it off straight away.
        private void SpringNextToThePiper()
        {
            var hidden = new MinionType("Hidden", 60f, 1, 0f, 1f, Statue, Plain);
            Session.Ambushes.Add(new Ambush(Session.Maze.Entrance, new[] { hidden, hidden, hidden }));
        }

        // Steps the game itself. Waiting real seconds would race it, because a frame slower than its
        // longest step moves the game less than the time that passed.
        private void PassGameTime(float seconds)
        {
            for (float passed = 0f; passed < seconds; passed += GameStep)
            {
                Session.Tick(Vector2.zero, GameStep);
            }
        }

        private float DistanceFromDone(Color color) => ((Vector4)(color - _art.BalloonDoneColor)).magnitude;

        // Picks in the same frame, so the game never gets as far as stopping for the choice.
        private void LevelUpAndChoose(int xp)
        {
            Session.Levels.Add(xp);

            while (Session.WaitingChoice != null)
            {
                Session.Choose(0);
            }
        }

        private void Press(string buttonName)
        {
            Button button = _screens.Card.Q<Button>(buttonName);
            Assert.That(button, Is.Not.Null, $"There is no {buttonName} on screen.");

            using (NavigationSubmitEvent press = NavigationSubmitEvent.GetPooled())
            {
                press.target = button;
                button.SendEvent(press);
            }
        }

        [Test]
        public void Scene_WhenLoaded_HasMinionsAndSeveralBosses()
        {
            Assert.That(Session.Battle.Minions.Count, Is.GreaterThan(100));
            Assert.That(Session.Battle.Minions.Count(minion => minion.IsBoss), Is.GreaterThanOrEqualTo(3));
            Assert.That(Session.Battle.Minions.Count(minion => minion.IsFinalBoss), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Scene_AtTheEntrance_IsCalm()
        {
            yield return new WaitForSeconds(0.3f);

            Assert.That(Session.Battle.IsInBattle, Is.False);
        }

        [UnityTest]
        public IEnumerator Units_EveryFighter_IsDrawn()
        {
            StartBattle(Statue);
            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(_units.DrawnCount, Is.EqualTo(Session.Battle.Minions.Count + Session.Battle.Creatures.Count));
            Assert.That(Session.Battle.Creatures.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Effects_WhenACreatureArrives_AreShown()
        {
            StartBattle(Statue);

            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(_effects.ActiveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Effects_WhenTheyFinish_LeaveTheScreen()
        {
            StartBattle(Statue);
            Session.Press(H);
            Session.Press(J);
            yield return new WaitForSeconds(1f);

            Assert.That(_effects.ActiveCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Hud_Bars_FollowTheRules()
        {
            Session.PiperHealth.Damage(Session.PiperHealth.Max / 2f);
            Session.Stamina.Lose(Session.Stamina.Max / 4f);
            yield return null;

            Assert.That(_hud.HealthBar.Share, Is.EqualTo(0.5f).Within(0.02f));
            Assert.That(_hud.StaminaBar.Share, Is.EqualTo(0.75f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator Hud_AtLevelOne_OffersOneCombo()
        {
            yield return null;

            Assert.That(_hud.ShownCombos, Is.EqualTo(1));
            Assert.That(_hud.ShownLevel, Does.EndWith("1"));
        }

        [UnityTest]
        public IEnumerator Hud_OnLevellingUp_OffersTheNewCombo()
        {
            LevelUpAndChoose(1000);
            yield return null;

            Assert.That(_hud.ShownCombos, Is.GreaterThan(1));
            Assert.That(_hud.ShownLevel, Does.EndWith(Session.Levels.Level.ToString()));
        }

        [UnityTest]
        public IEnumerator Hud_WhenAComboIsSummonedInPeace_NamesTheCreature()
        {
            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(Session.Battle.Creatures.Count, Is.EqualTo(1));
            Assert.That(_hud.ShownMessage, Is.EqualTo(Session.Battle.Creatures[0].Type.Name));
        }

        [UnityTest]
        public IEnumerator Hud_WhileTheCreaturesAreBoosted_ShowsIt()
        {
            Assert.That(_hud.IsShowingABoost, Is.False);

            Session.Battle.Play(new BattleSong("Test", "Test", 1, new[] { H }, 1f, 1f, new SongBuff(seconds: 5f, damageMultiplier: 2f)));
            yield return null;

            Assert.That(_hud.IsShowingABoost, Is.True);
        }

        [Test]
        public void Hud_WhenAnAmbushIsComing_Warns()
        {
            SpringNextToThePiper();
            PassGameTime(_ambush.DelaySeconds + 0.2f);

            Assert.That(_hud.ShownMessage, Is.EqualTo(_texts.AmbushMessage));
        }

        [Test]
        public void Effects_WhenAnAmbushIsComing_MarkWhere()
        {
            SpringNextToThePiper();
            PassGameTime(_ambush.DelaySeconds + 0.2f);

            Assert.That(_effects.ActiveCount, Is.GreaterThan(0));
            Assert.That(_sound.LastPlayed, Is.SameAs(_soundConfig.AmbushWarning));
        }

        [UnityTest]
        public IEnumerator Ambush_AfterItsWarning_DrawsItsEnemies()
        {
            int before = Session.Battle.Minions.Count;
            SpringNextToThePiper();
            PassGameTime(_ambush.DelaySeconds + _ambush.WarningSeconds + 0.3f);
            yield return null;

            Assert.That(Session.Battle.Minions.Count, Is.EqualTo(before + 3));
            Assert.That(_units.DrawnCount, Is.EqualTo(Session.Battle.Minions.Count + Session.Battle.Creatures.Count));
        }

        [UnityTest]
        public IEnumerator Balloon_WithNoCombo_IsHidden()
        {
            yield return null;

            Assert.That(_balloon.IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator Balloon_OnTheFirstNote_ListsTheCombosItCouldBecome()
        {
            LevelUpAndChoose(1000000);
            yield return null;

            Session.Press(H);
            yield return null;

            Assert.That(_balloon.IsShowing, Is.True);
            Assert.That(_balloon.ShownRows, Is.EqualTo(Session.Combos.Candidates.Count));
            Assert.That(_balloon.ShownRows, Is.EqualTo(2), "Mouse and Bear both start with H.");
            Assert.That(_balloon.ShownName(0), Is.EqualTo(Session.Combos.Candidates[0].Name));
            Assert.That(_balloon.ShownLetters(0), Is.EqualTo("HJ"));
        }

        [UnityTest]
        public IEnumerator Balloon_ThePlayedLetters_AreColored()
        {
            LevelUpAndChoose(1000000);
            Session.Press(H);
            Session.Press(L);
            Session.Press(K);
            yield return null;

            Assert.That(_balloon.ShownName(0), Is.EqualTo("Bear"));
            Assert.That(_balloon.LetterColor(0, 0), Is.Not.EqualTo(_art.BalloonWaitingColor));
            Assert.That(_balloon.LetterColor(0, 2), Is.Not.EqualTo(_art.BalloonWaitingColor));
            Assert.That(_balloon.LetterColor(0, 3), Is.EqualTo(_art.BalloonWaitingColor));
        }

        [UnityTest]
        public IEnumerator Balloon_MoreLettersPlayed_MovesTheColorTowardDone()
        {
            LevelUpAndChoose(1000000);
            Session.Press(H);
            Session.Press(L);
            yield return null;
            Color earlier = _balloon.LetterColor(0, 0);

            Session.Press(K);
            Session.Press(J);
            Session.Press(H);
            yield return null;
            Color later = _balloon.LetterColor(0, 0);

            Assert.That(DistanceFromDone(later), Is.LessThan(DistanceFromDone(earlier)));
        }

        [UnityTest]
        public IEnumerator Balloon_ANarrowingCombo_DropsTheOthers()
        {
            LevelUpAndChoose(1000000);
            Session.Press(H);
            yield return null;
            Assert.That(_balloon.ShownRows, Is.EqualTo(2));

            Session.Press(L);
            yield return null;

            Assert.That(_balloon.ShownRows, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Balloon_WhenTheComboFinishes_ShowsItDoneThenGoes()
        {
            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(_balloon.IsShowing, Is.True);
            Assert.That(DistanceFromDone(_balloon.LetterColor(0, 1)), Is.LessThan(0.01f));

            yield return new WaitForSeconds(_art.BalloonEndSeconds + 0.2f);

            Assert.That(_balloon.IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator Balloon_WhenTheComboFails_TurnsRedThenGoes()
        {
            Session.Press(H);
            Session.Press(L);
            yield return null;

            Assert.That(_balloon.IsShowing, Is.True);
            Assert.That(_balloon.ShownColor, Is.EqualTo(_art.BalloonFailedColor));

            yield return new WaitForSeconds(_art.BalloonEndSeconds + 0.2f);

            Assert.That(_balloon.IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator Balloon_AWrongFirstNote_StillShowsIt()
        {
            Session.Press(L);
            yield return null;

            Assert.That(_balloon.IsShowing, Is.True);
            Assert.That(_balloon.ShownLetters(0), Is.EqualTo("L"));
            Assert.That(_balloon.ShownColor, Is.EqualTo(_art.BalloonFailedColor));
        }

        [UnityTest]
        public IEnumerator Balloon_FloatsOverThePiper()
        {
            Session.Press(H);
            yield return null;
            yield return null;

            var screen = Object.FindAnyObjectByType<UIDocument>().rootVisualElement.worldBound;
            Assert.That(_balloon.ShownPosition.x, Is.EqualTo(screen.width / 2f).Within(screen.width / 10f));
            Assert.That(_balloon.ShownPosition.y, Is.LessThan(screen.height / 2f), "It sits above the Piper.");
        }

        [UnityTest]
        public IEnumerator Hud_WhenABossNotices_ShowsItsBar()
        {
            Assert.That(_hud.IsShowingABoss, Is.False);
            var type = new MinionType("Test Boss", 12f, 10, 0f, 1f, Statue, Plain);

            Session.Battle.AddMinion(type, Session.Piper.Position, MinionRank.Boss);
            yield return new WaitForSeconds(0.1f);

            Assert.That(_hud.IsShowingABoss, Is.True);
        }

        [UnityTest]
        public IEnumerator Sound_WhenACreatureArrives_PlaysItsSong()
        {
            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(_sound.LastSong, Is.SameAs(Session.Songs[0]));
            Assert.That(_sound.IsPlayingASong, Is.True);
        }

        [UnityTest]
        public IEnumerator Sound_ANewSong_ReplacesTheLast()
        {
            LevelUpAndChoose(1000000);
            Session.Press(H);
            Session.Press(J);
            yield return null;

            Session.Press(J);
            Session.Press(H);
            Session.Press(K);
            yield return null;

            Assert.That(_sound.LastSong.Name, Is.EqualTo("Rat"));
            Assert.That(_sound.IsPlayingASong, Is.True);
        }

        [UnityTest]
        public IEnumerator Sound_ASong_ReachesTheListener()
        {
            yield return ListenerMeter.Connect();

            Session.Press(H);
            Session.Press(J);
            yield return new WaitForSeconds(0.2f);

            float loudest = 0f;

            for (int frame = 0; frame < 30; frame++)
            {
                loudest = Mathf.Max(loudest, ListenerMeter.Peak());
                yield return null;
            }

            Assert.That(loudest, Is.GreaterThan(0.01f));
        }

        [UnityTest]
        public IEnumerator Sound_EverySong_StaysInsideFullVolume()
        {
            yield return null;

            foreach (IComboSong song in Session.AllSongs)
            {
                float[] samples = ToneBaker.Bake(ComboSongWriter.Write(song, _soundConfig.Band), 48000);
                float peak = samples.Max(sample => Mathf.Abs(sample));

                Assert.That(peak, Is.LessThanOrEqualTo(1f), song.Name);
                Assert.That(peak, Is.GreaterThan(0.1f), song.Name);
            }
        }

        [UnityTest]
        public IEnumerator Sound_WhenAComboIsRefused_PlaysTheRefusal()
        {
            Session.Press(H);
            Session.Press(J);
            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(_sound.LastPlayed, Is.SameAs(_soundConfig.Refused));
        }

        [UnityTest]
        public IEnumerator Sound_ManyHitsAtOnce_PlayOnlyOnce()
        {
            yield return null;
            int strikes = 0;
            Session.Battle.Struck += _ => strikes++;
            Session.Battle.AddMinion(new MinionType("Test", 12f, 1, 0f, 1f, Statue, Plain), Session.Piper.Position);
            var mouse = (CreatureType)Session.Songs[0];

            for (int creature = 0; creature < 5; creature++)
            {
                Session.Battle.AddCreature(mouse);
            }

            int soundsBefore = _sound.HitSoundsPlayed;
            Session.Tick(Vector2.zero, 0.01f);
            Session.Tick(Vector2.zero, 0.01f);

            Assert.That(strikes, Is.GreaterThanOrEqualTo(5));
            Assert.That(_sound.HitSoundsPlayed - soundsBefore, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Pause_WhileShown_StopsTheRun()
        {
            yield return null;
            _root.Flow.TogglePause();
            yield return null;
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.PausedTitle));

            float stamina = Session.Stamina.Current;
            Session.Stamina.Lose(10f);
            yield return null;
            yield return null;

            Assert.That(Session.Stamina.Current, Is.EqualTo(stamina - 10f).Within(0.001f), "Nothing recovers while paused.");
        }

        [UnityTest]
        public IEnumerator Dying_AsksForNewGameOrContinue()
        {
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            yield return null;
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Dead));
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.DeadTitle));
            Assert.That(_screens.Card.Q<Button>("continueButton"), Is.Not.Null);
            Assert.That(_screens.Card.Q<Button>("newGameButton"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ContinueButton_KeepsTheLevelAndGoesOn()
        {
            LevelUpAndChoose(200);
            int level = Session.Levels.Level;
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            yield return null;
            yield return null;

            Press("continueButton");
            yield return null;

            Assert.That(_root.Flow.IsPlaying, Is.True);
            Assert.That(Session.Levels.Level, Is.EqualTo(level));
            Assert.That(Session.PiperHealth.Current, Is.EqualTo(Session.PiperHealth.Max));
            Assert.That(_screens.ShownTitle, Is.Null);
        }

        [UnityTest]
        public IEnumerator NewGameButton_StartsAgainAtLevelOne()
        {
            LevelUpAndChoose(200);
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            yield return null;
            yield return null;

            Press("newGameButton");
            yield return null;
            yield return null;
            yield return null;

            _root = Object.FindAnyObjectByType<LabRoot>();
            Assert.That(_root.Flow.IsPlaying, Is.True);
            Assert.That(_root.Session.Levels.Level, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BeatingTheFinalBoss_ShowsTheEnd()
        {
            Minion boss = Session.Battle.Minions.Single(minion => minion.IsFinalBoss);
            boss.Health.Damage(boss.Health.Max);
            yield return null;
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Won));
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.WonTitle));
        }
    }
}
