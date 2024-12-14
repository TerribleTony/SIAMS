using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace SIAMS.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly string _smtpHost = string.Empty;
        private readonly int _smtpPort = 587; // default SMTP port
        private readonly string _smtpUser = string.Empty;
        private readonly string _smtpPass = string.Empty;

        public EmailService(IConfiguration configuration)
        {
            var emailConfig = configuration.GetSection("Email");
            _smtpHost = emailConfig["SmtpHost"] 
                ?? throw new ArgumentNullException(nameof(configuration), "SmtpHost parameter cannot be null.");
            _smtpPort = int.Parse(emailConfig["SmtpPort"] ?? throw new ArgumentNullException(nameof(configuration), "_smtpPort parameter cannot be null"));
            _smtpUser = emailConfig["SmtpUser"] ?? throw new ArgumentNullException(nameof(configuration), "_smtpUser parameter cannot be null.");
            _smtpPass = emailConfig["SmtpPass"] ?? throw new ArgumentNullException(nameof(configuration), "_smtpPass parameter cannot be null.");
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpUser),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
