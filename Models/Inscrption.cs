using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEvenements.Models
{
    public class Inscription
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("Event")]
        public int EventId { get; set; }
        public virtual Event Event { get; set; }

        public DateTime DateInscription { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Statut { get; set; } = "En attente"; // "En attente", "Confirmée", "Rejetée"

        [StringLength(500)]
        public string? CommentaireAdmin { get; set; }
    }
}
