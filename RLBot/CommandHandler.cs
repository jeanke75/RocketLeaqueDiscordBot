using System;
using System.Reflection;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

namespace RLBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _Client;
        private readonly CommandService _Commands;
        private IServiceProvider _Services;

        public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService commands)
        {
            _Client = client;
            _Commands = commands;
            _Services = services;

            _Client.MessageReceived += HandleCommandAsync;
        }

        public async Task InitAsync(IServiceProvider services)
        {
            _Services = services;

            await _Commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix(RLBot.botPrefix, ref argPos) || message.HasMentionPrefix(_Client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new SocketCommandContext(_Client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _Commands.ExecuteAsync(context, argPos, _Services);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}
