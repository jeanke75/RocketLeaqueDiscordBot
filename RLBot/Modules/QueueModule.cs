using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RLBot.Models;

namespace RLBot.Modules
{
    [Name("Queue")]
    [Summary("Creation and utilization of a queue")]
    public class QueueModule : ModuleBase<SocketCommandContext>
    {
        private static ConcurrentDictionary<ulong, RLQueue> queues = new ConcurrentDictionary<ulong, RLQueue>();
        private static Random rnd = new Random();
        private readonly string NOT_OPEN = "There is no open queue atm. Type \"" + RLBot.COMMAND_PREFIX + "qopen\", to start a new one.";
        private readonly string NOT_ENOUGH_PLAYERS = "Not enough players have joined the queue yet! {0}/{1}";

        private readonly string DB_QUEUE_SELECT = "SELECT * FROM Queue WHERE QueueID = @QueueID;";
        private readonly string DB_QUEUEPLAYER_SELECT = "SELECT * FROM QueuePlayer WHERE QueueID = @QueueID AND UserID = @UserID;";
        private readonly string DB_QUEUE_UPDATE = "UPDATE Queue SET ScoreTeamA = @ScoreTeamA, ScoreTeamB = @ScoreTeamB WHERE QueueID = @QueueID;";
        private readonly string DB_QUEUE_SUBSTITUTE = "UPDATE QueuePlayer SET UserID = @NewUserID WHERE QueueID = @QueueID AND UserID = @CurrentUserID;";
        
        [Command("qopen")]
        [Alias("qo")]
        [Summary("Create a new queue from which two 3man teams will be picked")]
        [Remarks("qopen")]
        [RequireBotPermission(GuildPermission.SendMessages)]
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
        [Remarks("qsub <queue ID> <player to sub in> <player to sub out>")]
        [RequireOwner]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task SubstitutePlayerAsync(long queueId, SocketUser playerIn, SocketUser playerOut)
        {
            if (playerIn.Id == playerOut.Id)
            {
                await ReplyAsync("You cannot sub the user for himself!");
                return;
            }

            if (playerIn.IsBot || playerOut.IsBot)
            {
                await ReplyAsync("You can't sub a bot into a queue!");
                return;
            }

            using (SqlConnection conn = RLBot.GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        bool queueExists = false;
                        bool scoreAlreadySet = false;

                        // retrieve the queue data if it exists
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                            cmd.CommandText = DB_QUEUE_SELECT;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    await reader.ReadAsync();
                                    queueExists = true;
                                    scoreAlreadySet = (byte)reader["ScoreTeamA"] != 0 || (byte)reader["ScoreTeamB"] != 0;
                                }
                                reader.Close();
                            }
                        }

                        // check if the queue exists
                        if (!queueExists)
                        {
                            await ReplyAsync($"Didn't find queue {queueId}");
                            return;
                        }

                        // check if queue score is not yet set
                        if (scoreAlreadySet)
                        {
                            await ReplyAsync($"The score for queue {queueId} has already been submitted, so players can't be substituted anymore");
                            return;
                        }

                        bool currentInQueue = false;
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                            cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)playerOut.Id;
                            cmd.CommandText = DB_QUEUEPLAYER_SELECT;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                currentInQueue = reader.HasRows;
                                reader.Close();
                            }
                        }

                        if (!currentInQueue)
                        {
                            await ReplyAsync($"The player {playerOut}, who is to be subbed out is not in queue {queueId}");
                            return;
                        }

                        // check if the player in isn't already part of the queue
                        bool subInQueue = false;
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                            cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)playerIn.Id;
                            cmd.CommandText = DB_QUEUEPLAYER_SELECT;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                subInQueue = reader.HasRows;
                                reader.Close();
                            }
                        }

                        if (!currentInQueue)
                        {
                            await ReplyAsync($"The player {playerIn}, who is to be subbed in is already in queue {queueId}");
                            return;
                        }

                        // substitute players
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@QueueID", DbType.Decimal).Value = (decimal)queueId;
                            cmd.Parameters.AddWithValue("@NewUserID", DbType.Decimal).Value = (decimal)playerIn.Id;
                            cmd.Parameters.AddWithValue("@CurrentUserID", DbType.Decimal).Value = (decimal)playerOut.Id;
                            cmd.CommandText = DB_QUEUE_SUBSTITUTE;
                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();

                        await ReplyAsync($"Queue {queueId}: {playerIn} substituted {playerOut}!");
                    }
                    catch (Exception ex)
                    {
                        await ReplyAsync(ex.Message);
                        throw ex;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }
        
        [Command("qreset")]
        [Alias("qr", "qclose")]
        [Summary("Removes the current queue")]
        [Remarks("qreset")]
        [RequireBotPermission(GuildPermission.SendMessages)]
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
        public async Task PickTeamsFromQueueAsync()
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
                    .AddField("Team A", $"{string.Join("\n", team_a)}", true)
                    .AddField("Team B", $"{string.Join("\n", team_b)}", true);
                
                if (queue.IsLeaderboardQueue)
                {
                    long queueId = await InsertQueueData(queue.Playlist, team_a, team_b);

                    embedBuilder.AddField("ID", queueId);
                    embedBuilder.WithFooter($"Submit the result using {RLBot.COMMAND_PREFIX}qresult {queueId} <score A> <score B>");

                    var msg = ReplyAsync(mentions, false, embedBuilder.Build());

                    // create voice channels, set player permissions and move the players into the channels
                    var teamA = CreateTeamVoiceChannelAsync(queueId, "Team A", team_a);
                    var teamB = CreateTeamVoiceChannelAsync(queueId, "Team B", team_b);

                    await Task.WhenAll(msg, teamA, teamB);
                }
                else
                {
                    await ReplyAsync(mentions, false, embedBuilder.Build());
                }
            }
            else
                await ReplyAsync(string.Format(NOT_ENOUGH_PLAYERS, queue.Users.Count, queue.GetSize()));
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

        [Command("qresult")]
        [Summary("Submit the score for a queue")]
        [Remarks("qresult <queue ID> <score team A> <score team B>")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task SetResultAsync(long queueId, byte scoreTeamA, byte scoreTeamB)
        {
            if (scoreTeamA < 0 || scoreTeamB < 0 || scoreTeamA == scoreTeamB)
            {
                await ReplyAsync("Invalid scores.");
                return;
            }

            using (SqlConnection conn = RLBot.GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        bool queueExists = false;
                        bool scoreAlreadySet = false;

                        // retrieve the queue data if it exists
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                            cmd.CommandText = DB_QUEUE_SELECT;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    await reader.ReadAsync();
                                    queueExists = true;
                                    scoreAlreadySet = (byte)reader["ScoreTeamA"] != 0 || (byte)reader["ScoreTeamB"] != 0;
                                }
                                reader.Close();
                            }
                        }

                        // check if the queue exists
                        if (!queueExists)
                        {
                            await ReplyAsync($"Didn't find queue {queueId}");
                            return;
                        }

                        // check if queue score is not yet set
                        if (scoreAlreadySet)
                        {
                            await ReplyAsync($"The score for queue {queueId} has already been submitted");
                            return;
                        }

                        // check if author is part of the queue
                        bool authorInQueue = false;
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                            cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)Context.Message.Author.Id;
                            cmd.CommandText = DB_QUEUEPLAYER_SELECT;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                authorInQueue = reader.HasRows;
                                reader.Close();
                            }
                        }

                        if (!authorInQueue)
                        {
                            await ReplyAsync($"Only players from queue {queueId} can set the score");
                            return;
                        }

                        // set the score
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@QueueID", DbType.Decimal).Value = (decimal)queueId;
                            cmd.Parameters.AddWithValue("@ScoreTeamA", DbType.Byte).Value = scoreTeamA;
                            cmd.Parameters.AddWithValue("@ScoreTeamB", DbType.Byte).Value = scoreTeamB;
                            cmd.CommandText = DB_QUEUE_UPDATE;
                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();

                        var channels = Context.Guild.VoiceChannels.Where(x => x.Name.Contains($"#{queueId}")).ToArray();
                        var tasks = new Task[channels.Count()];
                        for (int i = 0; i < channels.Length; i++)
                        {
                            tasks[i] = channels[i].DeleteAsync();
                        }
                        await Task.WhenAll(tasks);

                        await ReplyAsync($"The score for queue {queueId} has been submitted");
                    }
                    catch (Exception ex)
                    {
                        await ReplyAsync(ex.Message);
                        throw ex;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }

        private async Task<long> InsertQueueData(RLPlaylist type, List<SocketUser> team_a, List<SocketUser> team_b)
        {
            long queueId = -1;
            using (SqlConnection conn = RLBot.GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@Type", DbType.Byte).Value = (byte)type;
                            cmd.CommandText = "INSERT INTO Queue(ScoreTeamA, ScoreTeamB, Created, Playlist) OUTPUT INSERTED.QueueID VALUES(0, 0, GETDATE(), @Type);";
                            var res = await cmd.ExecuteScalarAsync();
                            queueId = (long)res;
                        }

                        var tasks = new Task[team_a.Count + team_b.Count];
                        int i = 0;
                        foreach(SocketUser user in team_a)
                        {
                            tasks[i] = InsertQueuePlayer(conn, tr, queueId, user.Id, 0);
                            i++;
                        }
                        foreach (SocketUser user in team_b)
                        {
                            tasks[i] = InsertQueuePlayer(conn, tr, queueId, user.Id, 1);
                            i++;
                        }

                        await Task.WhenAll(tasks);

                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        await ReplyAsync(ex.Message);

                        tr.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            return queueId;
        }

        private async Task InsertQueuePlayer(SqlConnection conn, SqlTransaction tr, long queueId, ulong userId, byte team)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;
                cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                cmd.Parameters.AddWithValue("@Team", DbType.Byte).Value = team;
                cmd.CommandText = "INSERT INTO QueuePlayer(QueueId, UserID, Team) VALUES(@QueueId, @UserID, @Team);";
                await cmd.ExecuteNonQueryAsync();
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
            OverwritePermissions botPerms = new OverwritePermissions(PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow);
            await voiceChannel.AddPermissionOverwriteAsync(Context.Client.CurrentUser, botPerms);

            // set the team's players permissions and move player
            OverwritePermissions teamPerms = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny);
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
            OverwritePermissions everyonePerms = new OverwritePermissions(PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);
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
    }
}