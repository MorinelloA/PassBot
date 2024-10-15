namespace PassBot.Services.Interfaces
{
    public interface IBotService
    {
        Task SendMessageToChannelAsync(ulong channelId, string message);
    }
}
