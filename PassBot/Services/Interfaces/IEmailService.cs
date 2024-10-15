namespace PassBot.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string emailTemplate, string emailSubject, IEnumerable<string> toEmails);
    }
}
