using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry
{
    abstract class BaseSecretsStore
    {
        protected Logger logger;

        /**
         * NotifyServices should implement this constructor.
         * Ideally implement a sub-class called "SecretsStoreOptions" and it will be casted here
         */
        public BaseSecretsStore(object SecretsStoreOptions) : this() { }

        protected BaseSecretsStore()
        {
            this.logger = LogManager.GetLogger(this.GetType().Name);
        }

        public virtual string GetSecret(string id)
        {
            throw new NotImplementedException();
        }

        public class UnableToRetrieveSecretException : Exception { }
    }
}
