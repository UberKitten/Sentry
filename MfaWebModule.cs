using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;

namespace Sentry
{
    public class MfaWebModule : WebModuleBase
    {
        public override string Name => nameof(MfaWebModule);

        public MfaWebModule()
        {
            AddHandler("/mfa/", Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Get, (context, ct) => HandleGet(context, ct, true));
            AddHandler("/mfa/", Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Head, (context, ct) => HandleGet(context, ct, false));
            AddHandler("/mfa/", Unosquare.Labs.EmbedIO.Constants.HttpVerbs.Post, (context, ct) => HandlePost(context, ct));
        }

        private Task<bool> HandleGet(HttpListenerContext context, CancellationToken ct, bool sendBuffer)
        {
            return Task.FromResult(true);
        }

        private Task<bool> HandlePost(HttpListenerContext context, CancellationToken ct)
        {
            return Task.FromResult(true);
        }

        public async Task HandleMfaRequest(string token)
        {

        }
    }
}
