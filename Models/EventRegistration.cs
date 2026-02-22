namespace GestionEvenements.Models
{
    public class EventRegistration
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        
        public int EventId { get; set; }
        public Event Event { get; set; }
        
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
    }
}
