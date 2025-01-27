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

            // Generate the spreadsheet
            var stream = await _spreadsheetService.GeneratePointsReportCSVUploadAsync(users);

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

                UserCheckAPISent payload = new UserCheckAPISent();
                payload.WalletAddress = user.WalletAddress.Trim();
                payload.Email = user.Email.Trim();

                var endpoint = _config["UserProfileAPIEndpoint"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    var noDataEmbed = EmbedUtils.CreateWarningEmbed("No Endpoint", "The API endpoint is not set.");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(noDataEmbed).AsEphemeral(true));
                    return;
                }

                UserCheckApiResponse? response;
                                
                if(endpoint == "debug")
                {
                    Random random = new Random();
                    int randomNumber = random.Next(1, 14);

                    response = new UserCheckApiResponse();

                    response.Data = new UserCheckData();

                    if (randomNumber != 3)
                    {
                        if (randomNumber == 10)
                        {
                            response.Data.VerifiedEmail = new VerifiedEmail();
                            response.Data.VerifiedWalletAddress = new VerifiedWalletAddress();
                            response.Data.MatchStatus = new MatchStatus();

                            response.Data.VerifiedEmail.IsPassEmail = true;
                            response.Data.VerifiedWalletAddress.IsPassWalletAddress = true;
                            response.Data.MatchStatus.IsEmailMatchWithWalletAddress = false;
                        }
                        else
                        {
                            if (randomNumber != 1)
                            {
                                response.Data.VerifiedEmail = new VerifiedEmail();
                                if (randomNumber > 5)
                                {
                                    response.Data.VerifiedEmail.IsPassEmail = true;
                                }
                                else
                                {
                                    response.Data.VerifiedEmail.IsPassEmail = false;
                                }
                            }
                            if (randomNumber != 2)
                            {
                                response.Data.VerifiedWalletAddress = new VerifiedWalletAddress();

                                if (randomNumber > 7)
                                {
                                    response.Data.VerifiedWalletAddress.IsPassWalletAddress = true;
                                }
                                else
                                {
                                    response.Data.VerifiedWalletAddress.IsPassWalletAddress = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    HttpResponseMessage _response = await _httpClient.GetAsync(endpoint);

                    // Ensure the response was successful
                    _response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    var responseContent = await _response.Content.ReadAsStringAsync();

                    // Deserialize the JSON response into the ApiResponse object
                    response = JsonSerializer.Deserialize<UserCheckApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // To handle case-insensitive JSON keys
                    });
                }

                

                if (response == null)
                {
                    UserCheckError userCheckError = new UserCheckError();
                    userCheckError.user = user;
                    userCheckError.error = "API Response is null";

                    errors.Add(userCheckError);
                }
                else if (response.Data == null)
                {
                    UserCheckError userCheckError = new UserCheckError();
                    userCheckError.user = user;
                    userCheckError.error = "API Data is null";

                    errors.Add(userCheckError);
                }
                else if (response.Data.VerifiedEmail != null && !response.Data.VerifiedEmail.IsPassEmail)
                {
                    if (response.Data.VerifiedWalletAddress != null && !response.Data.VerifiedWalletAddress.IsPassWalletAddress)
                    {
                        UserCheckError userCheckError = new UserCheckError();
                        userCheckError.user = user;
                        userCheckError.error = "Both Email and Wallet are invalid";

                        errors.Add(userCheckError);
                    }
                    else
                    {
                        UserCheckError userCheckError = new UserCheckError();
                        userCheckError.user = user;
                        userCheckError.error = "Email is invalid";

                        errors.Add(userCheckError);
                    }
                }
                else if (response.Data.VerifiedWalletAddress != null && !response.Data.VerifiedWalletAddress.IsPassWalletAddress)
                {
                    UserCheckError userCheckError = new UserCheckError();
                    userCheckError.user = user;
                    userCheckError.error = "Wallet is invalid";

                    errors.Add(userCheckError);
                }
                else if(response.Data.VerifiedWalletAddress != null && response.Data.VerifiedEmail != null && response.Data.MatchStatus != null && !response.Data.MatchStatus.IsEmailMatchWithWalletAddress)
                {
                    UserCheckError userCheckError = new UserCheckError();
                    userCheckError.user = user;
                    userCheckError.error = "Email & Wallet don't match";

                    errors.Add(userCheckError);
                }
                else if (response.Data.VerifiedWalletAddress == null && response.Data.VerifiedEmail == null)
                {
                    UserCheckError userCheckError = new UserCheckError();
                    userCheckError.user = user;
                    userCheckError.error = "Issue with Data Verified statuses";

                    errors.Add(userCheckError);
                }
                else
                {
                    validatedProfiles.Add(user);
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
