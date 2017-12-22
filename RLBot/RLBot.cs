using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RLBot.Handlers;
using RLBot.Models;
using RLBot.TypeReaders;

namespace RLBot
{
    class RLBot
    {
        private CommandService _commands;
        private DiscordSocketClient _client;

        public static readonly char COMMAND_PREFIX = '!';
        public static readonly Color EMBED_COLOR = Color.Red;
        public static readonly ulong APPLICATION_OWNER_ID = 140470317440040960;

        public static void Main(string[] args)
           => new RLBot().StartAsync(args).GetAwaiter().GetResult();

        private async Task StartAsync(params string[] args)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig{
                LogLevel = LogSeverity.Info
            });
            _commands = new CommandService();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            _commands.AddTypeReader<RLPlaylist>(new RLPlaylistTypeReader());

            var serv = InstallServices();
            serv.GetRequiredService<CommandHandler>();
            serv.GetRequiredService<ReactionHandler>();

            _client.Log += Log;
            _commands.Log += Log;

            await _client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["TOKEN"]);
            await _client.StartAsync();

            await _client.SetGameAsync(ConfigurationManager.AppSettings["GAME"]);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        private IServiceProvider InstallServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<CommandHandler>()
                .AddSingleton<ReactionHandler>()
                .BuildServiceProvider();
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(string.Concat("[", DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss"), "] [", msg.Severity, "] ", msg.Message, msg.Exception));
            return Task.CompletedTask;
        }

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
    }
}