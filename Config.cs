using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Sentry.Config
{
    /**
     * Config is loaded from either the config.json file by default or the ConfigFile/ConfigText command line options
     */ 
    class Config
    {
        public Config()
        {
            LoopDelay = 600;
            Cooldown = 864000;
            EnableMultiFactorRequests = true;

            Quorum = new QuorumConfig();
            Triggers = new List<Trigger>();
            Services = new List<ServiceConfig>();
        }

        /**
         * How long, in seconds, to wait between each check.
         */
        public int LoopDelay { get; set; }

        /**
         * How long in seconds to wait after a trigger activates before checking again.
         */
        public int Cooldown { get; set; }

        public bool EnableMultiFactorRequests { get; set; }

        public QuorumConfig Quorum { get; set; }

        public List<Trigger> Triggers { get; set; }
        public List<ServiceConfig> Services { get; set; }
        public List<NotifyServiceConfig> NotifyServices { get; set; }

        public static Config GetExample()
        {
            return new Config
            {
                Triggers = new List<Trigger>
                {
                    new Trigger
                    {
                        TriggerCriteria = new Services.TwitterApi.ServiceTriggerCriteria()
                        {
                            TweetContains = new List<string>()
                            {
                                "pen",
                                "pineapple",
                                "apple",
                                "pen"
                            },
                            RetweetsOver = 10000,
                            FavoritesOver = 100
                        },
                        Check = new List<string>
                        {
                            "publictwitter"
                        },
                        Services = new List<TriggerAction>
                        {
                            new TriggerAction
                            {
                                Id = "publictwitter",
                                Actions = new List<string>
                                {
                                    "lock"
                                }
                            },
                            new TriggerAction
                            {
                                Id = "sinfultwitter",
                                Actions = new List<string>
                                {
                                    "scorch",
                                    "delete"
                                }
                            }
                        }
                    }
                },
                Services = new List<ServiceConfig>
                { 
                    new ServiceConfig
                    {
                        Id = "publictwitter",
                        Type = "twitterapi",
                        Options = new Dictionary<string, string>
                        {
                            ["ConsumerKey"] = "example",
                            ["ConsumerSecret"] = "example",
                            ["Token"] = "example",
                            ["TokenSecret"] = "example",
                        }
                    },
                    new ServiceConfig
                    {
                        Id = "sinfultwitter",
                        Type = "twitterapi",
                        Options = new Dictionary<string, string>
                        {
                            ["ConsumerKey"] = "example",
                            ["ConsumerSecret"] = "example",
                            ["Token"] = "example",
                            ["TokenSecret"] = "example",
                        }
                    }
                },
                NotifyServices = new List<NotifyServiceConfig>
                {
                    new NotifyServiceConfig
                    {
                        Type = "pushover",
                        NotifyStartup = true,
                        NotifyMultiFactorRequests = true,
                        NotifyOnTrigger = true,
                        
                    }
                }
            };
        }
    }

    class QuorumConfig
    {
        public QuorumConfig()
        {
            Enabled = false;
            CheckDelayMultiplier = 1.25;
            QuorumJitterLowerBound = 30;
            QuorumJitterUpperBound = 60;
        }
        
        public bool Enabled { get; set; }

        /**
         * Multiplied by LoopDelay to determine the minimum time to wait before assuming quorum success.
         */
        public double CheckDelayMultiplier { get; set; }

        /**
         * Minimum and maximum number of seconds for the random delay when checking for quorum
         */
        public int QuorumJitterLowerBound { get; set; }
        public int QuorumJitterUpperBound { get; set; }

    }

    class Trigger
    {
        /**
         * This is based on the ServiceTriggerCriteria in the Service.
         */
        public object TriggerCriteria { get; set; }
        public List<string> Check { get; set; }
        public List<TriggerAction> Services { get; set; }
        public List<NotifyServiceConfig> NotifyServices { get; set; }
    }
    
    class TriggerAction
    {
        // Matches up with Action below
        public string Id { get; set; }
        
        public List<string> Actions { get; set; }
    }

    class ServiceConfig
    {
        public ServiceConfig()
        {
            CheckQuorum = true;
        }

        // The two required parameters for each service

        // What needs to run, e.g. Twitter, Facebook, email, etc
        [JsonRequired]
        public string Type { get; set; }

        // Matches up with TriggerAction above
        // Allows e.g. multiple Twitter accounts with different credentials
        [JsonRequired]
        public string Id { get; set; }
        
        public bool CheckQuorum { get; set; }

        /**
         * This is based on the ServiceOptions in the Service.
         */
        public object Options { get; set; }
    }

    class NotifyServiceConfig
    {
        public NotifyServiceConfig()
        {
            NotifyStartup = true;
            NotifyMultiFactorRequests = true;
            NotifyOnTrigger = true;
        }

        // What service to use, e.g. Pushover, Pushbullet, email, etc
        [JsonRequired]
        public string Type { get; set; }

        public bool NotifyStartup { get; set; }
        public bool NotifyMultiFactorRequests { get; set; }
        public bool NotifyOnTrigger { get; set; }

        /**
         * This is based on the NotifyServiceOptions in the NotifyService.
         */
        public object Options { get; set; }
    }
}
