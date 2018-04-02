using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry
{
    abstract class BaseNotifyService
    {
        protected Logger logger;

        /**
         * NotifyServices should implement this constructor.
         * Ideally implement a sub-class called "NotifyServiceOptions" and it will be casted here
         */
        public BaseNotifyService(object NotifyServiceOptions) : this() { }
        
        protected BaseNotifyService()
        {
            this.logger = LogManager.GetLogger(this.GetType().Name);
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
