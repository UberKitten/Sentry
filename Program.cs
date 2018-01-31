using CommandLine;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;

namespace Sentry
{
    class Program
    {
        private static Logger logger = LogManager.GetLogger("Sentry");

        static void Main(string[] args)
        {

            var options = Parser.Default.ParseArguments<RunOptions, ExampleConfigOptions>(args)
                .WithParsed<RunOptions>(opts => Run(opts))
                .WithParsed<ExampleConfigOptions>(opts => ExampleConfig(opts));
            
        }

        private static void ExampleConfig(ExampleConfigOptions options)
        {
            logger.Info("Generating example config file {0}", options.ConfigFile);

            var config = Config.GetExample();

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            logger.Trace("Example JSON:");
            logger.Trace(json);

            if (options.Overwrite || !File.Exists(options.ConfigFile))
            {
                try
                {
                    File.WriteAllText(options.ConfigFile, json);
                    logger.Info("Example config file written");
                }
                catch (IOException ex)
                {
                    logger.Error(ex, "Error writing example config file");
                }
            }
            else
            {
                logger.Error("Not overwriting {0}", options.ConfigFile);
            }
        }

        private static void Run(RunOptions options)
        {

        }
    }
}
