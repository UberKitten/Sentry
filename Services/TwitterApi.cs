using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using RestSharp;
using RestSharp.Authenticators;
using Sentry.Config;

namespace Sentry.Services
{
    class TwitterApi : BaseService
    {
        public class ServiceOptions {
            public string ConsumerKey { get; set; }
            public string ConsumerSecret { get; set; }
            public string Token { get; set; }
            public string TokenSecret { get; set; }
            public string PostStatus { get; set; }
        };

        protected ServiceOptions Options { get; set; }

        public class ServiceTriggerCriteria {
            public List<string> TweetContains { get; set; }
            public int RetweetsOver { get; set; }
            public int FavoritesOver { get; set; }
        };

        protected RestClient client = new RestClient("https://api.twitter.com/");
        protected long user_id = -1;

        public TwitterApi(SecretsStoreManager secretsStoreManager, string id, object ServiceOptions) : base(secretsStoreManager, id)
        {
            Options = (ServiceOptions)ServiceOptions;

            client.Authenticator = OAuth1Authenticator.ForProtectedResource(
                secretsStoreManager.TryGetSecret(Options.ConsumerKey),
                secretsStoreManager.TryGetSecret(Options.ConsumerSecret),
                secretsStoreManager.TryGetSecret(Options.Token),
                secretsStoreManager.TryGetSecret(Options.TokenSecret)
            );
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

        private IRestResponse<dynamic> Delete(string id)
        {
            var request = new RestRequest("/1.1/statuses/destroy/{id}.json", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddUrlSegment("id", id);

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("Delete response: {0}", response.Content);
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

        public override bool Check(object _triggerCriteria)
        {
            var triggerCriteria = (ServiceTriggerCriteria)_triggerCriteria;

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
                // Check TweetContains
                foreach (var triggerString in triggerCriteria.TweetContains)
                {
                    if (tweet.text.Value.Contains(triggerString))
                    {
                        logger.Debug("Found trigger string in text: {0}", tweet.text);
                        return true;
                    }
                }

                // Check RetweetsOver
                if (triggerCriteria.RetweetsOver > 0) // 0 if omitted
                {
                    if (tweet.retweet_count > triggerCriteria.RetweetsOver)
                    {
                        logger.Debug("Found tweet with retweet count {0} which is greater than criteria {1}", tweet.retweet_count, triggerCriteria.RetweetsOver);
                        return true;
                    }
                }
                
                // Check FavoritesOver
                if (triggerCriteria.FavoritesOver > 0) // 0 if omitted
                {
                    if (tweet.favorite_count > triggerCriteria.FavoritesOver)
                    {
                        logger.Debug("Found tweet with favorite count {0} which is greater than criteria {1}", tweet.favorite_count, triggerCriteria.FavoritesOver);
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Action(List<string> actions)
        {
            // Scorch means delete all statuses (as opposed to delete, which means delete the account)
            if (actions.Contains("scorch", StringComparer.InvariantCultureIgnoreCase))
            {
                bool tweetsLeft = true;
                while (tweetsLeft)
                {
                    // Get user's tweets
                    var tweetsResponse = UserTimeline();
                    var tweets = tweetsResponse.Data.ToObject<List<dynamic>>();

                    if (tweets.Count <= 0)
                    {
                        tweetsLeft = false;
                    }

                    // Keep using the same cached list of tweets to save API requests
                    foreach (var tweet in tweets)
                    {
                        var deleteResponse = Delete(tweet.id_str.Value);
                        if (deleteResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            logger.Info("Deleted Twitter status: {0}", tweet.id_str);
                        }
                        else if ((int)deleteResponse.StatusCode == 429) // "too many requests"
                        {
                            var rateLimitResetHeader = tweetsResponse.Headers.Single(t => t.Name.Equals("x-rate-limit-reset", StringComparison.InvariantCultureIgnoreCase));
                            var rateLimitReset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(rateLimitResetHeader.Value)).DateTime;
                            var difference = rateLimitReset.Subtract(DateTime.Now);
                            logger.Warn("Rate limited by Twitter for {0} seconds", Math.Round(difference.TotalSeconds));
                            Thread.Sleep(difference);
                        }
                        else
                        {
                            logger.Error("Unexpected response to Twitter delete: {0}", deleteResponse.ResponseStatus);
                        }
                    }
                }
            }

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
