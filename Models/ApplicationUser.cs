using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GestionEvenements.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public string? ProfilePictureUrl { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [StringLength(100)]
        public string? Ville { get; set; }

        [StringLength(100)]
        public string? Pays { get; set; }

        public DateTime? DateNaissance { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DerniereConnexion { get; set; }

        // Navigation properties
        public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
        public ICollection<Inscription> Inscriptions { get; set; } = new List<Inscription>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        
        public ICollection<Comment> Commentaires { get; set; } = new List<Comment>();
    }
}
