using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEvenements.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("Event")]
        public int EventId { get; set; }
        public virtual Event Event { get; set; }

        [Required(ErrorMessage = "Le commentaire est obligatoire")]
        [StringLength(1000, ErrorMessage = "Le commentaire ne peut pas dépasser 1000 caractères")]
        public string Texte { get; set; }

        [Range(1, 5, ErrorMessage = "La note doit être entre 1 et 5")]
        public int Note { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DateModification { get; set; }

        public bool UtilisateurAParticipe { get; set; } = false;
    }
}
