using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using PassBot.Models;

public class ProfileButtonHandler
{
    private readonly IProfileService _profileService;

    public ProfileButtonHandler(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task HandleButtonInteraction(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        switch (e.Id)
        {
            case "btn_view_profile":
                await HandleViewProfile(e);
                break;
            case "btn_set_email":
                await HandleSetEmail(e);
                break;
            case "btn_set_wallet":
                await HandleSetWallet(e);
                break;
            case "btn_set_x":
                await HandleSetXAccount(e);
                break;
        }
    }

    private async Task HandleViewProfile(ComponentInteractionCreateEventArgs e)
    {
        var discordId = e.User.Id.ToString();

        var profile = await _profileService.GetUserProfileWithPointsByDiscordIdAsync(discordId);
        if (profile == null)
        {
            var warningEmbed = new DiscordEmbedBuilder()
                .WithTitle("Profile not found")
                .WithDescription("Try setting an email or wallet address")
                .WithColor(DiscordColor.Red);

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(warningEmbed).AsEphemeral(true));
            return;
        }

        if (profile == null)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, $"Profile not found", $"Try setting an email or wallet address");
            return;
        }

        await EmbedUtils.CreateAndSendFullProfileEmbed(e.Interaction, e.User, profile, true);
    }

    private async Task HandleSetEmail(ComponentInteractionCreateEventArgs e)
    {
        var modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Set Email")
            .WithCustomId("modal_set_email")  // Custom ID must match the handler
            .AddComponents(new TextInputComponent("Enter your email", "email", "example@example.com", required: true));

        await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }

    private async Task HandleSetWallet(ComponentInteractionCreateEventArgs e)
    {
        var modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Set Wallet Address")
            .WithCustomId("modal_set_wallet")
            .AddComponents(new TextInputComponent("Enter your wallet address", "wallet", "0x123456...", required: true));

        await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }

    private async Task HandleSetXAccount(ComponentInteractionCreateEventArgs e)
    {
        var modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Set X Account")
            .WithCustomId("modal_set_x")
            .AddComponents(new TextInputComponent("Enter your X (Twitter) account", "x_account", "username", required: true));

        await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }

    public async Task HandleSetEmailModal(DiscordClient client, ModalSubmitEventArgs e)
    {
        if (e.Interaction.Data.CustomId != "modal_set_email")
            return;

        var email = e.Values["email"]; // Retrieve email from modal input

        if (string.IsNullOrEmpty(email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Invalid Input", "You must enter a valid email address.");
            return;
        }

        bool isLocked = await _profileService.IsProfilesLockedAsync();
        if (isLocked)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Access Denied",
                "Profiles are currently locked. Please message an administrator/moderator.");
            return;
        }

        var profile = await _profileService.GetUserProfileWithPointsByDiscordIdAsync(e.Interaction.User.Id.ToString());
        string walletAddress = profile?.WalletAddress;

        email = email.ToLower().Trim();

        var timeUntilNextChange = await _profileService.GetTimeUntilNextProfileChangeAsync(e.Interaction.User.Id.ToString(), "Email");

        if (timeUntilNextChange.HasValue)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Cooldown Active",
                $"You cannot change your email yet. Please wait {timeUntilNextChange.Value.Days} days, {timeUntilNextChange.Value.Hours} hours.");
            return;
        }

        if (!ValidationUtils.IsValidEmail(email))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, $"Invalid Email",
                $"The email address '{email}' is not valid. Please enter a valid email.");
            return;
        }

        UserCheckAPISent payload = new UserCheckAPISent
        {
            WalletAddress = walletAddress,
            Email = email
        };

        var userCheckError = await _profileService.CheckUserProfileAsync(payload);

        if (userCheckError?.isError == true)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Error", userCheckError.error);
            return;
        }

        await _profileService.SetEmailAsync(e.Interaction.User, email);
        await EmbedUtils.CreateAndSendSuccessEmbed(e.Interaction, "Success!", $"Your email has been set to {email}", true);
    }

    public async Task HandleSetWalletModal(DiscordClient client, ModalSubmitEventArgs e)
    {
        if (e.Interaction.Data.CustomId != "modal_set_wallet")
            return;

        var walletAddress = e.Values["wallet"]; // Retrieve wallet address from modal input

        if (string.IsNullOrEmpty(walletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Invalid Input", "You must enter a valid wallet address.");
            return;
        }

        bool isLocked = await _profileService.IsProfilesLockedAsync();
        if (isLocked)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Access Denied",
                "Profiles are currently locked. Please message an administrator/moderator.");
            return;
        }

        var profile = await _profileService.GetUserProfileWithPointsByDiscordIdAsync(e.Interaction.User.Id.ToString());
        string email = profile?.Email;

        walletAddress = walletAddress.ToLower().Trim();

        var timeUntilNextChange = await _profileService.GetTimeUntilNextProfileChangeAsync(e.Interaction.User.Id.ToString(), "Wallet Address");

        if (timeUntilNextChange.HasValue)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Cooldown Active",
                $"You cannot change your wallet address yet. Please wait {timeUntilNextChange.Value.Days} days, {timeUntilNextChange.Value.Hours} hours.");
            return;
        }

        if (!ValidationUtils.IsValidEthereumAddress(walletAddress))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Invalid Wallet Address",
                $"The wallet address '{walletAddress}' is not valid. Please enter a valid Pass wallet address.");
            return;
        }

        UserCheckAPISent payload = new UserCheckAPISent
        {
            WalletAddress = walletAddress,
            Email = email
        };

        var userCheckError = await _profileService.CheckUserProfileAsync(payload);

        if (userCheckError?.isError == true)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Error", userCheckError.error);
            return;
        }

        await _profileService.SetWalletAddressAsync(e.Interaction.User, walletAddress);
        await EmbedUtils.CreateAndSendSuccessEmbed(e.Interaction, "Success!", $"Your wallet address has been set to {walletAddress}", true);
    }

    public async Task HandleSetXAccountModal(DiscordClient client, ModalSubmitEventArgs e)
    {
        if (e.Interaction.Data.CustomId != "modal_set_x")
            return;

        var xAccount = e.Values["x_account"]; // Retrieve X account from modal input

        if (string.IsNullOrEmpty(xAccount))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Invalid Input", "You must enter your X account.");
            return;
        }

        bool isLocked = await _profileService.IsProfilesLockedAsync();
        if (isLocked)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Access Denied",
                "Profiles are currently locked. Please message an administrator/moderator.");
            return;
        }

        var timeUntilNextChange = await _profileService.GetTimeUntilNextProfileChangeAsync(e.Interaction.User.Id.ToString(), "X Account");

        if (timeUntilNextChange.HasValue)
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Cooldown Active",
                $"You cannot change your X account yet. Please wait {timeUntilNextChange.Value.Days} days, {timeUntilNextChange.Value.Hours} hours.");
            return;
        }

        if (xAccount.Contains("@"))
        {
            await EmbedUtils.CreateAndSendWarningEmbed(e.Interaction, "Invalid Format",
                "Please provide your X account **without** the '@' symbol.");
            return;
        }

        await _profileService.SetXAccountAsync(e.Interaction.User, xAccount);
        await EmbedUtils.CreateAndSendSuccessEmbed(e.Interaction, "Success!", $"Your X account has been updated to {xAccount}", true);
    }
}
