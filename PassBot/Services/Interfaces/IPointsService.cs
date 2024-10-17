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
        Task TruncateUserPointsTableAsync(string removerDiscordId);
        Task<(bool IsAllowed, TimeSpan? RemainingTime)> CanCheckInAsync(string discordId);
        Task CheckInUserAsync(string discordId, string discordUsername);
        Task<DateTime?> GetLastCheckInAsync(string discordId);
        Task UpdateCheckInTimeAsync(string discordId, string discordUsername);
        Task<List<UserPointsLog>> GetUserPointsLogByDiscordIdAsync(string discordId, bool includeRemoved = true);
    }
}
