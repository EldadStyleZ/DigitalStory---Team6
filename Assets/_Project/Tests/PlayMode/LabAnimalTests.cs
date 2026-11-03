using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class LabAnimalTests
    {
        private const string SceneName = "eldad-lab-scene";
        private const int AnimalCount = 9;
        private const float Tolerance = 0.01f;
        private const float South = -90f;

        private static readonly FighterStats Statue = new FighterStats(5000f, 0f, 0f, 1000f, 1f);

        private const int WalkFrameCount = 4;
        private const float GameStep = 0.05f;

        private LabRoot _root;
        private LabUnitsView _units;
        private CreatureRoster _roster;
        private TownArtConfig _sprites;
        private LabArtConfig _art;

        private LabSession Session => _root.Session;

        private IEnumerable<CreatureType> EveryCreature => _roster.Choices
            .SelectMany(choice => choice.Options)
            .Select(offer => offer.Creature)
            .Prepend(_roster.Starter.Creature);

        private IEnumerable<Sprite> EveryPicture => EveryCreature.SelectMany(creature => creature.Look.WalkFrames.Prepend(creature.Look.Sprite));

        private IEnumerable<SpriteRenderer> DrawnSprites => _units
            .GetComponentsInChildren<SpriteRenderer>()
            .Where(sprite => sprite.enabled);

        private IEnumerable<SpriteRenderer> Shadows => DrawnSprites.Where(sprite => sprite.sprite == _art.ShadowSprite);

        // The body is whichever of the fighter's own pictures is drawn.
        private SpriteRenderer BodyOf(Fighter fighter)
        {
            FighterLook look = fighter.Look;
            return DrawnSprites.Single(sprite => sprite.sprite == look.Sprite || look.WalkFrames.Contains(sprite.sprite));
        }

        // An open passage out of the entrance, so a walk has room.
        private Vector2 NextCellOverFromTheEntrance()
        {
            Maze maze = Session.Maze;

            for (int direction = 0; direction < Maze.DirectionCount; direction++)
            {
                int cell = maze.NeighbourThrough(maze.Entrance, direction);

                if (cell != Maze.NoCell)
                {
                    return maze.CenterOf(cell);
                }
            }

            throw new AssertionException("The entrance has no way out.");
        }

        private void PassGameTime(float seconds)
        {
            for (float passed = 0f; passed < seconds; passed += GameStep)
            {
                Session.Tick(Vector2.zero, GameStep);
            }
        }

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            _root = Object.FindAnyObjectByType<LabRoot>();
            _units = Object.FindAnyObjectByType<LabUnitsView>();
            _roster = Resources.FindObjectsOfTypeAll<CreatureRoster>()[0];
            _sprites = Resources.FindObjectsOfTypeAll<TownArtConfig>()[0];
            _art = Resources.FindObjectsOfTypeAll<LabArtConfig>()[0];
        }

        [Test]
        public void Roster_EveryCreature_HasThePictureNamedAfterIt()
        {
            Assert.That(EveryCreature.Count(), Is.EqualTo(AnimalCount));

            foreach (CreatureType creature in EveryCreature)
            {
                Assert.That(creature.Look.Sprite, Is.Not.Null, creature.Name);
                Assert.That(creature.Look.Sprite.name, Is.EqualTo(creature.Name));
            }
        }

        [Test]
        public void Roster_EveryCreature_KeepsThePicturesOwnColors()
        {
            foreach (CreatureType creature in EveryCreature)
            {
                Assert.That(creature.Look.Color, Is.EqualTo(Color.white), creature.Name);
            }
        }

        [Test]
        public void Roster_EveryCreature_HasItsWalkFramesInStepOrder()
        {
            foreach (CreatureType creature in EveryCreature)
            {
                string[] names = creature.Look.WalkFrames.Select(frame => frame != null ? frame.name : null).ToArray();
                string[] expected = Enumerable.Range(1, WalkFrameCount).Select(step => $"{creature.Name}Walk{step}").ToArray();
                Assert.That(names, Is.EqualTo(expected), creature.Name);
            }
        }

        [Test]
        public void Pictures_EveryOne_ShrinksSmoothlyToItsSmallSize()
        {
            foreach (Sprite picture in EveryPicture)
            {
                Assert.That(picture.texture.mipmapCount, Is.GreaterThan(1), picture.name);
            }
        }

        // Every pose has to be drawn to the same scale, or an animal would grow and shrink as it walks.
        [Test]
        public void Pictures_EveryPose_IsTheSameSizeAsItsStandingPicture()
        {
            foreach (CreatureType creature in EveryCreature)
            {
                foreach (Sprite frame in creature.Look.WalkFrames)
                {
                    Assert.That(frame.bounds.size, Is.EqualTo(creature.Look.Sprite.bounds.size), frame.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Units_EveryCreature_IsDrawnAsItsPictureAtItsSize()
        {
            foreach (CreatureType creature in EveryCreature)
            {
                Session.Battle.AddCreature(creature);
            }

            yield return null;

            foreach (CreatureType creature in EveryCreature)
            {
                SpriteRenderer body = DrawnSprites.Single(sprite => sprite.sprite == creature.Look.Sprite);
                float drawnLength = body.sprite.bounds.size.x * body.transform.localScale.x;
                float breath = creature.Look.Size * _art.BreathShare;
                Assert.That(drawnLength, Is.EqualTo(creature.Look.Size).Within(Tolerance + breath), creature.Name);
            }
        }

        [UnityTest]
        public IEnumerator Units_ACreature_PointsTheWayItWalks()
        {
            _root.Flow.TogglePause();
            Creature creature = Session.Battle.AddCreature(_roster.Starter.Creature);

            creature.WalkToward(creature.Position + Vector2.down, Session.Maze, 0f, 0.01f);
            yield return null;

            SpriteRenderer body = BodyOf(creature);
            Assert.That(Mathf.DeltaAngle(body.transform.eulerAngles.z, South), Is.EqualTo(0f).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Units_AWalkingCreature_StepsThroughItsFramesInOrder()
        {
            _root.Flow.TogglePause();
            Creature creature = Session.Battle.AddCreature(_roster.Starter.Creature);
            Vector2 goal = NextCellOverFromTheEntrance();
            float quarterStride = creature.Look.Size * _art.StrideBodyLengths / WalkFrameCount;
            var seen = new List<Sprite>();

            for (int step = 0; step < WalkFrameCount; step++)
            {
                creature.WalkToward(goal, Session.Maze, 0f, (quarterStride * (step == 0 ? 0.5f : 1f)) / creature.Stats.MoveSpeed);
                yield return null;
                seen.Add(BodyOf(creature).sprite);
            }

            Assert.That(seen, Is.EqualTo(creature.Look.WalkFrames));
        }

        [UnityTest]
        public IEnumerator Units_ACreatureThatStops_StandsAgain()
        {
            _root.Flow.TogglePause();
            Creature creature = Session.Battle.AddCreature(_roster.Starter.Creature);
            creature.WalkToward(NextCellOverFromTheEntrance(), Session.Maze, 0f, 0.05f);
            yield return null;
            Assert.That(BodyOf(creature).sprite, Is.Not.EqualTo(creature.Look.Sprite), "It has to walk first for this test to mean anything.");

            creature.PassTime(_art.StillSeconds + 0.01f);
            yield return null;

            Assert.That(BodyOf(creature).sprite, Is.EqualTo(creature.Look.Sprite));
        }

        [UnityTest]
        public IEnumerator Units_AStrikingCreature_LungesTheWayItFacesAndComesBack()
        {
            _root.Flow.TogglePause();
            Creature creature = Session.Battle.AddCreature(EveryCreature.First(type => !type.Stats.Traits.Heals));
            creature.TurnToward(creature.Position + Vector2.right);
            creature.MarkHit();

            creature.PassTime(_art.LungeSeconds / 2f);
            yield return null;
            float reach = BodyOf(creature).transform.position.x - creature.Position.x;
            Assert.That(reach, Is.EqualTo(creature.Look.Size * _art.LungeBodyLengths).Within(Tolerance));

            creature.PassTime(_art.LungeSeconds);
            yield return null;
            Assert.That(Vector2.Distance(BodyOf(creature).transform.position, creature.Position), Is.LessThan(Tolerance));
        }

        [UnityTest]
        public IEnumerator Units_AHealingCreature_SwellsInsteadOfLunging()
        {
            _root.Flow.TogglePause();
            Creature healer = Session.Battle.AddCreature(EveryCreature.First(type => type.Stats.Traits.Heals));
            yield return null;
            float standing = BodyOf(healer).transform.localScale.x;

            healer.MarkHit();
            healer.PassTime(_art.LungeSeconds / 2f);
            yield return null;

            SpriteRenderer body = BodyOf(healer);
            Assert.That(Vector2.Distance(body.transform.position, healer.Position), Is.LessThan(Tolerance));
            Assert.That(body.transform.localScale.x / standing, Is.EqualTo(1f + _art.HealPulseShare).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Units_AHurtCreature_FlashesThenFades()
        {
            _root.Flow.TogglePause();
            Creature creature = Session.Battle.AddCreature(_roster.Starter.Creature);

            creature.TakeHit(1f);
            yield return null;
            Assert.That(BodyOf(creature).color, Is.EqualTo(_art.HurtColor).Using(new ColorEqualityComparer(Tolerance)));

            creature.PassTime(_art.HurtSeconds);
            yield return null;
            Assert.That(BodyOf(creature).color, Is.EqualTo(creature.Look.Color).Using(new ColorEqualityComparer(Tolerance)));
        }

        // Four samples a quarter breath apart always spread at least 1.4 breaths wide, whatever its phase.
        [UnityTest]
        public IEnumerator Units_AStandingCreature_Breathes()
        {
            Creature creature = Session.Battle.AddCreature(_roster.Starter.Creature);
            float standing = creature.Look.Size / creature.Look.Sprite.bounds.size.x;
            var sizes = new List<float>();

            for (int sample = 0; sample < 4; sample++)
            {
                yield return null;
                sizes.Add(BodyOf(creature).transform.localScale.x);
                PassGameTime(_art.BreathSeconds / 4f);
            }

            Assert.That(sizes.Max() - sizes.Min(), Is.GreaterThan(standing * _art.BreathShare * 1.2f));
            Assert.That(sizes, Has.All.EqualTo(standing).Within(standing * _art.BreathShare * 1.01f));
        }

        // Pausing stops the battle's clock, so nothing breathes on the pause screen.
        [UnityTest]
        public IEnumerator Units_WhileThePauseIsOn_KeepStill()
        {
            Creature creature = Session.Battle.AddCreature(_roster.Starter.Creature);
            _root.Flow.TogglePause();
            yield return null;
            float before = BodyOf(creature).transform.localScale.x;

            yield return new WaitForSeconds(_art.BreathSeconds / 4f);

            Assert.That(BodyOf(creature).transform.localScale.x, Is.EqualTo(before));
        }

        // It faces north, so a shadow that wrongly turned with it would fall somewhere else.
        [UnityTest]
        public IEnumerator Units_EveryFighter_HasAShadowBeneathIt()
        {
            _root.Flow.TogglePause();
            Creature creature = Session.Battle.AddCreature(_roster.Starter.Creature);
            creature.TurnToward(creature.Position + Vector2.up);
            yield return null;

            Assert.That(_art.ShadowSprite, Is.Not.Null);
            Assert.That(Shadows.Count(), Is.EqualTo(_units.DrawnCount));

            SpriteRenderer shadow = Shadows.OrderBy(sprite => Vector2.Distance(sprite.transform.position, creature.Position)).First();
            Vector2 offset = (Vector2)shadow.transform.position - creature.Position;
            Assert.That(offset, Is.EqualTo(_art.ShadowOffset * creature.Look.Size).Using(new Vector2EqualityComparer(Tolerance)));
            Assert.That(shadow.sortingOrder, Is.LessThan(BodyOf(creature).sortingOrder));
            Assert.That(shadow.color, Is.EqualTo(_art.ShadowColor));
        }

        [UnityTest]
        public IEnumerator Units_AFighterWithNoPicture_KeepsItsShape()
        {
            var look = new FighterLook(Color.red, 1f, FighterShape.Cat);
            var type = new MinionType("Test", 12f, 1, 0f, 1f, Statue, look);
            Minion minion = Session.Battle.AddMinion(type, Session.Maze.CenterOf(Session.Maze.Entrance));

            yield return null;

            bool isDrawn = DrawnSprites.Any(sprite => sprite.sprite == _sprites.CatSprite
                && Vector2.Distance(sprite.transform.position, minion.Position) < Tolerance);
            Assert.That(isDrawn, Is.True);
        }
    }
}
