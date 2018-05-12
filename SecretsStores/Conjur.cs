using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry.SecretsStores
{
    class Conjur : BaseSecretsStore
    {
        public class SecretsStoreOptions
        {
            public string ApplianceUrl { get; set; }
            public string Account { get; set; }
            public string Login { get; set; }
            public string ApiKey { get; set; }
        }

        protected SecretsStoreOptions Options { get; set; }

        protected RestClient client;

        public Conjur(object SecretsStoreOptions) : base(SecretsStoreOptions)
        {
            Options = (SecretsStoreOptions)SecretsStoreOptions;
            client = new RestClient(Options.ApplianceUrl);
        }

        private IRestResponse<dynamic> Authenticate()
        {
            var request = new RestRequest("/authn/{account}/{login}/authenticate", Method.POST);
            request.AddUrlSegment("account", Options.Account);
            request.AddUrlSegment("login", Options.Login);
            request.AddParameter("text/plain", Options.ApiKey, ParameterType.RequestBody);

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("Authenticate response: {0}", response.Content);
            return response;
        }

        private void AddToken(ref RestRequest request, string accessToken)
        {
            // Convert access token to Base64
            var accessTokenBytes = System.Text.Encoding.UTF8.GetBytes(accessToken);
            var accessTokenBase64 = System.Convert.ToBase64String(accessTokenBytes);
            request.AddHeader("Authorization", String.Format("Token token=\"{0}\"", accessTokenBase64));
        }

        private IRestResponse<dynamic> RetrieveSecret(string accessToken, string id)
        {
            var request = new RestRequest("/secrets/{account}/{kind}/{identifier}", Method.GET);
            request.AddUrlSegment("account", Options.Account);
            request.AddUrlSegment("kind", "variable"); // this is Conjur best practice, please raise a GitHub issue with details if you need to customize this
            request.AddUrlSegment("identifier", id);
            request.AddParameter("text/plain", Options.ApiKey, ParameterType.RequestBody);

            AddToken(ref request, accessToken);

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("RetrieveSecret response: {0}", response.Content);
            return response;
        }

        public override string GetSecret(string id)
        {
            string accessToken;
            var authenticateResponse = Authenticate();
            if (authenticateResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                accessToken = authenticateResponse.Content;
            }
            else
            {
                logger.Error("Unexpected response to Conjur authenticate: {0}", authenticateResponse.StatusDescription);
                throw new UnableToRetrieveSecretException();
            }

            var retrieveSecretResponse = RetrieveSecret(accessToken, id);
            if (retrieveSecretResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var secret = retrieveSecretResponse.Content;
                return secret;
            }
            else
            {
                logger.Error("Unexpected response to Conjur retrieve secret: {0}", retrieveSecretResponse.StatusDescription);
                throw new UnableToRetrieveSecretException();
            }
        }
    }
}
