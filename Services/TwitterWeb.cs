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
        public class ServiceOptions
        {
            public string Username { get; set; }
            public string Password { get; set; }
        };

        protected ServiceOptions Options { get; set; }

        public class ServiceTriggerCriteria
        {
            // Currently no triggers, only actions
        };

        protected ChromeDriver driver;

        public TwitterWeb(SecretsStoreManager secretsStoreManager, string id, object ServiceOptions) : base(secretsStoreManager, id)
        {
            Options = (ServiceOptions)ServiceOptions;

            var driverOptions = new ChromeOptions();

#if !DEBUG
            driverOptions.AddArgument("headless");
#endif
            driverOptions.AddArgument("disable-gpu");

            driver = new ChromeDriver(Directory.GetCurrentDirectory(), driverOptions);
            driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 30);
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
            username.SendKeys(secretsStoreManager.TryGetSecret(Options.Username));

            var password = driver.FindElementByClassName("js-password-field");
            password.Click();
            password.SendKeys(secretsStoreManager.TryGetSecret(Options.Password));

            password.Submit();
        }
        
        protected bool IsLoggedIn()
        {
            driver.Navigate().GoToUrl("https://twitter.com/login");
            // If we get redirected to the home page we're logged in
            return !driver.Url.Contains("/login");
        }

        public override bool Check(object _triggerCriteria)
        {
            var triggerCriteria = (ServiceTriggerCriteria)_triggerCriteria;
            if (!IsLoggedIn())
            {
                Login();
            }
            return base.Check(triggerCriteria);
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
                    confirmPassword.SendKeys(secretsStoreManager.TryGetSecret(Options.Password));
                    confirmPassword.Submit();

                    logger.Info("Twitter account {0} locked",Options.Username);
                }
            }


            if (actions.Contains("delete", StringComparer.InvariantCultureIgnoreCase))
            {
                driver.Navigate().GoToUrl("https://twitter.com/settings/accounts/confirm_deactivation");

                // "deactivate @username" button
                var submit = driver.FindElementById("settings_save");
                submit.Click();

                // Password confirmation box
                var confirmPassword = driver.FindElementById("auth_password");
                confirmPassword.Click();
                confirmPassword.SendKeys(secretsStoreManager.TryGetSecret(Options.Password));
                confirmPassword.Submit();

                logger.Info("Twitter account {0} deactivated", Options.Username);
            }
        }
    }
}
