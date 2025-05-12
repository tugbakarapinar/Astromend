using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using app_api.Auth;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

[assembly: OwinStartup(typeof(app_api.App_Start.Startup))]

namespace app_api.App_Start
{
    public class Startup
    {
        public void Configuration(Owin.IAppBuilder app)
        {
            HttpConfiguration httpConfiguration = new HttpConfiguration();
            ConfigureOAuth(app);

            WebApiConfig.Register(httpConfiguration);
            app.UseWebApi(httpConfiguration);
        }
        public void ConfigureOAuth(IAppBuilder app)
        {
            OAuthAuthorizationServerOptions oAuthAuthorizationServerOptions = new OAuthAuthorizationServerOptions()
            {
                TokenEndpointPath = new PathString("/app/api/security/login"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(5000),
                AllowInsecureHttp = true,
                Provider = new AuthorizationServerProvider()

            };

            app.UseOAuthAuthorizationServer(oAuthAuthorizationServerOptions);
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());
        }
    }
}