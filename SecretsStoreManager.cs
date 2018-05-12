using NLog;
using Sentry.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Sentry
{
    class SecretsStoreManager
    {
        protected Logger logger;

        protected List<Tuple<BaseSecretsStore, SecretsStoreConfig>> secretsStores;

        public SecretsStoreManager(List<Tuple<BaseSecretsStore, SecretsStoreConfig>> secretsStores)
        {
            this.logger = LogManager.GetLogger("SecretsStoreManager");
            this.secretsStores = secretsStores;
        }

        public string TryGetSecret(string optionValue)
        {
            bool success;
            return TryGetSecret(optionValue, out success);
        }

        public string TryGetSecret(string optionValue, out bool success)
        {
            if (secretsStores != null && secretsStores.Count > 0)
            {
                // Try to identify if it's in the standard "id:secretid" format
                int colonPos = optionValue.IndexOf(':');

                // Ignore if the colon is the first character, or not found (-1)
                if (colonPos > 0)
                {
                    string secretsStoreId = optionValue.Substring(0, colonPos);
                    string secretId = optionValue.Substring(colonPos + 1, optionValue.Length - colonPos - 1);
                    logger.Trace("Attempting to retrieve secret ID {0} from secrets store {1}", secretId, secretsStoreId);

                    // This method must be thread safe
                    lock (secretsStores)
                    {
                        try
                        {
                            var secretsStore = secretsStores.Single(t => secretsStoreId.Equals(t.Item2.Id, StringComparison.InvariantCultureIgnoreCase));
                            var secret = secretsStore.Item1.GetSecret(secretId);
                            success = true;
                            logger.Trace("Retrieved secret id {0} value {1}", secretId, secret);
                            return secret;
                        }
                        catch (InvalidOperationException)
                        {
                            logger.Trace("Unable to find secrets store {0}", secretsStoreId);
                        }
                    }
                }
            }

            success = false;
            return optionValue;
        }
    }
}
