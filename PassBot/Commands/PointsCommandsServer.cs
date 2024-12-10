using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

namespace PassBot.Commands
{ 
    public class PointsCommandsServer : ApplicationCommandModule
    {
        private readonly IBotService _botService;
        private readonly IPointsService _pointsService;
        private readonly IConfiguration _config;

        public PointsCommandsServer(IBotService botService, IPointsService pointsService, IConfiguration config)
        {
            _botService = botService;
            _pointsService = pointsService;
            _config = config;
        }

        [SlashCommand("remove-points", "Removes points from a specified user based on their mention.")]
        public async Task RemovePointsCommand(InteractionContext ctx,
            [Option("user", "The user to remove points from")] DiscordUser user,
            [Option("points", "The number of points to remove")] long pointsToRemove,
            [Option("message", "The reason for the points assignment")] string message)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            // Ensure the points to remove is positive
            if (pointsToRemove <= 0)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Please specify a valid number of points to remove");
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Please give a reason points are being removed");
                return;
            }

            long totalPoints = await _pointsService.GetUserPointsAsync(user.Id.ToString());

            if (totalPoints - pointsToRemove < 0)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"This would give {user.Username} negative points");
                return;
            }

            await _pointsService.UpdatePointsAsync(ctx.User, user, -pointsToRemove, message);
            totalPoints -= pointsToRemove;            

            await EmbedUtils.CreateAndSendUpdatePointsEmbed(ctx, user, -pointsToRemove, totalPoints, message);
        }

        [SlashCommand("add-points", "Adds points to a specified user.")]
        public async Task AddPointsCommand(InteractionContext ctx,
            [Option("user", "The user to give points to")] DiscordUser user,
            [Option("points", "The number of points to add")] long? pointsToAdd = null,
            [Choice("Answer Poll", "AnswerPoll")]
            [Choice("Attend a Call", "AttendCall")]
            [Choice("Attend Feedback Meeting", "AttendFeedbackMeeting")]
            [Choice("Beta Signup", "BetaSignup")]
            [Choice("Beta Testing", "BetaTesting")]
            [Choice("Have Call Question Answered", "HaveCallQuestionAnswered")]
            [Choice("Take a Survey", "TakeSurvey")]
            [Option("category", "Category for points")] string category = null,
            [Option("message", "Reason for points assignment")] string message = null)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            // Define a dictionary for mapping category keys to readable strings
            var categoryDisplayNames = new Dictionary<string, string>()
            {
                { "AnswerPoll", "Answer Poll" },
                { "AttendCall", "Attend a Call" },
                { "AttendFeedbackMeeting", "Attend Feedback Meeting" },
                { "BetaSignup", "Beta Signup" },
                { "BetaTesting", "Beta Testing" },
                { "HaveCallQuestionAnswered", "Have Call Question Answered" },
                { "TakeSurvey", "Take a Survey" }
            };

            string _message;

            if (!string.IsNullOrEmpty(category) && categoryDisplayNames.ContainsKey(category) && string.IsNullOrEmpty(message))
            {
                _message = categoryDisplayNames[category]; // Get the human-readable version of the category
            }
            else
            {
                _message = message; // Fallback to the custom message if no category is selected
            }

            if (pointsToAdd.HasValue && !string.IsNullOrEmpty(category))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"You cannot provide both points and a category");
                return;
            }

            long points = await _pointsService.GetPointsToAssignAsync(pointsToAdd, category);

            if (points <= 0)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Please specify a valid number of points to add");
                return;
            }

            long maxPointsAllowed = _config.GetValue<long>("MaxPointsAllowed");
            if (maxPointsAllowed < points)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"You can not assign this many points at once");
                return;
            }

            await _pointsService.UpdatePointsAsync(ctx.User, user, points, _message);

            long totalPoints = await _pointsService.GetUserPointsAsync(user.Id.ToString());            

            await EmbedUtils.CreateAndSendUpdatePointsEmbed(ctx, user, points, totalPoints, _message);
        }

        [SlashCommand("view-user-points", "View the total points of a specified user.")]
        public async Task ViewUserPointsCommand(InteractionContext ctx, [Option("user", "The user to view points for")] DiscordUser user)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            long points = await _pointsService.GetUserPointsAsync(user.Id.ToString());
            await EmbedUtils.CreateAndSendViewPointsEmbed(ctx, user, points);
        }

        /*
        [SlashCommand("gm", "This can only be done once every 23 hours. Check-in enough times to earn points!")]
        public async Task CheckIn(InteractionContext ctx)
        {
            var discordId = ctx.User.Id.ToString();
            var discordUsername = ctx.User.Discriminator == "0" ? ctx.User.Username : $"{ctx.User.Username}#{ctx.User.Discriminator}";

            // Check if the user can check-in
            var lastCheckin = await _pointsService.GetLastCheckInAsync(discordId);

            if (lastCheckin != null && lastCheckin.IsAllowed == false)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "You have already checked in", $"Please try again in {lastCheckin.RemainingTime.Value.Hours} hours and {lastCheckin.RemainingTime.Value.Minutes} minutes");
                return;
            }

            int checkinsToPoints = _config.GetValue<int>("CheckInTimes");
            long checkInPoints = _config.GetValue<long>("CheckInPoints");

            // Process the check-in and update points
            CheckInHelper cih = await _pointsService.CheckInUserAsync(discordId, discordUsername, lastCheckin);
            if (cih.didEarnPoints)
            {
                // Get the user's updated points balance
                long totalBalance = await _pointsService.GetUserPointsAsync(discordId);

                // Use the EmbedUtils method to send the embed
                await EmbedUtils.CreateAndSendCheckInEmbed(ctx, checkInPoints, totalBalance);
            }
            else
            {
                // Use the EmbedUtils method to send the embed
                await EmbedUtils.CreateAndSendCheckInIteratorEmbed(ctx, cih);
            }
        }*/

        [SlashCommand("clear-points", "Clears all points from the system.")]
        public async Task ClearPointsCommand(InteractionContext ctx)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            // Create a confirmation button
            var confirmButton = new DiscordButtonComponent(ButtonStyle.Danger, "confirm_clear_points", "Confirm", false);

            // Create a message with the confirmation button using DiscordInteractionResponseBuilder
            var builder = new DiscordInteractionResponseBuilder()
                .WithContent("Are you sure you want to clear all points?")
                .AddComponents(confirmButton);

            // Send the message (no need to store the result, as it returns void)
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);

            // Get the interactivity module
            var interactivity = ctx.Client.GetInteractivity();

            // Fetch the original response message to wait for button click
            var confirmationMessage = await ctx.GetOriginalResponseAsync();

            // Wait for the user to click the confirm button (timeout after 60 seconds)
            var result = await interactivity.WaitForButtonAsync(confirmationMessage, ctx.User, TimeSpan.FromSeconds(60));

            if (result.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Confirmation timed out. No changes were made."));
                return;
            }

            if (result.Result.Id == "confirm_clear_points")
            {
                // User confirmed the action
                await _pointsService.TruncateUserPointsTableAsync(ctx.User.Id.ToString());

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("All points have been cleared."));
            }
        }

    }
}
