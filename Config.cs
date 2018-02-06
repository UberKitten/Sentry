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
    class Config
    {
        public Guid Guid { get; set; }
        public List<Trigger> Triggers { get; set; }
        public List<ServiceConfig> Services { get; set; }

        public static Config GetExample()
        {
            return new Config
            {
                Guid = Guid.NewGuid(),
                Triggers = new List<Trigger>
                {
                    new Trigger
                    {
                        TriggerStrings = new List<string>
                        {
                            "pineapple",
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
                        Type = "twitter",
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
                        Type = "twitter",
                        Options = new Dictionary<string, string>
                        {
                            ["ConsumerKey"] = "example",
                            ["ConsumerSecret"] = "example",
                            ["Token"] = "example",
                            ["TokenSecret"] = "example",
                        }
                    }
                }
            };
        }
    }

    class Trigger
    {
        public List<string> TriggerStrings { get; set; }
        public List<string> Check { get; set; }
        public List<TriggerAction> Services { get; set; }
    }
    
    class TriggerAction
    {
        // Matches up with Action below
        public string Id { get; set; }
        
        public List<string> Actions { get; set; }
    }
    class ServiceConfig
    {
        // The two required parameters for each service

        // What needs to run, e.g. Twitter, Facebook, email, etc
        [JsonRequired]
        public string Type { get; set; }

        // Matches up with TriggerAction above
        // Allows e.g. multiple Twitter accounts with different credentials
        [JsonRequired]
        public string Id { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool CheckQuorum { get; set; }
        
        public Dictionary<string, string> Options { get; set; }
    }
}
