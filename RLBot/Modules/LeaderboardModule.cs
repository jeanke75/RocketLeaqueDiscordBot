using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RLBot.Models;

namespace RLBot.Modules
{
    [Name("Leaderboard")]
    [Summary("Everything involving the leaderboard")]
    public class LeaderboardModule : ModuleBase<SocketCommandContext>
    {
        readonly DiscordSocketClient _client;
        readonly string DB_QUEUE_SELECT = "SELECT ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR(qp.Team = 1 AND q.ScoreTeamA<q.ScoreTeamB)) THEN 1 END),0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.QueueID = q.QueueID WHERE qp.UserID = @UserID";

        public LeaderboardModule(DiscordSocketClient client)
        {
            _client = client;
        }

        [Command("stats")]
        [Summary("Returns leaderboard info about the current user, or the user parameter, if one passed.")]
        [Remarks("stats")]
        public async Task StatsAsync(IUser user = null)
        {
            var userInfo = user ?? Context.Message.Author;

            LeaderboardRecord recordTotal = null;
            LeaderboardRecord recordMonthly = null;
            using (SqlConnection conn = RLBot.GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    var queueTotalTask = GetQueueLeaderboardRecordAsync(conn, DB_QUEUE_SELECT, userInfo.Id);
                    string monthly = " AND q.Created >= CAST(DATEADD(dd, -DAY(GETDATE()) + 1, GETDATE()) AS DATE) AND q.Created < CAST(DATEADD(month, DATEDIFF(month, 0, GETDATE()) + 1, 0) AS DATE)";
                    var queueMonthlyTask = GetQueueLeaderboardRecordAsync(conn, DB_QUEUE_SELECT + monthly, userInfo.Id);

                    await Task.WhenAll(queueTotalTask, queueMonthlyTask);
                    
                    recordTotal = await queueTotalTask;
                    recordMonthly = await queueMonthlyTask;
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

            if (recordTotal != null && recordMonthly != null)
                await ReplyAsync("", embed: new EmbedBuilder()
                                                .WithColor(RLBot.EMBED_COLOR)
                                                .WithTitle($":trophy: Leaderboard info - {userInfo} :trophy:")
                                                .AddInlineField("Monthly", $"Wins: {recordMonthly.Wins}\nGames Played: {recordMonthly.TotalGames}")
                                                .AddInlineField("All-Time", $"Wins: {recordTotal.Wins}\nGames Played: {recordTotal.TotalGames}")
                                                .Build());
            else
            {
                var application = await _client.GetApplicationInfoAsync();
                await ReplyAsync($"Something went wrong retrieving the leaderboard data. Please contact {application.Owner}.");
            }
        }

        private async Task<LeaderboardRecord> GetQueueLeaderboardRecordAsync(SqlConnection conn, string command, ulong userId)
        {
            LeaderboardRecord rec = null;
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                cmd.CommandText = command;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        rec = new LeaderboardRecord()
                        {
                            Wins = (int)reader["Wins"],
                            TotalGames = (int)reader["TotalGames"]
                        };
                    }
                    reader.Close();
                }
            }
            return rec;
        }
    }
}