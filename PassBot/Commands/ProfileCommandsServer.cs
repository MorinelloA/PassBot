using DocumentFormat.OpenXml.Spreadsheet;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using System.Net.Http;
using System.Text.Json;
using DSharpPlus.Entities;
using System.Linq;

public class ProfileCommandsServer : ApplicationCommandModule
{
    private readonly IBotService _botService;
    private readonly IProfileService _profileService;
    private readonly IPointsService _pointsService;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public ProfileCommandsServer(IBotService botService, IPointsService pointsService, IProfileService profileService, IConfiguration config, HttpClient httpClient)
    {
        _botService = botService;
        _pointsService = pointsService;
        _profileService = profileService;
        _config = config;
        _httpClient = httpClient;
    }

    [SlashCommand("lock-profiles", "Lock users from setting profile items.")]
    public async Task LockProfilesCommand(InteractionContext ctx)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        await _profileService.LockProfilesAsync();

        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success", "Profiles Locked Successfully", true);
    }

    [SlashCommand("unlock-profiles", "Unlock users from setting profile items.")]
    public async Task UnlockProfilesCommand(InteractionContext ctx)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        await _profileService.UnlockProfilesAsync();

        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success", "Profiles Unlocked Successfully", true);
    }

    [SlashCommand("warning-incomplete-profiles", "Warn users who have points but have not yet completed their profile.")]
    public async Task WarningIncompleteProfilesCommand(InteractionContext ctx)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("🚫 Access Denied: You do not have permission to use this command."));
            return;
        }

        // ✅ Send an immediate response to let users know the process has started
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("🔍 Checking for users with incomplete profiles..."));

        // ✅ Fetch valid members directly, no extra API calls needed
        var validMembers = await _profileService.GetListOfDiscordMembersWithIncompleteProfilesAsync(ctx);

        Console.WriteLine($"Total Valid Members: {validMembers.Count}");

        if (validMembers.Count == 0)
        {
            await ctx.Channel.SendMessageAsync("✅ All users with Pass Perks Points have completed their profiles!");
            return;
        }

        // ✅ Send the initial message before pings
        await ctx.Channel.SendMessageAsync("The following users have Pass Perks Points but have not yet completed their profile:");

        // ✅ Batch mentions into groups of 10 to avoid rate limits
        const int batchSize = 10;
        List<string> mentionBatches = validMembers
            .Select(m => m.Mention) // Get mentions directly
            .Chunk(batchSize) // Splits the list into groups of `batchSize`
            .Select(batch => string.Join(" ", batch))
            .ToList();

        foreach (var batch in mentionBatches)
        {
            await ctx.Channel.SendMessageAsync(batch);
            await Task.Delay(1000); // Add delay to prevent rate limits
        }

        // ✅ Send additional instructions separately (only once)
        string instructions = @"
Please remember that you need to complete your profile for points to transfer to the app at the end of each month.

To set your Pass email, type `/set-email email:<YOUR EMAIL HERE>`
To set your Pass wallet address, type `/set-wallet-address wallet-address:<YOUR WALLET ADDRESS HERE>`
OPTIONAL: To set your X account, type `/set-x-account x-account:<YOUR X ACCOUNT HERE>`
";

        await ctx.Channel.SendMessageAsync(instructions);
    }




    [SlashCommand("set-user-email", "Set the email address of a specified user.")]
    public async Task SetUserEmailCommand(InteractionContext ctx, [Option("user", "The user to set the email for")] DiscordUser user, [Option("email", "The email address to set")] string email)
    {
        try
        {
            var profile = await _profileService.GetUserProfileWithPointsByDiscordIdAsync(user.Id.ToString());
            string walletAddress = profile == null ? null : profile.WalletAddress;

            if (!string.IsNullOrEmpty(email))
            {
                email = email.ToLower().Trim();
            }

            // Check if the user has permission
            if (!_botService.HasPermission(ctx.User))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
                return;
            }

            bool isLocked = await _profileService.IsProfilesLockedAsync();
            if (isLocked)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "Profiles are currently locked. This is probably temporary, but please message an administrator/moderator to be sure.");
                return;
            }

            if (!ValidationUtils.IsValidEmail(email))
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The email address '{email}' is not valid", $"Please enter a valid email address");
                return;
            }

            UserCheckAPISent payload = new UserCheckAPISent();
            payload.WalletAddress = walletAddress;
            payload.Email = email;

            var userCheckError = await _profileService.CheckUserProfileAsync(payload);

            if (userCheckError != null)
            {
                if (userCheckError.isError)
                {
                    await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", userCheckError.error);
                    return;
                }
                else
                {
                    await _profileService.SetEmailAsync(user, email);
                    await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"{user.Mention}'s email has been updated to {email}", true);
                }
            }
            else
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", "Issue with UserCheck. Please Contact Admin");
                return;
            }
        }
        catch (Exception e)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", "Issue on Server End. Please try again later.");
            return;
        }
    }

    [SlashCommand("view-user-email", "View the email address of a specified user.")]
    public async Task ViewUserEmailCommand(InteractionContext ctx, [Option("user", "The user to view the email for")] DiscordUser user)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        var profile = await _profileService.GetUserProfileAsync(user.Id.ToString());
        if (profile == null || String.IsNullOrEmpty(profile.Email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Email Found", $"It looks like this user hasn't set an email address yet! Use `/set-user-email` to add an email to their profile");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, user, profile.Email, "Email", true);
    }

    [SlashCommand("set-user-wallet-address", "Set the wallet address of a specified user.")]
    public async Task SetUserWalletAddressCommand(InteractionContext ctx, [Option("user", "The user to set the wallet address for")] DiscordUser user, [Option("wallet-address", "The Pass wallet address to set")] string walletAddress)
    {
        var profile = await _profileService.GetUserProfileWithPointsByDiscordIdAsync(user.Id.ToString());
        string email = profile == null ? null : profile.Email;

        if (!string.IsNullOrEmpty(email))
        {
            email = email.ToLower().Trim();
        }
        if (!string.IsNullOrEmpty(walletAddress))
        {
            walletAddress = walletAddress.ToLower().Trim();
        }


        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        bool isLocked = await _profileService.IsProfilesLockedAsync();
        if (isLocked)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "Profiles are currently locked. This is probably temporary, but please message an administrator/moderator to be sure.");
            return;
        }

        if (!ValidationUtils.IsValidEthereumAddress(walletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The wallet address '{walletAddress}' is not valid", $"Please enter a valid Pass wallet address");
            return;
        }

        UserCheckAPISent payload = new UserCheckAPISent();
        payload.WalletAddress = walletAddress;
        payload.Email = email;

        var userCheckError = await _profileService.CheckUserProfileAsync(payload);

        if (userCheckError != null)
        {
            if (userCheckError.isError)
            {
                await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", userCheckError.error);
                return;
            }
            else
            {
                await _profileService.SetWalletAddressAsync(user, walletAddress);
                await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"{user.Mention}'s wallet address has been updated to {walletAddress}", true);
            }
        }
        else
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Error", "Issue with UserCheck. Please Contact Admin");
            return;
        }
    }

    [SlashCommand("view-user-wallet", "View the wallet address of a specified user.")]
    public async Task ViewUserWalletCommand(InteractionContext ctx, [Option("user", "The user to view the wallet address for")] DiscordUser user)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        var profile = await _profileService.GetUserProfileAsync(user.Id.ToString());
        if (profile == null || String.IsNullOrEmpty(profile.WalletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Wallet Found", $"It looks like this user hasn't set an wallet address yet! Use `/set-user-wallet` to add a wallet to their profile.");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, user, profile.WalletAddress, "Wallet Address", true);
    }

    [SlashCommand("set-user-x-account", "Set the X account of a specified user.")]
    public async Task SetUserXAccountCommand(InteractionContext ctx, [Option("user", "The user to set the X account for")] DiscordUser user, [Option("x-account", "The X account (without the @) to set.")] string xAccount)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        bool isLocked = await _profileService.IsProfilesLockedAsync();
        if (isLocked)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "Profiles are currently locked. This is probably temporary, but please message an administrator/moderator to be sure.");
            return;
        }

        // Ensure no '@' symbol is used
        if (xAccount.Contains("@"))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Invalid Format", "Please provide an X account without the '@' symbol.");
            return;
        }

        await _profileService.SetXAccountAsync(user, xAccount);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"{user.Mention}'s X account has been updated to {xAccount}", true);
    }

    [SlashCommand("view-user-x-account", "View the X account of a specified user.")]
    public async Task ViewUserXAccountCommand(InteractionContext ctx, [Option("user", "The user to view the X account for")] DiscordUser user)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        var profile = await _profileService.GetUserProfileAsync(user.Id.ToString());
        if (profile == null || String.IsNullOrEmpty(profile.XAccount))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No X Account Found", $"It looks like this user hasn't set an X account yet! Use `/set-user-x-account` to add an X account to their profile.");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, user, profile.XAccount, "X Account", true);
    }

    [SlashCommand("view-user-profile", "View the profile of a specified user.")]
    public async Task ViewUserProfileCommand(InteractionContext ctx, [Option("user", "The user to view the profile for")] DiscordUser user)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        var profile = await _profileService.GetUserProfileWithPointsByDiscordIdAsync(user.Id.ToString());
        if (profile == null)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Profile not found", $"Try setting an email or wallet address");
            return;
        }

        await EmbedUtils.CreateAndSendFullProfileEmbed(ctx, user, profile, true);
    }
}
