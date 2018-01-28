using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using RLBot.Data.Models;
using RLBot.Models;

namespace RLBot.Data
{
    public static class Database
    {
        public static SqlConnection GetSqlConnection()
        {
            Uri uri = new Uri(ConfigurationManager.AppSettings["SQLSERVER_URI"]);
            string connectionString = new SqlConnectionStringBuilder
            {
                DataSource = uri.Host,
                InitialCatalog = uri.AbsolutePath.Trim('/'),
                UserID = uri.UserInfo.Split(':').First(),
                Password = uri.UserInfo.Split(':').Last(),
                MultipleActiveResultSets = true
            }.ConnectionString;

            return new SqlConnection(connectionString);
        }

        #region UserInfo
        public static async Task<UserInfo> GetUserInfoAsync(ulong userId)
        {
            UserInfo result = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                    cmd.CommandText = "SELECT * FROM UserInfo WHERE UserID = @UserID";

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            result = new UserInfo()
                            {
                                UserID = userId,
                                UniqueID = (string)reader["UniqueID"],
                                JoinDate = (DateTime)reader["JoinDate"],
                                Elo1s = (short)reader["Elo1s"],
                                Elo2s = (short)reader["Elo2s"],
                                Elo3s = (short)reader["Elo3s"]
                            };
                        }
                        reader.Close();
                    }
                }
            }
            return result;
        }

        public static async Task InsertUserInfoAsync(ulong userId, string uniqueId, int elo1s, int elo2s, int elo3s)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                    cmd.Parameters.AddWithValue("@UniqueID", DbType.String).Value = uniqueId;
                    cmd.Parameters.AddWithValue("@Elo1s", DbType.Int16).Value = elo1s;
                    cmd.Parameters.AddWithValue("@Elo2s", DbType.Int16).Value = elo2s;
                    cmd.Parameters.AddWithValue("@Elo3s", DbType.Int16).Value = elo3s;
                    cmd.CommandText = "INSERT INTO UserInfo(UserID, UniqueID, JoinDate, Elo1s, Elo2s, Elo3s) VALUES(@UserID, @UniqueID, GetDate(), @Elo1s, @Elo2s, @Elo3s)";

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task EditUserInfoAsync(ulong userId, int elo1s, int elo2s, int elo3s)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                    cmd.Parameters.AddWithValue("@Elo1s", DbType.Int16).Value = elo1s;
                    cmd.Parameters.AddWithValue("@Elo2s", DbType.Int16).Value = elo2s;
                    cmd.Parameters.AddWithValue("@Elo3s", DbType.Int16).Value = elo3s;
                    cmd.CommandText = "UPDATE UserInfo set Elo1s = @Elo1s, Elo2s = @Elo2s, Elo3s = @Elo3s WHERE UserID = @UserID";

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        #endregion

        #region Leaderboard
        public static async Task<LeaderboardRecord> GetLeaderboardUserStatsAsync(ulong userId, RLPlaylist playlist, bool monthly, SqlConnection conn = null)
        {
            LeaderboardRecord rec = null;
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)playlist;
                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                if (monthly)
                    cmd.CommandText = "select * from (select row_number() OVER (ORDER BY x.Wins DESC, x.TotalGames ASC) as Rank, x.UserID, x.Wins, x.TotalGames from (SELECT qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24))  AND q.Created >= CAST(DATEADD(dd, -DAY(GETDATE()) + 1, GETDATE()) AS DATE) AND q.Created < CAST(DATEADD(month, DATEDIFF(month, 0, GETDATE()) + 1, 0) AS DATE) AND q.Playlist = @Playlist GROUP BY qp.UserID) x ) y WHERE y.UserID = @UserID";
                else
                    cmd.CommandText = "select * from (select row_number() OVER (ORDER BY x.Wins DESC, x.TotalGames ASC) as Rank, x.UserID, x.Wins, x.TotalGames from (SELECT qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24)) AND q.Playlist = @Playlist GROUP BY qp.UserID) x ) y WHERE y.UserID = @UserID";
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        rec = new LeaderboardRecord()
                        {
                            UserID = userId,
                            Rank = (long)reader["Rank"], 
                            Wins = (int)reader["Wins"],
                            TotalGames = (int)reader["TotalGames"]
                        };
                    }
                    reader.Close();
                }
            }
            return rec;
        }

        public static async Task<List<LeaderboardRecord>> GetLeaderboardTop5Async(RLPlaylist playlist, bool monthly, SqlConnection conn)
        {
            List<LeaderboardRecord> records = new List<LeaderboardRecord>();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)playlist;
                if (monthly)
                    cmd.CommandText = "select TOP 5 * from (select row_number() OVER (ORDER BY x.Wins DESC, x.TotalGames ASC) as Rank, x.UserID, x.Wins, x.TotalGames from (SELECT qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24))  AND q.Created >= CAST(DATEADD(dd, -DAY(GETDATE()) + 1, GETDATE()) AS DATE) AND q.Created < CAST(DATEADD(month, DATEDIFF(month, 0, GETDATE()) + 1, 0) AS DATE) AND q.Playlist = @Playlist GROUP BY qp.UserID) x ) y order by y.Rank";
                else
                    cmd.CommandText = "select TOP 5 * from (select row_number() OVER (ORDER BY x.Wins DESC, x.TotalGames ASC) as Rank, x.UserID, x.Wins, x.TotalGames from (SELECT qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24)) AND q.Playlist = @Playlist GROUP BY qp.UserID) x ) y order by y.Rank";
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
        #endregion
    }
}
