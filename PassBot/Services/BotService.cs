using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using System;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PassBot.Services
{
    public class BotService : IBotService
    {
        private readonly DiscordClient _discordClient;
        private readonly IConfiguration _config;
        private static readonly Random _random = new Random();

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

        public void RegisterMessageMonitoring()
        {
            _discordClient.MessageCreated += async (s, e) =>
            {
                if (e.Author.IsBot)
                    return;

                var triggerWords = _config.GetSection("TriggerWords").Get<List<string>>();

                // Normalize the incoming message (to lowercase and remove special characters)
                var normalizedMessage = ValidationUtils.NormalizeText(e.Message.Content);

                foreach (var word in triggerWords)
                {
                    // Normalize the trigger word (lowercase and remove special characters)
                    var normalizedWord = ValidationUtils.NormalizeText(word);

                    if (normalizedMessage.Contains(normalizedWord))
                    {
                        var monitorChannel = await _discordClient.GetChannelAsync(ulong.Parse(_config["MonitorChannelId"]));
                        var staffRole = e.Guild.GetRole(ulong.Parse(_config["StaffRoleId"]));

                        var messageLink = $"[Jump to message](https://discord.com/channels/{e.Guild.Id}/{e.Channel.Id}/{e.Message.Id})";
                        await monitorChannel.SendMessageAsync($"{staffRole.Mention}, potential app related issue detected in {e.Channel.Mention}: \nMentioned Word/Phrase: {word}\n\n{messageLink}");

                        break;
                    }
                }
            };
        }

        public void StartScheduler()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    DateTime now = DateTime.UtcNow;
                    // Check if it's the right time to send
                    if ((now.DayOfWeek == DayOfWeek.Monday || now.DayOfWeek == DayOfWeek.Wednesday || now.DayOfWeek == DayOfWeek.Friday) && now.Hour == 10)                    
                    {
                        await SendScheduledMessage();
                        // Wait a day to avoid sending multiple messages
                        await Task.Delay(TimeSpan.FromMinutes(60 * 24));
                    }

                    // Check every 30 minutes
                    await Task.Delay(TimeSpan.FromMinutes(30));
                }
            });
        }

        private async Task SendScheduledMessage()
        {
            var channelId = ulong.Parse(_config["WarningScheduleChannel"]);
            var channel = await _discordClient.GetChannelAsync(channelId);

            if (channel != null)
            {
                List<string> listOfWarnings = _config.GetSection("Warnings").Get<List<string>>();
                var message = "Reminder: " + listOfWarnings[_random.Next(listOfWarnings.Count)];
                await channel.SendMessageAsync(message);
            }
        }
    }
}
