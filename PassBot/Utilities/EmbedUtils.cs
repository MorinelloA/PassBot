using DocumentFormat.OpenXml.Bibliography;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PassBot.Utilities
{
    public static class EmbedUtils
    {
        public static DiscordEmbedBuilder CreateMonthlyPassPerksKPIEmbed(InteractionContext ctx, MonthlyKPIHelper kpis)
        {
            // Create an embed builder for the points response
            var embed = new DiscordEmbedBuilder
            {
                Title = $@"KPIs for {kpis.month}-{kpis.year}",
                Description = $"Total Users: {kpis.totalUsers}",
                Color = DiscordColor.Azure // You can customize the color here
            };

            embed.AddField("Active users", $"{kpis.activeUsers}", true);
            embed.AddField("Points Distributed", $"{kpis.pointsDistributed}", true);
            embed.AddField("Actions", $"{kpis.actions}", true);
            embed.AddField("Polls", $"{kpis.polls}", true);
            embed.AddField("Poll Responses", $"{kpis.pollResponses}", true);
            embed.AddField("Average responses per poll", $"{kpis.averageNumOfPollResponses:0.0}", true);

            return embed;

        }


        public static DiscordEmbedBuilder CreateProfileValidationEmbed(InteractionContext ctx, DiscordUser user, List<UserProfileWithPoints> validatedProfiles, List<UserCheckError> errors, bool ephemeral = true)
        {
            int validatedCount = validatedProfiles == null ? 0 : validatedProfiles.Count;
            int errosCount = errors == null ? 0 : errors.Count;

            var embed = new DiscordEmbedBuilder
            {
                Title = "",//$"{profile.DiscordUsername}'s Profile",
                Color = DiscordColor.Azure
            };

            if (validatedCount <= 0 && errosCount <= 0)
            {
                embed.Title = "No profiled validated";
            }
            else if(errors.Count <= 0)
            {
                embed.Title = $"All {validatedCount} profiles validated!";
            }
            else
            {
                embed.Title = $"{validatedCount} profiles validated, {errosCount} had errors";
            }

            if (errors.Count > 0)
            {
                for (int i = 0; i < errosCount; i++)
                {
                    // Display each field on a separate line
                    embed.AddField($"{(errors[i]?.user?.DiscordUsername ?? "N/A")} : {(errors[i]?.user?.DiscordId ?? "N/A")}", errors[i]?.error ?? "N/A", false);
                }
            }

            return embed;
        }

        public static async Task CreateAndSendUpdatePointsEmbed(InteractionContext ctx, DiscordUser user, long points, long totalPoints, string message = "", bool ephemeral = false)
        {
            // Create the base description for the points response
            string description = points > 0
                ? $"{points} points added to {user.Username}"
                : $"{-points} points removed from {user.Username}";

            // If a message is provided, append it below the description
            if (!string.IsNullOrEmpty(message))
            {
                description = $"**{message}**\n{description}";
            }

            // Create an embed builder for the points response
            var embed = new DiscordEmbedBuilder
            {
                Title = points > 0 ? "Points Added" : "Points Removed",
                Description = description,
                Color = DiscordColor.Azure // You can customize the color here
            };

            // Add additional fields for balance or any other info you want to include
            embed.AddField("Balance", $"{totalPoints} points", true);

            // Add the user's avatar to the embed
            embed.WithThumbnail(user.AvatarUrl);

            // Send a notification message mentioning the user first
            await ctx.Channel.SendMessageAsync(points > 0 ? $"{user.Mention}, you have earned points!" : $"{user.Mention}, you have lost points :(");

            // Then send the embed as a follow-up
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));

        }



        /*public static async Task CreateAndSendUpdatePointsEmbed(InteractionContext ctx, DiscordUser user, long points, long totalPoints, string message = "")
        {
            // Create the base description for the points response
            string description = points > 0
                ? $"{points} points added to {user.Mention}"
                : $"{-points} points removed from {user.Mention}";

            // If a message is provided, append it below the description
            if (!string.IsNullOrEmpty(message))
            {
                description = $"**{message}**\n{description}";
            }

            await ctx.CreateResponseAsync($"{user.Mention}, you have been assigned points!");

            // Create an embed builder for the points response
            var embed = new DiscordEmbedBuilder
            {
                Title = points > 0 ? "Points Added" : "Points Removed",
                Description = description,
                Color = DiscordColor.Azure // You can customize the color here
            };

            // Add additional fields for balance or any other info you want to include
            embed.AddField("Balance", $"{totalPoints} points", true);

            // Add the user's avatar to the embed
            embed.WithThumbnail(user.AvatarUrl);

            // Send the embed response
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .AddEmbed(embed));
        }*/


        public static async Task CreateAndSendViewPointsEmbed(InteractionContext ctx, DiscordUser user, long points, bool ephemeral = true)
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
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static async Task CreateAndSendProfileFieldEmbed(InteractionContext ctx, DiscordUser user, string value, string field, bool ephemeral = true)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = $"{field} for {user.Username}",
                Description = string.IsNullOrEmpty(value) ? "Not set" : value,
                Color = DiscordColor.Azure
            };

            // Use the recipient's avatar as the thumbnail
            embed.WithThumbnail(user.AvatarUrl);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static async Task CreateAndSendFullProfileEmbed(InteractionContext ctx, DiscordUser user, UserProfileWithPoints profile, bool ephemeral = true)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = $"{profile.DiscordUsername}'s Profile",
                Color = DiscordColor.Azure
            };

            // Display each field on a separate line
            embed.AddField("Email", string.IsNullOrEmpty(profile.Email) ? "Not set" : profile.Email, false);
            embed.AddField("Wallet Address", string.IsNullOrEmpty(profile.WalletAddress) ? "Not set" : profile.WalletAddress, false);
            embed.AddField("X Account", string.IsNullOrEmpty(profile.XAccount) ? "Not set" : profile.XAccount, false);
            embed.AddField("Points Balance", $"{profile.Points} points", false);

            // Use the recipient's avatar as the thumbnail
            embed.WithThumbnail(user.AvatarUrl);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static async Task CreateAndSendCheckInEmbed(InteractionContext ctx, long checkInPoints, long totalBalance, bool ephemeral = false)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Check-in Successful",
                Description = $"{ctx.User.Mention} has checked in and received {checkInPoints} points!",
                Color = DiscordColor.Green
            };

            // Display the user's updated balance
            embed.AddField("Total Balance", $"{totalBalance} points", true);

            // Use the user's avatar as the thumbnail
            embed.WithThumbnail(ctx.User.AvatarUrl);

            // Respond with the embed message
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        /*
        public static async Task CreateAndSendCheckInIteratorEmbed(InteractionContext ctx, CheckInHelper cih, bool ephemeral = false)
        {
            int overallNeededCheckins = cih.overallNeededCheckins;

            var embed = new DiscordEmbedBuilder
            {
                Title = "Check-in Successful",
                Description = $"{ctx.User.Mention} has checked in {cih.currentIterator} time(s).",
                Color = DiscordColor.Green
            };

            // Display the user's updated balance
            embed.AddField($"Check-ins until {cih.pointsCanEarn} points", $"{cih.checkinsToPoints}", true);

            // Use the user's avatar as the thumbnail
            embed.WithThumbnail(ctx.User.AvatarUrl);

            // Respond with the embed message
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }
        */

        
        public static async Task CreateAndSendCheckInIteratorEmbed(InteractionContext ctx, CheckInHelper cih, bool ephemeral = false)
        {
            int overallNeededCheckins = cih.overallNeededCheckins;
            int currentCheckins = cih.currentIterator;

            // Calculate progress percentage
            double progressPercentage = (double)currentCheckins / overallNeededCheckins;

            // Create a progress bar out of 10 blocks (e.g. "█████░░░░░")
            int progressBlocks = (int)(progressPercentage * 10);
            string progressBar = new string('█', progressBlocks) + new string('░', 10 - progressBlocks);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Check-in Successful",
                Description = currentCheckins == 1 ? $"{ctx.User.Mention} has checked in {currentCheckins} time." : $"{ctx.User.Mention} has checked in {currentCheckins} times.",
                Color = DiscordColor.Green
            };

            // Add the progress bar
            embed.AddField($"Progress to {cih.pointsCanEarn} points", $"{progressBar} ({currentCheckins}/{overallNeededCheckins} check-ins)", true);

            // Display the user's updated balance and how many check-ins are needed
            //embed.AddField($"Check-ins until {cih.pointsCanEarn} points", $"{cih.checkinsToPoints}", true);

            // Use the user's avatar as the thumbnail
            embed.WithThumbnail(ctx.User.AvatarUrl);

            // Respond with the embed message
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static async Task CreateAndSendWarningEmbed(InteractionContext ctx, string title, string description, bool ephemeral = true)
        {
            var embed = CreateWarningEmbed(title, description);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static async Task CreateAndSendSuccessEmbed(InteractionContext ctx, string title, string description, bool ephemeral = false)
        {
            var embed = CreateSuccessEmbed(title, description);
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static DiscordEmbedBuilder CreateSuccessEmbed(string title, string description)
        {
            return new DiscordEmbedBuilder
            {
                Title = title,
                Description = description,
                Color = DiscordColor.Green
            };
        }

        public static DiscordEmbedBuilder CreateWarningEmbed(string title, string description)
        {
            return new DiscordEmbedBuilder
            {
                Title = title,
                Description = description,
                Color = DiscordColor.Orange
            };
        }

        public static async Task CreateAndSendItemsListEmbed(InteractionContext ctx, List<Item> items, bool ephemeral = false)
        {
            // Create an embed builder
            var embed = new DiscordEmbedBuilder
            {
                Title = "Available Items",
                Description = "Here are the currently available items:",
                Color = DiscordColor.Azure
            };

            // Add each item as a field in the embed
            foreach (var item in items)
            {
                embed.AddField(item.Name, $"Cost: {item.Cost} points", false); // False means the fields will appear in a list
            }

            // Send the embed response
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static async Task CreateAndSendOpenRedemptionsEmbed(InteractionContext ctx, List<Redemption> openRedemptions, bool ephemeral = true)
        {
            // Check if there are any open redemptions
            if (openRedemptions == null || !openRedemptions.Any())
            {
                await CreateAndSendWarningEmbed(ctx, "No Open Redemptions", "There are currently no open redemptions.");
                return;
            }

            // Create the embed
            var embed = new DiscordEmbedBuilder
            {
                Title = "Open Redemptions",
                Color = DiscordColor.Azure
            };

            // Add each redemption to the embed
            foreach (var redemption in openRedemptions)
            {
                var redemptionInfo = $@"
                    **ID**: {redemption.RedemptionId}
                    **Username**: {redemption.DiscordUsername}
                    **Claimed On**: {redemption.ClaimedOn:yyyy-MM-dd HH:mm:ss}";

                embed.AddField(redemption.ItemName, redemptionInfo, false);
            }

            // Send the embed
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static async Task CreateAndSendUserRedemptionsEmbed(InteractionContext ctx, List<Redemption> userRedemptions, DiscordUser user, bool ephemeral = true)
        {
            // Check if the user has any redemptions
            if (userRedemptions == null || !userRedemptions.Any())
            {
                await CreateAndSendWarningEmbed(ctx, "No Redemptions Found", $"{user.Username} has not made any redemptions.");
                return;
            }

            // Create the embed
            var embed = new DiscordEmbedBuilder
            {
                Title = $"{user.Username}'s Redemptions",
                Color = DiscordColor.Azure
            };

            // Add each redemption to the embed
            foreach (var redemption in userRedemptions)
            {
                var redemptionStatus = redemption.SentOn == null ? "Not Filled" : "Filled";
                var redemptionInfo = $@"
                    **ID**: {redemption.RedemptionId}
                    **Claimed On**: {redemption.ClaimedOn:yyyy-MM-dd}
                    **Status**: {redemptionStatus}";

                embed.AddField(redemption.ItemName, redemptionInfo, false);
            }

            // Send the embed
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static async Task CreateAndSendCommandListEmbed(InteractionContext ctx, List<(string Command, string Description)> commands, bool ephemeral = false)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Available Commands",
                Description = "Here is a list of commands you may use:",
                Color = DiscordColor.Azure
            };

            // Add fields for each command with its description
            foreach (var (command, description) in commands)
            {
                embed.AddField($"/{command}", description, inline: false);
            }

            // Send the embed response
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(ephemeral));
        }

        public static List<(string Command, string Description)> GetAvailableCommands()
        {
            return new List<(string Command, string Description)>
            {
                
                //("gm", "Check-in. This can only be done once every 23 hours."),

                ("view-profile", "Views your profile."),
                ("view-points", "View your total points."),
                ("set-email", "Set your Pass email address."),
                ("view-email", "View your Pass email address."),
                ("set-wallet-address", "Set your Pass wallet address."),
                ("view-wallet", "View your Pass wallet address."),
                ("set-x-account", "Set your X account."),
                ("view-x-account", "View your X account."),

                /*
                ("view-items", "View all available items for redemption."),
                ("redeem-item", "Redeem an item by spending points."),
                ("view-redemptions", "View all of your redemptions."),
                */

                ("view-commands", "Shows you the list of commands you may use.")
            };
        }

        public static List<(string Command, string Description)> GetAvailableAdminCommands()
        {
            return new List<(string Command, string Description)>
            {

                ("add-points", "Adds points to a specified user."),
                ("remove-points", "Removes points from a specified user."),
                ("clear-points", "Clears all points from the system."),
                ("view-user-points", "View the total points of a specified user."),

                ("set-user-email", "Set the email address of a specified user."),
                ("view-user-email", "View the email address of a specified user."),
                ("set-user-wallet-address", "Set the wallet address of a specified user."),
                ("view-user-wallet", "View the total points of a specified user."),
                ("set-user-x-account", "Set the X account of a specified user."),
                ("view-user-x-account", "View the X account of a specified user."),

                ("view-user-profile", "Views the profile of a specified user."),

                /*
                ("add-item", "Add an item for users to redeem."),
                ("remove-item", "Remove an item by marking it as expired."),
                ("view-open-redemptions", "View all open (unsent) redemptions."),
                ("view-user-redemptions", "View all redemptions made by a specific user."),
                ("close-redemption", "Mark a redemption as completed."),
                */

                ("generate-points-report", "Generates an .xlsx report of current points for every user and sends it to you."),
                ("generate-user-report", "Generates an .xlsx report of all point actions for a specific user and sends it to you."),

                ("ping", "Checks if the bot is active."),
                ("view-admin-commands", "Shows you the list of admin commands you may use.")
            };
        }
    }
}
