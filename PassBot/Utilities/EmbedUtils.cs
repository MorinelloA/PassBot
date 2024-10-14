using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Models;

namespace PassBot.Utilities
{
    public static class EmbedUtils
    {
        public static async Task CreateAndSendUpdatePointsEmbed(InteractionContext ctx, DiscordUser user, long points, long totalPoints)
        {
            // Create an embed builder for the points response
            var embed = new DiscordEmbedBuilder
            {
                Title = points > 0 ? "Points Added" : "Points Removed",
                Description = points > 0 ? $"{points} points added to {user.Mention}" : $"{-points} points removed from {user.Mention}",
                Color = DiscordColor.Azure // You can customize the color here
            };

            // Add additional fields for balance or any other info you want to include
            embed.AddField("Balance", $"{totalPoints} points", true);

            // Add the user's avatar to the embed
            embed.WithThumbnail(user.AvatarUrl);

            // Send the embed response
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .AddEmbed(embed));
        }

        public static async Task CreateAndSendViewPointsEmbed(InteractionContext ctx, DiscordUser user, long points)
        {
            // Create an embed builder for the points response
            var embed = new DiscordEmbedBuilder
            {
                Title = "Points Summary",
                Description = $"{user.Mention} has {points} points",
                Color = DiscordColor.Azure // You can customize the color here
            };

            // Add the user's avatar to the embed
            embed.WithThumbnail(user.AvatarUrl);

            // Send the embed response
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .AddEmbed(embed));
        }

        public static async Task CreateAndSendProfileFieldEmbed(InteractionContext ctx, DiscordUser user, string value, string field)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = $"{field} for {user.Username}",
                Description = string.IsNullOrEmpty(value) ? "Not set" : value,
                Color = DiscordColor.Azure
            };

            // Use the recipient's avatar as the thumbnail
            embed.WithThumbnail(user.AvatarUrl);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        public static async Task CreateAndSendFullProfileEmbed(InteractionContext ctx, DiscordUser user, UserProfileWithPoints profile)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = $"{profile.DiscordUsername}'s Profile",
                Color = DiscordColor.Azure
            };

            // Display each field on a separate line
            embed.AddField("Email", string.IsNullOrEmpty(profile.Email) ? "Not set" : profile.Email, false);
            embed.AddField("Wallet Address", string.IsNullOrEmpty(profile.WalletAddress) ? "Not set" : profile.WalletAddress, false);
            embed.AddField("Points Balance", $"{profile.Points} points", false);

            // Use the recipient's avatar as the thumbnail
            embed.WithThumbnail(user.AvatarUrl);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        public static async Task CreateAndSendCheckInEmbed(InteractionContext ctx, long totalBalance)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Check-in Successful",
                Description = $"{ctx.User.Mention} has checked in and received 5 points!",
                Color = DiscordColor.Green
            };

            // Display the user's updated balance
            embed.AddField("Total Balance", $"{totalBalance} points", true);

            // Use the user's avatar as the thumbnail
            embed.WithThumbnail(ctx.User.AvatarUrl);

            // Respond with the embed message
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        public static async Task CreateAndSendWarningEmbed(InteractionContext ctx, string title, string description)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = title,
                Description = description,
                Color = DiscordColor.Orange
            };

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        public static async Task CreateAndSendSuccessEmbed(InteractionContext ctx, string title, string description)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = title,
                Description = description,
                Color = DiscordColor.Green
            };

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}
