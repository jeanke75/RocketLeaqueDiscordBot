using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RLBot.API.RLS;
using RLBot.API.RLS.Data;
using RLBot.API.RLS.Net.Models;
using RLBot.Data;
using RLBot.Preconditions;
using RLBot.TypeReaders;

namespace RLBot.Modules
{
    [Name("Set Up")]
    [Summary("This is where you get started")]
    public class SetUpModule : InteractiveBase<SocketCommandContext>
    {
        private RlsClient _RLSClient;
        public SetUpModule()
        {
            _RLSClient = new RlsClient(ConfigurationManager.AppSettings["RLSAPI_TOKEN"]);
        }
        
        [Command("link", RunMode = RunMode.Async)]
        [Summary("Update your matchmaking account if not all playlists are linked yet.")]
        [Remarks("link>")]
        [RequireBotPermission(GuildPermission.EmbedLinks | GuildPermission.ManageRoles)]
        [RequireChannel(385503590619545611)]
        public async Task LinkAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            var user = Context.Message.Author as SocketGuildUser;
            try
            {
                // check if discord id is already in the database
                var userinfo = await Database.GetUserInfoAsync(user.Id);
                if (userinfo == null)
                {
                    await ReplyAsync($"{user.Mention}, no account info provided.");
                    return;
                }

                // check if there are still unlinked playlists
                if (userinfo.Elo1s != -1 && userinfo.Elo2s != -1 && userinfo.Elo3s != -1)
                {
                    await ReplyAsync($"{user.Mention}, you've already linked all your rocket league playlists to your discord.");
                    return;
                }

                // get the platform from the user
                RlsPlatform platform = RlsPlatform.Steam;
                if (user.Roles.Contains(Context.Guild.GetRole(386067580630073356)))
                    platform = RlsPlatform.Steam;
                else if (user.Roles.Contains(Context.Guild.GetRole(386067821035126784)))
                    platform = RlsPlatform.Xbox;
                else if (user.Roles.Contains(Context.Guild.GetRole(386067907093856256)))
                    platform = RlsPlatform.Ps4;
                else
                {
                    await ReplyAsync($"{user.Mention}, couldn't retrieve your platform.");
                    return;
                }

                // try to retrieve the account info and check if previously unranked playlists are now ranked
                var player = await _RLSClient.GetPlayerAsync(platform, userinfo.UniqueID);
                if (player == null)
                {
                    await ReplyAsync($"Failed to retrieve player information!");
                    return;
                }

                if (!player.RankedSeasons.TryGetValue(Enum.GetValues(typeof(RlsSeason)).Cast<RlsSeason>().Max(), out var season))
                {
                    await ReplyAsync($"{user.Mention}, the account was found, but no information could be retrieved for the current season.");
                    return;
                }

                List<IRole> roles = new List<IRole>();
                int rp1 = userinfo.Elo1s;
                if (rp1 == -1 && season.TryGetValue(RlsPlaylistRanked.Duel, out PlayerRank duel))
                {
                    rp1 = duel.RankPoints;
                    roles.Add(Context.Guild.GetRole(GetPlaylistRole(RlsPlaylistRanked.Duel, duel.RankPoints)));
                }

                int rp2 = userinfo.Elo2s;
                if (rp2 == -1 && season.TryGetValue(RlsPlaylistRanked.Doubles, out PlayerRank doubles))
                {
                    rp2 = doubles.RankPoints;
                    roles.Add(Context.Guild.GetRole(GetPlaylistRole(RlsPlaylistRanked.Doubles, doubles.RankPoints)));
                }

                int rp3 = userinfo.Elo3s;
                if (rp3 == -1 && season.TryGetValue(RlsPlaylistRanked.Standard, out PlayerRank standard))
                {
                    rp3 = standard.RankPoints;
                    roles.Add(Context.Guild.GetRole(GetPlaylistRole(RlsPlaylistRanked.Standard, rp3)));
                }

                if (rp1 == userinfo.Elo1s && rp2 == userinfo.Elo2s && rp3 == userinfo.Elo3s)
                {
                    await ReplyAsync($"{user.Mention}, no new playlists have been linked.");
                    return;
                }

                // try to add the user to the database with their current elo
                await Database.UpdateUserInfoAsync(user.Id, (short)rp1, (short)rp2, (short)rp3);

                // give the rolse to the user
                await user.AddRolesAsync(roles);

                await ReplyAsync($"{user.Mention}, new playlists have been linked!");
            }
            catch (Exception ex)
            {
                await ReplyAsync($"{user.Mention}, " + ex.Message);
            }
        }
        
        [Command("link", RunMode = RunMode.Async)]
        [Summary("Set up your matchmaking account.")]
        [Remarks("link <region> <platform> <account ID>")]
        [RequireBotPermission(GuildPermission.EmbedLinks | GuildPermission.ManageRoles)]
        [RequireChannel(385503590619545611)]
        public async Task LinkAsync([OverrideTypeReader(typeof(RLRegionTypeReader))] RlsRegion region, [OverrideTypeReader(typeof(RLPlatformTypeReader))] RlsPlatform platform, [Remainder]string uniqueId)
        {
            await Context.Channel.TriggerTypingAsync();
            var user = Context.Message.Author as SocketGuildUser;
            try
            {
                // check if discord id is already in the database
                if (await Database.GetUserInfoAsync(user.Id) != null)
                {
                    await ReplyAsync($"{user.Mention}, you've already linked your rocket league account to your discord. If not all playlists are linked do `!link`.");
                    return;
                }

                // try to retrieve the account info and show it to the user so he/she can confirm it's them
                var player = await _RLSClient.GetPlayerAsync(platform, uniqueId);
                if (player == null)
                {
                    await ReplyAsync($"Failed to retrieve player information!");
                    return;
                }

                var embedBuilder = new EmbedBuilder()
                    .WithColor(RLBot.EMBED_COLOR)
                    .WithTitle($"Rocket League Stats")
                    .AddField("Username", player.DisplayName)
                    .WithThumbnailUrl(player.Avatar);
                
                if (!player.RankedSeasons.TryGetValue(Enum.GetValues(typeof(RlsSeason)).Cast<RlsSeason>().Max(), out var season))
                {
                    await ReplyAsync($"{user.Mention}, the following account was found, but no information could be retrieved for the current season.", false, embedBuilder.Build());
                    return;
                }

                List<IRole> roles = new List<IRole>();
                int rp1 = -1;
                if (season.TryGetValue(RlsPlaylistRanked.Duel, out PlayerRank duels))
                {
                    embedBuilder.AddField("1V1", GetRankString(duels.Tier) + $" ({duels.RankPoints})", true);
                    rp1 = duels.RankPoints;
                    roles.Add(Context.Guild.GetRole(GetPlaylistRole(RlsPlaylistRanked.Duel, rp1)));
                }
                else
                {
                    embedBuilder.AddField("1V1", "Unranked");
                }

                int rp2 = -1;
                if (season.TryGetValue(RlsPlaylistRanked.Doubles, out PlayerRank doubles))
                {
                    embedBuilder.AddField("2V2", GetRankString(doubles.Tier) + $" ({doubles.RankPoints})", true);
                    rp2 = doubles.RankPoints;
                    roles.Add(Context.Guild.GetRole(GetPlaylistRole(RlsPlaylistRanked.Doubles, rp2)));
                }
                else
                {
                    embedBuilder.AddField("2V2", "Unranked");
                }

                int rp3 = -1;
                if (season.TryGetValue(RlsPlaylistRanked.Standard, out PlayerRank standard))
                {
                    embedBuilder.AddField("3V3", GetRankString(standard.Tier) + $" ({standard.RankPoints})", true);
                    rp3 = standard.RankPoints;
                    roles.Add(Context.Guild.GetRole(GetPlaylistRole(RlsPlaylistRanked.Standard, rp3)));
                }
                else
                {
                    embedBuilder.AddField("3V3", "Unranked");
                }

                await ReplyAsync($"{user.Mention}, reply with yes if this is you. (Cannot be undone!)", false, embedBuilder.Build());
                var msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                if (msg == null)
                {
                    await ReplyAsync("Message timed out..");
                    return;
                }
                else if (msg.Content.ToLower() != "yes" && msg.Content.ToLower() != "y")
                {
                    await ReplyAsync($"{user.Mention}, account linking cancelled.");
                    return;
                }

                // check if the user has all the required ranks
                if (rp1 == -1 && rp2 == -1 && rp3 == -1)
                {
                    await ReplyAsync($"{user.Mention}, the accounts can't be linked untill you have received a rank in at least 1 playlist.");
                    return;
                }

                // prepare the roles for the user
                roles.Add(Context.Guild.GetRole(GetRegionRole(region)));
                roles.Add(Context.Guild.GetRole(GetPlatformRole(platform)));

                // try to add the user to the database with their current elo
                await Database.InsertUserInfoAsync(user.Id, uniqueId, (short)rp1, (short)rp2, (short)rp3);

                // give the rolse to the user
                await user.AddRolesAsync(roles);

                await ReplyAsync($"Accounts linked succesfull!");
            }
            catch (SqlException ex)
            when (ex.Number == 2627)
            {
                await ReplyAsync($"{user.Mention}, this account is already linked to someone else.");
            }
            catch (Exception ex)
            {
                await ReplyAsync($"{user.Mention}, " + ex.Message);
            }
        }

        private string GetRankString(int? tier)
        {
            switch(tier)
            {
                case 1:
                    return "Bronze I";
                case 2:
                    return "Bronze II";
                case 3:
                    return "Bronze III";
                case 4:
                    return "Silver I";
                case 5:
                    return "Silver II";
                case 6:
                    return "Silver III";
                case 7:
                    return "Gold I";
                case 8:
                    return "Gold II";
                case 9:
                    return "Gold III";
                case 10:
                    return "Platinum I";
                case 11:
                    return "Platinum II";
                case 12:
                    return "Platinum III";
                case 13:
                    return "Diamond I";
                case 14:
                    return "Diamond II";
                case 15:
                    return "Diamond III";
                case 16:
                    return "Champion I";
                case 17:
                    return "Champion II";
                case 18:
                    return "Champion III";
                case 19:
                    return "Grand Champion";
                default:
                    return "Unranked";
            }
        }

        private ulong GetPlaylistRole(RlsPlaylistRanked playlist, int points)
        {
            switch(playlist)
            {
                case RlsPlaylistRanked.Duel:
                    if (points > 1400)
                        return 407225051968962590;
                    else if (points > 1308)
                        return 407225054590664729;
                    else if (points > 1155)
                        return 407225057677410304;
                    else if (points > 915)
                        return 407225059963568139;
                    else
                        return 407225064602468353;
                case RlsPlaylistRanked.Doubles:
                    if (points > 1550)
                        return 407224039363444736;
                    else if (points > 1395)
                        return 407224239079424031;
                    else if (points > 1195)
                        return 407224549801984001;
                    else if (points > 935)
                        return 407224673001275392;
                    else
                        return 407224786956189696;
                case RlsPlaylistRanked.Standard:
                    if (points > 1550)
                        return 386211454040276992;
                    else if (points > 1395)
                        return 373857123110617099;
                    else if (points > 1195)
                        return 375035896753553409;
                    else if (points > 935)
                        return 375020227328475138;
                    else
                        return 373857511146782721;
            }

            throw new ArgumentException("Invalid playlist.");
        }

        private ulong GetRegionRole(RlsRegion region)
        {
            switch (region)
            {
                case RlsRegion.Europe:
                    return 385785137960452106;
                case RlsRegion.NorthAmerica:
                    return 385780548016013312;
                case RlsRegion.SouthAmerica:
                    return 407255951406530561;
                case RlsRegion.Oceania:
                    return 407256306190385175;
                case RlsRegion.AsiaCentral:
                    return 407256310435020813;
                case RlsRegion.MiddleEast:
                    return 407256685074317322;
                case RlsRegion.Africa:
                    return 407256688610246660;
            }

            throw new ArgumentException("Invalid region.");
        }

        private ulong GetPlatformRole(RlsPlatform platform)
        {
            switch (platform)
            {
                case RlsPlatform.Steam:
                    return 386067580630073356;
                case RlsPlatform.Xbox:
                    return 386067821035126784;
                case RlsPlatform.Ps4:
                    return 386067907093856256;
            }

            throw new ArgumentException("Invalid platform.");
        }
    }
}