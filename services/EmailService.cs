using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionEvenements.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendConfirmationEmailAsync(string email, string userName, string eventTitle, string eventDate, string eventLocation)
        {
            try
            {
                var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortStr = _configuration["Email:SmtpPort"] ?? "587";
                var senderEmail = _configuration["Email:SenderEmail"] ?? "noreply@gestionevenements.com";
                var senderPassword = _configuration["Email:SenderPassword"] ?? "";

                Console.WriteLine($"[EmailService - DEBUG] ======= CONFIRMATION EMAIL DEBUG =======");
                Console.WriteLine($"[EmailService - DEBUG] Recipient Email: {email}");
                Console.WriteLine($"[EmailService - DEBUG] SMTP Server: {smtpServer}");
                Console.WriteLine($"[EmailService - DEBUG] SMTP Port: {smtpPortStr}");
                Console.WriteLine($"[EmailService - DEBUG] Sender Email: {senderEmail}");
                Console.WriteLine($"[EmailService - DEBUG] Sender Password Configured: {!string.IsNullOrEmpty(senderPassword)}");
                Console.WriteLine($"[EmailService - DEBUG] User Name: {userName}");
                Console.WriteLine($"[EmailService - DEBUG] Event Title: {eventTitle}");

                _logger.LogInformation("[EmailService] Attempting to send confirmation email to {Email}", email);
                _logger.LogInformation("[EmailService] SMTP Config - Server: {Server}, Port: {Port}, Sender: {Sender}", smtpServer, smtpPortStr, senderEmail);

                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine($"[EmailService - ERROR] Recipient email is empty!");
                    _logger.LogError("[EmailService] Recipient email is empty");
                    return;
                }

                if (string.IsNullOrEmpty(senderPassword))
                {
                    Console.WriteLine($"[EmailService - ERROR] Sender password not configured!");
                    _logger.LogWarning("[EmailService] Sender password not configured");
                    return;
                }

                int smtpPort = int.Parse(smtpPortStr);

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Timeout = 10000; // 10 seconds timeout
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, "Gestion Événements"),
                        Subject = $"Confirmation d'inscription - {eventTitle}",
                        Body = GenerateConfirmationEmailBody(userName, eventTitle, eventDate, eventLocation),
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    Console.WriteLine($"[EmailService - DEBUG] Sending email now...");
                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine($"[EmailService - SUCCESS] Confirmation email sent successfully to {email}");
                    _logger.LogInformation("[EmailService] Confirmation email sent successfully to {Email}", email);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService - ERROR] Exception occurred: {ex.GetType().Name}");
                Console.WriteLine($"[EmailService - ERROR] Message: {ex.Message}");
                Console.WriteLine($"[EmailService - ERROR] Stack Trace: {ex.StackTrace}");
                _logger.LogError("[EmailService] Error sending confirmation email: {Message} | {StackTrace}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task SendRejectionEmailAsync(string email, string userName, string eventTitle, string? reason = null)
        {
            try
            {
                var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortStr = _configuration["Email:SmtpPort"] ?? "587";
                var senderEmail = _configuration["Email:SenderEmail"] ?? "noreply@gestionevenements.com";
                var senderPassword = _configuration["Email:SenderPassword"] ?? "";

                Console.WriteLine($"[EmailService - DEBUG] ======= REJECTION EMAIL DEBUG =======");
                Console.WriteLine($"[EmailService - DEBUG] Recipient Email: {email}");
                Console.WriteLine($"[EmailService - DEBUG] SMTP Server: {smtpServer}");
                Console.WriteLine($"[EmailService - DEBUG] SMTP Port: {smtpPortStr}");
                Console.WriteLine($"[EmailService - DEBUG] Sender Email: {senderEmail}");
                Console.WriteLine($"[EmailService - DEBUG] Sender Password Configured: {!string.IsNullOrEmpty(senderPassword)}");
                Console.WriteLine($"[EmailService - DEBUG] User Name: {userName}");
                Console.WriteLine($"[EmailService - DEBUG] Event Title: {eventTitle}");

                _logger.LogInformation("[EmailService] Attempting to send rejection email to {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine($"[EmailService - ERROR] Recipient email is empty!");
                    _logger.LogError("[EmailService] Recipient email is empty");
                    return;
                }

                if (string.IsNullOrEmpty(senderPassword))
                {
                    Console.WriteLine($"[EmailService - ERROR] Sender password not configured!");
                    _logger.LogWarning("[EmailService] Sender password not configured");
                    return;
                }

                int smtpPort = int.Parse(smtpPortStr);

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Timeout = 10000; // 10 seconds timeout
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, "Gestion Événements"),
                        Subject = $"Statut de votre inscription - {eventTitle}",
                        Body = GenerateRejectionEmailBody(userName, eventTitle, reason),
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    Console.WriteLine($"[EmailService - DEBUG] Sending rejection email now...");
                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine($"[EmailService - SUCCESS] Rejection email sent successfully to {email}");
                    _logger.LogInformation("[EmailService] Rejection email sent successfully to {Email}", email);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService - ERROR] Exception occurred: {ex.GetType().Name}");
                Console.WriteLine($"[EmailService - ERROR] Message: {ex.Message}");
                Console.WriteLine($"[EmailService - ERROR] Stack Trace: {ex.StackTrace}");
                _logger.LogError("[EmailService] Error sending rejection email: {Message} | {StackTrace}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        private string GenerateConfirmationEmailBody(string userName, string eventTitle, string eventDate, string eventLocation)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #1e40af 0%, #ec4899 100%); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .event-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #1e40af; }}
        .info-row {{ display: flex; margin: 10px 0; }}
        .label {{ font-weight: bold; width: 120px; color: #1e40af; }}
        .value {{ flex: 1; }}
        .footer {{ text-align: center; margin-top: 20px; color: #9ca3af; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Inscription Confirmée ✓</h1>
            <p>Votre demande a été acceptée</p>
        </div>
        <div class='content'>
            <p>Bonjour <strong>{userName}</strong>,</p>
            <p>Nous sommes heureux de vous confirmer que votre inscription à l'événement suivant a été acceptée :</p>
            <div class='event-info'>
                <div class='info-row'>
                    <span class='label'>Événement :</span>
                    <span class='value'><strong>{eventTitle}</strong></span>
                </div>
                <div class='info-row'>
                    <span class='label'>Date :</span>
                    <span class='value'><strong>{eventDate}</strong></span>
                </div>
                <div class='info-row'>
                    <span class='label'>Lieu :</span>
                    <span class='value'><strong>{eventLocation}</strong></span>
                </div>
            </div>
            <p>Vous êtes maintenant prêt à participer à cet événement. Assurez-vous d'arriver quelques minutes avant l'heure de début.</p>
            <p style='margin-top: 30px; color: #6b7280;'>Si vous avez des questions, n'hésitez pas à nous contacter.</p>
            <div class='footer'>
                <p>&copy; 2025 Gestion Événements. Tous droits réservés.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateRejectionEmailBody(string userName, string eventTitle, string? reason = null)
        {
            var reasonText = string.IsNullOrEmpty(reason) 
                ? "Votre inscription n'a pas pu être acceptée pour le moment." 
                : $"Raison : {reason}";

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #9ca3af; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Inscription Non Acceptée</h1>
        </div>
        <div class='content'>
            <p>Bonjour <strong>{userName}</strong>,</p>
            <p>Nous vous informons du statut de votre inscription pour l'événement <strong>{eventTitle}</strong>.</p>
            <p>{reasonText}</p>
            <p>Nous vous remercions de votre intérêt et vous invitons à consulter nos autres événements.</p>
            <div class='footer'>
                <p>&copy; 2025 Gestion Événements. Tous droits réservés.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}
