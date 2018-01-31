using NLog;
using Sentry.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry
{
    abstract class BaseService
    {
        protected ServiceConfig Config { get; set; }

        protected Logger logger;

        /**
         * Services are recommended to cache session data in instance variables
         * For example, keep a session cookie and continue using it until invalid
         * Instances are kept alive between checks
         */ 
        public BaseService(ServiceConfig config)
        {
            this.Config = config;
            this.logger = LogManager.GetLogger(config.Id);
        }

        /**
         * Called upon startup to verify that configuration is correct
         * For example: Check that all parameters are specified,
         * check that login credentials work, etc
         */ 
        public virtual void Verify()
        {
            logger.Debug("Verify not implemented, skipping verification");
        }

        /**
         * Called upon loop to check for trigger string
         * Service must check for recent posts/events/etc to determine if trigger appears
         * Comparison must be case-sensitive by default
         */
        public virtual bool Check(string triggerString)
        {
            throw new NotImplementedException();
        }

        /**
         * Called after an action is triggered when attempting to determine if this node should act
         * Service must check if another node has posted a quorum message (within reasonable timeframe/number of messages)
         * If other nodes have posted, return false. Else return true
         * Service MUST ignore quorum messages from this node, check using GUID
         * How exactly to determine a quorum message from a normal message is up to the service
         * For example, something as simple as: "quorum check: {guid}" in a post will work
         * Services that can't implement the quorum message functions feasibly should not implement them
         * If any errors occur, an exception must be thrown
         */
        public virtual bool CheckQuorumMessage(string guid)
        {
            logger.Debug("CheckQuorumMessage not implemented, skipping check");
            return true;
        }

        /**
         * Called after checking for a quorum and finding no messages
         * Service must post in a format that identifies this node to other nodes
         * This should match with CheckQuorumMessage
         * Services that can't implement the quorum message functions feasibly should not implement them
         * If any errors occur, an exception must be thrown
         */
        public virtual void PostQuorumMessage(string guid)
        {
            logger.Debug("PostQuorumMessage not implemented, skipping post");
        }

        /**
         * Called after attempting to grab quorum, but another node is identified
         * Service must delete previous quorum message posted by this node only
         * The message deleted should match with CheckQuorumMessage
         * Services that can't implement the quorum message functions feasibly should not implement them
         * If any errors occur, an exception must be thrown
         */ 
        public virtual void DeleteQuorumMessage(string guid)
        {
            logger.Debug("DeleteQuorumMessage not implemented, skipping delete");
        }

        /**
         * Called when a trigger is detected
         * Multiple actions can be requested, e.g. "lock" "scorch" "delete"
         * Service must determine appropriate order (if any)
         * Recommend starting with least destructive first
         * Service must ensure that regardless of specified order, actions should complete (if allowed)
         * IF order of operations is so important that a failure of an action should cause other actions to halt
         * then those actions should be specified as one
         * For example, on reddit one must edit/delete old comments before deleting the account or else comments will be stuck
         * This should be called "scorchdelete" instead of having "scorch" and "delete" separately
         */
        public virtual void Action(List<string> actions)
        {
            throw new NotImplementedException();
        }
    }
}
