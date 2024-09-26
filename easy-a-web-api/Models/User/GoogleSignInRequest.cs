namespace easy_a_web_api.Models.User
{
    // DTO for Google Sign-In request
    public class GoogleSignInRequest
    {
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // Add Email field
        public string Firstname { get; set; } = string.Empty; // Add Email field
        public string LastName { get; set; } = string.Empty; // Add Email field
        public string ProfilePicture { get; set; } = string.Empty; // Add Email field
    }
}