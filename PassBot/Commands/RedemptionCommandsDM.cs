using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Models;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

namespace PassBot.Commands
{ 
    public class RedemptionCommandsDM : ApplicationCommandModule
    {
        private readonly IPointsService _pointsService;
        private readonly IRedemptionService _redemptionService;

        public RedemptionCommandsDM(IPointsService pointsService, IRedemptionService redemptionService)
        {
            _pointsService = pointsService;
            _redemptionService = redemptionService;
        }

        [SlashCommand("view-items", "View all available items.")]
        public async Task ViewItemsCommand(InteractionContext ctx)
        {
            // Retrieve the list of non-expired items
            var items = await _redemptionService.GetNonExpiredItemsAsync();

            if (items == null || !items.Any())
            {
                // If no items are found, send an informative message
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Items Found", "There are currently no available items.");
                return;
            }

            // Sort items alphabetically by name and create the embed
            await EmbedUtils.CreateAndSendItemsListEmbed(ctx, items);
        }

        [SlashCommand("redeem-item", "Redeem an item by spending points.")]
        public async Task RedeemItemCommand(InteractionContext ctx, [Option("name", "The name of the item to redeem")] string itemName)
        {
            // Check if the item exists and is not expired
            var item = await _redemptionService.GetActiveItemByNameAsync(itemName);
            if (item == null)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Item Not Found", $"The item '{itemName}' does not exist or has expired.");
                return;
            }

            // Check if the user has enough points
            var userPoints = await _pointsService.GetUserPoints(ctx.User.Id.ToString());
            if (userPoints < item.Cost)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Insufficient Points", $"You don't have enough points to redeem '{item.Name}'. Required: {item.Cost}, Available: {userPoints}.");
                return;
            }

            // Redeem the item (save to Redemption table) and deduct points
            await _redemptionService.RedeemItemAsync(item, ctx.User.Id.ToString(), ctx.User.Username, item.Cost);

            // Update user points and log the redemption
            await _pointsService.AddPoints(ctx.User.Id.ToString(), ctx.User.Username, -item.Cost);
            await _pointsService.LogPointsAssignment(ctx.User.Id.ToString(), ctx.User.Username, ctx.User.Id.ToString(), ctx.User.Username, -item.Cost, $"Redeemed item: {item.Name}");

            // Send a success embed (no need to display the RedemptionId)
            await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Item Redeemed", $"You have successfully redeemed '{item.Name}' for {item.Cost} points.");
        }

        [SlashCommand("view-redemptions", "View all of your redemptions.")]
        public async Task ViewRedemptionsCommand(InteractionContext ctx)
        {
            var userRedemptions = await _redemptionService.GetUserRedemptionsAsync(ctx.User.Id.ToString());
            await EmbedUtils.CreateAndSendUserRedemptionsEmbed(ctx, userRedemptions, ctx.User);
        }
    }
}
