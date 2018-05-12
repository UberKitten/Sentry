using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry.NotifyServices
{
    class Pushover : BaseNotifyService
    {
        public class NotifyServiceOptions
        {
            public string Token { get; set; }
            public string User { get; set; }
            public string Device { get; set; }
            public string Priority { get; set; }
            public string Sound { get; set; }
        };

        protected NotifyServiceOptions Options { get; set; }

        protected RestClient client = new RestClient("https://api.pushover.net/");

        public Pushover(SecretsStoreManager secretsStoreManager, object NotifyServiceOptions) : base(secretsStoreManager)
        {
            Options = (NotifyServiceOptions)NotifyServiceOptions;
        }

        protected void Notify(string message, string url)
        {
            var request = new RestRequest("/1/messages.json", Method.POST);
            request.AddParameter("token", secretsStoreManager.TryGetSecret(Options.Token));
            request.AddParameter("user", secretsStoreManager.TryGetSecret(Options.User));
            request.AddParameter("message", message);
            request.AddParameter("title", "Sentry");

            if (!String.IsNullOrEmpty(url))
            {
                request.AddParameter("url", message);
            }
            if (!String.IsNullOrEmpty(Options.Device))
            {
                request.AddParameter("device", Options.Device);
            }
            if (!String.IsNullOrEmpty(Options.Priority))
            {
                request.AddParameter("priority", Options.Priority);
            }
            if (!String.IsNullOrEmpty(Options.Sound))
            {
                request.AddParameter("sound", Options.Sound);
            }

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("Notify response: {0}", response.Content);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.Error("Unexpected response to Pushover notification: {0} - {1}", response.ResponseStatus, response.Content);
            }
        }

        public override void NotifyStartup()
        {
            Notify("Sentry has started", null);
        }

        public override void NotifyMultiFactorRequest(string id, string url)
        {
            Notify(String.Format("Multi factor needed for {0}", id), url);
        }

        public override void NotifyOnTrigger(string id)
        {
            Notify(String.Format("Service {0} has been triggered", id), null);
        }
    }
}
