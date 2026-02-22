using System.ComponentModel.DataAnnotations;

namespace GestionEvenements.Models
{
    public class EventCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de la catégorie est obligatoire")]
        [StringLength(100)]
        public string Nom { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public string Couleur { get; set; } = "#1e40af"; // Couleur par défaut
        public string Icone { get; set; } = "fas fa-tag";
    }
}
