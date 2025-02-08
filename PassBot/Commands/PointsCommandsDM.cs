using DSharpPlus.SlashCommands;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

namespace PassBot.Commands
{ 
    public class PointsCommandsDM : ApplicationCommandModule
    {
        private readonly IPointsService _pointsService;

        public PointsCommandsDM(IPointsService pointsService)
        {
            _pointsService = pointsService;
        }

        [SlashCommand("view-points", "View your total points.")]
        public async Task ViewMyPointsCommand(InteractionContext ctx)
        {
            //TODO: Create a call that contains both points and transferred points to avoid 2 calls
            long points = await _pointsService.GetUserPointsAsync(ctx.User.Id.ToString());
            long transferredPoints = await _pointsService.GetUserTransferredPointsAsync(ctx.User.Id.ToString());
            await EmbedUtils.CreateAndSendViewPointsEmbed(ctx, ctx.User, points, transferredPoints, true);
        }
    }
}
