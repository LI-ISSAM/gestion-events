using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System;

namespace GestionEvenements.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le titre est obligatoire")]
        [StringLength(200)]
        public string Titre { get; set; }

        [Required(ErrorMessage = "La description est obligatoire")]
        [StringLength(2000)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Le lieu est obligatoire")]
        [StringLength(200)]
        public string Lieu { get; set; }

        [Required(ErrorMessage = "La date est obligatoire")]
        [DataType(DataType.DateTime)]
        public DateTime DateEvent { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le prix doit être positif")]
        public decimal Prix { get; set; }

        public string? ImageUrl { get; set; }

        [ForeignKey("CreatedByUser")]
        public string? CreatedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("Categorie")]
        public int? CategorieId { get; set; }
        public virtual EventCategory? Categorie { get; set; }

        public bool EstTermine { get; set; } = false;
        public bool EstAnnule { get; set; } = false;

        [StringLength(500)]
        public string? RaisonAnnulation { get; set; }

        public DateTime? DateAnnulation { get; set; }

        // Navigation properties
        public virtual ApplicationUser? CreatedByUser { get; set; }
        public virtual ICollection<Inscription> Inscriptions { get; set; } = new List<Inscription>();
        
        public virtual ICollection<Comment> Commentaires { get; set; } = new List<Comment>();
        
        public virtual StatisticViewModel? Statistiques { get; set; }
    }
}
