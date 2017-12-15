using System;
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
        static List<RLQueue> queues = new List<RLQueue>();
        static Random rnd = new Random();
        readonly string DM = "The queue cannot be used in a DM.";
        readonly string NOT_OPEN = "There is no open queue atm. Type \"" + RLBot.COMMAND_PREFIX + "qopen\", to start a new one.";
        readonly string NOT_ENOUGH_PLAYERS = "Not enough players have joined the queue yet! {0}/6";

        readonly string DB_QUEUE_SELECT = "SELECT * FROM Queue WHERE QueueID = @QueueID;";
        readonly string DB_QUEUEPLAYER_SELECT = "SELECT * FROM QueuePlayer WHERE QueueID = @QueueID AND UserID = @UserID;";
        readonly string DB_QUEUE_UPDATE = "UPDATE Queue SET ScoreTeamA = @ScoreTeamA, ScoreTeamB = @ScoreTeamB WHERE QueueID = @QueueID;";
        
        [Command("qopen")]
        [Alias("qo")]
        [Summary("Create a new queue from which two 3man teams will be picked")]
        [Remarks("qopen")]
        public async Task OpenQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                queue = new RLQueue();
                queue.created = DateTime.Now;
                queue.channel = Context.Channel as SocketGuildChannel;
                queues.Add(queue);

                await ReplyAsync("The queue is open. Type \"" + RLBot.COMMAND_PREFIX + "qjoin\", to join it.");
            }
            else
                await ReplyAsync("There is already an active queue. Type \"" + RLBot.COMMAND_PREFIX + "qjoin\", to join it.");                
        }

        [Command("qjoin")]
        [Alias("qj")]
        [Summary("Join the queue for 6man games")]
        [Remarks("qjoin")]
        public async Task JoinQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            if (queue.users.Count < 6)
            {
                if (queue.users.Where(x => x.Id == Context.Message.Author.Id).FirstOrDefault() == null)
                {
                    queue.users.Add(Context.Message.Author);
                    await ReplyAsync($"{Context.Message.Author.Mention} joined the queue.");
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
        public async Task LeaveQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
                await ReplyAsync("There is no active queue.");
            else
            {
                var user = queue.users.Where(x => x.Id == Context.Message.Author.Id).FirstOrDefault();
                if (user != null)
                {
                    queue.users.Remove(user);
                    await ReplyAsync($"{Context.Message.Author.Mention} left the queue.");
                }
                else
                    await ReplyAsync("You're not in the current queue.");
            }
        }

        [Command("qstatus")]
        [Alias("qs")]
        [Summary("Show a list of all the people in the queue")]
        [Remarks("qstatus")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task ListOfPlayersInQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            if (queue.users.Count == 0)
            {
                await ReplyAsync("There current queue is empty.");
                return;
            }

            string users = string.Join(", ", queue.users);
            await ReplyAsync("", embed: new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithTitle("Current queue")
                        .WithDescription(users)
                        .Build());
        }

        [Command("qreset")]
        [Alias("qr")]
        [Summary("Removes the current queue")]
        [Remarks("qreset")]
        public async Task ResetQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            queue.users.Clear();
            queues.Remove(queue);
            await ReplyAsync("The queue has been reset!");
        }

        [Command("qpick")]
        [Alias("qp")]
        [Summary("Randomly divide the 6 players into 2 even teams")]
        [Remarks("qpick")]
        [RequireBotPermission(GuildPermission.ManageChannels | GuildPermission.MoveMembers | GuildPermission.EmbedLinks)]
        public async Task PickTeamsFromQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            // remove offline users from the queue
            queue.users.RemoveAll(x => x.Status == UserStatus.Offline);

            if (queue.users.Count == 6)
            {
                List<SocketUser> team_a = new List<SocketUser>();
                List<SocketUser> team_b = new List<SocketUser>();
                for (int i = 0; i < 6; i++)
                {
                    int rng = rnd.Next(0, queue.users.Count);
                    if (i % 2 == 0)
                        team_a.Add(queue.users[rng]);
                    else
                        team_b.Add(queue.users[rng]);
                    
                    queue.users.Remove(queue.users[rng]);
                }
                queue.users.Clear();
                queues.Remove(queue);

                long queueId = await InsertQueueData(team_a, team_b);

                // send message to channel
                await ReplyAsync("", embed: new EmbedBuilder()
                    .WithColor(RLBot.EMBED_COLOR)
                    .WithTitle("Inhouse 3v3 teams")
                    .AddField("Team A", $"{team_a[0].Mention}\n{team_a[1].Mention}\n{team_a[2].Mention}", true)
                    .AddField("Team B", $"{team_b[0].Mention}\n{team_b[1].Mention}\n{team_b[2].Mention}", true)
                    .AddField("ID", queueId)
                    .WithFooter($"Submit the result using {RLBot.COMMAND_PREFIX}qresult")
                    .Build());
            }
            else
                await ReplyAsync(string.Format(NOT_ENOUGH_PLAYERS, queue.users.Count));
        }

        [Command("qcaptain")]
        [Alias("qc")]
        [Summary("Randomly select 2 captains from the queue")]
        [Remarks("qcaptain")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task PickCaptainsFromQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            // remove offline users from the queue
            queue.users.RemoveAll(x => x.Status == UserStatus.Offline);

            if (queue.users.Count == 6)
            {
                List<SocketUser> captains = new List<SocketUser>();

                for (int i = 0; i < 2; i++)
                {
                    int rng = rnd.Next(0, queue.users.Count);
                    captains.Add(queue.users[rng]);
                    queue.users.Remove(queue.users[rng]);
                }

                await ReplyAsync("", embed: new EmbedBuilder()
                    .WithColor(RLBot.EMBED_COLOR)
                    .WithTitle("Inhouse captains")
                    .AddField("Captain A", captains[0].Mention, true)
                    .AddField("Captain B", captains[1].Mention, true)
                    .AddField("Remaining", string.Join(", ", queue.users.Select(x => x.Mention)))
                    .Build());

                queue.users.Clear();
                queues.Remove(queue);
            }
            else
                await ReplyAsync(string.Format(NOT_ENOUGH_PLAYERS, queue.users.Count));
        }

        [Command("qresult")]
        [Summary("Submit the score for a queue")]
        [Remarks("qresult <queue ID> <score team A> <score team B>")]
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

        private async Task<long> InsertQueueData(List<SocketUser> team_a, List<SocketUser> team_b)
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
                            cmd.CommandText = "INSERT INTO Queue(ScoreTeamA, ScoreTeamB, Created) OUTPUT INSERTED.QueueID VALUES(0, 0, GETDATE());";
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
    }
}