using System.Collections.Generic;

namespace GestionEvenements.Models
{
    public class StatisticsViewModel
    {
        public List<RegistrationStatusDto> RegistrationsByStatus { get; set; }
        public List<EventByMonthDto> EventsByMonth { get; set; }
        public List<TopCategoryDto> TopCategories { get; set; }
        public List<RatingDistributionDto> RatingDistribution { get; set; }
        public List<MostActiveUserDto> MostActiveUsers { get; set; }
    }

    public class RegistrationStatusDto
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class EventByMonthDto
    {
        public int Month { get; set; }
        public int Count { get; set; }
    }

    public class TopCategoryDto
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }

    public class RatingDistributionDto
    {
        public int Rating { get; set; }
        public int Count { get; set; }
    }

    public class MostActiveUserDto
    {
        public string User { get; set; }
        public int Registrations { get; set; }
    }
}
