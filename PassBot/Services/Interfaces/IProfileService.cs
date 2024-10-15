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
    }
}
