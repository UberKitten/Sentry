using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry
{
    /**
     * Options are passed on the command line.
     * 
     * In general: The Config file should be shared by all hosts.
     * Options should be limited to things that may change depending on the host, or things that cause unusual execution (for example, LoopOnce)
     */ 
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

        [Option(Default = false, HelpText = "Whether to skip the Verify step after loading services.")]
        public bool SkipVerify { get; set; }

        [Option(Default = false, HelpText = "Skip trigger checks and proceeds straight to performing all actions.")]
        public bool JustFuckMyShitUpFam { get; set; }

        [Option(Default = 5, Hidden = true, HelpText = "How long, in seconds, to wait when attempting to launch actions.")]
        public int JFMSUFLoopDelay { get; set; }
        
        [Option(Default = false, Hidden = true, HelpText = "Skip confirmation prompt before proceeding in JFMSUF mode.")]
        public bool SkipJFMSUFConfirmation { get; set; }
    }

    [Verb("makeexample", HelpText = "Create example config.json file")]
    class ExampleConfigOptions : Options
    {
        [Option('o', Default = true, HelpText = "Whether to overwrite the existing config file.")]
        public bool Overwrite { get; set; }
    }
}
