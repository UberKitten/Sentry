using System;
using System.Collections.Generic;
using System.Text;
using Sentry.Config;
using OpenQA.Selenium.Chrome;
using System.IO;
using System.Linq;

namespace Sentry.Services
{
    class TwitterWeb : BaseService
    {
        protected class ServiceOptions
        {
            public string Username { get; set; }
            public string Password { get; set; }
        };

        protected ServiceOptions Options { get; set; }

        protected ChromeDriver driver;

        public TwitterWeb(ServiceConfig config) : base(config)
        {
            Options = InitializeOptions<ServiceOptions>();
            var driverOptions = new ChromeOptions();

#if !DEBUG
            driverOptions.AddArgument("headless");
#endif
            driverOptions.AddArgument("disable-gpu");

            driver = new ChromeDriver(Directory.GetCurrentDirectory(), driverOptions);
        }

        public override void Verify()
        {
            if (!IsLoggedIn())
            {
                Login();
            }

            if (!IsLoggedIn())
            {
                logger.Error("Unable to login to Twitter");
                throw new UnableToVerifyException();
            }
        }

        protected void Login()
        {
            driver.Navigate().GoToUrl("https://twitter.com/login");

            if (!driver.Url.Contains("/login"))
            {
                logger.Debug("Already logged in");
                return;
            }
            var username = driver.FindElementByClassName("js-username-field");
            username.Click();
            username.SendKeys(Options.Username);

            var password = driver.FindElementByClassName("js-password-field");
            password.Click();
            password.SendKeys(Options.Password);

            password.Submit();
        }
        
        protected bool IsLoggedIn()
        {
            driver.Navigate().GoToUrl("https://twitter.com/login");
            // If we get redirected to the home page we're logged in
            return !driver.Url.Contains("/login");
        }

        public override bool Check(string triggerString)
        {
            if (!IsLoggedIn())
            {
                Login();
            }
            return base.Check(triggerString);
        }

        public override void Action(List<string> actions)
        {
            if (!IsLoggedIn())
            {
                Login();
            }

            if (actions.Contains("lock", StringComparer.InvariantCultureIgnoreCase))
            {
                driver.Navigate().GoToUrl("https://twitter.com/settings/safety");

                // "protect your tweets" box
                var protect = driver.FindElementById("user_protected");
                if (protect.Selected)
                {
                    logger.Info("Twitter account {0} appears to already be locked", Options.Username);
                }
                else
                {
                    // Check the box
                    protect.Click();
                    protect.Submit();

                    // Password confirmation box
                    var confirmPassword = driver.FindElementById("auth_password");
                    confirmPassword.Click();
                    confirmPassword.SendKeys(Options.Password);
                    confirmPassword.Submit();

                    logger.Info("Twitter account {0} locked",Options.Username);
                }
            }
        }

        public override void DeleteQuorumMessage(string guid)
        {
            base.DeleteQuorumMessage(guid);
        }

        public override void PostQuorumMessage(string guid)
        {
            base.PostQuorumMessage(guid);
        }

        public override bool QuorumMessageExists(string guid)
        {
            return base.QuorumMessageExists(guid);
        }

    }
}
