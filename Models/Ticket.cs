using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IllusionMuseum.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Выберите дату")]
        [Display(Name = "Дата посещения")]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; }

        [Required(ErrorMessage = "Выберите время")]
        [Display(Name = "Время")]
        public string VisitTime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите тип билета")]
        [Display(Name = "Тип билета")]
        public string TicketType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите количество")]
        [Range(1, 10, ErrorMessage = "Можно купить максимум 10 билетов за раз")]
        [Display(Name = "Количество")]
        public int Quantity { get; set; }

        [Display(Name = "Итоговая цена")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public string? Status { get; set; } = "confirmed";

        // Навигационное свойство
        public User? User { get; set; }

        [NotMapped]
        public decimal UnitPrice => TicketType switch
        {
            "Взрослый" => 600,
            "Детский (до 12 лет)" => 350,
            "Студенческий" => 450,
            "Льготный (пенсионеры и ветераны)" => 300,
            _ => 600
        };
    }
}