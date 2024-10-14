using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PassBot.Services;
using PassBot.Services.Interfaces;
using PassBot.Commands;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true) 
    .AddEnvironmentVariables();

IConfiguration configuration = builder.Build();

var services = new ServiceCollection();
services.AddSingleton(configuration);

// Services
services.AddSingleton<IPointsService, PointsService>();
services.AddSingleton<IProfileService, ProfileService>();
services.AddSingleton<ISpreadsheetService, SpreadsheetService>();

// Register command modules as services
services.AddSingleton<PointsCommands>();
services.AddSingleton<ProfileCommands>();
services.AddSingleton<AdminCommands>();

// Build the service provider
var serviceProvider = services.BuildServiceProvider();

// Discord Client
var discord = new DiscordClient(new DiscordConfiguration()
{
    Token = configuration["DiscordToken"],
    TokenType = TokenType.Bot,
    Intents = DiscordIntents.AllUnprivileged
});

discord.UseInteractivity(new InteractivityConfiguration
{
    Timeout = TimeSpan.FromMinutes(2)
});

// Log when the bot is ready
discord.Ready += async (s, e) =>
{
    Console.WriteLine("Bot is connected and ready.");
};

var slash = discord.UseSlashCommands(new SlashCommandsConfiguration
{
    Services = serviceProvider
});

slash.RegisterCommands<PointsCommands>(guildId: ulong.Parse(configuration["GuildId"]));
slash.RegisterCommands<ProfileCommands>(guildId: ulong.Parse(configuration["GuildId"]));
slash.RegisterCommands<AdminCommands>(guildId: ulong.Parse(configuration["GuildId"]));

// Connect the bot
await discord.ConnectAsync();
await Task.Delay(-1);
