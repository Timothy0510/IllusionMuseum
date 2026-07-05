using System.ComponentModel.DataAnnotations;

namespace IllusionMuseum.Models
{
    public class MuseumInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Address { get; set; }

        [Display(Name = "Часы работы")]
        public string? WorkingHours { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        [Display(Name = "URL фото")]
        public string? ImageUrl { get; set; }
    }
}