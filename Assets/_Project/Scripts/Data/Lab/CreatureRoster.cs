using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The creature every run starts with, and the choices that bring the rest. Add a choice here to add
    /// two creatures and their songs, with no new code. Each creature's pictures are the files in Art/Animals
    /// with its name, standing and Walk1 to Walk4, and rebuilding the lab links them.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Creature Roster", fileName = "CreatureRoster")]
    public sealed class CreatureRoster : ScriptableObject
    {
        private const int H = 0;
        private const int J = 1;
        private const int K = 2;
        private const int L = 3;

        private static readonly int[] SecondCreature = { J, H, K };
        private static readonly int[] SecondSong = { K, H, L };
        private static readonly int[] ThirdCreature = { K, L, H, J };
        private static readonly int[] ThirdSong = { L, J, K };
        private static readonly int[] FourthCreature = { L, K, J, H, L };
        private static readonly int[] FourthSong = { J, L, H, K };
        private static readonly int[] FifthCreature = { H, L, K, J, H, L };
        private static readonly int[] FifthSong = { K, J, L, H, J };

        [SerializeField, Tooltip("The creature every run starts with, and its song. Its song may unlock later.")]
        private CreatureOffer _starter = new CreatureOffer(
            new CreatureType(
                "Mouse", 1, new[] { H, J }, 10f, 1f,
                new FighterStats(14f, 6.5f, 3f, 0.7f, 0.5f, 0f, new FighterTraits(packBonus: 0.3f)),
                new FighterLook(Color.white, 0.8f, FighterShape.Rat),
                "Swarm: stronger together",
                new SongStyle(200f, ToneShape.Triangle, 2f, 0.5f, 3, 0.5f)),
            new BattleSong(
                "War Song", "Creatures hit 50% harder for 8s", 2, new[] { J, K, L }, 30f, 20f,
                new SongBuff(seconds: 8f, damageMultiplier: 1.5f),
                new SongStyle(150f, ToneShape.Square, 1f, 0.5f, 2, 1f)));

        [SerializeField, Tooltip("The choices, by level. No combo may be the start of another the Piper could also know.")]
        private CreatureChoice[] _choices =
        {
            new CreatureChoice(
                2,
                "Keep your side alive",
                new CreatureOffer(
                    new CreatureType(
                        "Rat", 2, SecondCreature, 15f, 2f,
                        new FighterStats(24f, 6f, 0f, 1f, 4f, 0f, new FighterTraits(healing: 6f)),
                        new FighterLook(Color.white, 1.1f, FighterShape.Rat),
                        "Healer: mends allies and you",
                        new SongStyle(120f, ToneShape.Sine, 2f, 1f, 1, 0f)),
                    new BattleSong(
                        "Mending Song", "Heals all creatures and you at once", 2, SecondSong, 25f, 18f,
                        new SongBuff(healing: 30f),
                        new SongStyle(112f, ToneShape.Sine, 1f, 1f, 1, 0f))),
                new CreatureOffer(
                    new CreatureType(
                        "Beetle", 2, SecondCreature, 15f, 2f,
                        new FighterStats(70f, 4.5f, 2f, 1f, 0.6f, 0f, new FighterTraits(guardTiles: 4f, armour: 0.4f)),
                        new FighterLook(Color.white, 1f, FighterShape.Dot),
                        "Shield: tough, draws nearby attacks",
                        new SongStyle(96f, ToneShape.Triangle, 1f, 0.5f, 2, 0.6f)),
                    new BattleSong(
                        "Shell Song", "Creatures take 40% less damage for 8s", 2, SecondSong, 25f, 18f,
                        new SongBuff(seconds: 8f, shieldShare: 0.4f),
                        new SongStyle(104f, ToneShape.Triangle, 0.5f, 1f, 1, 0.5f)))),
            new CreatureChoice(
                3,
                "Reach the back line",
                new CreatureOffer(
                    new CreatureType(
                        "Cat", 3, ThirdCreature, 25f, 4f,
                        new FighterStats(36f, 8f, 11f, 0.6f, 0.6f, 0f, new FighterTraits(huntsBackline: true)),
                        new FighterLook(Color.white, 1.5f, FighterShape.Cat),
                        "Hunter: runs in, hits hard, dies fast",
                        new SongStyle(170f, ToneShape.Square, 1f, 0.5f, 2, 0.8f)),
                    new BattleSong(
                        "Hunt Song", "Creatures move and hit 40% faster for 8s", 3, ThirdSong, 30f, 20f,
                        new SongBuff(seconds: 8f, paceMultiplier: 1.4f),
                        new SongStyle(180f, ToneShape.Square, 2f, 0.5f, 2, 0.7f))),
                new CreatureOffer(
                    new CreatureType(
                        "Owl", 3, ThirdCreature, 25f, 4f,
                        new FighterStats(28f, 7f, 11f, 0.8f, 6f, 0f, new FighterTraits(huntsBackline: true)),
                        new FighterLook(Color.white, 1f, FighterShape.Dot),
                        "Sniper: hits the back line from afar",
                        new SongStyle(132f, ToneShape.Sine, 2f, 0.5f, 1, 0.3f)),
                    new BattleSong(
                        "Hush Song", "Enemy healers stop for 8s", 3, ThirdSong, 30f, 20f,
                        new SongBuff(seconds: 8f, stopsEnemyHealing: true),
                        new SongStyle(84f, ToneShape.Sine, 1f, 1f, 1, 0f)))),
            new CreatureChoice(
                4,
                "Handle a group",
                new CreatureOffer(
                    new CreatureType(
                        "Skunk", 4, FourthCreature, 35f, 7f,
                        new FighterStats(45f, 5f, 6f, 1.2f, 1.5f, 3.5f, new FighterTraits(slowShare: 0.5f, slowSeconds: 2.5f)),
                        new FighterLook(Color.white, 1.4f, FighterShape.Cat),
                        "Cloud: slows a whole group",
                        new SongStyle(100f, ToneShape.Sawtooth, 1f, 0.5f, 1, 0.5f)),
                    new BattleSong(
                        "Stink Song", "Every awake enemy slows by half for 5s", 4, FourthSong, 35f, 22f,
                        new SongBuff(enemySlowShare: 0.5f, enemySlowSeconds: 5f),
                        new SongStyle(108f, ToneShape.Sawtooth, 0.5f, 0.5f, 1, 0.4f))),
                new CreatureOffer(
                    new CreatureType(
                        "Hornet", 4, FourthCreature, 35f, 7f,
                        new FighterStats(26f, 6.5f, 12f, 1.1f, 1.5f, 3.5f),
                        new FighterLook(Color.white, 0.9f, FighterShape.Rat),
                        "Burst: heavy area hits, very frail",
                        new SongStyle(210f, ToneShape.Sawtooth, 2f, 0.5f, 1, 0.8f)),
                    new BattleSong(
                        "Sting Song", "Hits every awake enemy for 25 at once", 4, FourthSong, 35f, 22f,
                        new SongBuff(burstDamage: 25f),
                        new SongStyle(190f, ToneShape.Square, 1f, 0.5f, 1, 1f)))),
            new CreatureChoice(
                5,
                "Beat a heavy",
                new CreatureOffer(
                    new CreatureType(
                        "Bear", 5, FifthCreature, 50f, 12f,
                        new FighterStats(180f, 5.5f, 8f, 1.2f, 1f, 0f, new FighterTraits(guardTiles: 8f, armour: 0.5f)),
                        new FighterLook(Color.white, 2.2f, FighterShape.Cat),
                        "Guardian: draws every attack",
                        new SongStyle(90f, ToneShape.Sawtooth, 0.5f, 0.5f, 1, 1f)),
                    new BattleSong(
                        "Stand Song", "Creatures take no damage for 4s", 5, FifthSong, 40f, 25f,
                        new SongBuff(seconds: 4f, shieldShare: 1f),
                        new SongStyle(88f, ToneShape.Triangle, 0.5f, 0.5f, 1, 1f))),
                new CreatureOffer(
                    new CreatureType(
                        "Wolf", 5, FifthCreature, 50f, 12f,
                        new FighterStats(70f, 7f, 30f, 0.9f, 1f),
                        new FighterLook(Color.white, 1.7f, FighterShape.Cat),
                        "Slayer: huge damage to one target",
                        new SongStyle(140f, ToneShape.Sawtooth, 1f, 0.5f, 1, 1f)),
                    new BattleSong(
                        "Howl", "Double damage to enemies under half health for 8s", 5, FifthSong, 40f, 25f,
                        new SongBuff(seconds: 8f, finisherMultiplier: 2f),
                        new SongStyle(124f, ToneShape.Sawtooth, 0.5f, 1f, 1, 0.6f)))),
        };

        public CreatureOffer Starter => _starter;
        public IReadOnlyList<CreatureChoice> Choices => _choices;
    }
}
