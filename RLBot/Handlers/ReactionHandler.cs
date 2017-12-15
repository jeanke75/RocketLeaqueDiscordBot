using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace RLBot.Handlers
{
    public class ReactionHandler : Handler
    {
        const ulong GUILD_ID = 373829959187169280;
        const ulong CHANNEL_ID = 385503590619545611;
        const ulong PLATFORM_MESSAGE_ID = 385508762514882560;
        const ulong RANK_MESSAGE_ID = 385537545745989633;

        public ReactionHandler(IServiceProvider services, DiscordSocketClient client, CommandService commands) : base(services, client, commands)
        {
            _client.ReactionAdded += HandleReactionAddedAsync;
            _client.ReactionRemoved += HandleReactionRemovedAsync;
        }

        private async Task HandleReactionAddedAsync(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var guild = (channel as SocketGuildChannel)?.Guild;
            if (guild == null || (guild.Id != GUILD_ID && channel.Id == CHANNEL_ID)) return;

            var message = await cache.GetOrDownloadAsync();
            if (message == null)
            {
                await RLBot.Log(new LogMessage(LogSeverity.Info, "HandleReactionAddedAsync", $"Dumped message (not in cache) with id {reaction.MessageId}"));
                return;
            }

            if (!reaction.User.IsSpecified)
            {
                await RLBot.Log(new LogMessage(LogSeverity.Info, "HandleReactionAddedAsync", $"Dumped message (invalid user) with id {message.Id}"));
                return;
            }

            // get the SocketGuildUser data for the user that reacted
            var guildUser = guild.GetUser(reaction.UserId);
            if (guildUser == null)
            {
                await RLBot.Log(new LogMessage(LogSeverity.Info, "HandleReactionAddedAsync", $"No such user found in this server"));
                return;
            }

            switch (reaction.MessageId)
            {
                case PLATFORM_MESSAGE_ID:
                    var platformMsg = await channel.GetMessageAsync(PLATFORM_MESSAGE_ID) as IUserMessage;
                    if (platformMsg == null) return;

                    await HandlePlatformReactionAsync(platformMsg, guild, guildUser, reaction);
                    break;
                case RANK_MESSAGE_ID:
                    var rankMsg = await channel.GetMessageAsync(RANK_MESSAGE_ID) as IUserMessage;
                    if (rankMsg == null) return;

                    await HandleRankReactionAsync(rankMsg, guild, guildUser);
                    break;
            }
        }

        private async Task HandleReactionRemovedAsync(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var guild = (channel as SocketGuildChannel)?.Guild;
            if (guild == null || (guild.Id != GUILD_ID && channel.Id == CHANNEL_ID)) return;

            var message = await cache.GetOrDownloadAsync();
            if (message == null)
            {
                await RLBot.Log(new LogMessage(LogSeverity.Info, "HandleReactionRemovedAsync", $"Dumped message (not in cache) with id {reaction.MessageId}"));
                return;
            }

            if (!reaction.User.IsSpecified)
            {
                await RLBot.Log(new LogMessage(LogSeverity.Info, "HandleReactionRemovedAsync", $"Dumped message (invalid user) with id {message.Id}"));
                return;
            }

            // get the SocketGuildUser data for the user that reacted
            var guildUser = guild.GetUser(reaction.UserId);
            if (guildUser == null)
            {
                await RLBot.Log(new LogMessage(LogSeverity.Info, "HandleReactionRemovedAsync", $"No such user found in this server"));
                return;
            }

            switch (reaction.MessageId)
            {
                case PLATFORM_MESSAGE_ID:
                    var platformMsg = await channel.GetMessageAsync(PLATFORM_MESSAGE_ID) as IUserMessage;
                    if (platformMsg == null) return;

                    await HandlePlatformReactionRemovedAsync(guild, guildUser, reaction);
                    break;
                case RANK_MESSAGE_ID:
                    var rankMsg = await channel.GetMessageAsync(RANK_MESSAGE_ID) as IUserMessage;
                    if (rankMsg == null) return;

                    await HandleRankReactionAsync(rankMsg, guild, guildUser);
                    break;
            }
        }

        private async Task HandlePlatformReactionAsync(IUserMessage platformMsg, SocketGuild guild, SocketGuildUser user, SocketReaction reaction)
        {
            Emoji steamEmote = new Emoji("STEAM:385533217547223042");
            Emoji xboxEmote = new Emoji("XBOX:385533231824371713");
            Emoji ps4Emote = new Emoji("PS4:385533264133095435");

            var steamTask = platformMsg.GetReactionUsersAsync(steamEmote, 1, user.Id);
            var xboxTask = platformMsg.GetReactionUsersAsync(xboxEmote, 1, user.Id);
            var ps4Task = platformMsg.GetReactionUsersAsync(ps4Emote, 1, user.Id);
            await Task.WhenAll(steamTask, xboxTask, ps4Task);

            bool steam = (await steamTask).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool xbox = (await xboxTask).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool ps4 = (await ps4Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;

            // remove other platforms
            if ((reaction.Emote.Name == "STEAM" || reaction.Emote.Name == "PS4") && xbox)
                await platformMsg.RemoveReactionAsync(xboxEmote, user);
            if ((reaction.Emote.Name == "STEAM" || reaction.Emote.Name == "XBOX") && ps4)
                await platformMsg.RemoveReactionAsync(ps4Emote, user);
            if ((reaction.Emote.Name == "XBOX" || reaction.Emote.Name == "PS4") && steam)
                await platformMsg.RemoveReactionAsync(steamEmote, user);
            
            // manage roles
            var steamRoleTask = ManageRole(guild, 386067580630073356, user, reaction.Emote.Name == "STEAM");
            var xboxRoleask = ManageRole(guild, 386067821035126784, user, reaction.Emote.Name == "XBOX");
            var ps4RoleTask = ManageRole(guild, 386067907093856256, user, reaction.Emote.Name == "PS4");

            await Task.WhenAll(steamRoleTask, xboxRoleask, ps4RoleTask);
        }

        private async Task HandlePlatformReactionRemovedAsync(SocketGuild guild, SocketGuildUser user, SocketReaction reaction)
        {
            switch(reaction.Emote.Name)
            {
                case "STEAM":
                    await ManageRole(guild, 386067580630073356, user, false);
                    break;
                case "XBOX":
                    await ManageRole(guild, 386067821035126784, user, false);
                    break;
                case "PS4":
                    await ManageRole(guild, 386067907093856256, user, false);
                    break;
            }
        }

        private async Task HandleRankReactionAsync(IUserMessage rankMsg, SocketGuild guild, SocketGuildUser user)
        {
            var gcTask = rankMsg.GetReactionUsersAsync(new Emoji("GRANDCHAMPION:385533112039243777"), 1, user.Id);
            var c3Task = rankMsg.GetReactionUsersAsync(new Emoji("CHAMPIONIII:385533131253481473"), 1, user.Id);
            var c2Task = rankMsg.GetReactionUsersAsync(new Emoji("CHAMPIONII:385533146101186561"), 1, user.Id);
            var c1Task = rankMsg.GetReactionUsersAsync(new Emoji("CHAMPIONI:385533163730108428"), 1, user.Id);
            var d3Task = rankMsg.GetReactionUsersAsync(new Emoji("DIAMONDIII:385533012747747331"), 1, user.Id);
            var d2Task = rankMsg.GetReactionUsersAsync(new Emoji("DIAMONDII:385533027121496064"), 1, user.Id);
            var d1Task = rankMsg.GetReactionUsersAsync(new Emoji("DIAMONDI:385533052820127744"), 1, user.Id);
            var p3Task = rankMsg.GetReactionUsersAsync(new Emoji("PLATINUMIII:385532956921298956"), 1, user.Id);
            var p2Task = rankMsg.GetReactionUsersAsync(new Emoji("PLATINUMII:385532977775378432"), 1, user.Id);
            var p1Task = rankMsg.GetReactionUsersAsync(new Emoji("PLATINUMI:385532994175238144"), 1, user.Id);
            /*var g3Task = rankMsg.GetReactionUsersAsync(Emote.Parse("GOLDIII:385532111966306316"), 1, user.Id);
            var g2Task = rankMsg.GetReactionUsersAsync(Emote.Parse("GOLDII:385532145814208512"), 1, user.Id);
            var g1Task = rankMsg.GetReactionUsersAsync(Emote.Parse("GOLDI:385532172917932033"), 1, user.Id);
            var s3Task = rankMsg.GetReactionUsersAsync(Emote.Parse("SILVERIII:385531240985395212"), 1, user.Id);
            var s2Task = rankMsg.GetReactionUsersAsync(Emote.Parse("SILVERII:385531259419492357"), 1, user.Id);
            var s1Task = rankMsg.GetReactionUsersAsync(Emote.Parse("SILVERI:385531276574326785"), 1, user.Id);
            var b3Task = rankMsg.GetReactionUsersAsync(Emote.Parse("BRONZEIII:385531174199492608"), 1, user.Id);
            var b2Task = rankMsg.GetReactionUsersAsync(Emote.Parse("BRONZEII:385531191844929540"), 1, user.Id);
            var b1Task = rankMsg.GetReactionUsersAsync(Emote.Parse("BRONZEI:385531207179567125"), 1, user.Id);*/
            await Task.WhenAll(gcTask, c3Task, c2Task, c1Task, d3Task, d2Task, d1Task, p3Task, p2Task, p1Task);//, g3Task, g2Task, g1Task, s3Task, s2Task, s1Task, b3Task, b2Task, b1Task);

            bool gc = (await gcTask).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool c3 = gc || (await c3Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool c2 = c3 || (await c2Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool c1 = c2 || (await c1Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool d3 = c1 || (await d3Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool d2 = d3 || (await d2Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool d1 = d2 || (await d1Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool p3 = d1 || (await p3Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool p2 = p3 || (await p2Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool p1 = p2 || (await p1Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            /*bool g3 = p1 || (await g3Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool g2 = g3 || (await g2Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool g1 = g2 || (await g1Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool s3 = g1 || (await s3Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool s2 = s3 || (await s2Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool s1 = s2 || (await s1Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool b3 = s1 || (await b3Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool b2 = b3 || (await b2Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;
            bool b1 = b2 || (await b1Task).Where(x => x.Id == user.Id).FirstOrDefault() != null;*/

            // manage roles
            var zTask = ManageRole(guild, 373857123110617099, user, c3);
            var yTask = ManageRole(guild, 375035896753553409, user, !c3 && c1);
            var xTask = ManageRole(guild, 375020227328475138, user, !c1 && d1);
            var wTask = ManageRole(guild, 373857511146782721, user, !d1 && p1);

            await Task.WhenAll(zTask, yTask, xTask, wTask);
        }

        private Task ManageRole(SocketGuild guild, ulong roleId, SocketGuildUser user, bool activate)
        {
            // if the current state for this role is correct do nothing
            bool hasRole = user.Roles.FirstOrDefault(x => x.Id == roleId) != null;
            if ((activate && hasRole) || (!activate && !hasRole))
                return Task.FromResult(false);

            // otherwise retrieve the role
            var c_role = guild.GetRole(roleId);
            if (c_role == null) 
                return RLBot.Log(new LogMessage(LogSeverity.Info, "ManageRole", $"No such role found in this server"));

            // and give it or take it from the user
            if (activate)
                return user.AddRoleAsync(c_role);
            else
                return user.RemoveRoleAsync(c_role);
        }
    }
}