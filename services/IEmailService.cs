namespace GestionEvenements.Services
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string email, string userName, string eventTitle, string eventDate, string eventLocation);
        Task SendRejectionEmailAsync(string email, string userName, string eventTitle, string? reason = null);
    }
}
