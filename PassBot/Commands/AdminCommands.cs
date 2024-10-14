using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Models;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using System.IO;

public class AdminCommands : ApplicationCommandModule
{
    private readonly ISpreadsheetService _spreadsheetService;
    private readonly IProfileService _profileService;

    public AdminCommands(IProfileService profileService, ISpreadsheetService spreadsheetService)
    {
        _profileService = profileService;
        _spreadsheetService = spreadsheetService;
    }

    [SlashCommand("generate-user-report", "Generates a user .xlsx report and sends it to you")]
    public async Task GenerateReportCommand(InteractionContext ctx)
    {
        var users = await _profileService.GetAllUserProfilesWithPoints(); // Fetch from database
        var stream = await _spreadsheetService.GenerateUserReport(users);

        var response = new DiscordInteractionResponseBuilder()
            .AddFile("pass_user_report.xlsx", stream);

        await ctx.CreateResponseAsync(response);
    }

    [SlashCommand("ping", "Checks if the bot is active")]
    public async Task PingCommand(InteractionContext ctx)
    {
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", "Server is active!");
    }
}
