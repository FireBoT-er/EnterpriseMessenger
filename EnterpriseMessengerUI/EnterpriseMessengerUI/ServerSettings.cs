using System;

namespace EnterpriseMessengerUI
{
    public static class ServerSettings
    {
        public static string? ServerAddress { get; set; }

        public static bool ConnectionLost { get; set; } = false;

        public static TimeSpan? EditDeleteTimeLimit { get; set; }
    }
}
