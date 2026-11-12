namespace DigitalStory.View
{
    /// <summary>Development tool, not game code. One thing the benchmark switches off to see what it costs.</summary>
    internal enum BenchmarkSwitch
    {
        /// <summary>Nothing is switched off: the reference.</summary>
        None = 0,

        /// <summary>The glow quads are drawn with no instances.</summary>
        NoGlow = 1,

        /// <summary>The swarm's two point lights (the river and the attack light) are off.</summary>
        NoSwarmLights = 2,

        /// <summary>The point lights of the two hives and of every nest tree are off.</summary>
        NoHiveNestLights = 3,

        /// <summary>No light casts shadows (the moonlight is the only one that does).</summary>
        NoShadows = 4,

        /// <summary>The wasp meshes are drawn with no instances.</summary>
        NoWaspDraw = 5,

        /// <summary>The compute shader moves only its first thread group (64 wasps) instead of all of them.</summary>
        NoCompute = 6,

        /// <summary>The ground plane is not drawn.</summary>
        NoGround = 7,

        /// <summary>The scenery (ruins, rocks and trees around the play area) is not drawn.</summary>
        NoScenery = 8,

        /// <summary>The beekeepers' Animators are off.</summary>
        NoAnimators = 9,

        /// <summary>The swarm holds half the wasps.</summary>
        HalfWasps = 10,

        /// <summary>The HUD's UIDocument and its view are off. Not restored, so it runs last.</summary>
        NoHud = 11,
    }
}
