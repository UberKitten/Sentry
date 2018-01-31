using Newtonsoft.Json;
using System;
using System.IO;

namespace Sentry
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = Config.GetExample();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText("config.json", json);
        }
    }
}
