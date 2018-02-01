using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry
{
    class Options
    {
        [Option('c', Default = "config.json", HelpText = "Configuration file in JSON format to use.")]
        public string ConfigFile { get; set; }

        [Option('j', Default = null, HelpText = "Configuration string in JSON format to use.")]
        public string ConfigText { get; set; }
    }

    [Verb("run", HelpText = "Normal Sentry operation mode")]
    class RunOptions : Options
    {
        [Option(Default = false, HelpText = "Whether to loop through checks a single time before stopping.")]
        public bool LoopOnce { get; set; }

        [Option('t', Default = 600, HelpText = "How long, in seconds, to wait between each check.")]
        public int LoopDelay { get; set; }

        [Option(Default = true, HelpText = "Whether to check for other Sentries when attempting to run an action.")]
        public bool CheckQuorum { get; set; }

        [Option(Default = false, HelpText = "Whether to skip the Verify step after loading services.")]
        public bool SkipVerify { get; set; }

        [Option(Default = false, HelpText = "Skip triggers checks and proceeds straight to performing all actions.")]
        public bool JustFuckMyShitUpFam { get; set; }

        [Option(Default = false, HelpText = "Bypass confirmation prompt before proceeding in JFMSUF mode.")]
        public bool BypassJFMSUFConfirmation { get; set; }

        // Modify the below settings at your own risk!

        [Option(Default = 1.25, Hidden = true, HelpText = "Multiplied by LoopDelay to determine the minimum time to wait before assuming quorum success.")]
        public double QuorumCheckDelayMultiplier { get; set; }

        [Option(Default = 30, Hidden = true, HelpText = "Minimum number of seconds for the random delay when checking for quorum")]
        public int QuorumJitterLowerBound { get; set; }

        [Option(Default = 60, Hidden = true, HelpText = "Maximum number of seconds for the random delay when checking for quorum")]
        public int QuorumJitterUpperBound { get; set; }
    }

    [Verb("makeexample", HelpText = "Create example config.json file")]
    class ExampleConfigOptions : Options
    {
        [Option('o', Default = true, HelpText = "Whether to overwrite the existing config file.")]
        public bool Overwrite { get; set; }
    }
}
