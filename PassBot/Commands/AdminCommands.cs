using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

public class AdminCommands : ApplicationCommandModule
{
    private readonly IBotService _botService;
    private readonly ISpreadsheetService _spreadsheetService;
    private readonly IProfileService _profileService;

    public AdminCommands(IBotService botService, IProfileService profileService, ISpreadsheetService spreadsheetService)
    {
        _botService = botService;
        _profileService = profileService;
        _spreadsheetService = spreadsheetService;
    }

    [SlashCommand("generate-user-report", "Generates a user .xlsx report and sends it to you")]
    public async Task GenerateReportCommand(InteractionContext ctx)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        var users = await _profileService.GetAllUserProfilesWithPoints();
        var stream = await _spreadsheetService.GenerateUserReport(users);

        var response = new DiscordInteractionResponseBuilder()
            .AddFile("pass_user_report.xlsx", stream);

        await ctx.CreateResponseAsync(response);
    }

    [SlashCommand("ping", "Checks if the bot is active")]
    public async Task PingCommand(InteractionContext ctx)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", "Server is active!");
    }
}
