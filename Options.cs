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
    }

    [Verb("run", HelpText = "Normal Sentry operation mode")]
    class RunOptions : Options
    {
        [Option(Default = false, HelpText = "Whether to loop through checks a single time before stopping.")]
        public bool LoopOnce { get; set; }

        [Option('t', Default = 600, HelpText = "How long, in seconds, to wait between each check.")]
        public int LoopDelay { get; set; }
    }

    [Verb("makeexample", HelpText = "Create example config.json file")]
    class ExampleConfigOptions : Options
    {
        [Option('o', Default = true, HelpText = "Whether to overwrite the existing config file.")]
        public bool Overwrite { get; set; }
    }
}
