using DSharpPlus.Entities;

namespace PassBot.Services.Interfaces
{
    public interface IBotService
    {
        Task SendMessageToChannelAsync(ulong channelId, string message);
        bool HasPermission(DiscordUser user);
        void RegisterMessageMonitoring();
    }
}
