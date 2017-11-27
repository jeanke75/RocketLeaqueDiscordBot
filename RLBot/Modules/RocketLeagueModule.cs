﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace RLBot.Modules
{
    [Group("6man")]
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
                await ReplyAsync("The queue is open. Type \"" + RLBot.prefix + "6man join\", to join it.");
                six_man_open = true;
            }
            else
                await ReplyAsync("There is already an active queue. Type \"" + RLBot.prefix + "6man join\", to join it.");
        }

        [Command("join")]
        [Summary("Join the queue for 6man games.")]
        public async Task JoinQueueAsync()
        {
            if (!six_man_open)
            {
                await ReplyAsync("There is no open queue atm. Type \"" + RLBot.prefix + "6man open\", to start a new one.");
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

        [Command("pick")]
        [Summary("Pick 6 random players from the queue and divide them into 2 teams.")]
        public async Task RankAsync()
        {
            if (!six_man_open)
            {
                await ReplyAsync("There is no open queue atm. Type \"" + RLBot.prefix + "6man open\", to start a new one.");
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

                    await ReplyAsync($"Team A: {team_a[0].Mention}, {team_a[1].Mention}, {team_a[2].Mention}  // Team B: {team_b[0].Mention}, {team_b[1].Mention}, {team_b[2].Mention}");

                    six_man_open = false;
                }
                else
                {
                    await ReplyAsync($"Not enough players have joined the queue yet! {six_man.Count}/6");
                }
            }
        }
    }
}