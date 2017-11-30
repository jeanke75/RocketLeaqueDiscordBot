using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;

namespace RLBot.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;

        public InfoModule(DiscordSocketClient client)
        {
            _client = client;
        }

        [Command("invite")]
        [Summary("Returns the OAuth2 Invite URL of the bot")]
        public async Task Invite()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            await ReplyAsync($"A user with `MANAGE_SERVER` can invite me to your server here: <https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot>");
        }

        [Command("ping")]
        [Summary("Check if the bot is still running.")]
        public async Task PingAsync()
            => await ReplyAsync($"Pong! - {_client.Latency}ms");

        [Command("botinfo")]
        public async Task InfoAsync()
        {
            var application = await _client.GetApplicationInfoAsync();
            string latestChanges = null;
            
            var _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            using (var response = await _http.GetAsync("https://api.github.com/repos/jeanke75/RocketLeaqueDiscordBot/commits"))
            {
                if (!response.IsSuccessStatusCode)
                    latestChanges = "Error Recieving Latest Changes :x:";
                else
                {
                    dynamic Info = JArray.Parse(await response.Content.ReadAsStringAsync());

                    latestChanges =
                    $"[{((string)Info[0].sha).Substring(0, 5)}]({Info[0].html_url}) {Info[0].commit.message}\n" +
                    $"[{((string)Info[1].sha).Substring(0, 5)}]({Info[1].html_url}) {Info[1].commit.message}\n" +
                    $"[{((string)Info[2].sha).Substring(0, 5)}]({Info[2].html_url}) {Info[2].commit.message}";
                }

                response.Dispose();
            }

            await ReplyAsync("", embed: new EmbedBuilder()
                .WithColor(RLBot.EMBED_COLOR)
                .AddField("Info",
                    $"**Author:** `{application.Owner}` [ID: {application.Owner.Id}]\n" +
                    $"**Library:** Discord.Net - Version: {DiscordConfig.Version}\n" +
                    $"**Total Guilds:** {_client.Guilds.Count()}\n" +
                    $"**Total Channels:** {_client.Guilds.Sum(g => g.Channels.Count())}\n" +
                    $"**Total Users:** {_client.Guilds.Sum(g => g.Users.Where(b => !b.IsBot).Count())}")
                .AddField("Process Info",
                    $"**Runtime:** {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                    $"**Heap Size:** {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString()}\n" +
                    $"**Uptime**: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"d\d\ h\h\ m\m\ s\s")}MB")
                .AddField("Latest Changes on Github", latestChanges)
                .Build());
        }

        [Command("userinfo")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync([Summary("The (optional) user to get info for")] SocketUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync(userInfo.ToString());
        }
    }
}