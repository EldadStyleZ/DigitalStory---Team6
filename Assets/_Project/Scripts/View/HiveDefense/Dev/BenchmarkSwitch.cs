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

        /// <summary>The attack light (the second swarm light, which follows a sting or a meteor) is off. A first-cast switch.</summary>
        NoAttackLight = 12,

        /// <summary>A cast's squad flash never reaches the swarm's shader. A first-cast switch.</summary>
        NoCastFlash = 13,

        /// <summary>A cast plays no sound. A first-cast switch.</summary>
        NoCastSound = 14,

        /// <summary>A cast does not kick the camera. A first-cast switch.</summary>
        NoCameraKick = 15,

        /// <summary>No hit raises a damage number. A first-cast switch.</summary>
        NoDamageNumbers = 16,

        /// <summary>The giant's chase view does not lock the mouse cursor (plan section Z: is the lock the long frame at the giant's start and end?).</summary>
        NoCursorLock = 17,

        /// <summary>The painted and covering grass (the Grass Field) is not drawn. Plan section AD.</summary>
        NoGrass = 18,

        /// <summary>The Crest water ("Water (Crest)") is switched off: its surface, its simulations and its reflection camera.</summary>
        NoWater = 19,

        /// <summary>Only the Crest water's planar reflection camera is off; the surface still draws.</summary>
        NoWaterReflections = 20,

        /// <summary>The set dressing (the edge forest, the treeline and the reeds) is not drawn.</summary>
        NoForest = 21,

        /// <summary>The terrain's heightmap and its trees and details are not drawn.</summary>
        NoTerrain = 22,

        /// <summary>The ground mist's sheets are not drawn.</summary>
        NoMist = 23,

        /// <summary>The HUD's minimap is hidden, so it is never painted.</summary>
        NoMinimap = 24,

        /// <summary>The smoke pool's renderers (the beekeepers' smoke) are not drawn.</summary>
        NoSmoke = 25,

        /// <summary>The ambience's synthesised and recorded sources are off, so their audio callbacks stop.</summary>
        NoAmbience = 26,

        /// <summary>The game renders at half the width and height (a quarter of the pixels), to show what resolution costs.</summary>
        HalfRes = 27,

        /// <summary>The water keeps 6 detail rings (LOD slices) instead of the scene's 9.</summary>
        WaterSlices6 = 28,

        /// <summary>The water's simulation textures are 256 texels wide instead of the scene's 384.</summary>
        WaterRes256 = 29,

        /// <summary>The water stops simulating the shadows that fall on it.</summary>
        WaterNoShadowSim = 30,

        /// <summary>The water stops simulating foam.</summary>
        WaterNoFoamSim = 31,

        /// <summary>The water's underwater effect and its meniscus are off (the camera never goes under).</summary>
        WaterNoUnderwater = 32,

        /// <summary>The water's reflection camera renders every second frame instead of every frame.</summary>
        WaterReflectHalf = 33,

        /// <summary>The game camera renders without HDR.</summary>
        NoHdr = 34,

        /// <summary>No multisample anti-aliasing (the quality level asks for 2x).</summary>
        NoMsaa = 35,

        /// <summary>The far grass thins out: from 35 m to 90 m the field keeps 35% of its clumps.</summary>
        GrassThin = 36,

        /// <summary>Shadows reach 30 m instead of 45 m, at medium instead of high resolution.</summary>
        ShadowLite = 37,

        /// <summary>The window's backbuffer is 75% of the display's width and height; the system scales it up, HUD and all.</summary>
        Scale75 = 38,

        /// <summary>The game camera renders at 75% through dynamic resolution; the HUD stays at the display's resolution.</summary>
        DynRes75 = 39,

        /// <summary>The water stops rendering its surface's depth a second time to merge it into the camera's depth texture.</summary>
        WaterNoDepthCopy = 40,

        /// <summary>The water stops copying the camera's colour for its own use (the opaque texture written before transparents).</summary>
        WaterNoColorCopy = 41,
    }
}
