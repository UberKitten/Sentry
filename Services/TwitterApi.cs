using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using RestSharp.Authenticators;
using Sentry.Config;

namespace Sentry.Services
{
    class TwitterApi : BaseService
    {
        protected class ServiceOptions {
            public string ConsumerKey { get; set; }
            public string ConsumerSecret { get; set; }
            public string Token { get; set; }
            public string TokenSecret { get; set; }
            public string PostStatus { get; set; }
        };

        protected ServiceOptions Options { get; set; }

        protected RestClient client = new RestClient("https://api.twitter.com/");
        protected long user_id = -1;

        public TwitterApi(ServiceConfig config) : base(config)
        {
            Options = InitializeOptions<ServiceOptions>();
            client.Authenticator = OAuth1Authenticator.ForProtectedResource(Options.ConsumerKey, Options.ConsumerSecret, Options.Token, Options.TokenSecret);
            client.AddHandler("application/json", new DynamicJsonDeserializer());
        }

        private IRestResponse<dynamic> VerifyCredentials()
        {
            var request = new RestRequest("/1.1/account/verify_credentials.json", Method.GET);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Content-Type", "application/json; charset=utf-8");

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("VerifyCredentials response: {0}", response.Content);
            return response;
        }

        private IRestResponse<dynamic> UserTimeline()
        {
            var request = new RestRequest("/1.1/statuses/user_timeline.json");
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("user_id", user_id);
            request.AddParameter("include_rts", false);
            request.AddParameter("trim_user", true);
            request.AddParameter("count", 200);

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("UserTimeline response: {0}", response.Content);
            return response;
        }

        private IRestResponse<dynamic> Post(string message)
        {
            var request = new RestRequest("/1.1/statuses/update.json", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("status", message);

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("Post response: {0}", response.Content);
            return response;
        }

        public override void Verify()
        {
            var response = VerifyCredentials();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                user_id = response.Data.id.Value;
            }
            else
            {
                logger.Error("Unexpected response to Twitter login: {0}", response.StatusDescription);
                throw new UnableToVerifyException();
            }
        }

        public override bool Check(string triggerString)
        {
            if (user_id == -1)
            {
                var verifyResponse = VerifyCredentials();
                if (verifyResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    user_id = verifyResponse.Data.id.Value;
                }
                else
                {
                    logger.Error("Unexpected response to Twitter login: {0}", verifyResponse.ResponseStatus);
                    throw new UnableToCheckException();
                }
            }

            var tweetsResponse = UserTimeline();
            var tweets = tweetsResponse.Data.ToObject<List<dynamic>>();
            foreach (var tweet in tweets)
            {
                if (tweet.text.Value.Contains(triggerString))
                {
                    logger.Debug("Found trigger string in text: {0}", tweet.Text);
                    return true;
                }
            }
            return false;
        }

        public override void Action(List<string> actions)
        {
            if (actions.Contains("post", StringComparer.InvariantCultureIgnoreCase))
            {
                var postResponse = Post(Options.PostStatus);
                if (postResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Info("Posted Twitter status: {0}", Options.PostStatus);
                }
                else
                {
                    logger.Error("Unexpected response to Twitter post: {0}", postResponse.ResponseStatus);
                }
            }
        }
    }
}
