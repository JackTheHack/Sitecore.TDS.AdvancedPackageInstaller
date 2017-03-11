using System;
using Microsoft.AspNet.SignalR;
using Owin;

namespace HedgehogDevelopment.PackageInstaller
{
    public class SignalRStartup
    {
        public static IAppBuilder App = null;

        public void Configuration(IAppBuilder app)
        {
            AppDomain.CurrentDomain.Load(typeof(InstallerHub).Assembly.FullName);

            app.Map("/signalr", map => {                
                var hubConfig = new HubConfiguration
                {
                    EnableDetailedErrors = true,
                    EnableJSONP = true
                };
                map.RunSignalR(hubConfig);
            });
        }
    }
}
