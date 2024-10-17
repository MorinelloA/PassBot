using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

public class ProfileCommandsServer : ApplicationCommandModule
{
    private readonly IBotService _botService;
    private readonly IProfileService _profileService;
    private readonly IPointsService _pointsService;

    public ProfileCommandsServer(IBotService botService, IPointsService pointsService, IProfileService profileService)
    {
        _botService = botService;
        _pointsService = pointsService;
        _profileService = profileService;
    }

    [SlashCommand("set-user-email", "Set the email address of a specified user.")]
    public async Task SetUserEmailCommand(InteractionContext ctx, [Option("user", "The user to set the email for")] DiscordUser user, [Option("email", "The email address to set")] string email)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        if (!ValidationUtils.IsValidEmail(email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The email address '{email}' is not valid", $"Please enter a valid email address");
            return;
        }

        await _profileService.SetEmailAsync(user, email);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"{user.Mention}'s email address has been updated to {email}");
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
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, user, profile.Email, "Email");
    }

    [SlashCommand("set-user-wallet-address", "Set the wallet address of a specified user.")]
    public async Task SetUserWalletAddressCommand(InteractionContext ctx, [Option("user", "The user to set the wallet address for")] DiscordUser user, [Option("wallet-address", "The Pass wallet address to set")] string walletAddress)
    {
        // Check if the user has permission
        if (!_botService.HasPermission(ctx.User))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Access Denied", "You do not have permission to use this command.");
            return;
        }

        if (!ValidationUtils.IsValidEthereumAddress(walletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The wallet address '{walletAddress}' is not valid", $"Please enter a valid Pass wallet address");
            return;
        }

        await _profileService.SetWalletAddressAsync(user, walletAddress);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"{user.Mention}'s wallet address has been updated to {walletAddress}");
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
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, user, profile.WalletAddress, "Wallet Address");
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

        // Ensure no '@' symbol is used
        if (xAccount.Contains("@"))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "Invalid Format", "Please provide an X account without the '@' symbol.");
            return;
        }

        await _profileService.SetXAccountAsync(user, xAccount);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"{user.Mention}'s X account has been updated to {xAccount}");
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
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, user, profile.XAccount, "X Account");
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

        await EmbedUtils.CreateAndSendFullProfileEmbed(ctx, user, profile);
    }
}
