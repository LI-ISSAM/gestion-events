using System.ComponentModel.DataAnnotations;

namespace GestionEvenements.Models
{
    public class UserProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public int TotalEventsCreated { get; set; }
        public int TotalEventsAttended { get; set; }
        public int TotalComments { get; set; }
        public double AverageRating { get; set; }
        public List<Event> RecentEvents { get; set; } = new();
        public List<Event> RegisteredEvents { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }

    public class UserProfileEditViewModel
    {
        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        [StringLength(100)]
        public string Ville { get; set; }

        [StringLength(100)]
        public string Pays { get; set; }

        public string DateNaissance { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Ancien mot de passe obligatoire")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Nouveau mot de passe obligatoire")]
        [StringLength(100, ErrorMessage = "Le {0} doit être d'au moins {2} et au maximum {1} caractères.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string ConfirmPassword { get; set; }
    }
}
