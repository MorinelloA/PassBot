using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassBot.Services.Interfaces
{
    public interface IPointsService
    {
        Task<long> GetPointsToAssign(long? pointsToAdd, string category);
        Task UpdatePoints(DiscordUser assigner, DiscordUser user, long points, string message);
        Task AddPoints(string discordId, string discordUsername, long points);
        Task<long> GetUserPoints(string discordId);
        Task LogPointsAssignment(string discordId, string discordUsername, string assignerId, string assignerUsername, long points, string message = null);
        Task TruncateUserPointsTable(string removerDiscordId);



        Task<(bool IsAllowed, TimeSpan? RemainingTime)> CanCheckIn(string discordId);
        Task CheckInUser(string discordId, string discordUsername);
        Task<DateTime?> GetLastCheckIn(string discordId);
        Task UpdateCheckInTime(string discordId, string discordUsername);
    }
}
