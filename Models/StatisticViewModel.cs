using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEvenements.Models
{
    public class StatisticViewModel
    {
        public int Id { get; set; }

        [ForeignKey("Event")]
        public int EventId { get; set; }
        public virtual Event Event { get; set; }

        public int TotalViews { get; set; } = 0;
        public int TotalRegistrations { get; set; } = 0;
        public int ConfirmedRegistrations { get; set; } = 0;
        public int TotalComments { get; set; } = 0;
        public double AverageRating { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

  
}
