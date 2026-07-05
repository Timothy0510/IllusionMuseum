using System.ComponentModel.DataAnnotations;

namespace IllusionMuseum.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введите логин или email")]
        [Display(Name = "Логин или Email")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }
}