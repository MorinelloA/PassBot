using DSharpPlus.Entities;
using PassBot.Models;

namespace PassBot.Services.Interfaces
{
    public interface IProfileService
    {
        Task<UserProfile> GetUserProfileAsync(string discordId);
        Task SetEmailAsync(DiscordUser user, string email);
        Task SetWalletAddressAsync(DiscordUser user, string walletAddress);
        Task<List<UserProfileWithPoints>> GetAllUserProfilesWithPointsAsync();
        Task<UserProfileWithPoints> GetUserProfileWithPointsByDiscordIdAsync(string discordId);
        Task AddProfileChangeLogAsync(ProfileChangeLog changeLog);
        Task<TimeSpan?> GetTimeUntilNextProfileChangeAsync(string discordId, string item);
        Task<DateTime?> GetLastChangeTimeAsync(string discordId, string itemName);
    }
}
