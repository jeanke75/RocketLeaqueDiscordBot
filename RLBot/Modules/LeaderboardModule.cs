using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RLBot.Models;
using RLBot.TypeReaders;

namespace RLBot.Modules
{
    [Name("Leaderboard")]
    [Summary("Everything involving the leaderboard")]
    public class LeaderboardModule : ModuleBase<SocketCommandContext>
    {
        readonly string DB_QUEUE_SELECT = "SELECT ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA<q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24)) AND q.Playlist = @Playlist AND qp.UserID = @UserID";
        readonly string DB_TOP_5_MONTHLY = "SELECT TOP 5 qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24)) AND q.Created >= CAST(DATEADD(dd, -DAY(GETDATE()) + 1, GETDATE()) AS DATE) AND q.Created < CAST(DATEADD(month, DATEDIFF(month, 0, GETDATE()) + 1, 0) AS DATE) AND q.Playlist = @Playlist GROUP BY qp.UserID ORDER BY 2 DESC, 3 ASC";
        readonly string DB_TOP_5_ALL_TIME = "SELECT TOP 5 qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24)) AND q.Playlist = @Playlist GROUP BY qp.UserID ORDER BY 2 DESC, 3 ASC";

        [Command("stats")]
        [Summary("Returns leaderboard info about the current user, or the user parameter, if one passed.")]
        [Remarks("stats")]
        public async Task StatsAsync([OverrideTypeReader(typeof(RLPlaylistTypeReader))] RLPlaylist playlist, IUser user = null)
        {
            var userInfo = user ?? Context.Message.Author;

            LeaderboardRecord recordTotal = null;
            LeaderboardRecord recordMonthly = null; 
            
            using (SqlConnection conn = RLBot.GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    var queueTotalTask = GetQueueStatsAsync(conn, DB_QUEUE_SELECT, playlist, userInfo.Id);
                    string monthly = " AND q.Created >= CAST(DATEADD(dd, -DAY(GETDATE()) + 1, GETDATE()) AS DATE) AND q.Created < CAST(DATEADD(month, DATEDIFF(month, 0, GETDATE()) + 1, 0) AS DATE)";
                    var queueMonthlyTask = GetQueueStatsAsync(conn, DB_QUEUE_SELECT + monthly, playlist, userInfo.Id);

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
                                                .WithTitle($":trophy: {playlist} Leaderboard - {userInfo} :trophy:")
                                                .AddField("Monthly", $"Wins: {recordMonthly.Wins}\nGames Played: {recordMonthly.TotalGames}", true)
                                                .AddField("All-Time", $"Wins: {recordTotal.Wins}\nGames Played: {recordTotal.TotalGames}", true)
                                                .Build());
            else
            {
                var application = await Context.Client.GetApplicationInfoAsync();
                await ReplyAsync($"Something went wrong retrieving the leaderboard data. Please contact {application.Owner}.");
            }
        }

        [Command("top5")]
        [Summary("Returns the 5 players with the most wins for both Monthly and All-Time.")]
        [Remarks("top5")]
        public async Task Top5Async([OverrideTypeReader(typeof(RLPlaylistTypeReader))] RLPlaylist playlist)
        {
            LeaderboardRecord[] monthlyTop5 = null;
            LeaderboardRecord[] allTimeTop5 = null;
            using (SqlConnection conn = RLBot.GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    var monthlyTask = GetQueueTop5Async(conn, DB_TOP_5_MONTHLY, playlist);
                    var allTimeTask = GetQueueTop5Async(conn, DB_TOP_5_ALL_TIME, playlist);
                    await Task.WhenAll(monthlyTask, allTimeTask);
                    monthlyTop5 = (await monthlyTask).ToArray();
                    allTimeTop5 = (await allTimeTask).ToArray();
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

            var builder = new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithTitle($":trophy: {playlist} Leaderboard :trophy:");

            EmbedWithTop5(builder, "Monthly", monthlyTop5);
            EmbedWithTop5(builder, "All-Time", allTimeTop5);

            if (builder.Fields.Count > 0)
                await ReplyAsync("", false, builder.Build());
            else
                await ReplyAsync("No leaderboard data found.");
        }

        private async Task<LeaderboardRecord> GetQueueStatsAsync(SqlConnection conn, string command, RLPlaylist playlist, ulong userId)
        {
            LeaderboardRecord rec = null;
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)playlist;
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

        private async Task<List<LeaderboardRecord>> GetQueueTop5Async(SqlConnection conn, string command, RLPlaylist playlist)
        {
            List<LeaderboardRecord> records = new List<LeaderboardRecord>();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)playlist;
                cmd.CommandText = command;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (await reader.ReadAsync())
                    {
                        LeaderboardRecord rec = new LeaderboardRecord()
                        {
                            UserID = (ulong)(decimal)reader["UserID"],
                            Wins = (int)reader["Wins"],
                            TotalGames = (int)reader["TotalGames"]
                        };
                        records.Add(rec);
                    }
                    reader.Close();
                }
            }
            return records;
        }

        private void EmbedWithTop5(EmbedBuilder builder, string title, LeaderboardRecord[] top5)
        {
            if (top5 == null) return;
            if (top5.Length != 5) return;
            
            string s = "";
            for (int i = 0; i < 5; i++)
            {
                string icon = "";
                if (i == 0)
                    icon = ":first_place:";
                else if (i == 1)
                    icon = ":second_place:";
                else if (i == 2)
                    icon = ":third_place:";
                else
                    icon = ":eight_pointed_black_star:";
                
                float perc = (float)Math.Round(top5[i].Wins * 100.0f / top5[i].TotalGames, 2);

                // in case the user isn't in any of the servers the bot is in anymore show the user id
                var user = Context.Client.GetUser(top5[i].UserID);
                string username = (user != null ? user.ToString() : $"<{top5[i].UserID}>");
                s += $"{icon} {username} - {top5[i].Wins}wins ({perc}%)\n";
            }

            builder.AddField(title, s, true);
        }
    }
}