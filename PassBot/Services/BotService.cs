using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Data.SqlClient;
using PassBot.Services.Interfaces;

namespace PassBot.Services
{
    public class BotService : IBotService
    {
        private readonly DiscordClient _discordClient;

        public BotService(DiscordClient discordClient)
        {
            _discordClient = discordClient;
        }

        public async Task SendMessageToChannelAsync(ulong channelId, string message)
        {
            try
            {
                // Retrieve the channel
                var channel = await _discordClient.GetChannelAsync(channelId);
                if (channel != null)
                {
                    // Send the message to the specified channel
                    await channel.SendMessageAsync(message);
                }
                else
                {
                    Console.WriteLine("Channel not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error");
            }
        }
    }
}
