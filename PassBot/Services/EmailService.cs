using Microsoft.Extensions.Configuration;
using PassBot.Services.Interfaces;
using System.Net.Mail;
using System.Net;

namespace PassBot.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        private SmtpClient SetupSmtpClient()
        {
            var smtp = new SmtpClient
            {
                Host = _configuration["EmailSettings:EmailSmtp"],
                Port = int.Parse(_configuration["EmailSettings:EmailPort"]),
                EnableSsl = bool.Parse(_configuration["EmailSettings:EmailSSL"]),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_configuration["EmailSettings:EmailAddress"], _configuration["EmailSettings:EmailPassword"])
            };

            return smtp;
        }

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string emailTemplate, string emailSubject, IEnumerable<string> toEmails)
        {
            if (toEmails == null || !toEmails.Any())
            {
                return;
            }

            //Added using to prevent memory leak
            using var smtp = SetupSmtpClient();

            var receipts = string.Join(",", toEmails);

            try
            {
                MailMessage message = new MailMessage();
                message.From = new MailAddress(_configuration["EmailSettings:EmailAddress"], _configuration["EmailSettings:MailFromName"]);

                foreach (var receipt in toEmails)
                {
                    message.To.Add(new MailAddress(receipt));
                }

                message.IsBodyHtml = true;
                message.Body = emailTemplate;
                message.Subject = emailSubject;

                await smtp.SendMailAsync(message);                
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}
