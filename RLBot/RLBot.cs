using System;
using System.Configuration;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RLBot.Services;

namespace RLBot
{
    class RLBot
    {
        private CommandService _commands;
        private DiscordSocketClient _client;

        public static readonly char COMMAND_PREFIX = '!';
        public static readonly Color EMBED_COLOR = Color.Red;

        public static void Main(string[] args)
           => new RLBot().StartAsync(args).GetAwaiter().GetResult();

        private async Task StartAsync(params string[] args)
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            var serv = InstallServices();
            await serv.GetRequiredService<CommandHandler>().InitAsync();

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
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(string.Concat("[", DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss"), "] [", msg.Severity, "] ", msg.Message, msg.Exception));
            return Task.CompletedTask;
        }
    }
}