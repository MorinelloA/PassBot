using DSharpPlus.SlashCommands;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

public class ProfileCommandsDM : ApplicationCommandModule
{
    private readonly IProfileService _profileService;
    private readonly IPointsService _pointsService;

    public ProfileCommandsDM(IPointsService pointsService, IProfileService profileService)
    {
        _pointsService = pointsService;
        _profileService = profileService;
    }

    [SlashCommand("set-email", "Set your email address.")]
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

        await _profileService.SetEmail(ctx.User, email);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"Your email has been set to {email}");
    }

    [SlashCommand("view-email", "View your email address.")]
    public async Task ViewEmailCommand(InteractionContext ctx)
    {
        var profile = await _profileService.GetUserProfile(ctx.User.Id.ToString());
        if (profile == null || String.IsNullOrEmpty(profile.Email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Email Found", $"It looks like you haven't set an email address yet! Use `/set-email` to add your email to your profile");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, ctx.User, profile.Email, "Email");
    }

    [SlashCommand("set-wallet-address", "Set your wallet address.")]
    public async Task SetWalletAddressCommand(InteractionContext ctx, [Option("wallet-address", "Your Ethereum wallet address")] string walletAddress)
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
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"The wallet address '{walletAddress}' is not valid", $"Please enter a valid Ethereum wallet address");
            return;
        }

        await _profileService.SetWalletAddress(ctx.User, walletAddress);
        await EmbedUtils.CreateAndSendSuccessEmbed(ctx, "Success!", $"Your wallet address has been updated to {walletAddress}");
    }

    [SlashCommand("view-wallet", "View your wallet address.")]
    public async Task ViewWalletCommand(InteractionContext ctx)
    {
        var profile = await _profileService.GetUserProfile(ctx.User.Id.ToString());
        if (profile == null || String.IsNullOrEmpty(profile.WalletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, "No Wallet Found", $"It looks like you haven't set a wallet address yet! Use `/set-wallet` to add your wallet to your profile.");
            return;
        }
        await EmbedUtils.CreateAndSendProfileFieldEmbed(ctx, ctx.User, profile.WalletAddress, "Wallet Address");
    }

    [SlashCommand("view-profile", "View your profile.")]
    public async Task ViewProfileCommand(InteractionContext ctx)
    {
        var profile = await _profileService.GetUserProfileWithPointsByDiscordId(ctx.User.Id.ToString());
        if (profile == null)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(ctx, $"Profile not found", $"Try setting an email or wallet address");
            return;
        }

        await EmbedUtils.CreateAndSendFullProfileEmbed(ctx, ctx.User, profile);
    }
}
