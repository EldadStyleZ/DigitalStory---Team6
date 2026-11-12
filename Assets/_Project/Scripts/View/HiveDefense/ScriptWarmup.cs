using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DigitalStory.View
{
    /// <summary>
    /// Compiles the game's own methods ahead of their first call, a little each frame over the first moments of a match. The
    /// Mono player compiles a method the first time it runs, so without this the first cast of a song paid for compiling all
    /// the code the cast runs in one frame: the first Sting of a session took 15.5 ms against 8.3 ms, with the attack light,
    /// the cast sound and the cast flash each switched off in turn making no difference (the benchmark's first-cast runs).
    /// Methods that cannot be compiled ahead (abstract ones, open generics) are left for their first call, as before.
    /// </summary>
    public sealed class ScriptWarmup
    {
        private const BindingFlags Declared =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly List<RuntimeMethodHandle> _methods = new List<RuntimeMethodHandle>();
        private readonly Stopwatch _clock = new Stopwatch();
        private int _next;

        /// <summary>Finds every method of the assemblies that can be compiled ahead. Allocates, so call it once, at load.</summary>
        /// <exception cref="ArgumentNullException">The assemblies are missing.</exception>
        public ScriptWarmup(params Assembly[] assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            foreach (Assembly assembly in assemblies)
            {
                if (assembly == null)
                {
                    throw new ArgumentNullException(nameof(assemblies), "One of the assemblies to warm up is null.");
                }

                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.ContainsGenericParameters)
                    {
                        Collect(type);
                    }
                }
            }
        }

        /// <summary>How many methods the warm-up compiles in all.</summary>
        public int MethodCount => _methods.Count;

        /// <summary>How many it has compiled so far, or tried to.</summary>
        public int PreparedCount => _next;

        /// <summary>How many could not be compiled ahead; they compile on their first call instead.</summary>
        public int FailedCount { get; private set; }

        /// <summary>True once every method has been compiled.</summary>
        public bool IsDone => _next >= _methods.Count;

        /// <summary>
        /// Compiles methods until this frame's budget, in milliseconds, is spent, or none is left. Allocates nothing of its
        /// own; a method already compiled costs almost nothing, so a restart's warm-up ends at once.
        /// </summary>
        public void Step(float budgetMilliseconds)
        {
            if (IsDone)
            {
                return;
            }

            _clock.Restart();
            do
            {
                RuntimeMethodHandle method = _methods[_next];
                _next++;
                try
                {
                    RuntimeHelpers.PrepareMethod(method);
                }
                catch (Exception)
                {
                    // A method the runtime will not compile ahead (an unusual signature, a missing dependency) simply
                    // compiles on its first call, as every method did before; nothing of the game depends on it here.
                    FailedCount++;
                }
            }
            while (!IsDone && _clock.Elapsed.TotalMilliseconds < budgetMilliseconds);
        }

        private void Collect(Type type)
        {
            foreach (MethodInfo method in type.GetMethods(Declared))
            {
                if (!method.IsAbstract && !method.ContainsGenericParameters)
                {
                    _methods.Add(method.MethodHandle);
                }
            }

            foreach (ConstructorInfo constructor in type.GetConstructors(Declared))
            {
                if (!constructor.IsStatic)
                {
                    _methods.Add(constructor.MethodHandle);
                }
            }
        }
    }
}
