using System;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RLBot.Models;
using RLBot.Services;
using RLBot.TypeReaders;

namespace RLBot
{
    class RLBot
    {
        private CommandService _commands;
        private DiscordSocketClient _client;

        public static readonly char COMMAND_PREFIX = '!';
        public static readonly Color EMBED_COLOR = Color.Red;
        public static readonly ulong APPLICATION_OWNER_ID = 140470317440040960;

        public static void Main(string[] args)
           => new RLBot().StartAsync(args).GetAwaiter().GetResult();

        private async Task StartAsync(params string[] args)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig{
                LogLevel = LogSeverity.Info
            });
            _commands = new CommandService();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            _commands.AddTypeReader<RLPlaylist>(new RLPlatformTypeReader());
            _commands.AddTypeReader<RLRegion>(new RLRegionTypeReader());
            _commands.AddTypeReader<RLPlaylist>(new RLPlaylistTypeReader());
            
            var serv = InstallServices();
            serv.GetRequiredService<ReliabilityService>();
            serv.GetRequiredService<CommandHandlerService>();

            _client.Log += Log;
            _commands.Log += Log;

            await _client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["TOKEN"]);
            await _client.StartAsync();

            await _client.SetGameAsync(ConfigurationManager.AppSettings["GAME"]);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        private IServiceProvider InstallServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<ReliabilityService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(string.Concat("[", DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss"), "] [", msg.Severity, "] ", msg.Message, msg.Exception));
            return Task.CompletedTask;
        }
    }
}