using CommandLine;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using Sentry.Config;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace Sentry
{
    class Program
    {
        private Logger logger = LogManager.GetLogger("Sentry");
        protected CancellationTokenSource cancellationToken = new CancellationTokenSource();
        protected Dictionary<string, BaseService> services = new Dictionary<string, BaseService>();
        protected Thread loopThread = null;

        static void Main(string[] args)
        {
            var program = new Program();

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            var options = Parser.Default.ParseArguments<RunOptions, ExampleConfigOptions>(args)
                .WithParsed<RunOptions>(opts => program.Run(opts))
                .WithParsed<ExampleConfigOptions>(opts => program.ExampleConfig(opts));

            // Wait here for loop to complete
            exitEvent.WaitOne();
        }

        public void ExampleConfig(ExampleConfigOptions options)
        {
            logger.Info("Generating example config file {0}", options.ConfigFile);

            var config = Config.Config.GetExample();

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

        protected Config.Config GetConfig(Options options)
        {
            if (!String.IsNullOrEmpty(options.ConfigText) && !String.IsNullOrEmpty(options.ConfigFile))
            {
                logger.Fatal("Only one config option must be specified");
                throw new ArgumentException();
            }

            string json = options.ConfigText;

            if (!String.IsNullOrEmpty(options.ConfigFile))
            {
                try
                {
                    json = File.ReadAllText(options.ConfigFile);
                }
                catch (FileNotFoundException ex)
                {
                    logger.Fatal("Config file {0} not found", ex.FileName);
                    logger.Fatal("Please run Sentry.exe makeconfig to generate an example {0}", options.ConfigFile);
                    throw ex;
                }
            }
            logger.Trace("Configuration read:");
            logger.Trace(json);

            return JsonConvert.DeserializeObject<Config.Config>(json);
        }

        public void Run(RunOptions options)
        {
            var config = GetConfig(options);
            logger.Info("Loaded {0} with {1} triggers and {2} services", options.ConfigFile, config.Triggers.Count, config.Services.Count);

            // Build list of Services
            // https://stackoverflow.com/a/6944605
            var serviceTypes = new List<Type>();
            foreach (Type type in Assembly.GetAssembly(typeof(BaseService)).GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseService))))
            {
                logger.Trace("Found service {0}", type.FullName);
                serviceTypes.Add(type);
            }
            logger.Debug("Loaded {0} service types", serviceTypes.Count);

            foreach (var serviceConfig in config.Services)
            {
                var serviceType = serviceTypes.SingleOrDefault(t => t.Name.Equals(serviceConfig.Type, StringComparison.InvariantCultureIgnoreCase));

                if (serviceType == null)
                {
                    logger.Error("Unable to find service {0}, skipping {1}", serviceConfig.Type, serviceConfig.Id);
                }
                else
                {
                    try
                    {
                        logger.Debug("Adding service {0} with Id {1}", serviceType.FullName, serviceConfig.Id);
                        var service = (BaseService)Activator.CreateInstance(serviceType, serviceConfig);
                        services.Add(serviceConfig.Id.ToLowerInvariant(), service);
                        logger.Info("Loaded service {0}", serviceConfig.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error while loading service {0}, skipping", serviceConfig.Id);
                    }
                }
            }

            if (options.SkipVerify)
            {
                logger.Info("Skipping verify step");
            }
            else
            {
                foreach (var service in services)
                {
                    logger.Debug("Verifying {0}", service.Key);
                    try
                    {
                        service.Value.Verify();
                        logger.Info("Verified {0}", service.Key);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error verifying {0}, removing", service.Key);
                        services.Remove(service.Key);
                    }
                }
            }

            // Start loop
            Loop(options);
        }

        protected void Loop(RunOptions options)
        {
            logger.Info("Starting main loop");
            while (!cancellationToken.IsCancellationRequested)
            {
                // Start counting how long it's taken to run checks
                // So we can check each service roughly every 10 minutes instead of every 10 minutes + time spent checking
                var timeElapsed = new Stopwatch();
                timeElapsed.Start();



                timeElapsed.Stop();
                logger.Debug("Time taken in loop: {0} ms", timeElapsed.ElapsedMilliseconds);
            }
        }
    }
}
