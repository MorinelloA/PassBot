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
    private readonly IPointsService _pointsService;

    public AdminCommands(IBotService botService, IPointsService pointsService, IProfileService profileService, ISpreadsheetService spreadsheetService)
    {
        _botService = botService;
        _profileService = profileService;
        _pointsService = pointsService;
        _spreadsheetService = spreadsheetService;
    }

    [SlashCommand("generate-points-report", "Generates an .xlsx report of current points for every user and sends it to you.")]
    public async Task GeneratePointsReportCommand(InteractionContext ctx)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        var users = await _profileService.GetAllUserProfilesWithPointsAsync();
        var stream = await _spreadsheetService.GeneratePointsReportAsync(users);

        var response = new DiscordInteractionResponseBuilder()
            .AddFile("pass_points_report.xlsx", stream);

        await ctx.CreateResponseAsync(response);
    }

    [SlashCommand("generate-user-report", "Generates an .xlsx report of all point actions for a specific user and sends it to you.")]
    public async Task GenerateUserReportCommand(InteractionContext ctx, [Option("user", "The user to generate the report for.")] DiscordUser user, [Option("include-cleared", "Should it included cleared records? Default is true.")] bool includeRemoved = true)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        var userPointsLog = await _pointsService.GetUserPointsLogByDiscordIdAsync(user.Id.ToString(), includeRemoved);
        var stream = await _spreadsheetService.GenerateUserReportAsync(userPointsLog);

        var response = new DiscordInteractionResponseBuilder()
            .AddFile($"{user.Username}_report.xlsx", stream);

        await ctx.CreateResponseAsync(response);
    }

    [SlashCommand("ping", "Checks if the bot is active.")]
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
