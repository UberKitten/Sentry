using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;

namespace Sentry
{
    class SentryWebServer : IDisposable
    {
        private WebServer webServer = null;

        public SentryWebServer(string bindUrl)
        {
            webServer = new WebServer(bindUrl);
            webServer.RegisterModule(new MfaWebModule());
        }

        public void Start()
        {
            webServer.RunAsync();
        }

        public async Task HandleMfaRequest(string token)
        {
            await webServer.Module<MfaWebModule>().HandleMfaRequest(token);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (webServer != null)
                    {
                        webServer.Dispose();
                    }
                }
                webServer = null;
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
