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
            long points = await _pointsService.GetUserPoints(ctx.User.Id.ToString());
            await EmbedUtils.CreateAndSendViewPointsEmbed(ctx, ctx.User, points);
        }
    }
}
