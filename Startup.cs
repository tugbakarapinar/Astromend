using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Microsoft.Owin.Cors;
using Microsoft.AspNet.SignalR;

[assembly: OwinStartup(typeof(RealTimeHub.Startup))]

namespace RealTimeHub
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var RHUBUserIdProvider = new RHUBUserIdProvider();
            GlobalHost.DependencyResolver.Register(typeof(IUserIdProvider), () => RHUBUserIdProvider);
            app.Map("/signalr/hubs", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                var hubConfiguration = new HubConfiguration
                {
                    EnableJSONP = true
                };
                map.RunSignalR(hubConfiguration);
            });        
        }
    }
}
