using DSharpPlus.Entities;
using PassBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
