using PassBot.Models;

namespace PassBot.Services.Interfaces
{
    public interface ISpreadsheetService
    {
        Task<MemoryStream> GenerateUserReportAsync(List<UserProfileWithPoints> users);
    }
}
