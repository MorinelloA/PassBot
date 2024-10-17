using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Utilities;

public class UserCommands : ApplicationCommandModule
{

    public UserCommands()
    {
    }

    [SlashCommand("view-commands", "Gives you the list of commands you may use.")]
    public async Task ViewCommandsCommand(InteractionContext ctx)
    {
        // Fetch the available commands
        var availableCommands = EmbedUtils.GetAvailableCommands();

        // Create and send the embed with the list of commands
        await EmbedUtils.CreateAndSendCommandListEmbed(ctx, availableCommands);
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
