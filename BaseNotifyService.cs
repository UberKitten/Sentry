using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry
{
    abstract class BaseNotifyService
    {
        protected Logger logger;
        protected SecretsStoreManager secretsStoreManager;

        /**
         * NotifyServices should implement this constructor.
         * Ideally implement a sub-class called "NotifyServiceOptions" and it will be casted here
         */
        public BaseNotifyService(SecretsStoreManager secretsStoreManager, object NotifyServiceOptions) : this(secretsStoreManager) { }
        
        protected BaseNotifyService(SecretsStoreManager secretsStoreManager)
        {
            this.logger = LogManager.GetLogger(this.GetType().Name);
            this.secretsStoreManager = secretsStoreManager;
        }

        public virtual void NotifyStartup()
        {
            throw new NotImplementedException();
        }

        public virtual void NotifyMultiFactorRequest(string id, string url)
        {
            throw new NotImplementedException();
        }

        public virtual void NotifyOnTrigger(string id)
        {
            throw new NotImplementedException();
        }
    }

}
