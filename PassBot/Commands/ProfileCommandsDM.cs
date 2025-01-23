using Azure;
using DocumentFormat.OpenXml.Spreadsheet;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using System.Net.Http;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class ProfileCommandsDM : ApplicationCommandModule
{
    private readonly IProfileService _profileService;
    private readonly IPointsService _pointsService;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public ProfileCommandsDM(IPointsService pointsService, IProfileService profileService, IConfiguration config, HttpClient httpClient)
    {
        _pointsService = pointsService;
        _profileService = profileService;
        _config = config;
        _httpClient = httpClient;
    }
        
    [SlashCommand("set-email", "Set your Pass email address. This can only be done once every 30 days.")]
    public async Task SetEmailCommand(InteractionContext ctx, [Option("email", "Your email address")] string email)
    {
        // Get the time until the user can update their email again
        var timeUntilNextChange = await _profileService.GetTimeUntilNextProfileChangeAsync(ctx.User.Id.ToString(), "Email");

        if (timeUntilNextChange.HasValue)
        {
            // Inform the user about the cooldown period remaining
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Cooldown Active",
                $"You cannot change your email yet. Please wait {timeUntilNextChange.Value.Days} days, {timeUntilNextChange.Value.Hours} hours.");
            return;
        }

        if (!ValidationUtils.IsValidEmail(email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The email address '{email}' is not valid", $"Please enter a valid email address");
            return;
        }

        var apiEndpoint = _config["UserProfileAPIEndpoint"];

        if(!string.IsNullOrEmpty(apiEndpoint) && apiEndpoint != "debug")
        {
            HttpResponseMessage _response = await _httpClient.GetAsync(apiEndpoint);

            // Ensure the response was successful
            _response.EnsureSuccessStatusCode();

            // Read the response content as a string
            var responseContent = await _response.Content.ReadAsStringAsync();

            // Deserialize the JSON response into the ApiResponse object
            UserCheckApiResponse? response = JsonSerializer.Deserialize<UserCheckApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // To handle case-insensitive JSON keys
            });

            if (response == null)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Response is null. Contact Admin");
                return;
            }
            else if (response.Data == null)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"API Data is null. Contact Admin");
                return;
            }
            else if (response.Data.VerifiedEmail != null && !response.Data.VerifiedEmail.IsPassEmail)
            {
                if (response.Data.VerifiedWalletAddress != null && !response.Data.VerifiedWalletAddress.IsPassWalletAddress)
                {
                    await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Both Email and Wallet are not connected to a Pass account");
                    return;
                }
                else
                {
                    await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Email is not connected to a Pass account");
                    return;
                }
            }
            else if (response.Data.VerifiedWalletAddress != null && !response.Data.VerifiedWalletAddress.IsPassWalletAddress)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Wallet is not connected to a Pass account");
                return;
            }
            else if (response.Data.VerifiedWalletAddress != null && response.Data.VerifiedEmail != null && response.Data.MatchStatus != null && !response.Data.MatchStatus.IsEmailMatchWithWalletAddress)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Email & Wallet don't match the same Pass user");
                return;
            }
            else if (response.Data.VerifiedWalletAddress == null && response.Data.VerifiedEmail == null)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Issue with Data Verified statuses. Contact Admin");
                return;
            }
            else
            {
                //Nothing to do here. Continue
            }
        }

        await _profileService.SetEmailAsync(ctx.User, email);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"Your email has been set to {email}", true);
    }

    [SlashCommand("view-email", "View your email address.")]
    public async Task ViewEmailCommand(InteractionContext ctx)
    {
        var profile = await _profileService.GetUserProfileAsync(ctx.User.Id.ToString());
        if (profile == null || string.IsNullOrEmpty(profile.Email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Email Found", $"It looks like you haven't set an email address yet! Use `/set-email` to add your email to your profile");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, ctx.User, profile.Email, "Email", true);
    }

    [SlashCommand("set-wallet-address", "Set your Pass wallet address. This can only be done once every 30 days.")]
    public async Task SetWalletAddressCommand(InteractionContext ctx, [Option("wallet-address", "Your Pass wallet address")] string walletAddress)
    {
        // Get the time until the user can update their email again
        var timeUntilNextChange = await _profileService.GetTimeUntilNextProfileChangeAsync(ctx.User.Id.ToString(), "Wallet Address");

        if (timeUntilNextChange.HasValue)
        {
            // Inform the user about the cooldown period remaining
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Cooldown Active",
                $"You cannot change your wallet address yet. Please wait {timeUntilNextChange.Value.Days} days, {timeUntilNextChange.Value.Hours} hours.");
            return;
        }

        if (!ValidationUtils.IsValidEthereumAddress(walletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The wallet address '{walletAddress}' is not valid", $"Please enter a valid Pass wallet address");
            return;
        }

        var apiEndpoint = _config["UserProfileAPIEndpoint"];

        if (!string.IsNullOrEmpty(apiEndpoint) && apiEndpoint != "debug")
        {
            HttpResponseMessage _response = await _httpClient.GetAsync(apiEndpoint);

            // Ensure the response was successful
            _response.EnsureSuccessStatusCode();

            // Read the response content as a string
            var responseContent = await _response.Content.ReadAsStringAsync();

            // Deserialize the JSON response into the ApiResponse object
            UserCheckApiResponse? response = JsonSerializer.Deserialize<UserCheckApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // To handle case-insensitive JSON keys
            });

            if (response == null)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Response is null. Contact Admin");
                return;
            }
            else if (response.Data == null)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"API Data is null. Contact Admin");
                return;
            }
            else if (response.Data.VerifiedEmail != null && !response.Data.VerifiedEmail.IsPassEmail)
            {
                if (response.Data.VerifiedWalletAddress != null && !response.Data.VerifiedWalletAddress.IsPassWalletAddress)
                {
                    await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Both Email and Wallet are not connected to a Pass account");
                    return;
                }
                else
                {
                    await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Email is not connected to a Pass account");
                    return;
                }
            }
            else if (response.Data.VerifiedWalletAddress != null && !response.Data.VerifiedWalletAddress.IsPassWalletAddress)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Wallet is not connected to a Pass account");
                return;
            }
            else if (response.Data.VerifiedWalletAddress != null && response.Data.VerifiedEmail != null && response.Data.MatchStatus != null && !response.Data.MatchStatus.IsEmailMatchWithWalletAddress)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Email & Wallet don't match the same Pass user");
                return;
            }
            else if (response.Data.VerifiedWalletAddress == null && response.Data.VerifiedEmail == null)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", $"Issue with Data Verified statuses. Contact Admin");
                return;
            }
            else
            {
                //Nothing to do here. Continue
            }
        }

        await _profileService.SetWalletAddressAsync(ctx.User, walletAddress);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"Your wallet address has been updated to {walletAddress}", true);
    }

    [SlashCommand("view-wallet", "View your wallet address.")]
    public async Task ViewWalletCommand(InteractionContext ctx)
    {
        var profile = await _profileService.GetUserProfileAsync(ctx.User.Id.ToString());
        if (profile == null || string.IsNullOrEmpty(profile.WalletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Wallet Found", $"It looks like you haven't set a wallet address yet! Use `/set-wallet` to add your wallet to your profile.");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, ctx.User, profile.WalletAddress, "Wallet Address", true);
    }

    [SlashCommand("set-x-account", "Sets your X (formerly Twitter) account in your profile. This can only be done once every 30 days.")]
    public async Task SetXAccountCommand(InteractionContext ctx, [Option("x-account", "Your X account (without the @)")] string xAccount)
    {
        // Get the time until the user can update their email again
        var timeUntilNextChange = await _profileService.GetTimeUntilNextProfileChangeAsync(ctx.User.Id.ToString(), "X Account");

        if (timeUntilNextChange.HasValue)
        {
            // Inform the user about the cooldown period remaining
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Cooldown Active",
                $"You cannot change your X account yet. Please wait {timeUntilNextChange.Value.Days} days, {timeUntilNextChange.Value.Hours} hours.");
            return;
        }

        // Ensure no '@' symbol is used
        if (xAccount.Contains("@"))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Invalid Format", "Please provide your X account without the '@' symbol.");
            return;
        }

        await _profileService.SetXAccountAsync(ctx.User, xAccount);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"Your X account has been updated to {xAccount}", true);
    }

    [SlashCommand("view-x-account", "View your X account.")]
    public async Task ViewXAccountCommand(InteractionContext ctx)
    {
        var profile = await _profileService.GetUserProfileAsync(ctx.User.Id.ToString());
        if (profile == null || string.IsNullOrEmpty(profile.XAccount))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No X Account Found", $"It looks like you haven't set an x account yet! Use `/set-x-account` to add your X account to your profile.");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, ctx.User, profile.XAccount, "X Account", true);
    }

    [SlashCommand("view-profile", "View your profile.")]
    public async Task ViewProfileCommand(InteractionContext ctx)
    {
        var profile = await _profileService.GetUserProfileWithPointsByDiscordIdAsync(ctx.User.Id.ToString());
        if (profile == null)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Profile not found", $"Try setting an email or wallet address");
            return;
        }

        await EmbedUtils.CreateAndSendFullProfileEmbed(ctx, ctx.User, profile, true);
    }
}
