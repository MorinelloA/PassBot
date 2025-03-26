using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using PassBot.Models;

public class XButtonHandler
{
    private readonly IProfileService _profileService;

    public XButtonHandler(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task HandleButtonInteraction(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        switch (e.Id)
        {
            case "btn_view_quests":
                await HandleViewQuests(e);
                break;
            case "btn_set_quest":
                await HandleSetQuest(e);
                break;
            case "btn_distribute_x_points":
                await HandleDistributeXPoints(e);
                break;
        }
    }

    private async Task HandleViewQuests(ComponentInteractionCreateEventArgs e)
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

    private async Task HandleSetQuest(ComponentInteractionCreateEventArgs e)
    {
        var modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Set Email")
            .WithCustomId("modal_set_email")  // Custom ID must match the handler
            .AddComponents(new TextInputComponent("Enter your email", "email", "example@example.com", required: true));

        await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }

    private async Task HandleDistributeXPoints(ComponentInteractionCreateEventArgs e)
    {
        var modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Set Wallet Address")
            .WithCustomId("modal_set_wallet")
            .AddComponents(new TextInputComponent("Enter your wallet address", "wallet", "0x123456...", required: true));

        await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }


    public async Task HandleSetQuestModal(DiscordClient client, ModalSubmitEventArgs e)
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
        
}
