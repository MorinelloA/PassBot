using DSharpPlus.Entities;
using PassBot.Models;

namespace PassBot.Services.Interfaces
{
    public interface IPointsService
    {
        Task<long> GetPointsToAssignAsync(long? pointsToAdd, string category);
        Task UpdatePointsAsync(DiscordUser assigner, DiscordUser user, long points, string message);
        Task AddPointsAsync(string discordId, string discordUsername, long points);
        Task<long> GetUserPointsAsync(string discordId);
        Task LogPointsAssignmentAsync(string discordId, string discordUsername, string assignerId, string assignerUsername, long points, string message = null);
        Task DeleteUserPointsOfUsersFromListAsync(List<UserProfileWithPoints> users, string removerDiscordId);
        Task TruncateUserPointsTableAsync(string removerDiscordId);
        Task<CheckInHelper> CheckInUserAsync(string discordId, string discordUsername, CheckInLog? lastCheckIn);
        Task<CheckInLog?> GetLastCheckInAsync(string discordId);
        Task UpdateCheckInTimeAsync(string discordId, string discordUsername);
        Task ResetCheckInTimeAsync(string discordId, string discordUsername);
        Task<List<UserPointsLog>> GetUserPointsLogByDiscordIdAsync(string discordId, bool includeRemoved = true);
    }
}
