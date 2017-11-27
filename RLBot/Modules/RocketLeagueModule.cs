using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace RLBot.Modules
{
    [Group("queue")]
    public class RocketLeagueModule : ModuleBase<SocketCommandContext>
    {
        static List<SocketUser> six_man = new List<SocketUser>();
        static bool six_man_open = false;
        static Random rnd = new Random();

        [Command("open")]
        [Summary("Open the queue for players to join 6man games.")]
        public async Task OpenQueueAsync()
        {
            if (!six_man_open)
            {
                await ReplyAsync("The queue is open. Type \"" + RLBot.prefix + "queue join\", to join it.");
                six_man_open = true;
            }
            else
                await ReplyAsync("There is already an active queue. Type \"" + RLBot.prefix + "queue join\", to join it.");
        }

        [Command("join")]
        [Summary("Join the queue for 6man games.")]
        public async Task JoinQueueAsync()
        {
            if (!six_man_open)
            {
                await ReplyAsync("There is no open queue atm. Type \"" + RLBot.prefix + "queue open\", to start a new one.");
            }
            else
            {
                if (six_man.Where(x => x.Id == Context.Message.Author.Id).FirstOrDefault() == null)
                {
                    six_man.Add(Context.Message.Author);
                    await ReplyAsync($"{Context.Message.Author.Mention} joined the queue.");
                }
                else
                {
                    await ReplyAsync("You've already joined the queue.");
                }
            }
        }

        [Command("leave")]
        [Summary("Leave the queue for 6man games.")]
        public async Task LeaveQueueAsync()
        {
            if (!six_man_open)
            {
                await ReplyAsync("There is no open queue atm.");
            }
            else
            {
                var user = six_man.Where(x => x.Id == Context.Message.Author.Id).FirstOrDefault();
                if (user != null)
                {
                    six_man.Remove(user);
                    await ReplyAsync($"{Context.Message.Author.Mention} left the queue.");
                }
                else
                {
                    await ReplyAsync("You're not in the current queue.");
                }
            }
        }

        [Command("reset")]
        [Summary("Reset the queue.")]
        public async Task ResetQueueAsync()
        {
            if (!six_man_open)
            {
                await ReplyAsync("There is no open queue atm. Type \"" + RLBot.prefix + "queue open\", to start a new one.");
            }
            else
            {
                six_man.Clear();
                six_man_open = false;
                await ReplyAsync("The queue has been reset!");
            }
        }

        [Command("pick")]
        [Summary("Pick 6 random players from the queue and divide them into 2 teams.")]
        public async Task PickQueueAsync()
        {
            if (!six_man_open)
            {
                await ReplyAsync("There is no open queue atm. Type \"" + RLBot.prefix + "queue open\", to start a new one.");
            }
            else
            {
                if (six_man.Count >= 6)
                {
                    List<SocketUser> team_a = new List<SocketUser>();
                    List<SocketUser> team_b = new List<SocketUser>();
                    for (int i = 0; i < 6; i++)
                    {
                        int rng = rnd.Next(0, six_man.Count);
                        if (i % 2 == 0)
                        {
                            team_a.Add(six_man[rng]);
                        }
                        else
                        {
                            team_b.Add(six_man[rng]);
                        }
                        six_man.Remove(six_man[rng]);
                    }
                    six_man.Clear();
                    six_man_open = false;
                    await ReplyAsync($"Team A: {team_a[0].Mention}, {team_a[1].Mention}, {team_a[2].Mention}  // Team B: {team_b[0].Mention}, {team_b[1].Mention}, {team_b[2].Mention}");
                }
                else
                {
                    await ReplyAsync($"Not enough players have joined the queue yet! {six_man.Count}/6");
                }
            }
        }
    }
}