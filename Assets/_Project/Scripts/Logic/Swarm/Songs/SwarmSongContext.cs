using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Everything a swarm song may read or change when it is cast, and how strong the swarm's level makes it (plan section
    /// H): a song multiplies its radius, reach and height by <see cref="SizeScale"/> and its damage by
    /// <see cref="DamageScale"/>, and takes what grows by whole steps, such as the Hydra's heads, from <see cref="Level"/>,
    /// and keeps them for that cast. No song reads the live wasp count for its strength.
    /// </summary>
    public readonly struct SwarmSongContext
    {
        /// <summary>Makes a context with no aim chooser: aimed songs strike <paramref name="aimTarget"/> or <paramref name="aimPoint"/>.</summary>
        public SwarmSongContext(
            Vector3 corePosition,
            ISwarmTarget aimTarget,
            Vector3 aimPoint,
            SwarmBody swarm,
            SwarmFormMachine forms,
            FuryMeter fury,
            HiveSet hives,
            IReadOnlyList<ISwarmTarget> enemies,
            ISmokeClearer smoke)
            : this(corePosition, aimTarget, aimPoint, swarm, forms, fury, hives, enemies, smoke, null)
        {
        }

        /// <summary>
        /// Makes a context in which songs cost their wasps. When <paramref name="aim"/> is set, an aimed song asks it for its
        /// target by its own aim rule as it is cast, instead of taking <paramref name="aimTarget"/> and <paramref name="aimPoint"/>.
        /// </summary>
        public SwarmSongContext(
            Vector3 corePosition,
            ISwarmTarget aimTarget,
            Vector3 aimPoint,
            SwarmBody swarm,
            SwarmFormMachine forms,
            FuryMeter fury,
            HiveSet hives,
            IReadOnlyList<ISwarmTarget> enemies,
            ISmokeClearer smoke,
            ISongAim aim)
            : this(corePosition, aimTarget, aimPoint, swarm, forms, fury, hives, enemies, smoke, aim, false)
        {
        }

        /// <summary>
        /// Makes a context at the swarm's first level. When <paramref name="aim"/> is set, an aimed song asks it for its
        /// target by its own aim rule as it is cast. While <paramref name="areSongsFree"/> is true (a farm phase), a cast
        /// costs no wasps.
        /// </summary>
        public SwarmSongContext(
            Vector3 corePosition,
            ISwarmTarget aimTarget,
            Vector3 aimPoint,
            SwarmBody swarm,
            SwarmFormMachine forms,
            FuryMeter fury,
            HiveSet hives,
            IReadOnlyList<ISwarmTarget> enemies,
            ISmokeClearer smoke,
            ISongAim aim,
            bool areSongsFree)
            : this(corePosition, aimTarget, aimPoint, swarm, forms, fury, hives, enemies, smoke, aim, areSongsFree, 1f, 1f)
        {
        }

        /// <summary>
        /// Makes a context whose sizes are multiplied by <paramref name="sizeScale"/> and whose damage by
        /// <paramref name="damageScale"/>, at the first level for what grows by whole levels (three Hydra heads).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A scale is 0 or less, or not a number.</exception>
        public SwarmSongContext(
            Vector3 corePosition,
            ISwarmTarget aimTarget,
            Vector3 aimPoint,
            SwarmBody swarm,
            SwarmFormMachine forms,
            FuryMeter fury,
            HiveSet hives,
            IReadOnlyList<ISwarmTarget> enemies,
            ISmokeClearer smoke,
            ISongAim aim,
            bool areSongsFree,
            float sizeScale,
            float damageScale)
            : this(corePosition, aimTarget, aimPoint, swarm, forms, fury, hives, enemies, smoke, aim, areSongsFree, sizeScale, damageScale, 1)
        {
        }

        /// <summary>
        /// Makes a context at swarm level <paramref name="level"/>, whose sizes are multiplied by <paramref name="sizeScale"/>
        /// and whose damage by <paramref name="damageScale"/> (<see cref="SwarmLevelConfig"/>). Both scales must be above 0,
        /// and the level at least 1.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A scale is 0 or less, or not a number, or the level is below 1.</exception>
        public SwarmSongContext(
            Vector3 corePosition,
            ISwarmTarget aimTarget,
            Vector3 aimPoint,
            SwarmBody swarm,
            SwarmFormMachine forms,
            FuryMeter fury,
            HiveSet hives,
            IReadOnlyList<ISwarmTarget> enemies,
            ISmokeClearer smoke,
            ISongAim aim,
            bool areSongsFree,
            float sizeScale,
            float damageScale,
            int level)
        {
            if (!(sizeScale > 0f) || float.IsInfinity(sizeScale))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeScale), sizeScale, "A song's size scale must be above 0.");
            }

            if (!(damageScale > 0f) || float.IsInfinity(damageScale))
            {
                throw new ArgumentOutOfRangeException(nameof(damageScale), damageScale, "A song's damage scale must be above 0.");
            }

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "The swarm's level starts at 1.");
            }

            CorePosition = corePosition;
            AimTarget = aimTarget;
            AimPoint = aimPoint;
            Swarm = swarm;
            Forms = forms;
            Fury = fury;
            Hives = hives;
            Enemies = enemies;
            Smoke = smoke;
            Aim = aim;
            AreSongsFree = areSongsFree;
            SizeScale = sizeScale;
            DamageScale = damageScale;
            Level = level;
        }

        /// <summary>Where the core stands.</summary>
        public Vector3 CorePosition { get; }

        /// <summary>The beekeeper the player aims at, or null when there is none.</summary>
        public ISwarmTarget AimTarget { get; }

        /// <summary>
        /// Where an aimed song strikes when it has no living beekeeper in reach: the aim on the ground, or the ground ahead of
        /// the core when nothing is aimed at. Songs pull it in to their own range.
        /// </summary>
        public Vector3 AimPoint { get; }

        /// <summary>The swarm's wasps.</summary>
        public SwarmBody Swarm { get; }

        /// <summary>The swarm's shape.</summary>
        public SwarmFormMachine Forms { get; }

        /// <summary>The Fury meter.</summary>
        public FuryMeter Fury { get; }

        /// <summary>The hives.</summary>
        public HiveSet Hives { get; }

        /// <summary>Every beekeeper, alive or not.</summary>
        public IReadOnlyList<ISwarmTarget> Enemies { get; }

        /// <summary>Clears smoke clouds. May be null.</summary>
        public ISmokeClearer Smoke { get; }

        /// <summary>Chooses an aimed song's target by the song's aim rule as it is cast. May be null.</summary>
        public ISongAim Aim { get; }

        /// <summary>
        /// True in a farm phase (plan section L), when a cast costs no wasps unless the song is a tower
        /// (<see cref="SwarmSongEffect.CostsInFarmPhase"/>). A song's first cast in a match is free in either phase.
        /// </summary>
        public bool AreSongsFree { get; }

        /// <summary>What the swarm's level multiplies a song's radius, reach and height by. 1 at the first level.</summary>
        public float SizeScale { get; }

        /// <summary>What the swarm's level multiplies a song's damage by. 1 at the first level.</summary>
        public float DamageScale { get; }

        /// <summary>
        /// The swarm's level, from 1: one more for each nest collected. Only for what grows by whole steps, such as the
        /// Hydra's heads; sizes and damage grow by <see cref="SizeScale"/> and <see cref="DamageScale"/>.
        /// </summary>
        public int Level { get; }

        /// <summary>The same context, aimed at <paramref name="target"/> (or null) and <paramref name="point"/>.</summary>
        public SwarmSongContext AimedAt(ISwarmTarget target, Vector3 point)
        {
            return new SwarmSongContext(
                CorePosition, target, point, Swarm, Forms, Fury, Hives, Enemies, Smoke, Aim, AreSongsFree, SizeScale, DamageScale, Level);
        }
    }
}
