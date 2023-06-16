using Microsoft.AspNetCore.SignalR;

namespace EnterpriseMessengerServer.Hubs
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.Identity?.Name;
        }
    }
}
