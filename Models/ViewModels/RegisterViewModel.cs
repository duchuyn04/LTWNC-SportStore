using System.ComponentModel.DataAnnotations;

namespace SportsStore.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string Username { get; set; } = String.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = String.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = String.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; } = String.Empty;
    }
}
