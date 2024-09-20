namespace easy_a_web_api.Models.User
{
    public class UpdateUserRequest
    {
        public string Uid { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public IFormFile? ProfileImage { get; set; }
    }
}
