namespace easy_a_web_api.Models.User
{
    public class UserResult
    {
        public string? Uid { get; set; }

        public string? Token { get; set; }

        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Gender { get; set; }

        public string? DateOfBirth { get; set; }

        public string? ProfilePicture { get; set; }

        public string? Language { get; set; }

        public bool Notifications { get; set; }

        public string? Theme { get; set; }
    }
}
