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
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailService(IConfiguration configuration)
        {
            var emailConfig = configuration.GetSection("Email");
            _smtpHost = emailConfig["SmtpHost"];
            _smtpPort = int.Parse(emailConfig["SmtpPort"]);
            _smtpUser = emailConfig["SmtpUser"];
            _smtpPass = emailConfig["SmtpPass"];
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
