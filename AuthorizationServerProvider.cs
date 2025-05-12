using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace app_api.Auth
{
    public class AuthorizationServerProvider : OAuthAuthorizationServerProvider
    {

        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
        }
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });
            Classes.Methods methods = new Classes.Methods();
            Classes.MReturn mReturn = new Classes.MReturn();
            mReturn = methods.Login(context.UserName, context.Password);
            if (mReturn.result.success)
            {
                var identity = new ClaimsIdentity(context.Options.AuthenticationType);
                identity.AddClaim(new Claim("userName", context.UserName));
                identity.AddClaim(new Claim("password", context.Password));
                identity.AddClaim(new Claim("userRecid", mReturn.key.ToString()));

                AuthenticationProperties properties = CustomProperties(mReturn);
                AuthenticationTicket ticket = new AuthenticationTicket(identity, properties);
                context.Validated(ticket);
            }
            else
            {
                context.SetError(mReturn.result.code.ToString(), mReturn.result.message);
            }
        }
        public static AuthenticationProperties CustomProperties(Classes.MReturn mReturn)
        {

            IDictionary<string, string> user = new Dictionary<string, string>();
            foreach (var property in typeof(Models.User.User).GetProperties())
            {
                string isKey = property.Name.ToString();
                var isValue = property.GetValue(mReturn.data, null);
                user.Add(isKey, isValue == null ? "" : isValue.ToString());

            }

            IDictionary<string, string> data = new Dictionary<string, string>
             {
                { "user", JsonConvert.SerializeObject(user, Formatting.Indented) },
             };
            return new AuthenticationProperties(data);
        }
        public override System.Threading.Tasks.Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return System.Threading.Tasks.Task.FromResult<object>(null);
        }
    }
}