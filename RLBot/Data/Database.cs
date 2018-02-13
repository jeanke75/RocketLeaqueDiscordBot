using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
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

        public static async Task InsertUserInfoAsync(ulong userId, string uniqueId, short elo1s, short elo2s, short elo3s)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                            cmd.Parameters.AddWithValue("@UniqueID", DbType.String).Value = uniqueId;
                            cmd.Parameters.AddWithValue("@Elo1s", DbType.Int16).Value = elo1s;
                            cmd.Parameters.AddWithValue("@Elo2s", DbType.Int16).Value = elo2s;
                            cmd.Parameters.AddWithValue("@Elo3s", DbType.Int16).Value = elo3s;
                            cmd.CommandText = "INSERT INTO UserInfo(UserID, UniqueID, JoinDate, Elo1s, Elo2s, Elo3s) VALUES(@UserID, @UniqueID, GetDate(), @Elo1s, @Elo2s, @Elo3s)";

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public static async Task UpdateUserInfoAsync(ulong userId, short elo1s, short elo2s, short elo3s)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        await UpdateUserInfoAsync(conn, tr, userId, elo1s, elo2s, elo3s);
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }

        private static async Task UpdateUserInfoAsync(SqlConnection conn, SqlTransaction tr, ulong userId, short elo1s, short elo2s, short elo3s)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;

                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                cmd.Parameters.AddWithValue("@Elo1s", DbType.Int16).Value = elo1s;
                cmd.Parameters.AddWithValue("@Elo2s", DbType.Int16).Value = elo2s;
                cmd.Parameters.AddWithValue("@Elo3s", DbType.Int16).Value = elo3s;
                cmd.CommandText = "UPDATE UserInfo set Elo1s = @Elo1s, Elo2s = @Elo2s, Elo3s = @Elo3s WHERE UserID = @UserID";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task UpdateUserInfoAsync(SqlConnection conn, SqlTransaction tr, ulong userId, RLPlaylist playlist, short elo)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;

                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                cmd.Parameters.AddWithValue("@Elo", DbType.Int16).Value = elo;
                switch (playlist)
                {
                    case RLPlaylist.Duel:
                        cmd.CommandText = "UPDATE UserInfo set Elo1s = @Elo WHERE UserID = @UserID";
                        break;
                    case RLPlaylist.Doubles:
                        cmd.CommandText = "UPDATE UserInfo set Elo2s = @Elo WHERE UserID = @UserID";
                        break;
                    case RLPlaylist.Standard:
                        cmd.CommandText = "UPDATE UserInfo set Elo3s = @Elo WHERE UserID = @UserID";
                        break;
                }

                await cmd.ExecuteNonQueryAsync();
            }
        }
        #endregion

        #region Invites
        public static async Task<int> GetInviteCountAsync(ulong userId)
        {
            int result = 0;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@ReferrerID", DbType.Decimal).Value = (decimal)userId;
                        cmd.CommandText = "SELECT COUNT(1) as Count FROM Invites WHERE ReferrerID = @ReferrerID GROUP BY ReferrerID";
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                result = (int)reader["Count"];
                            }
                            reader.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    conn.Close();
                }
            }
            return result;
        }

        public static async Task InsertInviteAsync(ulong newUserId, ulong referrerId)
        {
            if (newUserId == referrerId) return;

            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@NewUserID", DbType.Decimal).Value = (decimal)newUserId;
                        cmd.Parameters.AddWithValue("@ReferrerID", DbType.Decimal).Value = (decimal)referrerId;
                        cmd.CommandText = "INSERT INTO Invites(UserID, ReferrerID, JoinDate) VALUES(@NewUserID, @ReferrerID, GETDATE())";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (DbException ex)
                when (ex.HResult == -2146232060)
                {
                    // when it's a primary key violation do nothing
                }
            }
        }

        public static async Task InsertInvitesAsync(ConcurrentQueue<Invite> invites)
        {
            DateTime startTime = DateTime.Now;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                while (invites.TryPeek(out Invite invite) && invite.JoinDate <= startTime)
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Parameters.AddWithValue("@NewUserID", DbType.Decimal).Value = (decimal)invite.UserId;
                            cmd.Parameters.AddWithValue("@ReferrerID", DbType.Decimal).Value = (decimal)invite.ReferrerId;
                            cmd.Parameters.AddWithValue("@JoinDate", DbType.DateTime).Value = invite.JoinDate;
                            cmd.CommandText = "INSERT INTO Invites(UserID, ReferrerID, JoinDate) VALUES(@NewUserID, @ReferrerID, @JoinDate)";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (DbException ex)
                    when (ex.HResult == -2146232060)
                    {
                        // when it's a primary key violation do nothing
                    }

                    // remove the invite from the list
                    invites.TryDequeue(out Invite removedInvite);
                }
            }
        }
        #endregion

        #region Queues
        public static async Task<Queue> GetQueueAsync(long queueId)
        {
            Queue result = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                result = await GetQueueAsync(conn, null, queueId);
            }
            return result;
        }

        private static async Task<Queue> GetQueueAsync(SqlConnection conn, SqlTransaction tr, long queueId)
        {
            Queue result = null;
            using (SqlCommand cmd = conn.CreateCommand())
            {
                if (tr != null)
                    cmd.Transaction = tr;
                
                cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                cmd.CommandText = "SELECT * FROM Queue WHERE QueueID = @QueueID;";

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        result = new Queue()
                        {
                            QueueID = (long)reader["QueueID"],
                            ScoreTeamA = (byte)reader["ScoreTeamA"],
                            ScoreTeamB = (byte)reader["ScoreTeamB"],
                            Playlist = (RLPlaylist)(byte)reader["Playlist"],
                            Created = (DateTime)reader["Created"]
                        };
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public static async Task<long> InsertQueueAsync(RLPlaylist type, List<SocketUser> team_a, List<SocketUser> team_b)
        {
            long queueId = -1;
            using (SqlConnection conn = GetSqlConnection())
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
                        foreach (SocketUser user in team_a)
                        {
                            tasks[i] = InsertQueuePlayerAsync(conn, tr, queueId, user.Id, 0);
                            i++;
                        }
                        foreach (SocketUser user in team_b)
                        {
                            tasks[i] = InsertQueuePlayerAsync(conn, tr, queueId, user.Id, 1);
                            i++;
                        }

                        await Task.WhenAll(tasks);

                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
            return queueId;
        }

        private static async Task UpdateQueueAsync(SqlConnection conn, SqlTransaction tr, long queueId, byte scoreTeamA, byte scoreTeamB)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;

                cmd.Parameters.AddWithValue("@QueueID", DbType.Decimal).Value = (decimal)queueId;
                cmd.Parameters.AddWithValue("@ScoreTeamA", DbType.Byte).Value = scoreTeamA;
                cmd.Parameters.AddWithValue("@ScoreTeamB", DbType.Byte).Value = scoreTeamB;
                cmd.CommandText = "UPDATE Queue SET ScoreTeamA = @ScoreTeamA, ScoreTeamB = @ScoreTeamB WHERE QueueID = @QueueID;";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public static async Task<List<QueuePlayer>> GetQueuePlayersAsync(long queueId)
        {
            List<QueuePlayer> result = new List<QueuePlayer>();
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                    cmd.CommandText = "SELECT qp.UserID, qp.Team, Elo = case when q.Playlist = 1 then ui.Elo1s when q.Playlist = 2 then ui.Elo2s else ui.Elo3s end FROM QueuePlayer qp INNER JOIN Queue q ON q.QueueID = qp.QueueID INNER JOIN UserInfo ui ON ui.UserID = qp.UserID WHERE qp.QueueID = @QueueID;";
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new QueuePlayer()
                            {
                                UserId = (ulong)(decimal)reader["UserID"],
                                Team = (byte)reader["Team"],
                                Elo = (short)reader["Elo"]
                            });
                        }
                        reader.Close();
                    }
                }
            }
            return result;
        }

        private static async Task InsertQueuePlayerAsync(SqlConnection conn, SqlTransaction tr, long queueId, ulong userId, byte team)
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

        public static async Task SubstituteQueuePlayerAsync(long queueId, ulong subPlayer, ulong currentPlayer)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@QueueID", DbType.Decimal).Value = (decimal)queueId;
                            cmd.Parameters.AddWithValue("@NewUserID", DbType.Decimal).Value = (decimal)subPlayer;
                            cmd.Parameters.AddWithValue("@CurrentUserID", DbType.Decimal).Value = (decimal)currentPlayer;
                            cmd.CommandText = "UPDATE QueuePlayer SET UserID = @NewUserID WHERE QueueID = @QueueID AND UserID = @CurrentUserID;";

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public static async Task SetQueueResultAsync(long queueId, byte scoreTeamA, byte scoreTeamB, RLPlaylist playlist, List<QueuePlayer> players)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        // check if the queue exists and if the score hasn't been submitted yet
                        var queue = await GetQueueAsync(conn, tr, queueId);
                        if (queue == null)
                            throw new Exception($"Didn't find queue {queueId}!");
                        
                        if (queue.ScoreTeamA != 0 && queue.ScoreTeamB != 0)
                            throw new Exception($"The score for queue {queueId} has already been submitted!");
                        
                        // update the queue score
                        await UpdateQueueAsync(conn, tr, queueId, scoreTeamA, scoreTeamB);

                        // update player elos
                        foreach (QueuePlayer player in players)
                        {
                            await UpdateUserInfoAsync(conn, tr, player.UserId, playlist, player.Elo);
                        }

                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }
        #endregion

        #region Leaderboard
        public static async Task<Leaderboard> GetLeaderboardUserStatsAsync(ulong userId, RLPlaylist playlist, bool monthly, SqlConnection conn = null)
        {
            Leaderboard rec = null;
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
                        rec = new Leaderboard()
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

        public static async Task<List<Leaderboard>> GetLeaderboardTop5Async(RLPlaylist playlist, bool monthly, SqlConnection conn)
        {
            List<Leaderboard> records = new List<Leaderboard>();
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
                        Leaderboard rec = new Leaderboard()
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

        #region SQL
        public static async Task RunSQLAsync(string command)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tr;

                        cmd.CommandText = command;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    tr.Commit();
                }
            }
        }

        public static async Task<DataTable> DatabaseTablesAsync()
        {
            DataTable schemaDataTable = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    schemaDataTable = conn.GetSchema("Tables");
                }
                finally
                {
                    conn.Close();
                }
            }
            return schemaDataTable;
        }
        #endregion
    }
}