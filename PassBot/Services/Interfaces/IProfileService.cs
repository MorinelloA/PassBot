using DSharpPlus.Entities;
using PassBot.Models;

namespace PassBot.Services.Interfaces
{
    public interface IProfileService
    {
        Task<UserProfile> GetUserProfile(string discordId);
        Task SetEmail(DiscordUser user, string email);
        Task SetWalletAddress(DiscordUser user, string walletAddress);
        Task<List<UserProfileWithPoints>> GetAllUserProfilesWithPoints();
        Task<UserProfileWithPoints> GetUserProfileWithPointsByDiscordId(string discordId);
        Task AddProfileChangeLogAsync(ProfileChangeLog changeLog);
        Task<TimeSpan?> GetTimeUntilNextProfileChangeAsync(string discordId, string item);
        Task<DateTime?> GetLastChangeTimeAsync(string discordId, string itemName);
    }
}
