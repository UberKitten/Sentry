Sentry
======

Sentry will be/is/has been released at [BSides Rochester](https://www.bsidesroc.com/schedule/) on April 14th!

Sentry will also be featured at [BSides Denver](https://www.bsidesden.org/) on May 11/12.

Feel free to contact [@t3hub3rk1tten](https://twitter.com/t3hub3rk1tten) for help or to send feedback. Please open bug reports in [Github issues](https://github.com/T3hUb3rK1tten/Sentry/issues). Pull requests are also welcome!

Abstract
--------
With social media, anyone can become "incidentally infamous" in minutes. Your tweet could go viral, your gif could get posted by a president, or the media could single you out because they think you made Bitcoin. This happens to hackers too, @MalwareTechBlog was arrested after DEF CON 2017 and certain media started doxing him and painting him as a spendthrift criminal based on his Twitter posts. Rather than become a social media hermit to prevent this, just set up a Sentry. This talk will present Sentry, an automated cross-platform application that will silently watch your social media for trigger words and unusual behaviors before springing into action. In minutes Sentry can lock your Twitter account, delete your Reddit comments, disable your websites, and a whole host of other actions to keep attention away in high visibility, low-privacy situations. Released under the MIT license and easily extensible, virtually any site and any API can be scripted with a bit of C#.

Current Features
-------
JFMSUF Mode

Twitter (via API)
- Check for trigger string
- Check for >x RTs/favs
- Delete all tweets ("scorch")
- Post tweet

Twitter (via web)  
- Lock account
- Delete (deactivate) account

Cloudflare  
- Update DNS records
- Delete DNS records

Pushover  
- Notify on startup
- Notify on trigger activation

Features in Development
-------
[Conjur](https://github.com/cyberark/conjur)
- Store secrets securely and easily

Multi-factor support

Reddit (via API)
- Blank posts ("wipe")
- Delete posts

Reddit (via web)
- Delete account

Email (IMAP/POP)
- Check for trigger string

Clustering support

Building
--------
**At this stage in development, I recommend only using Sentry if you have a bit of programming experience to identify issues.**

To build the project, you can use the free [Visual Studio Community edition](https://www.visualstudio.com/vs/community/). Make sure you specify running C# apps so it installs the right libraries.

Running
-------
After building the project or downloading the binaries from the releases tab:
1. Install the [.NET Core Runtime](https://www.microsoft.com/net/download/Windows/run) (might be installed by VS Community, run `dotnet` to see)
2. If you want to use the web automation with Chrome, install Google Chrome and grab the latest [Chrome Driver](https://sites.google.com/a/chromium.org/chromedriver/downloads) for your platform and put it in the same directory as Sentry.dll (the Windows exe is included in the release zip)
3. Run `dotnet Sentry.dll` and verify you see the help output
4. Copy the `config.example.json` to `config.json` and edit to add your services
5. Run `dotnet Sentry.dll run` to start Sentry in normal mode

Couple notes:
- [NLog.config](https://github.com/nlog/nlog/wiki/Configuration-file) is very powerful and can be edited to send log information wherever you need it, for example to email you errors
- The Trace logging level exposes secrets, make sure any logging data that includes Trace level is handled securely
- The Debug logging level and up do not contain secrets
- MFA and Quorum (clustering support) is mentioned in a few parts of the config, but isn't quite working yet
- Debug release will start a web server that doesn't do anything yet (should have put that in a branch, oops)
