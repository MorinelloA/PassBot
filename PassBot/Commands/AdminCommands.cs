using Azure;
using Azure.Identity;
using DocumentFormat.OpenXml.Spreadsheet;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class AdminCommands : ApplicationCommandModule
{
    private readonly IBotService _botService;
    private readonly ISpreadsheetService _spreadsheetService;
    private readonly IProfileService _profileService;
    private readonly IPointsService _pointsService;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public AdminCommands(IBotService botService, IPointsService pointsService, IProfileService profileService, ISpreadsheetService spreadsheetService, IConfiguration config, HttpClient httpClient)
    {
        _botService = botService;
        _profileService = profileService;
        _pointsService = pointsService;
        _spreadsheetService = spreadsheetService;
        _config = config;
        _httpClient = httpClient;
    }

    [SlashCommand("generate-points-csv-upload", "Generates an .csv of current points for every user to upload.")]
    public async Task GeneratePointsCSVUploadCommand(InteractionContext ctx)
    {
        // Send a deferred response to avoid a timeout
        try
        {
            var response = new DiscordInteractionResponseBuilder()
            .WithContent("Processing your request...")
            .AsEphemeral(true); // Mark the initial response as ephemeral

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, response);
        }
        catch (Exception e)
        {
            return;
        }

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

            if (users == null || !users.Any())
            {
                var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Data", "There are no user points available to generate the report.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                return;
            }

            //Filter users who have both a email and wallet listed
            users = users.Where(x => !string.IsNullOrEmpty(x.WalletAddress) && !string.IsNullOrEmpty(x.Email)).ToList();

            if (users == null || !users.Any())
            {
                var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Data", "There are no user points available to generate the report.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                return;
            }

            List<UserProfileWithPoints?> validatedUsers = new List<UserProfileWithPoints?>();
            //Filter users who have valid Pass email and wallets
            foreach(var user in users)
            {
                if(user == null || string.IsNullOrEmpty(user.WalletAddress) || string.IsNullOrEmpty(user.Email))
                {
                    continue;
                }

                UserCheckAPISent payload = new UserCheckAPISent();
                payload.WalletAddress = user.WalletAddress.Trim();
                payload.Email = user.Email.Trim();
                          
                var userCheckError = await _profileService.CheckUserProfileAsync(payload);

                if (userCheckError != null && userCheckError.isError == false)
                {
                    validatedUsers.Add(user);
                }
            }

            if (validatedUsers == null || !validatedUsers.Any())
            {
                var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Data", "There are no user points available to generate the report.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                return;
            }

            // Generate the spreadsheet
            var stream = await _spreadsheetService.GeneratePointsReportCSVUploadAsync(validatedUsers);

            // Create the follow-up response with the file and send it
            var fileFollowUp = new DiscordFollowupMessageBuilder()
                .AddFile("pass_points_upload.csv", stream)
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

    [SlashCommand("generate-points-report", "Generates an .xlsx report of current points for every user and sends it to you.")]
    public async Task GeneratePointsReportCommand(InteractionContext ctx)
    {
        // Send a deferred response to avoid a timeout
        try
        {
            var response = new DiscordInteractionResponseBuilder()
            .WithContent("Processing your request...")
            .AsEphemeral(true); // Mark the initial response as ephemeral

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, response);
        }
        catch (Exception e)
        {
            return;
        }

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

            if (users == null || !users.Any())
            {
                var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Data", "There are no user points available to generate the report.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                return;
            }

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

    [SlashCommand("gather-monthly-pass-perk-kpis", "Gathers monthly KPIs for Pass Perks")]
    public async Task GatherMonthlyKPIs(InteractionContext ctx, [Option("month", "The month (1-12) to gather KPIs for.")] long month, [Option("year", "The year (yyyy) to gather KPIs for.")] long year)
    {
        // Send a deferred response to avoid a timeout
        try
        {
            var response = new DiscordInteractionResponseBuilder()
            .WithContent("Processing your request...")
            .AsEphemeral(true); // Mark the initial response as ephemeral

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, response);
        }
        catch (Exception e)
        {
            return;
        }

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
            var kpis = await _pointsService.GetMonthlyKPIDataAsync((int)month, (int)year);

            if (kpis == null)
            {
                var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Data", "There is no KPI available for this month. There may be an error.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                return;
            }

            // Generate embed
            var embed = EmbedUtils.CreateMonthlyPassPerksKPIEmbed(ctx, kpis);
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral(true));
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
        // Send a deferred response to avoid a timeout
        try
        {
            var response = new DiscordInteractionResponseBuilder()
            .WithContent("Processing your request...")
            .AsEphemeral(true); // Mark the initial response as ephemeral

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, response);
        }
        catch (Exception e)
        {
            return;
        }

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

            var userPointsLog = await _pointsService.GetUserPointsLogByDiscordIdAsync(user.Id.ToString(), includeRemoved);
            var stream = await _spreadsheetService.GenerateUserReportAsync(userPointsLog);

            // Create the follow-up response with the file and send it
            var fileFollowUp = new DiscordFollowupMessageBuilder()
                .AddFile($"{user.Username}_report.xlsx", stream)
                .AsEphemeral(true);

            await ctx.FollowUpAsync(fileFollowUp);
        }
        catch(Exception e)
        {
            // Create an error embed and send it as a follow-up message
            var errorEmbed = EmbedUtils.CreateWarningEmbed("Error", "An error occurred while generating the report. Please try again later.");

            var errorFollowUp = new DiscordFollowupMessageBuilder()
                .AddEmbed(errorEmbed)
                .AsEphemeral(true);

            await ctx.FollowUpAsync(errorFollowUp);
        }
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

    [SlashCommand("validate-user-profiles", "Validates that all emails and wallet addresses are associated with a Pass Account.")]
    public async Task ValidateUserProfilesCommand(InteractionContext ctx)
    {
        // Send a deferred response to avoid a timeout
        try
        {
            var response = new DiscordInteractionResponseBuilder()
            .WithContent("Processing your request...")
            .AsEphemeral(true); // Mark the initial response as ephemeral

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, response);
        }
        catch (Exception e)
        {
            return;
        }

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

            if (users == null || !users.Any())
            {
                var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Data", "There are no user points available to generate the report.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                return;
            }

            //Filter users who have both a email and wallet listed
            users = users.Where(x => !string.IsNullOrEmpty(x.WalletAddress) && !string.IsNullOrEmpty(x.Email)).ToList();

            if (users == null || !users.Any())
            {
                var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Data", "There are no user points available to generate the report.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                return;
            }

            List<UserProfileWithPoints> validatedProfiles = new List<UserProfileWithPoints>();
            List<UserCheckError> errors = new List<UserCheckError>();

            foreach (var user in users)
            {
                if(user == null || string.IsNullOrEmpty(user.WalletAddress) || string.IsNullOrEmpty(user.Email))
                {
                    continue;
                }

                /*
                if (string.IsNullOrEmpty(endpoint))
                {
                    var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Endpoint", "The API endpoint is not set.");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                    return;
                }
                */

                UserCheckAPISent payload = new UserCheckAPISent();
                payload.WalletAddress = user.WalletAddress.Trim();
                payload.Email = user.Email.Trim();
                          
                var userCheckError = await _profileService.CheckUserProfileAsync(payload);

                if(userCheckError != null)
                {
                    if (userCheckError.isError)
                    {
                        userCheckError.user = user;
                        errors.Add(userCheckError);
                    }
                    else
                    {
                        validatedProfiles.Add(user);
                    }
                }                    
            }

            var embed = EmbedUtils.CreateProfileValidationEmbed(ctx, ctx.User, validatedProfiles, errors, true);

            var embedFollowUp = new DiscordFollowupMessageBuilder()
                .AddEmbed(embed)
                .AsEphemeral(true);

            await ctx.FollowUpAsync(embedFollowUp);

        }
        catch (Exception e)
        {
            // Create an error embed and send it as a follow-up message
            var errorEmbed = EmbedUtils.CreateWarningEmbed("Error", "An error occurred while validating profiles. Please try again later.");

            var errorFollowUp = new DiscordFollowupMessageBuilder()
                .AddEmbed(errorEmbed)
                .AsEphemeral(true);

            await ctx.FollowUpAsync(errorFollowUp);
        }
    }

    [SlashCommand("generate-full-backup", "Generates a complete data backup as an .xlsx")]
    public async Task GenerateFullBackupCommand(InteractionContext ctx)
    {
        // Send a deferred response to avoid a timeout
        try
        {
            var response = new DiscordInteractionResponseBuilder()
            .WithContent("Processing your request...")
            .AsEphemeral(true); // Mark the initial response as ephemeral

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, response);
        }
        catch (Exception e)
        {
            return;
        }

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
            var backup = await _spreadsheetService.GetFullBackupAsync();

            if (backup == null)
            {
                var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Data", "There is no data available to backup.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                return;
            }

            // Generate the spreadsheet
            var stream = await _spreadsheetService.GenerateDatabaseBackupAsync(backup);

            // Create the follow-up response with the file and send it
            var fileFollowUp = new DiscordFollowupMessageBuilder()
                .AddFile($"pass_points_backup_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.xlsx", stream)
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
}
