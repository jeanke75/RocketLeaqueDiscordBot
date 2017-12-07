using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Discord.Commands;
using RLBot.Preconditions;

namespace RLBot.Modules
{
    [Name("Database")]
    [Hidden()]
    public class DatabaseModule : ModuleBase<SocketCommandContext>
    {
        [Command("sql")]
        [Summary("Run an sql command against the database that does not return a result. (insert, update, delete)")]
        [Remarks("sql <sql command>")]
        public async Task RunSQLCommand([Remainder]string command)
        {
            if (Context.Message.Author.Id != RLBot.APPLICATION_OWNER_ID) return;

            command = command.Trim();
            if (command.Equals(""))
            {
                await ReplyAsync("No SQL-command provided!");
                return;
            }

            try
            {
                using (SqlConnection conn = RLBot.GetSqlConnection())
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
                await ReplyAsync("SQL-command executed.");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.ToString());
            }
        }

        [Command("tables")]
        [Summary("Show a list of all the tables in the database")]
        [Remarks("tables")]
        public async Task TablesAsync()
        {
            if (Context.Message.Author.Id != RLBot.APPLICATION_OWNER_ID) return;

            try
            {
                using (SqlConnection conn = RLBot.GetSqlConnection())
                {
                    await conn.OpenAsync();
                    try
                    {
                        DataTable schemaDataTable = conn.GetSchema("Tables");
                        string colums = "";
                        foreach (DataColumn column in schemaDataTable.Columns)
                        {
                            colums += column.ColumnName + "\t";
                        }
                        await ReplyAsync(colums);
                        foreach (DataRow row in schemaDataTable.Rows)
                        {
                            string rows = "";
                            foreach (object value in row.ItemArray)
                            {
                                rows += value.ToString() + "\t";
                            }
                            await ReplyAsync(rows);
                        }
                        await ReplyAsync("-----done-----");
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.ToString());
            }
        }

        [Command("select")]
        [Summary("Show a list of all the tables in the database")]
        [Remarks("select <rest of the select command>")]
        public async Task SelectAsync([Remainder]string command)
        {
            command = command.Trim();
            if (Context.Message.Author.Id != RLBot.APPLICATION_OWNER_ID || command == "") return;
            try
            {
                using (SqlConnection conn = RLBot.GetSqlConnection())
                {
                    await conn.OpenAsync();
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "select " + command;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                bool showTypes = true;
                                string fieldtypes = "";
                                while (await reader.ReadAsync())
                                {
                                    string row = "";
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (showTypes)
                                            fieldtypes += reader.GetFieldType(i).ToString() + " ";
                                        row += reader.GetValue(i) + " ";
                                    }
                                    if (showTypes)
                                    {
                                        await ReplyAsync(fieldtypes);
                                        showTypes = false;
                                    }
                                    await ReplyAsync(row);
                                }
                            }
                        }
                        await ReplyAsync("-----done-----");
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.ToString());
            }
        }
    }
}
