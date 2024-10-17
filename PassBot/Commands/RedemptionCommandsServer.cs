using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Models;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

namespace PassBot.Commands
{ 
    public class RedemptionCommandsServer : ApplicationCommandModule
    {
        private readonly IBotService _botService;
        private readonly IPointsService _pointsService;
        private readonly IRedemptionService _redemptionService;

        public RedemptionCommandsServer(IBotService botService, IPointsService pointsService, IRedemptionService redemptionService)
        {
            _botService = botService;
            _pointsService = pointsService;
            _redemptionService = redemptionService;
        }

        [SlashCommand("add-item", "Add an item for users to redeem.")]
        public async Task AddItemCommand(InteractionContext ctx,
            [Option("name", "The name of the item")] string name,
            [Option("cost", "Cost of the item")] long cost,
            [Option("month", "Expiration month # (optional)")] long? month = null,
            [Option("day", "Expiration day # (optional)")] long? day = null,
            [Option("year", "Expiration year (optional)")] long? year = null)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            DateTime? expiresOn = null;

            // Validate and construct the expiration date if provided
            if (month.HasValue && day.HasValue && year.HasValue)
            {
                try
                {
                    expiresOn = new DateTime((int)year.Value, (int)month.Value, (int)day.Value);
                }
                catch
                {
                    await ctx.CreateResponseAsync("Invalid expiration date provided.");
                    return;
                }
            }

            // Check if an active item with the same name exists
            bool exists = await _redemptionService.DoesActiveItemExistAsync(name);

            if (exists)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Error", $"An active item with the name '{name}' already exists.");
                return;
            }

            // Create the new item
            Item newItem = new Item
            {
                Name = name,
                Cost = cost,
                ExpiresOn = expiresOn,
                CreatedBy = ctx.User.Id.ToString(),
                CreatedOn = DateTime.UtcNow
            };

            // Add the item
            await _redemptionService.AddItemAsync(newItem);

            // Respond with success if item is added
            await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Item Added", $"Item '{name}' was added with a cost of {cost} points.");
        }

        [SlashCommand("remove-item", "Remove an item by marking it as expired.")]
        public async Task RemoveItemCommand(InteractionContext ctx, [Option("name", "The name of the item")] string name)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            // Check if the item exists and is active (non-expired)
            var activeItem = await _redemptionService.GetActiveItemByNameAsync(name);
            if (activeItem == null)
            {
                // No active item found with the given name
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Error!", $"No active item found with the name: {name}");
                return;
            }

            // Mark the item as expired by setting ExpiresOn to the current date and time
            await _redemptionService.ExpireItemAsync(activeItem.Id);

            // Send success message to the user
            await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"The item '{name}' has been successfully removed.");
        }
          
        [SlashCommand("view-open-redemptions", "View all open (unsent) redemptions.")]
        public async Task ViewOpenRedemptionsCommand(InteractionContext ctx)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            var openRedemptions = await _redemptionService.GetOpenRedemptionsAsync();
            await EmbedUtils.CreateAndSendOpenRedemptionsEmbed(ctx, openRedemptions);
        }

        [SlashCommand("view-user-redemptions", "View all redemptions made by a specific user.")]
        public async Task ViewUserRedemptionsCommand(InteractionContext ctx, [Option("user", "The user whose redemptions to view")] DiscordUser user)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            var userRedemptions = await _redemptionService.GetUserRedemptionsAsync(user.Id.ToString());
            await EmbedUtils.CreateAndSendUserRedemptionsEmbed(ctx, userRedemptions, user);
        }

        [SlashCommand("close-redemption", "Mark a redemption as completed.")]
        public async Task CloseRedemptionCommand(InteractionContext ctx, [Option("id", "The redemption ID to close")] string redemptionId)
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            var openRedemption = await _redemptionService.GetOpenRedemptionByIdAsync(redemptionId);

            if (openRedemption == null)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Redemption Not Found", $"No open redemption with the ID '{redemptionId}' exists.");
                return;
            }

            await _redemptionService.CloseRedemptionAsync(openRedemption.Id, ctx.User.Id.ToString());

            await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Redemption Closed", $"Redemption '{redemptionId}' has been successfully marked as sent.");
        }
    }
}
