namespace SIAMS.Services
{
    public class EmailConfig
    {
        public required string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public required string SmtpUser { get; set; }
        public required string SmtpPass { get; set; }

        // Parameterized Constructor
        public EmailConfig(string smtpHost, int smtpPort, string smtpUser, string smtpPass)
        {
            SmtpHost = smtpHost;
            SmtpPort = smtpPort;
            SmtpUser = smtpUser;
            SmtpPass = smtpPass;
        }
    }
}
