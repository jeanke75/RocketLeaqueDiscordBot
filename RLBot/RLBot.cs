using System;
using System.Configuration;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

namespace RLBot
{
    public class RLBot
    {
        private DiscordSocketClient _Client;
        private CommandService _Commands;

        public const char botPrefix = '!';

        public async Task StartAsync()
        {
            _Client = new DiscordSocketClient();
            _Commands = new CommandService();

            var serv = InstallServices();
            await serv.GetRequiredService<CommandHandler>().InitAsync(serv);

            _Client.Log += Log;
            _Commands.Log += Log;

            await _Client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["TOKEN"]);
            await _Client.StartAsync();

            await _Client.SetGameAsync(ConfigurationManager.AppSettings["GAME"]);

            await Task.Delay(-1);
        }

        private IServiceProvider InstallServices()
        {
            return new ServiceCollection()
                .AddSingleton(_Client)
                .AddSingleton(_Commands)
                .AddSingleton<CommandHandler>()
                //
                .BuildServiceProvider();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(string.Concat("[", DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss"), "] [", msg.Severity, "] ", msg.Message, msg.Exception));
            return Task.CompletedTask;
        }
    }
}