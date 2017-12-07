using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RLBot.Modules
{
    [Name("Help")]
    [Group("help")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService commands;

        public HelpModule(CommandService commands)
        {
            this.commands = commands;
        }

        [Name("help")]
        [Command]
        [Summary("List all the modules in the bot")]
        public async Task HelpAsync()
        {
            string mList = "";
            foreach (var module in commands.Modules)
                mList += string.Concat($"**► Name: {module.Name}** ││", $"{module.Summary}", "\n\n");

            await ReplyAsync("", embed: new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithDescription(mList)
                        .WithFooter($"For a module's commands - Use {RLBot.COMMAND_PREFIX}help module <module name>")
                        .Build());
        }

        [Command("module")]
        [Summary("List all the commands in a module")]
        [Remarks("help module <module name>")]
        public async Task HelpModuleAsync(string moduleName)
        {
            string cList = "";
            var module = commands.Modules.FirstOrDefault(x => x.Name.ToLower() == moduleName.ToLower());

            if (module == null)
            {
                await Context.Channel.SendMessageAsync($"`{moduleName}` is an invalid module! :mag:");
                return;
            }

            foreach (var command in module.Commands)
                cList += string.Concat($"**► Name: {command.Name}**\n", $"• Summary: {command.Summary}\n\n");

            await ReplyAsync("", embed: new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithDescription(cList)
                        .WithFooter($"For a command's details - Use {RLBot.COMMAND_PREFIX}help command <command name>")
                        .Build());
        }

        [Command("command")]
        [Summary("List a command's details")]
        [Remarks("help command <command name>")]
        public async Task HelpCommandAsync(string c)
        {
            var result = commands.Search(Context, c);
            string cDetails = "";

            if (result.Commands == null)
            {
                await ReplyAsync($"`{c}` is an invalid command! :mag:");
                return;
            }

            foreach (var cmd in result.Commands)
            {
                var aliases = string.Join(", ", cmd.Command.Aliases.ToArray());
                cDetails += $"**► Name: {cmd.Command.Name}**\n• Aliases: {aliases}\n• Summary: {cmd.Command.Summary}\n• Usage: {RLBot.COMMAND_PREFIX + cmd.Command.Remarks}";
            }

            await ReplyAsync("", embed: new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithDescription(cDetails)
                        .Build());
        }
    }
}
