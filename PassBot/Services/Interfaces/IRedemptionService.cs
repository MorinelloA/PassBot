using PassBot.Models;

namespace PassBot.Services.Interfaces
{
    public interface IRedemptionService
    {
        Task<bool> DoesActiveItemExistAsync(string name);
        Task AddItemAsync(Item item);
        Task<Item> GetActiveItemByNameAsync(string name);
        Task ExpireItemAsync(int itemId);
        Task<List<Item>> GetNonExpiredItemsAsync();
        Task RedeemItemAsync(Item item, string discordId, string discordUsername, long spent);
        Task<List<Redemption>> GetOpenRedemptionsAsync();
        Task<List<Redemption>> GetUserRedemptionsAsync(string discordId);
        Task<Redemption> GetOpenRedemptionByIdAsync(string redemptionId);
        Task CloseRedemptionAsync(int redemptionId, string sentByDiscordId);
    }
}
