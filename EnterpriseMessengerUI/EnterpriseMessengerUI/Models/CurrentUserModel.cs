namespace EnterpriseMessengerUI.Models
{
    public static class CurrentUserModel
    {
        public static string? Id { get; set; }

        public static string? Surname { get; set; }

        public static string? Name { get; set; }

        public static string? Patronymic { get; set; }

        public static string? Position { get; set; }

        public static string? PhotoFileName { get; set; }

        public static string? Information { get; set; }

        public static NetworkStatus NetworkStatus { get; set; }
    }
}
