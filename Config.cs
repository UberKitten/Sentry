using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq.Expressions;
using System.Linq;
using System.Text;

namespace Sentry
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
                        TriggerString = "pineapple",
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
        public string TriggerString { get; set; }
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
        public string Type { get; set; }

        // Matches up with TriggerAction above
        // Allows e.g. multiple Twitter accounts with different credentials
        public string Id { get; set; }
        
        public Dictionary<string, string> Options { get; set; }
    }
}
