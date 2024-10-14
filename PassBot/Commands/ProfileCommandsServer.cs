using DocumentFormat.OpenXml.Spreadsheet;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Models;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

public class ProfileCommandsServer : ApplicationCommandModule
{
    private readonly IProfileService _profileService;
    private readonly IPointsService _pointsService;

    public ProfileCommandsServer(IPointsService pointsService, IProfileService profileService)
    {
        _pointsService = pointsService;
        _profileService = profileService;
    }

    [SlashCommand("set-user-email", "Set the email address of another user.")]
    public async Task SetUserEmailCommand(InteractionContext ctx, [Option("user", "The user to set the email for")] DiscordUser user, [Option("email", "The email address to set")] string email)
    {
        if (!ValidationUtils.IsValidEmail(email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The email address '{email}' is not valid", $"Please enter a valid email address");
            return;
        }

        await _profileService.SetEmail(user, email);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"{user.Mention}'s email address has been updated to {email}");
    }

    [SlashCommand("view-user-email", "View the email address of another user.")]
    public async Task ViewUserEmailCommand(InteractionContext ctx, [Option("user", "The user to view the email for")] DiscordUser user)
    {
        var profile = await _profileService.GetUserProfile(user.Id.ToString());
        if (profile == null || String.IsNullOrEmpty(profile.Email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Email Found", $"It looks like this user hasn't set an email address yet! Use `/set-user-email` to add an email to their profile");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, user, profile.Email, "Email");
    }

    [SlashCommand("set-user-wallet-address", "Set the wallet address of another user.")]
    public async Task SetUserWalletAddressCommand(InteractionContext ctx, [Option("user", "The user to set the wallet address for")] DiscordUser user, [Option("wallet-address", "The Ethereum wallet address to set")] string walletAddress)
    {
        if (!ValidationUtils.IsValidEthereumAddress(walletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The wallet address '{walletAddress}' is not valid", $"Please enter a valid Ethereum wallet address");
            return;
        }

        await _profileService.SetWalletAddress(user, walletAddress);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"{user.Mention}'s wallet address has been updated to {walletAddress}");
    }

    [SlashCommand("view-user-wallet", "View the wallet address of another user.")]
    public async Task ViewUserWalletCommand(InteractionContext ctx, [Option("user", "The user to view the wallet address for")] DiscordUser user)
    {
        var profile = await _profileService.GetUserProfile(user.Id.ToString());
        if (profile == null || String.IsNullOrEmpty(profile.WalletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Wallet Found", $"It looks like this user hasn't set an wallet address yet! Use `/set-user-wallet` to add a wallet to their profile.");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, user, profile.WalletAddress, "Wallet Address");
    }

    [SlashCommand("view-user-profile", "View the profile of another user.")]
    public async Task ViewUserProfileCommand(InteractionContext ctx, [Option("user", "The user to view the profile for")] DiscordUser user)
    {
        var profile = await _profileService.GetUserProfileWithPointsByDiscordId(user.Id.ToString());
        if (profile == null)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Profile not found", $"Try setting an email or wallet address");
            return;
        }

        await EmbedUtils.CreateAndSendFullProfileEmbed(ctx, user, profile);
    }
}
