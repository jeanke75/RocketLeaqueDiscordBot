using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace RLBot.Services
{
    public class Handler
    {
        protected readonly DiscordSocketClient _client;
        protected readonly CommandService _commands;
        protected IServiceProvider _services;

        public Handler(IServiceProvider services, DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InitAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}
