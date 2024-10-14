using DocumentFormat.OpenXml.Spreadsheet;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using PassBot.Models;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PassBot.Commands
{ 
    public class PointsCommandsServer : ApplicationCommandModule
    {
        private readonly IPointsService _pointsService;

        public PointsCommandsServer(IPointsService pointsService)
        {
            _pointsService = pointsService;
        }

        [SlashCommand("remove-points", "Removes points from a specified user based on their mention.")]
        public async Task RemovePointsCommand(InteractionContext ctx,
            [Option("user", "The user to remove points from")] DiscordUser user,
            [Option("points", "The number of points to remove")] long pointsToRemove,
            [Option("message", "The reason for the points assignment")] string message = null)
        {
            // Ensure the points to remove is positive
            if (pointsToRemove <= 0)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Please specify a valid number of points to remove");
                return;
            }

            await _pointsService.UpdatePoints(ctx.User, user, -pointsToRemove, message);

            long totalPoints = await _pointsService.GetUserPoints(user.Id.ToString());

            await EmbedUtils.CreateAndSendUpdatePointsEmbed(ctx, ctx.User, -pointsToRemove, totalPoints);
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
            if (pointsToAdd.HasValue && !string.IsNullOrEmpty(category))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"You cannot provide both points and a category");
                return;
            }

            long points = await _pointsService.GetPointsToAssign(pointsToAdd, category);

            if (points <= 0)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Please specify a valid number of points to add");
                return;
            }

            await _pointsService.UpdatePoints(ctx.User, user, points, message);

            long totalPoints = await _pointsService.GetUserPoints(user.Id.ToString());

            await EmbedUtils.CreateAndSendUpdatePointsEmbed(ctx, ctx.User, points, totalPoints);
        }

        [SlashCommand("view-user-points", "View the total points of a specified user.")]
        public async Task ViewUserPointsCommand(InteractionContext ctx, [Option("user", "The user to view points for")] DiscordUser user)
        {
            long points = await _pointsService.GetUserPoints(user.Id.ToString());
            await EmbedUtils.CreateAndSendViewPointsEmbed(ctx, user, points);
        }

        [SlashCommand("check-in", "Check-in to get points. This can only be done once every 23 hours.")]
        public async Task CheckIn(InteractionContext ctx)
        {
            var discordId = ctx.User.Id.ToString();
            var discordUsername = ctx.User.Discriminator == "0" ? ctx.User.Username : $"{ctx.User.Username}#{ctx.User.Discriminator}";

            // Check if the user can check-in
            var (isAllowed, remainingTime) = await _pointsService.CanCheckIn(discordId);

            if (!isAllowed && remainingTime.HasValue)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "You have already checked in", $"Please try again in {remainingTime.Value.Hours} hours and {remainingTime.Value.Minutes} minutes");
                return;
            }

            // Process the check-in and update points
            await _pointsService.CheckInUser(discordId, discordUsername);

            // Get the user's updated points balance
            long totalBalance = await _pointsService.GetUserPoints(discordId);

            // Use the EmbedUtils method to send the embed
            await EmbedUtils.CreateAndSendCheckInEmbed(ctx, totalBalance);
        }

        [SlashCommand("clear-points", "Clears all points from the system.")]
        public async Task ClearPointsCommand(InteractionContext ctx)
        {
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
                await _pointsService.TruncateUserPointsTable(ctx.User.Id.ToString());

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("All points have been cleared."));
            }
        }

    }
}
