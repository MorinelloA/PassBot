using DSharpPlus;
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

    /*
    [SlashCommand("generate-points-report", "Generates an .xlsx report of current points for every user and sends it to you.")]
    public async Task GeneratePointsReportCommand(InteractionContext ctx)
    {
        // Send a deferred response to avoid a timeout
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        try
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
                .AddFile("pass_points_report.xlsx", stream)
                .AsEphemeral(true);

            await ctx.CreateResponseAsync(response);
        }
        catch(Exception ex)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Error", "Please try again later.");
            return;
        }
    }
    */

    [SlashCommand("generate-points-report", "Generates an .xlsx report of current points for every user and sends it to you.")]
    public async Task GeneratePointsReportCommand(InteractionContext ctx)
    {
        // Send a deferred response to avoid a timeout
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        try
        {
            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                // Create a warning embed
                var warningEmbed = EmbedUtils.CreateWarningEmbed("Access Denied", "You do not have permission to use this command.");

                // Send the warning embed as a follow-up message
                var followUp = new DiscordFollowupMessageBuilder()
                    .AddEmbed(warningEmbed)
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(followUp);
                return;
            }

            // Fetch all user profiles with points
            var users = await _profileService.GetAllUserProfilesWithPointsAsync();

            // Generate the spreadsheet
            var stream = await _spreadsheetService.GeneratePointsReportAsync(users);

            // Create the follow-up response with the file and send it
            var fileFollowUp = new DiscordFollowupMessageBuilder()
                .AddFile("pass_points_report.xlsx", stream)
                .AsEphemeral(true);

            await ctx.FollowUpAsync(fileFollowUp);
        }
        catch (Exception ex)
        {
            // Create an error embed and send it as a follow-up message
            var errorEmbed = EmbedUtils.CreateWarningEmbed("Error", "An error occurred while generating the report. Please try again later.");

            var errorFollowUp = new DiscordFollowupMessageBuilder()
                .AddEmbed(errorEmbed)
                .AsEphemeral(true);

            await ctx.FollowUpAsync(errorFollowUp);
        }
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
            .AddFile($"{user.Username}_report.xlsx", stream)
            .AsEphemeral(true);

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

        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", "Server is active!", true);
    }

    [SlashCommand("view-admin-commands", "Gives you the list of admin commands you may use.")]
    public async Task ViewAdminCommandsCommand(InteractionContext ctx)
    {
        // Fetch the available commands
        var availableCommands = EmbedUtils.GetAvailableAdminCommands();

        // Create and send the embed with the list of commands
        await EmbedUtils.CreateAndSendCommandListEmbed(ctx, availableCommands);
    }
}
