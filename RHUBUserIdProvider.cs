using Microsoft.AspNet.SignalR;

namespace RealTimeHub
{
    public class RHUBUserIdProvider : IUserIdProvider
    {
        public string GetUserId(IRequest request)
        {
            string clientRecevierId = request.QueryString.Get("clientRecevierId");
            return clientRecevierId;
        }
    }
}