namespace easy_a_web_api.Models.User
{
    public class UpdateSettingsRequest
    {
        public string? Uid { get; set; }
        public string? Language { get; set; }
        public bool Notifications { get; set; }
        public string? Theme { get; set; }
        public bool? BiometricAuthentication { get; set; }
    }
}
