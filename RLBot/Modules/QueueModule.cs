using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using RLBot.Data;
using RLBot.Data.Models;
using RLBot.Models;
using RLBot.Preconditions;

namespace RLBot.Modules
{
    [Name("Queue")]
    [Summary("Creation and utilization of a queue")]
    public class QueueModule : InteractiveBase<SocketCommandContext>
    {
        private static ConcurrentDictionary<ulong, RLQueue> queues = new ConcurrentDictionary<ulong, RLQueue>();
        private static Random rnd = new Random();
        private readonly string NOT_OPEN = "There is no open queue atm. Type \"" + RLBot.COMMAND_PREFIX + "qopen\", to start a new one.";
        private readonly string NOT_ENOUGH_PLAYERS = "Not enough players have joined the queue yet! {0}/{1}";

        private readonly OverwritePermissions botPerms = new OverwritePermissions(PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow);
        private readonly OverwritePermissions teamPerms = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny);
        private readonly OverwritePermissions everyonePerms = new OverwritePermissions(PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);

        [Command("qopen")]
        [Alias("qo")]
        [Summary("Create a new queue from which two 3man teams will be picked")]
        [Remarks("qopen")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        [RequireChannel(393411399654703115, 393411426451980289, 384666738056232970, 393692333469597696, 393693176772296704, 393693726565728257, 393694545688133634,
            393695480946622465, 393695741941514240, 393695923026526208, 393696051607109632, 393696168300773386, 386213046672162838, 375039923603898369, 385070323449462785,
            385420072996438021, 385393503833948160)]
        public async Task OpenQueueAsync()
        {
            if (!queues.TryGetValue(Context.Channel.Id, out RLQueue queue))
            {
                var channel = Context.Channel as SocketGuildChannel;
                switch (Context.Guild.Id)
                {
                    case 384640081660739594: // test server
                        switch (channel.Id)
                        {
                            case 393411399654703115: // 1v1
                                queue = RLQueue.DuelQueue(channel, false);
                                break;
                            case 393411426451980289: // 2v2
                                queue = RLQueue.DoublesQueue(channel, false);
                                break;
                            case 384666738056232970: // 3v3
                                queue = RLQueue.StandardQueue(channel, false);
                                break;
                        }
                        break;
                    case 373829959187169280: // live server
                        switch (channel.Id)
                        {
                            case 393692333469597696: // 1v1 Rank-A
                            case 393693176772296704: // Rank-Z
                            case 393693726565728257: // Rank-Y
                            case 393693993050832896: // Rank-X
                            case 393694545688133634: // Rank-W
                                queue = RLQueue.DuelQueue(channel, true);
                                break;
                            case 393695480946622465: // 2v2 Rank-A
                            case 393695741941514240: // Rank-Z
                            case 393695923026526208: // Rank-Y
                            case 393696051607109632: // Rank-X
                            case 393696168300773386: // Rank-W
                                queue = RLQueue.DoublesQueue(channel, true);
                                break;
                            case 386213046672162838: // 3v3 Rank-A
                            case 375039923603898369: // Rank-Z
                            case 385070323449462785: // Rank-Y
                            case 385420072996438021: // Rank-X
                            case 385393503833948160: // Rank-W
                                queue = RLQueue.StandardQueue(channel, true);
                                break;
                        }
                        break;
                    default:
                        await ReplyAsync("Queue's can currently only be used in the Cross-Net server and the test server.");
                        return;
                }

                if (queue == null)
                {
                    await ReplyAsync("This command can only be used in the `1v1`, `2v2` and `3v3` channels.");
                    return;
                }

                if (queues.TryAdd(Context.Channel.Id, queue))
                    await ReplyAsync("The queue is open. Type \"" + RLBot.COMMAND_PREFIX + "qjoin\", to join it.");
                else
                    await ReplyAsync("Failed to create a new queue, please try again.");
            }
            else
                await ReplyAsync("There is already an active queue. Type \"" + RLBot.COMMAND_PREFIX + "qjoin\", to join it.");                
        }

        [Command("qjoin")]
        [Alias("qj")]
        [Summary("Join the queue for 6man games")]
        [Remarks("qjoin")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        [RequireChannel(393411399654703115, 393411426451980289, 384666738056232970, 393692333469597696, 393693176772296704, 393693726565728257, 393694545688133634,
            393695480946622465, 393695741941514240, 393695923026526208, 393696051607109632, 393696168300773386, 386213046672162838, 375039923603898369, 385070323449462785,
            385420072996438021, 385393503833948160)]
        public async Task JoinQueueAsync()
        {
            if (!queues.TryGetValue(Context.Channel.Id, out RLQueue queue))
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            if (queue.Users.Count < queue.GetSize())
            {
                if (!queue.Users.ContainsKey(Context.Message.Author.Id))
                {
                    if (queue.Users.TryAdd(Context.Message.Author.Id, Context.Message.Author))
                        await ReplyAsync($"{Context.Message.Author.Mention} joined the queue. {queue.Users.Count}/{queue.GetSize()}");
                    else
                        await ReplyAsync($"Failed to add {Context.Message.Author} to the queue, please try again.");
                }
                else
                    await ReplyAsync("You've already joined the queue.");
            }
            else
                await ReplyAsync("The queue is full.");
        }

        [Command("qleave")]
        [Alias("ql")]
        [Summary("Leave the queue for 6man games")]
        [Remarks("qleave")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        [RequireChannel(393411399654703115, 393411426451980289, 384666738056232970, 393692333469597696, 393693176772296704, 393693726565728257, 393694545688133634,
            393695480946622465, 393695741941514240, 393695923026526208, 393696051607109632, 393696168300773386, 386213046672162838, 375039923603898369, 385070323449462785,
            385420072996438021, 385393503833948160)]
        public async Task LeaveQueueAsync()
        {
            if (!queues.TryGetValue(Context.Channel.Id, out RLQueue queue))
            {
                await ReplyAsync("There is no active queue.");
                return;
            }

            if (queue.Users.TryRemove(Context.Message.Author.Id, out SocketUser user))
                await ReplyAsync($"{Context.Message.Author.Mention} left the queue. {queue.Users.Count}/{queue.GetSize()}");
            else
                await ReplyAsync($"{Context.Message.Author.Mention}, you're not in the current queue.");
        }
        
        [Command("qstatus")]
        [Alias("qs")]
        [Summary("Show a list of all the people in the queue")]
        [Remarks("qstatus")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireChannel(393411399654703115, 393411426451980289, 384666738056232970, 393692333469597696, 393693176772296704, 393693726565728257, 393694545688133634,
            393695480946622465, 393695741941514240, 393695923026526208, 393696051607109632, 393696168300773386, 386213046672162838, 375039923603898369, 385070323449462785,
            385420072996438021, 385393503833948160)]
        public async Task ListOfPlayersInQueueAsync()
        {
            if (!queues.TryGetValue(Context.Channel.Id, out RLQueue queue))
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            if (queue.Users.Count == 0)
            {
                await ReplyAsync("There current queue is empty.");
                return;
            }

            string users = string.Join(", ", queue.Users.Values);
            await ReplyAsync("", embed: new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithTitle($"Current {queue.Playlist} queue {queue.Users.Count}/{queue.GetSize()}")
                        .WithDescription(users)
                        .Build());
        }
        
        [Command("qsub")]
        [Summary("Substitue one player for another in a queue")]
        [Remarks("qsub <queue ID> <@substitute> <@current player>")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        [RequireChannel(393411399654703115, 393411426451980289, 384666738056232970, 393692333469597696, 393693176772296704, 393693726565728257, 393694545688133634,
            393695480946622465, 393695741941514240, 393695923026526208, 393696051607109632, 393696168300773386, 386213046672162838, 375039923603898369, 385070323449462785,
            385420072996438021, 385393503833948160)]
        public async Task SubstitutePlayerAsync(long queueId, SocketUser subPlayer, SocketUser currentPlayer)
        {
            try
            {
                if (subPlayer.Id == currentPlayer.Id)
                {
                    await ReplyAsync("You cannot sub the user for himself!");
                    return;
                }

                if (subPlayer.IsBot || currentPlayer.IsBot)
                {
                    await ReplyAsync("You can't sub a bot into a queue!");
                    return;
                }

                // retrieve the queue data if it exists
                var queue = await Database.GetQueueAsync(queueId);
                if (queue == null)
                {
                    await ReplyAsync($"Didn't find queue {queueId}");
                    return;
                }

                // check if queue score is not yet set
                if (queue.ScoreTeamA != 0 || queue.ScoreTeamB != 0)
                {
                    await ReplyAsync($"The score for queue {queueId} has already been submitted, so players can't be substituted anymore");
                    return;
                }

                List<QueuePlayer> players = await Database.GetQueuePlayersAsync(queueId);
                if (players.Count == 0)
                {
                    await ReplyAsync($"Failed to retrieve the players from queue {queueId}, please try again.");
                    return;
                }

                // check if the player to sub out is in the queue and the player to sub in isn't
                QueuePlayer currentInQueue = null;
                foreach (QueuePlayer rec in players)
                {
                    if (rec.UserId == currentPlayer.Id)
                    {
                        currentInQueue = rec;
                    }
                    else if (rec.UserId == subPlayer.Id)
                    {
                        await ReplyAsync($"The player {subPlayer}, who is to be subbed in is already in queue {queueId}");
                        return;
                    }
                }

                if (currentInQueue == null)
                {
                    await ReplyAsync($"The player {currentPlayer}, who is to be subbed out is not in queue {queueId}");
                    return;
                }

                if (Context.Message.Author.Id != RLBot.APPLICATION_OWNER_ID && players.SingleOrDefault(x => x.UserId == Context.Message.Author.Id).Team != currentInQueue.Team)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention}, you can only substitue players from your own team!");
                    return;
                }

                await Database.SubstituteQueuePlayerAsync(queueId, subPlayer.Id, currentPlayer.Id);

                var voiceChannel = Context.Guild.VoiceChannels.SingleOrDefault(x => x.Name.Equals($"Team {(currentInQueue.Team == 0 ? "A" : "B")} #{queueId}"));
                await voiceChannel.AddPermissionOverwriteAsync(subPlayer, teamPerms);
                await voiceChannel.RemovePermissionOverwriteAsync(currentPlayer);

                await ReplyAsync($"Queue {queueId}: {subPlayer} substituted {currentPlayer}!");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }
        
        [Command("qreset")]
        [Alias("qr", "qclose")]
        [Summary("Removes the current queue")]
        [Remarks("qreset")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        [RequireChannel(393411399654703115, 393411426451980289, 384666738056232970, 393692333469597696, 393693176772296704, 393693726565728257, 393694545688133634,
            393695480946622465, 393695741941514240, 393695923026526208, 393696051607109632, 393696168300773386, 386213046672162838, 375039923603898369, 385070323449462785,
            385420072996438021, 385393503833948160)]
        public async Task ResetQueueAsync()
        {
            if (!queues.TryRemove(Context.Channel.Id, out RLQueue queue))
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }
            
            queue.Users.Clear();
            await ReplyAsync("The queue has been reset!");
        }

        [Command("qpick")]
        [Alias("qp")]
        [Summary("Randomly divide the players into 2 even teams")]
        [Remarks("qpick")]
        [RequireBotPermission(GuildPermission.ManageChannels | GuildPermission.MoveMembers | GuildPermission.EmbedLinks)]
        [RequireChannel(393411399654703115, 393411426451980289, 384666738056232970, 393692333469597696, 393693176772296704, 393693726565728257, 393694545688133634,
            393695480946622465, 393695741941514240, 393695923026526208, 393696051607109632, 393696168300773386, 386213046672162838, 375039923603898369, 385070323449462785,
            385420072996438021, 385393503833948160)]
        public async Task PickTeamsFromQueueAsync()
        {
            try
            {
                if (!queues.TryGetValue(Context.Channel.Id, out RLQueue queue))
                {
                    await ReplyAsync(NOT_OPEN);
                    return;
                }

                // remove offline users from the queue
                foreach (SocketUser user in queue.Users.Values)
                    if (user.Status == UserStatus.Offline) queue.Users.TryRemove(user.Id, out SocketUser removedOfflineUser);

                if (queue.Users.Count == queue.GetSize())
                {
                    string mentions = string.Join(", ", queue.Users.Values.Select(x => x.Mention));
                    var users = queue.Users.Values.ToList();

                    List<SocketUser> team_a = new List<SocketUser>();
                    List<SocketUser> team_b = new List<SocketUser>();
                    for (int i = 0; i < queue.GetSize(); i++)
                    {
                        int rng = rnd.Next(0, queue.Users.Count);
                        var rngUser = users[rng];
                        if (i % 2 == 0)
                            team_a.Add(rngUser);
                        else
                            team_b.Add(rngUser);

                        users.Remove(rngUser);

                        // remove this user from every queue he might be in
                        var queuesUserIsIn = queues.Values?.Where(x => x.Users.ContainsKey(rngUser.Id));
                        foreach (RLQueue joinedQueue in queuesUserIsIn)
                        {
                            joinedQueue?.Users.TryRemove(rngUser.Id, out SocketUser removedUser);
                        }
                    }

                    queues.TryRemove(Context.Channel.Id, out RLQueue removedQueue);

                    var embedBuilder = new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithTitle($"{queue.Playlist} teams")
                        .AddField("Team A", $"{string.Join("\n", team_a.Select(x => x.Mention))}", true)
                        .AddField("Team B", $"{string.Join("\n", team_b.Select(x => x.Mention))}", true)
                        .AddField("Match host", team_a[0].Mention);

                    string matchDetails = $"**__Match details__**\n";
                    if (queue.IsLeaderboardQueue)
                    {
                        long queueId = await Database.InsertQueueAsync(queue.Playlist, team_a, team_b);

                        embedBuilder.AddField("ID", queueId);
                        embedBuilder.WithFooter($"Submit the result using {RLBot.COMMAND_PREFIX}qresult {queueId} <score A> <score B>");

                        await ReplyAsync(mentions, false, embedBuilder.Build());

                        if (queue.Playlist != RLPlaylist.Duel)
                        {
                            // create voice channels, set player permissions and move the players into the channels
                            var teamA = CreateTeamVoiceChannelAsync(queueId, "Team A", team_a);
                            var teamB = CreateTeamVoiceChannelAsync(queueId, "Team B", team_b);

                            await Task.WhenAll(teamA, teamB);
                        }

                        matchDetails += $"**ID:** {queueId}\n**Name:** CNQ{queueId}\n";
                    }
                    else
                    {
                        await ReplyAsync(mentions, false, embedBuilder.Build());
                        matchDetails += $"**Name:** CNQ{rnd.Next(100, 100000)}\n";
                    }

                    // DM all the players in the queue the server details
                    matchDetails += $"**Password:** {GeneratePassword()}";
                    foreach (SocketUser user in team_a.Union(team_b))
                    {
                        try
                        {
                            var DMChannel = await user.GetOrCreateDMChannelAsync();
                            await DMChannel.SendMessageAsync(matchDetails);
                        }
                        catch (HttpException ex)
                        when (ex.DiscordCode == 50007)
                        {
                            await ReplyAsync($"{user.Mention}, you are blocking DM's, unable to provide the match details.");
                        }
                    }
                }
                else
                    await ReplyAsync(string.Format(NOT_ENOUGH_PLAYERS, queue.Users.Count, queue.GetSize()));
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        /*
        [Command("qcaptain")]
        [Alias("qc")]
        [Summary("Randomly select 2 captains from the queue")]
        [Remarks("qcaptain")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task PickCaptainsFromQueueAsync()
        {
            var queue = queues.Where(x => x.Channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            if (queue.GetSize() == 2)
            {
                await ReplyAsync($"A {queue.Playlist} queue doesn't allow this function.");
                return;
            }

            // remove offline users from the queue
            queue.Users.RemoveAll(x => x.Status == UserStatus.Offline);

            if (queue.Users.Count == queue.GetSize())
            {
                string mentions = string.Join(", ", queue.Users.Select(x => x.Mention));

                List<SocketUser> captains = new List<SocketUser>();

                for (int i = 0; i < 2; i++)
                {
                    int rng = rnd.Next(0, queue.Users.Count);
                    captains.Add(queue.Users[rng]);
                    queue.Users.Remove(queue.Users[rng]);

                    // remove this user from every queue he might be in
                    Parallel.ForEach(queues.Where(x => x.Users.Contains(queue.Users[rng])), x => x.Users.Remove(queue.Users[rng]));
                }

                await ReplyAsync(mentions, embed: new EmbedBuilder()
                    .WithColor(RLBot.EMBED_COLOR)
                    .WithTitle("Inhouse captains")
                    .AddField("Captain A", captains[0].Mention, true)
                    .AddField("Captain B", captains[1].Mention, true)
                    .AddField("Remaining", string.Join(", ", queue.Users.Select(x => x.Mention)))
                    .Build());
                
                // remove the remaning users from queue's they might be in
                foreach (SocketUser user in queue.Users)
                    Parallel.ForEach(queues.Where(x => x.Users.Contains(user)), x => x.Users.Remove(user));
                queues.Remove(queue);
            }
            else
                await ReplyAsync(string.Format(NOT_ENOUGH_PLAYERS, queue.Users.Count, queue.GetSize()));
        }*/

        [Command("qresult", RunMode = RunMode.Async)]
        [Summary("Submit the score for a queue")]
        [Remarks("qresult <queue ID> <score team A> <score team B>")]
        [RequireBotPermission(GuildPermission.SendMessages | GuildPermission.ManageMessages | GuildPermission.AddReactions | GuildPermission.ManageChannels | GuildPermission.MoveMembers)]
        [RequireChannel(398133512902934529)]
        public async Task SetResultAsync(long queueId, byte scoreTeamA, byte scoreTeamB)
        {
            try
            {
                if (scoreTeamA < 0 || scoreTeamB < 0 || scoreTeamA == scoreTeamB)
                {
                    await ReplyErrorAndDeleteAsync(Context.Message, "Invalid scores.", new TimeSpan(0, 0, 5));
                    return;
                }

                var queue = await Database.GetQueueAsync(queueId);
                if (queue == null)
                {
                    await ReplyErrorAndDeleteAsync(Context.Message, $"Didn't find queue {queueId}!", new TimeSpan(0, 0, 5));
                    return;
                }

                // check if queue score is not yet set
                if (queue.ScoreTeamA != 0 || queue.ScoreTeamB != 0)
                {
                    await ReplyErrorAndDeleteAsync(Context.Message, $"The score for queue {queueId} has already been submitted!", new TimeSpan(0, 0, 5));
                    return;
                }

                List<QueuePlayer> players = await Database.GetQueuePlayersAsync(queueId);
                if (players.Count == 0)
                {
                    await ReplyErrorAndDeleteAsync(Context.Message, $"Failed to retrieve the players from queue {queueId}, please try again.", new TimeSpan(0, 0, 5));
                    return;
                }

                // check if the author is in the queue
                if (players.SingleOrDefault(x => x.UserId == Context.Message.Author.Id) == null)
                {
                    await ReplyErrorAndDeleteAsync(Context.Message, $"Only players from queue {queueId} can set the score!", new TimeSpan(0, 0, 5));
                    return;
                }

                // retrieve the voice channels and all the players from the queue
                var channels = Context.Guild.VoiceChannels.Where(x => x.Name.Contains($"#{queueId}")).ToArray();
                var channelUsers = channels.SelectMany(x => x.Users).ToArray();

                // move users back to the general voice channel
                var usersTasks = new Task[channelUsers.Count()];
                for (int i = 0; i < usersTasks.Length; i++)
                {
                    usersTasks[i] = channelUsers[i].ModifyAsync(x => x.ChannelId = 385131574817062915);
                }

                await Task.WhenAll(usersTasks);

                // delete the voice channels
                var channelsTasks = new Task[channels.Count()];
                for (int i = 0; i < channels.Length; i++)
                {
                    channelsTasks[i] = channels[i].DeleteAsync();
                }
                await Task.WhenAll(channelsTasks);

                // add reactions to the message so users can select if the result is correct or not
                var msg = Context.Message;
                Emoji check = new Emoji("✅");
                Emoji cancel = new Emoji("❌");
                await msg.AddReactionAsync(check);
                await msg.AddReactionAsync(cancel);

                bool success = false;
                var startTime = DateTime.Now;
                var votesNeeded = (players.Count / 2) + 1;
                int checkVotes = 0;
                int cancelVotes = 0;
                var playerIds = players.Select(x => x.UserId).ToList();
                while ((DateTime.Now - startTime).TotalMinutes < 10)
                {
                    var checkUsersTask = msg.GetReactionUsersAsync(check);
                    var cancelUsersTask = msg.GetReactionUsersAsync(cancel);
                    await Task.WhenAll(checkUsersTask, cancelUsersTask);

                    var checkUsers = await checkUsersTask;
                    var cancelUsers = await cancelUsersTask;

                    // remove reactions made by users not in the queue and count the votes
                    List<Task> tasks = new List<Task>();
                    checkVotes = 0;
                    foreach (IUser checkUser in checkUsers)
                    {
                        if (checkUser.IsBot) continue;

                        if (playerIds.Contains(checkUser.Id))
                            checkVotes++;
                        else
                            tasks.Add(msg.RemoveReactionAsync(check, checkUser));
                    }

                    cancelVotes = 0;
                    foreach (IUser cancelUser in cancelUsers)
                    {
                        if (cancelUser.IsBot) continue;

                        if (playerIds.Contains(cancelUser.Id))
                            cancelVotes++;
                        else
                            tasks.Add(msg.RemoveReactionAsync(cancel, cancelUser));
                    }

                    await Task.WhenAll(tasks);

                    // check the votes
                    if (checkVotes >= votesNeeded)
                    {
                        success = true;
                        break;
                    }
                    else if (cancelVotes >= votesNeeded)
                    {
                        success = false;
                        break;
                    }

                    await Task.Delay(1000);
                }

                // Check if a majority vote was reached for check or if the check votes were higher then the cancel votes
                if (success || checkVotes > cancelVotes)
                {
                    // calculate new elos
                    var teamA = players.Where(x => x.Team == 0);
                    var teamB = players.Where(x => x.Team == 1);
                    var teamAElo = teamA.Sum(x => x.Elo);
                    var teamBElo = teamB.Sum(x => x.Elo);
                    foreach (QueuePlayer player in teamA)
                    {
                        player.Elo = CalculateNewElo(player.Elo, teamAElo, teamBElo, teamA.Count(), scoreTeamA > scoreTeamB);
                    }
                    foreach (QueuePlayer player in teamB)
                    {
                        player.Elo = CalculateNewElo(player.Elo, teamBElo, teamAElo, teamB.Count(), scoreTeamB > scoreTeamA);
                    }

                    // set the score
                    await Database.SetQueueResultAsync(queueId, scoreTeamA, scoreTeamB, queue.Playlist, players);
                    await msg.AddReactionAsync(new Emoji("🆗"));

                    // promote/demote players TODO
                }
                else
                    await msg.DeleteAsync();
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        private async Task CreateTeamVoiceChannelAsync(long queueId, string roomName, List<SocketUser> team)
        {
            // create voice channel
            var voiceChannel = await Context.Guild.CreateVoiceChannelAsync($"{roomName} #{queueId}");
            (voiceChannel as IVoiceChannel)?.ModifyAsync(x =>
            {
                x.UserLimit = team.Count;
            });

            // set the bot's permissions
            await voiceChannel.AddPermissionOverwriteAsync(Context.Client.CurrentUser, botPerms);

            // set the team's players permissions and move player
            Task[] tasks = new Task[team.Count];
            int i = 0;
            foreach (SocketUser user in team)
            {
                var gu = Context.Guild.GetUser(user.Id);
                tasks[i] = voiceChannel.AddPermissionOverwriteAsync(user as SocketGuildUser, teamPerms);
                i++;
            }

            await Task.WhenAll(tasks);

            // remove all permissions for everyone else
            await voiceChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, everyonePerms);

            // move the team into the voice channel
            tasks = new Task[team.Count];
            i = 0;
            foreach (SocketUser user in team)
            {
                tasks[i] = Context.Guild.GetUser(user.Id)?.ModifyAsync(x => x.Channel = voiceChannel);
                i++;
            }

            await Task.WhenAll(tasks);
        }

        private string GeneratePassword()
        {
            string set = "abcdefghijklmnopqrstuvwxyz1234567890";
            string password = "";
            for (int i = 0; i < 3; i++)
            {
                int p = rnd.Next(set.Length);
                password += set.Substring(p, 1);
            }

            return password;
        } 

        private async Task ReplyErrorAndDeleteAsync(SocketMessage messageToDelete, string message, TimeSpan timeout)
        {
            if (messageToDelete == null) return;

            await ReplyAndDeleteAsync(message, timeout: timeout).ConfigureAwait(false);
            await Task.Delay(timeout).ContinueWith(async _ => await messageToDelete.DeleteAsync().ConfigureAwait(false)).ConfigureAwait(false);
        }

        private short CalculateNewElo(short elo, int teamElo, int otherTeamElo, int teamSize, bool win)
        {
            var streakMultiplier = 1; // to do later
            var x = Math.Round(10 * Math.Pow(otherTeamElo / (double)teamElo, 1) * Math.Pow((teamElo / (double)teamSize) / elo, 3) * streakMultiplier);
            if (x < 4)
                x = 4;
            else if (x > 36)
                x = 36;

            short newElo = (short)(win ? elo + x : elo - x);

            if (newElo < 0)
                newElo = 0;
            
            return newElo;
        }
    }
}