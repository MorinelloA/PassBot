using DSharpPlus.SlashCommands;
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
}
