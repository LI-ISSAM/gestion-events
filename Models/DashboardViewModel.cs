using GestionEvenements.Models;
using System.Collections.Generic;

namespace GestionEvenements.Models
{
    public class DashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int TotalUsers { get; set; }
        public int TotalInscriptions { get; set; }
        public int PendingInscriptions { get; set; }

        public List<Event> UpcomingEvents { get; set; } = new List<Event>();
        public List<Event> RecentEvents { get; set; } = new List<Event>();
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
        public List<Inscription> PendingRegistrations { get; set; } = new List<Inscription>();
        
        public List<Comment> RecentComments { get; set; } = new List<Comment>();
    }
}
