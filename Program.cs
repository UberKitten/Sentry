using CommandLine;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using Sentry.Config;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
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

        protected Dictionary<string, Thread> actionThreads = new Dictionary<string, Thread>();
        protected Dictionary<int, DateTime> lastActions = new Dictionary<int, DateTime>();
        protected Config.Config config = null;

        protected static ManualResetEvent exitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var program = new Program();
            
            Console.CancelKeyPress += (sender, eventArgs) => {
                program.cancellationToken.Cancel(); // let the loop exit (if running)
                eventArgs.Cancel = true;
            };
            var parser = new Parser(with => {
                with.CaseSensitive = false;
                with.HelpWriter = Console.Out;
            });
            var options = parser.ParseArguments<RunOptions, ExampleConfigOptions>(args)
                .WithParsed<RunOptions>(opts => program.Run(opts))
                .WithParsed<ExampleConfigOptions>(opts => program.ExampleConfig(opts))
                .WithNotParsed(t => exitEvent.Set()); // So we don't just hang

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

            exitEvent.Set();
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
            config = GetConfig(options);
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
                var servicesToRemove = new List<string>();
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
                        servicesToRemove.Add(service.Key);
                    }
                }
                foreach (var serviceToRemove in servicesToRemove)
                {
                    services.Remove(serviceToRemove);
                }
            }

            if (options.JustFuckMyShitUpFam)
            {
                if (options.SkipJFMSUFConfirmation)
                {
                    logger.Info("Skipping JustFuckMyShitUpFam mode confirmation");
                }
                else
                {
                    logger.Warn("JustFuckMyShitUpFam mode requested");
                    Console.WriteLine("**********************************************************");
                    Console.WriteLine(" WARNING: JustFuckMyShitUpFam mode will run ALL triggers");
                    Console.WriteLine("    As if ALL trigger phrases had been detected");
                    Console.WriteLine("      The following actions will be performed:");
                    Console.WriteLine("");

                    var triggerConfigs = config.Triggers.SelectMany(t => t.Services).ToList();
                    foreach (var trigger in triggerConfigs)
                    {
                        var actionsAggregated = trigger.Actions.Aggregate((sum, addition) => sum + ", " + addition);
                        Console.WriteLine("  Service: " + trigger.Id + " Actions: " + actionsAggregated);
                    }

                    Console.WriteLine("");
                    Console.WriteLine("       These actions may not be reversible");
                    Console.WriteLine("  There is no additional confirmation or delay");
                    Console.WriteLine("        Are you SURE you want to do this?");
                    Console.WriteLine("**********************************************************");
                    Console.Write("Enter JFMSUF to confirm: ");

                    var userInput = Console.ReadLine();
                    if (userInput != "JFMSUF")
                    {
                        logger.Info("JustFuckMyShitUpFam mode canceled");
                        Environment.Exit(1);
                        return;
                    }
                }
                JFMSUF(options);
            }
            else
            {
                // Start main checking loop
                Loop(options);
            }

            // If cancellation is requested via cancellationToken, this exits the main thread
            exitEvent.Set();
        }

        protected void JFMSUF(RunOptions options)
        {
            // This is easier to implement here rather than shoe-horning it into the main loop
            logger.Info("Starting JustFuckMyShitUpFam mode loop");

            if (!options.JustFuckMyShitUpFam)
            {
                // Just in case...
                throw new InvalidOperationException();
            }

            // Flatten a list of all actions so we can work through them
            var triggerConfigs = config.Triggers.SelectMany(t => t.Services).ToList();

            while (!cancellationToken.IsCancellationRequested)
            {
                var triggerConfigsToRemove = new List<TriggerAction>();

                foreach (var action in triggerConfigs)
                {
                    var actionsAggregated = action.Actions.Aggregate((sum, addition) => sum + ", " + addition);
                    logger.Debug("Attempting to launch {0}, actions {1}", action.Id, actionsAggregated);
                    
                    // We want to make sure we don't run multiple actions on the same service at the same time
                    actionThreads.TryGetValue(action.Id, out Thread currentThread);

                    if (currentThread != null)
                    {
                        logger.Trace("Pulled thread id {0} IsAlive {1}", currentThread.ManagedThreadId, currentThread.IsAlive);

                        if (currentThread.IsAlive)
                        {
                            // Remove dead thread
                            logger.Debug("Thread for {0} is finished, removing", action.Id);
                            actionThreads.Remove(action.Id);
                        }
                    }

                    if (currentThread == null || !currentThread.IsAlive)
                    {
                        logger.Info("Starting action for {0}, actions {1}", action.Id, actionsAggregated);

                        if (services.ContainsKey(action.Id))
                        {
                            var serviceToAct = services[action.Id.ToLowerInvariant()];
                            logger.Trace("Pulled service {0} to act", serviceToAct.GetType());

                            var actionThread = new Thread(() => RunServiceAction(serviceToAct, action.Actions));
                            actionThreads.Add(action.Id, actionThread);
                            actionThread.Start();

                            // Remove from list of triggerConfigs to work on so we don't keep trying to start it
                            // We use triggerConfigsToRemove because we're currently enumerating
                            triggerConfigsToRemove.Add(action);

                            TraceActionThreads();
                        }
                        else
                        {
                            logger.Error("Unable to find service {0}, did it fail verification?", action.Id);
                        }
                    }
                    else
                    {
                        logger.Debug("Actions for {0} still running, skipping", action.Id);
                    }
                }
                triggerConfigs.RemoveAll(t => triggerConfigsToRemove.Contains(t));

                TraceActionThreads();
                var threadsAlive = 0;
                foreach (var thread in actionThreads)
                {
                    if (thread.Value.IsAlive)
                    {
                        threadsAlive++;
                    }
                }
                logger.Trace("{0} action threads still alive", threadsAlive);

                if (triggerConfigs.Count <= 0 && threadsAlive <= 0)
                {
                    logger.Info("JustFuckMyShitUpFam mode finished, have a nice day!");
                    cancellationToken.Cancel(); // Main thread will exit and trigger exitEvent
                }
                else
                {
                    Thread.Sleep(options.JFMSUFLoopDelay * 1000);
                }
            }
        }

        protected void Loop(RunOptions options)
        {
            logger.Info("Starting main loop");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Start counting how long it's taken to run checks
                    // So we can check each service roughly every 10 minutes instead of every 10 minutes + time spent checking
                    var timeElapsed = new Stopwatch();
                    timeElapsed.Start();

                    var i = 0;
                    foreach (var trigger in config.Triggers)
                    {
                        // We don't assign an ID to Triggers, because it's not really necessary so the index in config.Triggers is the de facto ID
                        logger.Debug("Starting check for trigger index {0}", i);
                        
                        // If it's not in lastActions, no action has ever been run
                        if (lastActions.TryGetValue(i, out DateTime lastAction))
                        {
                            var secondsSince = (DateTime.UtcNow - lastAction).Seconds;
                            if (secondsSince < options.Cooldown)
                            {
                                logger.Debug("Skipping check for trigger index {0} as it has only been {1} seconds since last trigger", i, secondsSince);
                                continue;
                            }
                        }

                        foreach (var checkId in trigger.Check)
                        {
                            logger.Debug("Checking {0}", checkId);

                            // Pull relevant service to check
                            if (!services.ContainsKey(checkId.ToLowerInvariant()))
                            {
                                logger.Error("Missing service {0}, did it fail verification?", checkId);
                                continue;
                            }

                            var serviceToCheck = services[checkId.ToLowerInvariant()];
                            logger.Trace("Pulled service to check {0}", serviceToCheck.GetType());

                            // Avoid calling Check if a service is currently doing an Action in another thread
                            // Simplifies service code to not require thread safety, does reduce concurrency though
                            // I.e. a service can not be used for checking and acting at the same time (even different triggers)
                            // The check will wait until the service is done acting
                            if (actionThreads.ContainsKey(checkId.ToLowerInvariant()))
                            {
                                logger.Info("Skipping check for {0} because actions are currently running", checkId);
                                continue;
                            }

                            try
                            {
                                if (serviceToCheck.Check(trigger.TriggerCriteria))
                                {
                                    logger.Info("Trigger detected for service {0}", checkId);
                                    lastActions.Add(i, DateTime.UtcNow);

                                    // Loop through actions and start
                                    foreach (var action in trigger.Services)
                                    {
                                        var actionsAggregated = action.Actions.Aggregate((sum, addition) => sum + ", " + addition);
                                        logger.Info("Running actions {0} on service {1}", actionsAggregated, action.Id);

                                        var serviceToAct = services[action.Id.ToLowerInvariant()];
                                        logger.Trace("Pulled service {0} to act", serviceToAct.GetType());

                                        var actionThread = new Thread(() => RunServiceAction(serviceToAct, action.Actions));
                                        actionThreads.Add(action.Id, actionThread);
                                        actionThread.Start();

                                        TraceActionThreads();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, "Error while checking {0}", checkId);
                            }
                        }

                        i++;
                    }

                    TraceActionThreads();
                    if (actionThreads.Count > 0)
                    {
                        // Prevent memory leaks by unused threads still being referenced

                        logger.Debug("Attempting to clean up ActionThreads");
                        var actionThreadsToRemove = new List<string>();
                        
                        foreach (var actionThread in actionThreads)
                        {
                            if (!actionThread.Value.IsAlive)
                            {
                                logger.Trace("Removing ActionThread {0}", actionThread.Key);
                                actionThreadsToRemove.Add(actionThread.Key);
                            }
                        }
                        actionThreadsToRemove.ForEach(key => actionThreads.Remove(key));
                    }

                    timeElapsed.Stop();
                    logger.Debug("Time taken in loop: {0} ms", timeElapsed.ElapsedMilliseconds);

                    // Time left to wait in milliseconds (can be negative)
                    var timeLeft = options.LoopDelay * 1000 - timeElapsed.ElapsedMilliseconds;
                    logger.Trace("Time left to wait: {0} ms", timeLeft);

                    if (timeLeft > 0)
                    {
                        logger.Trace("Sleeping");
                        Thread.Sleep(Convert.ToInt32(timeLeft));
                    }
                }
                catch (Exception ex)
                {
                    // This should only trigger on a problem with Sentry's code itself
                    logger.Error(ex, "Error during main loop");

                    // Ideally we would exit or do some kind of action or cleanup here,
                    // but the show (and actions + checks) must go on
                }
            }
        }

        protected void RunServiceAction(BaseService service, List<string> actions)
        {
            // TODO: Quorum + clustering
            // TODO: Keep trying during exception
            try
            {
                service.Action(actions);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while running action");
            }
        }

        /**
         * Print trace information on actionThreads
         * Ideally should be run at the end of loops and after adding/removing to actionThreads
         */ 
        private void TraceActionThreads()
        {
            if (actionThreads.Count > 0)
            {
                logger.Trace("TraceActionThreads:");
                foreach (var thread in actionThreads)
                {
                    if (thread.Value == null)
                    {
                        // Shouldn't happen
                        logger.Warn("Null thread for {0} in actionThreads", thread.Key);
                    }
                    else
                    {
                        logger.Trace("ActionThread {0} Id {1} IsAlive {2} ThreadState {3}", thread.Key, thread.Value.ManagedThreadId, thread.Value.IsAlive, thread.Value.ThreadState);
                    }
                }
            }
        }
    }
}
