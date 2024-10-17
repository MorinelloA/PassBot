using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using PassBot.Services.Interfaces;

namespace PassBot.Services
{
    public class BotService : IBotService
    {
        private readonly DiscordClient _discordClient;
        private readonly IConfiguration _config;

        public BotService(DiscordClient discordClient, IConfiguration config)
        {
            _discordClient = discordClient;
            _config = config;
        }

        public async Task SendMessageToChannelAsync(ulong channelId, string message)
        {
            var channel = await _discordClient.GetChannelAsync(channelId);
            if (channel != null)
            {
                await channel.SendMessageAsync(message);
            }
            else
            {
                Console.WriteLine("Channel not found.");
            }
        }

        public bool HasPermission(DiscordUser user)
        {
            // Check if BackendPermissionCheck is enabled
            bool isPermissionCheckEnabled = _config.GetValue<bool>("BackendPermissionCheck");
            if (!isPermissionCheckEnabled)
                return true; // No permission check needed

            // Retrieve the list of allowed Discord IDs from config
            var allowedDiscordIds = _config.GetSection("PermissionCheckDiscordIds").Get<List<string>>();

            // Check if the user's ID is in the allowed list
            return allowedDiscordIds.Contains(user.Id.ToString());
        }
    }
}
