using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RLBot.Preconditions;

namespace RLBot.Modules
{
    [Name("Support")]
    [Summary("Bot support")]
    public class SupportModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;

        public SupportModule(DiscordSocketClient client)
        {
            _client = client;
        }

        [Command("suggestion")]
        [Cooldown(12, 0, 0)]
        [Summary("Send in suggestions for new features or improvements for the bot. (12h cooldown)")]
        [Remarks("suggestion <your suggestion>")]
        public async Task SuggestionAsync([Remainder] string suggestion)
        {
            if (suggestion == "") return;

            var channel = _client.GetChannel(385368298381901842) as IMessageChannel;
            if (channel == null)
            {
                var application = await _client.GetApplicationInfoAsync();
                await ReplyAsync($"Can't find the suggestion channel, please contact {application.Owner}.");
                return;
            }

            await channel.SendMessageAsync("", embed: new EmbedBuilder()
                .WithColor(RLBot.EMBED_COLOR)
                .WithAuthor(Context.Message.Author)
                .WithDescription(suggestion.Trim())
                .WithCurrentTimestamp()
                .Build());
        }

        [Command("bugreport")]
        [Cooldown(12, 0, 0)]
        [Summary("Send in a detailed bugreport for the bot. Try to describe how to reproduce it if possible. (12h cooldown)")]
        [Remarks("bugreport <your bugreport>")]
        public async Task BugReportAsync([Remainder] string bug)
        {
            if (bug == "") return;

            var channel = _client.GetChannel(387560503641374730) as IMessageChannel;
            if (channel == null)
            {
                var application = await _client.GetApplicationInfoAsync();
                await ReplyAsync($"Can't find the bug report channel, please contact {application.Owner}.");
                return;
            }

            await channel.SendMessageAsync("", embed: new EmbedBuilder()
                .WithColor(RLBot.EMBED_COLOR)
                .WithAuthor(Context.Message.Author)
                .WithDescription(bug.Trim())
                .WithCurrentTimestamp()
                .Build());
        }
    }
}
