using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>A small town with chosen rats and cats, for casting a song in a test.</summary>
    internal sealed class SongWorld
    {
        public static readonly Vector2 Piper = new Vector2(800f, 600f);

        public SongWorld(Rat[] rats, params Cat[] cats)
        {
            Random = new FixedRandomSource();
            Swarm = new RatSwarm(rats, TestRules.Default, TestRules.Bank(), TestRules.NoHouses(), Random);
            var town = new TownLayout(6, 7, Array.Empty<House>(), Array.Empty<Junction>(), Array.Empty<Vector2>());
            Cats = new CatPack(cats, TestRules.Default, town, TestRules.Bank(), TestRules.NoHouses(), Random);
        }

        public RatSwarm Swarm { get; }
        public CatPack Cats { get; }
        public IRandomSource Random { get; }

        public static Rat RatAt(Vector2 position) => new Rat(position, position, 0f, 0f);

        public static Rat RatBeside(float distance) => RatAt(Piper + (Vector2.left * distance));

        public static Cat CatBeside(float distance) => new Cat(new Junction(Piper + (Vector2.left * distance), 0, 0), 0f);

        public void Cast<T>() where T : SongEffect => Cast(ScriptableObject.CreateInstance<T>());

        public void Cast(SongEffect song) => song.Cast(new SongContext(Piper, Swarm, Cats, Random));

        public void Run(float seconds, float step = 1f / 60f)
        {
            var influence = new SwarmInfluence(Piper, 0f, FluteMode.None, 0f, Cats);

            for (float time = 0f; time < seconds; time += step)
            {
                Swarm.Tick(in influence, step);
            }
        }
    }
}
