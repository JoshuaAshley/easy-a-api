using System.ComponentModel.DataAnnotations;

namespace easy_a_web_api.Models.User
{
    public class DeleteAccountRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
