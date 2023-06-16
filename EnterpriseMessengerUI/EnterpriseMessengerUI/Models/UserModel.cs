namespace EnterpriseMessengerUI.Models
{
    public class UserModel
    {
        public string Id { get; }

        public string PhotoFileName { get; }

        public string FullName { get; }

        public string Position { get; }

        public bool FirstUser { get; }

        public UserModel(string id, bool hasPhoto, string fullName, string position, bool firstUser = false)
        {
            Id = id;
            PhotoFileName = hasPhoto ? $"{ServerSettings.ServerAddress}/UserFiles/Avatars/{id}.png" : "/Assets/user.png";
            FullName = fullName;
            Position = position;

            FirstUser = firstUser;
            if (firstUser)
            {
                Position += " (Создатель)";
            }
        }
    }
}
