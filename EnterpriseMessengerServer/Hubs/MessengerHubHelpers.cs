namespace EnterpriseMessengerServer.Hubs
{
    public enum NetworkStatus
    {
        Online,
        Busy,
        Offline
    }

    public static class MessengerHubHelpers
    {
        public static Dictionary<string, NetworkStatus> UsersOnline { get; set; } = new();
    }
}
