using PassBot.Models;

namespace PassBot.Services.Interfaces
{
    public interface ISpreadsheetService
    {
        Task<MemoryStream> GenerateUserReport(List<UserProfileWithPoints> users);
    }
}
