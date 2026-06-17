using System.ComponentModel.DataAnnotations;

namespace SportsStore.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = String.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = String.Empty;

        [Display(Name = "Ghi nhớ?")]
        public bool RememberMe { get; set; }
    }
}
