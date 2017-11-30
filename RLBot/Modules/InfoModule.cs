using System;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Newtonsoft.Json.Linq;

namespace RLBot.Modules
{
    public class Info : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _Client;

        public Info(DiscordSocketClient client)
            => _Client = client;

        /*[Command("invite")]
        [Summary("Returns the OAuth2 Invite URL of the bot")]
        public async Task Invite()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            await ReplyAsync($"A user with `MANAGE_SERVER` can invite me to your server here: <https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot>");
        }*/

        [Command("ping")]
        [Summary("Check if the bot is still running.")]
        public async Task PingAsync()
            => await ReplyAsync($"**Pong! - {_Client.Latency}ms**");

        [Command("botinfo")]
        [Summary("See statistics and info about the bot")]
        public async Task InfoAsync()
        {
            var application = await _Client.GetApplicationInfoAsync();
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

            await ReplyAsync("", false, new EmbedBuilder()
                .AddField(x => { x.Name = "Latest Changes on Github"; x.Value = latestChanges; x.IsInline = true; })
                .AddField(x => { x.Name = "Info"; x.Value =
                    $"**Author:** `{application.Owner.Username}#{application.Owner.Discriminator}` [ID: {application.Owner.Id}]\n" +
                    $"**Library:** Discord.Net - Version: {DiscordConfig.Version}\n" +
                    $"**Total Guilds:** {_Client.Guilds.Count()}\n" +
                    $"**Total Channels:** {_Client.Guilds.Sum(g => g.Channels.Count())}\n" +
                    $"**Total Users:** {_Client.Guilds.Sum(g => g.Users.Where(b => !b.IsBot).Count())}"; x.IsInline = false;
                })
                .AddField(x => { x.Name = "Process Info"; x.Value =
                    $"**Runtime:** {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                    $"**Heap Size:** {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"d\d\ h\h\ m\m\ s\s")}\n" +
                    $"**Uptime**: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString()}MB"; x.IsInline = true; }).Build());
        }

        [Command("userinfo")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync([Summary("The (optional) user to get info for")] SocketUser user = null)
            => await ReplyAsync($"{(user ?? Context.Client.CurrentUser).Username}#{(user ?? Context.Client.CurrentUser).Discriminator}");
    }
}