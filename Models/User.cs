using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Sockets;

namespace IllusionMuseum.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите логин")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 100 символов")]
        [Display(Name = "Логин")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Введите корректный email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string PasswordHash { get; set; } = string.Empty;

        [NotMapped]
        [Compare("PasswordHash", ErrorMessage = "Пароли не совпадают")]
        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Введите ваше имя")]
        [Display(Name = "Полное имя")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Телефон")]
        [Phone(ErrorMessage = "Введите корректный номер")]
        public string? Phone { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public bool IsAdmin { get; set; } = false;

        // Навигационное свойство
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}