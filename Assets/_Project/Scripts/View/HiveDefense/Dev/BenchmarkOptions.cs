using System;
using System.IO;

namespace DigitalStory.View
{
    /// <summary>
    /// Development tool, not game code. The benchmark's command-line switches. Without "-f010Benchmark &lt;folder&gt;"
    /// there are none and the benchmark never starts.
    /// </summary>
    /// <remarks>
    /// -f010Benchmark &lt;folder&gt;   turns the benchmark on and names the folder its files go to.
    /// -f010BenchLabel &lt;name&gt;      names the run's files (default "run").
    /// -f010BenchWasps &lt;count&gt;     the swarm size held through the run (default 2000).
    /// -f010BenchNormal             keeps the game's own vsync and frame rate; otherwise vsync is off and nothing caps the rate.
    /// -f010BenchSubtract           runs the subtraction script (one thing switched off at a time) instead of the baseline.
    /// -f010BenchStations &lt;a,b&gt;   the song stations the subtraction script repeats per switch (default "stand,domesting").
    /// -f010BenchOverdraw           counts the glow quads per pixel at the end of every measured window with nothing switched off.
    /// -f010BenchOff &lt;switch&gt;      holds one switch through the whole baseline script, such as noshadows or noglow
    ///                              (the names are BenchmarkSwitch's, lower case: see HiveDefenseBenchmark).
    /// </remarks>
    internal sealed class BenchmarkOptions
    {
        private const string BenchmarkFlag = "-f010Benchmark";
        private const string LabelFlag = "-f010BenchLabel";
        private const string WaspsFlag = "-f010BenchWasps";
        private const string NormalFlag = "-f010BenchNormal";
        private const string SubtractFlag = "-f010BenchSubtract";
        private const string StationsFlag = "-f010BenchStations";
        private const string OverdrawFlag = "-f010BenchOverdraw";
        private const string OffFlag = "-f010BenchOff";
        private const string DefaultLabel = "run";
        private const string DefaultStations = "stand,domesting";
        private const int DefaultWasps = 2000;

        private BenchmarkOptions(string[] arguments, string folder)
        {
            OutputFolder = folder;
            Label = Value(arguments, LabelFlag) ?? DefaultLabel;
            string wasps = Value(arguments, WaspsFlag);
            SwarmSize = wasps != null ? int.Parse(wasps, System.Globalization.CultureInfo.InvariantCulture) : DefaultWasps;
            KeepsGameSettings = Has(arguments, NormalFlag);
            IsSubtraction = Has(arguments, SubtractFlag);
            ProbesOverdraw = Has(arguments, OverdrawFlag);
            Stations = (Value(arguments, StationsFlag) ?? DefaultStations).Split(',');
            HeldSwitchName = Value(arguments, OffFlag);
            CommandLine = string.Join(" ", arguments);
        }

        /// <summary>The folder every file of the run is written to.</summary>
        public string OutputFolder { get; }

        /// <summary>The start of every file name of the run.</summary>
        public string Label { get; }

        /// <summary>How many wasps the swarm holds through the run.</summary>
        public int SwarmSize { get; }

        /// <summary>True to keep the game's vsync and frame-rate settings, as a player gets them.</summary>
        public bool KeepsGameSettings { get; }

        /// <summary>True for the subtraction script, false for the baseline script.</summary>
        public bool IsSubtraction { get; }

        /// <summary>True to count glow quads per pixel after each measured window with nothing switched off.</summary>
        public bool ProbesOverdraw { get; }

        /// <summary>The stations the subtraction script measures under each switch.</summary>
        public string[] Stations { get; }

        /// <summary>The switch held off through the whole baseline script, or null for none.</summary>
        public string HeldSwitchName { get; }

        /// <summary>The whole command line, for the run's notes.</summary>
        public string CommandLine { get; }

        /// <summary>Reads the switches. False, and no options, when the benchmark flag is absent.</summary>
        public static bool TryRead(out BenchmarkOptions options)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string folder = Value(arguments, BenchmarkFlag);
            if (string.IsNullOrEmpty(folder))
            {
                options = null;
                return false;
            }

            Directory.CreateDirectory(folder);
            options = new BenchmarkOptions(arguments, folder);
            return true;
        }

        /// <summary>A path in the output folder for a file of this run.</summary>
        public string PathFor(string suffix) => Path.Combine(OutputFolder, Label + "-" + suffix);

        private static string Value(string[] arguments, string name)
        {
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == name)
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        private static bool Has(string[] arguments, string name) => Array.IndexOf(arguments, name) >= 0;
    }
}
