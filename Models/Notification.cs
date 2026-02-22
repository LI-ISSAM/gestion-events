using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEvenements.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        [StringLength(200)]
        public string Titre { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; }

        public string? Type { get; set; } = "info"; // info, success, warning, error
        public string? Lien { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;
        public bool EstLue { get; set; } = false;
        public DateTime? DateLecture { get; set; }
    }
}
